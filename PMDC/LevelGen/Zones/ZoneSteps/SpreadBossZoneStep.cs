using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence;
using RogueEssence.Dev;
using RogueEssence.Dungeon;
using RogueEssence.LevelGen;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Generates boss encounters randomly distributed across an entire dungeon segment.
    /// This zone step spreads boss battles throughout multiple floors using a spread plan,
    /// and configures boss room generation, vault setup, and reward placement for each boss encounter.
    /// </summary>
    [Serializable]
    public class SpreadBossZoneStep : SpreadZoneStep
    {
        /// <summary>
        /// The priority level at which boss room generation steps are enqueued during floor generation.
        /// Lower priority values execute earlier in the generation pipeline.
        /// </summary>
        public Priority BossRoomPriority;

        /// <summary>
        /// The priority level at which reward item placement steps are enqueued during floor generation.
        /// Controls the timing of boss reward item placement relative to other generation steps.
        /// </summary>
        public Priority RewardPriority;

        /// <summary>
        /// Additional generation steps that execute during vault room setup for boss encounters.
        /// These steps allow for customization of vault-specific features and decorations.
        /// </summary>
        public List<IGenPriority> VaultSteps;

        /// <summary>
        /// Encounter table defining which items can spawn as boss room rewards, indexed by floor range.
        /// Items are selected randomly from this spawn list according to configured item amounts.
        /// </summary>
        [RangeBorder(0, true, true)]
        public SpawnRangeList<MapItem> Items;

        /// <summary>
        /// Encounter table for boss room generation steps, mapped to floor ranges.
        /// Determines which boss room generation configuration applies to each floor.
        /// </summary>
        [RangeBorder(0, true, true)]
        public SpawnRangeList<AddBossRoomStep<ListMapGenContext>> BossSteps;

        /// <summary>
        /// Range of item counts to randomly select for each boss encounter, indexed by floor range.
        /// Specifies how many items from the Items spawn list should be chosen for each boss reward set.
        /// </summary>
        [RangeBorder(0, true, true)]
        public RangeDict<RandRange> ItemAmount;

        /// <summary>
        /// Spawners that create specific guaranteed boss reward items, indexed by floor range.
        /// These spawners complement the random item selection and allow for deterministic reward guarantees.
        /// </summary>
        [RangeBorder(0, true, true)]
        public RangeDict<IStepSpawner<ListMapGenContext, MapItem>> ItemSpawners;

        /// <summary>
        /// Generation steps for placing boss reward items within the boss room, indexed by floor range.
        /// Controls the spatial distribution and placement logic for reward items.
        /// </summary>
        [RangeBorder(0, true, true)]
        public RangeDict<RandomRoomSpawnStep<ListMapGenContext, MapItem>> ItemPlacements;

        /// <summary>
        /// Initializes a new instance of <see cref="SpreadBossZoneStep"/> with default values.
        /// All collections are initialized as empty, and priorities are set to default values.
        /// </summary>
        public SpreadBossZoneStep()
        {
            VaultSteps = new List<IGenPriority>();
            Items = new SpawnRangeList<MapItem>();
            BossSteps = new SpawnRangeList<AddBossRoomStep<ListMapGenContext>>();
            ItemAmount = new RangeDict<RandRange>();
            ItemSpawners = new RangeDict<IStepSpawner<ListMapGenContext, MapItem>>();
            ItemPlacements = new RangeDict<RandomRoomSpawnStep<ListMapGenContext, MapItem>>();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SpreadBossZoneStep"/> with specified priorities and spread plan.
        /// </summary>
        /// <param name="bossRoomPriority">The priority at which boss room generation steps are executed.</param>
        /// <param name="rewardPriority">The priority at which reward item placement steps are executed.</param>
        /// <param name="plan">The spread plan that controls how bosses are distributed across floors in the segment.</param>
        public SpreadBossZoneStep(Priority bossRoomPriority, Priority rewardPriority, SpreadPlanBase plan) : base(plan)
        {
            VaultSteps = new List<IGenPriority>();
            Items = new SpawnRangeList<MapItem>();
            BossSteps = new SpawnRangeList<AddBossRoomStep<ListMapGenContext>>();
            ItemAmount = new RangeDict<RandRange>();
            ItemSpawners = new RangeDict<IStepSpawner<ListMapGenContext, MapItem>>();
            ItemPlacements = new RangeDict<RandomRoomSpawnStep<ListMapGenContext, MapItem>>();

            BossRoomPriority = bossRoomPriority;
            RewardPriority = rewardPriority;
        }

        /// <summary>
        /// Copy constructor for creating a cloned instance with a new random seed.
        /// Copies all configuration data from the source instance.
        /// </summary>
        /// <param name="other">The source <see cref="SpreadBossZoneStep"/> instance to clone.</param>
        /// <param name="seed">The random seed to use for this instance's random number generation.</param>
        protected SpreadBossZoneStep(SpreadBossZoneStep other, ulong seed) : base(other, seed)
        {
            VaultSteps = new List<IGenPriority>();
            VaultSteps.AddRange(other.VaultSteps);
            Items = other.Items.CopyState();
            BossSteps = other.BossSteps.CopyState();
            ItemAmount = other.ItemAmount;
            ItemSpawners = other.ItemSpawners;
            ItemPlacements = other.ItemPlacements;

            BossRoomPriority = other.BossRoomPriority;
            RewardPriority = other.RewardPriority;
        }

        /// <inheritdoc/>
        public override ZoneStep Instantiate(ulong seed) { return new SpreadBossZoneStep(this, seed); }

        /// <summary>
        /// Applies boss generation steps to a specific floor, enqueuing boss room and reward placement steps.
        /// This method is called for each floor where the spread plan indicates a boss should be placed.
        /// </summary>
        /// <param name="zoneContext">The zone-level generation context providing floor information.</param>
        /// <param name="context">The floor-level generation context with random number generator and other utilities.</param>
        /// <param name="queue">The priority queue where generation steps are enqueued for execution.</param>
        /// <param name="dropIdx">The index within the spread plan indicating which boss encounter this is.</param>
        /// <returns><c>true</c> if boss steps were successfully enqueued; <c>false</c> if no applicable boss configuration exists for this floor.</returns>
        /// <remarks>
        /// This method performs the following operations:
        /// 1. Selects an appropriate boss room generation step based on the current floor
        /// 2. Enqueues vault setup steps if configured
        /// 3. Constructs a combination of random and guaranteed item spawners
        /// 4. Enqueues the reward item placement step with the combined spawner
        /// </remarks>
        protected override bool ApplyToFloor(ZoneGenContext zoneContext, IGenContext context, StablePriorityQueue<Priority, IGenStep> queue, int dropIdx)
        {
            int id = zoneContext.CurrentID;
            {
                SpawnList<AddBossRoomStep<ListMapGenContext>> bossListSlice = BossSteps.GetSpawnList(id);
                if (!bossListSlice.CanPick)
                    return false;
                AddBossRoomStep<ListMapGenContext> bossStep = bossListSlice.Pick(context.Rand).Copy();
                queue.Enqueue(BossRoomPriority, bossStep);
            }

            foreach (IGenPriority vaultStep in VaultSteps)
                queue.Enqueue(vaultStep.Priority, vaultStep.GetItem());

            {
                SpawnList<MapItem> itemListSlice = Items.GetSpawnList(id);
                PickerSpawner<ListMapGenContext, MapItem> constructedSpawns = new PickerSpawner<ListMapGenContext, MapItem>(new LoopedRand<MapItem>(itemListSlice, ItemAmount[id]));

                IStepSpawner<ListMapGenContext, MapItem> treasures = ItemSpawners[id].Copy();

                PresetMultiRand<IStepSpawner<ListMapGenContext, MapItem>> groupRand = new PresetMultiRand<IStepSpawner<ListMapGenContext, MapItem>>(constructedSpawns, treasures);

                RandomRoomSpawnStep<ListMapGenContext, MapItem> detourItems = ItemPlacements[id].Copy();
                detourItems.Spawn = new MultiStepSpawner<ListMapGenContext, MapItem>(groupRand);
                queue.Enqueue(RewardPriority, detourItems);
            }
            return true;
        }
    }
}

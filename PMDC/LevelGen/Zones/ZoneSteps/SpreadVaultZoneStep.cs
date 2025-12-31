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
    /// Generates vaults randomly across the whole dungeon segment.
    /// Vaults are special rooms spread throughout a zone's floors that contain rewards (items, tiles) and enemies.
    /// Each floor in the specified range receives vault generation with configurable items, tiles, and mobs.
    /// </summary>
    [Serializable]
    public class SpreadVaultZoneStep : SpreadZoneStep
    {
        /// <summary>
        /// Gets or sets the priority level for item placement generation steps within the map generation pipeline.
        /// </summary>
        public Priority ItemPriority;

        /// <summary>
        /// Gets or sets the priority level for tile placement generation steps within the map generation pipeline.
        /// </summary>
        public Priority TilePriority;

        /// <summary>
        /// Gets or sets the priority level for mob placement generation steps within the map generation pipeline.
        /// </summary>
        public Priority MobPriority;

        /// <summary>
        /// Gets or sets the list of generation steps used to construct the vault room structures.
        /// These steps must be responsible for room creation and tile painting.
        /// Reward placement (items and mobs) within the vault rooms is handled separately by the item, tile, and mob properties.
        /// </summary>
        public List<IGenPriority> VaultSteps;

        /// <summary>
        /// Gets or sets the spawn range list for items that can be placed in vaults.
        /// This is an encounter table that determines which items appear and with what probability.
        /// </summary>
        [RangeBorder(0, true, true)]
        public SpawnRangeList<MapItem> Items;

        /// <summary>
        /// Gets or sets the amount of items to randomly choose from the spawn list for each floor level.
        /// Ranges are keyed by floor level and determine how many item spawn attempts occur.
        /// </summary>
        [RangeBorder(0, true, true)]
        public RangeDict<RandRange> ItemAmount;

        /// <summary>
        /// Gets or sets custom spawners for specific items in vaults, keyed by floor level.
        /// Allows for specialized item spawn logic beyond the standard encounter table.
        /// </summary>
        [RangeBorder(0, true, true)]
        public RangeDict<IStepSpawner<ListMapGenContext, MapItem>> ItemSpawners;

        /// <summary>
        /// Gets or sets the placement steps for distributing items across vault rooms, keyed by floor level.
        /// Defines where and how items are placed within vault structures.
        /// </summary>
        [RangeBorder(0, true, true)]
        public RangeDict<RandomRoomSpawnStep<ListMapGenContext, MapItem>> ItemPlacements;

        /// <summary>
        /// Gets or sets custom spawners for effect tiles in vaults, keyed by floor level.
        /// Allows for specialized tile spawn logic for environmental hazards and effects.
        /// </summary>
        [RangeBorder(0, true, true)]
        public RangeDict<IStepSpawner<ListMapGenContext, EffectTile>> TileSpawners;

        /// <summary>
        /// Gets or sets the placement steps for distributing effect tiles across vault rooms, keyed by floor level.
        /// Defines where and how tiles are placed within vault structures.
        /// </summary>
        [RangeBorder(0, true, true)]
        public RangeDict<RandomRoomSpawnStep<ListMapGenContext, EffectTile>> TilePlacements;

        /// <summary>
        /// Gets or sets the spawn range list for mobs that can be placed in vaults.
        /// This is an encounter table that determines which enemy teams appear and with what probability.
        /// Special enemies will have their level scaled according to the parameter range provided by the floor.
        /// </summary>
        [RangeBorder(0, true, true)]
        public SpawnRangeList<MobSpawn> Mobs;

        /// <summary>
        /// Gets or sets the amount of mobs to place in total across all available vault rooms, keyed by floor level.
        /// Determines the total enemy count spawned within vault structures.
        /// </summary>
        [RangeBorder(0, true, true)]
        public RangeDict<RandRange> MobAmount;

        /// <summary>
        /// Gets or sets the placement steps for distributing mobs across vault rooms, keyed by floor level.
        /// Defines where and how enemies are placed within vault structures.
        /// </summary>
        [RangeBorder(0, true, true)]
        public RangeDict<PlaceRandomMobsStep<ListMapGenContext>> MobPlacements;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpreadVaultZoneStep"/> class with default values.
        /// All collections are initialized as empty.
        /// </summary>
        public SpreadVaultZoneStep()
        {
            VaultSteps = new List<IGenPriority>();
            Items = new SpawnRangeList<MapItem>();
            Mobs = new SpawnRangeList<MobSpawn>();
            ItemAmount = new RangeDict<RandRange>();
            ItemSpawners = new RangeDict<IStepSpawner<ListMapGenContext, MapItem>>();
            ItemPlacements = new RangeDict<RandomRoomSpawnStep<ListMapGenContext, MapItem>>();
            TileSpawners = new RangeDict<IStepSpawner<ListMapGenContext, EffectTile>>();
            TilePlacements = new RangeDict<RandomRoomSpawnStep<ListMapGenContext, EffectTile>>();
            MobAmount = new RangeDict<RandRange>();
            MobPlacements = new RangeDict<PlaceRandomMobsStep<ListMapGenContext>>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpreadVaultZoneStep"/> class with specified priorities and a spread plan.
        /// </summary>
        /// <param name="itemPriority">The priority level for item placement steps in the generation pipeline.</param>
        /// <param name="tilePriority">The priority level for tile placement steps in the generation pipeline.</param>
        /// <param name="mobPriority">The priority level for mob placement steps in the generation pipeline.</param>
        /// <param name="plan">The spread plan defining how vaults are distributed across zone floors.</param>
        public SpreadVaultZoneStep(Priority itemPriority, Priority tilePriority, Priority mobPriority, SpreadPlanBase plan) : base(plan)
        {
            VaultSteps = new List<IGenPriority>();
            Items = new SpawnRangeList<MapItem>();
            Mobs = new SpawnRangeList<MobSpawn>();
            ItemAmount = new RangeDict<RandRange>();
            ItemSpawners = new RangeDict<IStepSpawner<ListMapGenContext, MapItem>>();
            ItemPlacements = new RangeDict<RandomRoomSpawnStep<ListMapGenContext, MapItem>>();
            TileSpawners = new RangeDict<IStepSpawner<ListMapGenContext, EffectTile>>();
            TilePlacements = new RangeDict<RandomRoomSpawnStep<ListMapGenContext, EffectTile>>();
            MobAmount = new RangeDict<RandRange>();
            MobPlacements = new RangeDict<PlaceRandomMobsStep<ListMapGenContext>>();

            ItemPriority = itemPriority;
            TilePriority = tilePriority;
            MobPriority = mobPriority;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpreadVaultZoneStep"/> class by copying another instance.
        /// Creates a deep copy of all configuration while sharing the specified seed.
        /// </summary>
        /// <param name="other">The instance to copy configuration from.</param>
        /// <param name="seed">The random seed for this instance.</param>
        protected SpreadVaultZoneStep(SpreadVaultZoneStep other, ulong seed) : base(other, seed)
        {
            VaultSteps = new List<IGenPriority>();
            VaultSteps.AddRange(other.VaultSteps);
            Items = other.Items.CopyState();
            Mobs = other.Mobs.CopyState();
            ItemAmount = other.ItemAmount;
            ItemSpawners = other.ItemSpawners;
            ItemPlacements = other.ItemPlacements;
            TileSpawners = other.TileSpawners;
            TilePlacements = other.TilePlacements;
            MobAmount = other.MobAmount;
            MobPlacements = other.MobPlacements;

            ItemPriority = other.ItemPriority;
            TilePriority = other.TilePriority;
            MobPriority = other.MobPriority;
        }

        /// <summary>
        /// Creates a new instance of this zone step with the specified random seed.
        /// </summary>
        /// <param name="seed">The random seed for the new instance.</param>
        /// <returns>A new <see cref="ZoneStep"/> configured with the same parameters but a different seed.</returns>
        public override ZoneStep Instantiate(ulong seed) { return new SpreadVaultZoneStep(this, seed); }

        /// <summary>
        /// Applies the vault generation configuration to a single floor within the zone.
        /// Enqueues vault construction steps, item/tile placement steps, and mob placement steps to the generation queue
        /// based on the current floor level and available configuration for that level.
        /// </summary>
        /// <param name="zoneContext">The context for zone-level generation providing current floor information.</param>
        /// <param name="context">The generic generation context for the current floor.</param>
        /// <param name="queue">The priority queue to which generation steps are enqueued.</param>
        /// <param name="dropIdx">The index position in the queue where items should be inserted.</param>
        /// <returns>True if the floor was successfully processed; false otherwise.</returns>
        protected override bool ApplyToFloor(ZoneGenContext zoneContext, IGenContext context, StablePriorityQueue<Priority, IGenStep> queue, int dropIdx)
        {
            int id = zoneContext.CurrentID;

            foreach (IGenPriority vaultStep in VaultSteps)
                queue.Enqueue(vaultStep.Priority, vaultStep.GetItem());

            if (ItemPlacements.ContainsItem(id))
            {
                SpawnList<MapItem> itemListSlice = Items.GetSpawnList(id);
                PickerSpawner<ListMapGenContext, MapItem> constructedSpawns = new PickerSpawner<ListMapGenContext, MapItem>(new LoopedRand<MapItem>(itemListSlice, ItemAmount[id]));

                List<IStepSpawner<ListMapGenContext, MapItem>> steps = new List<IStepSpawner<ListMapGenContext, MapItem>>();
                steps.Add(constructedSpawns);
                if (ItemSpawners.ContainsItem(id))
                {
                    IStepSpawner<ListMapGenContext, MapItem> treasures = ItemSpawners[id].Copy();
                    steps.Add(treasures);
                }
                PresetMultiRand<IStepSpawner<ListMapGenContext, MapItem>> groupRand = new PresetMultiRand<IStepSpawner<ListMapGenContext, MapItem>>(steps.ToArray());
                RandomRoomSpawnStep<ListMapGenContext, MapItem> detourItems = ItemPlacements[id].Copy();
                detourItems.Spawn = new MultiStepSpawner<ListMapGenContext, MapItem>(groupRand);
                queue.Enqueue(ItemPriority, detourItems);
            }

            if (TilePlacements.ContainsItem(id))
            {
                List<IStepSpawner<ListMapGenContext, EffectTile>> steps = new List<IStepSpawner<ListMapGenContext, EffectTile>>();
                if (TileSpawners.ContainsItem(id))
                {
                    IStepSpawner<ListMapGenContext, EffectTile> treasures = TileSpawners[id].Copy();
                    steps.Add(treasures);
                }
                PresetMultiRand<IStepSpawner<ListMapGenContext, EffectTile>> groupRand = new PresetMultiRand<IStepSpawner<ListMapGenContext, EffectTile>>(steps.ToArray());
                RandomRoomSpawnStep<ListMapGenContext, EffectTile> detourItems = TilePlacements[id].Copy();
                detourItems.Spawn = new MultiStepSpawner<ListMapGenContext, EffectTile>(groupRand);
                queue.Enqueue(TilePriority, detourItems);
            }


            SpawnList<MobSpawn> mobListSlice = Mobs.GetSpawnList(id);
            if (mobListSlice.CanPick && MobPlacements.ContainsItem(id))
            {
                //secret enemies
                SpecificTeamSpawner specificTeam = new SpecificTeamSpawner();

                MobSpawn newSpawn = mobListSlice.Pick(context.Rand).Copy();
                specificTeam.Spawns.Add(newSpawn);

                //use bruteforce clone for this
                PlaceRandomMobsStep<ListMapGenContext> secretMobPlacement = MobPlacements[id].Copy();
                secretMobPlacement.Spawn = new LoopedTeamSpawner<ListMapGenContext>(specificTeam, MobAmount[id]);
                queue.Enqueue(MobPriority, secretMobPlacement);
            }
            return true;
        }
    }
}

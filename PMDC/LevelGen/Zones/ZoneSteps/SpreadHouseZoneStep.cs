using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using RogueElements;
using RogueEssence;
using RogueEssence.Data;
using RogueEssence.Dev;
using RogueEssence.Dungeon;
using RogueEssence.LevelGen;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Generates monster houses randomly across the whole dungeon segment.
    /// </summary>
    /// <remarks>
    /// This zone step spreads monster house floors throughout a dungeon segment, with each floor receiving
    /// a randomly selected monster house configuration populated with themed items and mobs.
    /// </remarks>
    [Serializable]
    public class SpreadHouseZoneStep : SpreadZoneStep
    {
        /// <summary>
        /// Gets or sets the priority at which to execute this zone step in the map generation process.
        /// </summary>
        public Priority Priority;

        /// <summary>
        /// Gets or sets the specially designed pool of items to spawn for the monster house.
        /// </summary>
        /// <remarks>
        /// These items are combined with the items normally found on the monster house floor and fed into
        /// ItemThemes to determine what items actually spawn in the generated house.
        /// </remarks>
        [RangeBorder(0, true, true)]
        public SpawnRangeList<MapItem> Items;

        /// <summary>
        /// Gets or sets the item theme configurations for spawning items in the monster house.
        /// </summary>
        /// <remarks>
        /// Themes can use specific items from the Items list or the item pool of the floor itself.
        /// </remarks>
        [RangeBorder(0, true, true)]
        public SpawnRangeList<ItemTheme> ItemThemes;

        /// <summary>
        /// Gets or sets the specially designed pool of mobs to spawn for the monster house.
        /// </summary>
        /// <remarks>
        /// These mobs are combined with the mobs normally found on the monster house floor and fed into
        /// MobThemes to determine what mobs actually spawn in the generated house.
        /// </remarks>
        [RangeBorder(0, true, true)]
        public SpawnRangeList<MobSpawn> Mobs;

        /// <summary>
        /// Gets or sets the mob theme configurations for spawning mobs in the monster house.
        /// </summary>
        /// <remarks>
        /// Themes can use specific mobs from the Mobs list or the spawn pool of the floor itself.
        /// </remarks>
        [RangeBorder(0, true, true)]
        public SpawnRangeList<MobTheme> MobThemes;

        /// <summary>
        /// Gets or sets the spawn list of monster house base step implementations.
        /// </summary>
        /// <remarks>
        /// A randomly selected step from this list is used to initialize the base monster house structure
        /// before populating it with items and mobs.
        /// </remarks>
        public SpawnList<IMonsterHouseBaseStep> HouseStepSpawns;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public SpreadHouseZoneStep()
        {
            Items = new SpawnRangeList<MapItem>();
            ItemThemes = new SpawnRangeList<ItemTheme>();
            Mobs = new SpawnRangeList<MobSpawn>();
            MobThemes = new SpawnRangeList<MobTheme>();
            HouseStepSpawns = new SpawnList<IMonsterHouseBaseStep>();
        }

        /// <summary>
        /// Initializes a new instance with the specified priority and spread plan.
        /// </summary>
        /// <param name="priority">The priority level determining when this step executes in the map generation process.</param>
        /// <param name="plan">The spread plan that controls how monster houses are distributed across the dungeon segment.</param>
        public SpreadHouseZoneStep(Priority priority, SpreadPlanBase plan) : base(plan)
        {
            Items = new SpawnRangeList<MapItem>();
            ItemThemes = new SpawnRangeList<ItemTheme>();
            Mobs = new SpawnRangeList<MobSpawn>();
            MobThemes = new SpawnRangeList<MobTheme>();
            HouseStepSpawns = new SpawnList<IMonsterHouseBaseStep>();

            Priority = priority;
        }

        /// <summary>
        /// Initializes a new instance by copying another instance with a new random seed.
        /// </summary>
        /// <param name="other">The source instance to copy from.</param>
        /// <param name="seed">The random seed to use for this instance's random number generator.</param>
        protected SpreadHouseZoneStep(SpreadHouseZoneStep other, ulong seed) : base(other, seed)
        {
            Items = other.Items.CopyState();
            ItemThemes = other.ItemThemes.CopyState();
            Mobs = other.Mobs.CopyState();
            MobThemes = other.MobThemes.CopyState();
            HouseStepSpawns = (SpawnList<IMonsterHouseBaseStep>)other.HouseStepSpawns.CopyState();

            Priority = other.Priority;
        }

        /// <inheritdoc/>
        /// <returns>A new instance of SpreadHouseZoneStep with the specified seed.</returns>
        public override ZoneStep Instantiate(ulong seed) { return new SpreadHouseZoneStep(this, seed); }

        /// <inheritdoc/>
        /// <remarks>
        /// Creates a monster house for the current floor by selecting a base house step, populating it with themed
        /// items and mobs from the configured spawn lists, and enqueueing it for execution at the specified priority.
        /// </remarks>
        protected override bool ApplyToFloor(ZoneGenContext zoneContext, IGenContext context, StablePriorityQueue<Priority, IGenStep> queue, int dropIdx)
        {
            int id = zoneContext.CurrentID;

            IMonsterHouseBaseStep monsterHouseStep = HouseStepSpawns.Pick(context.Rand).CreateNew();
            SpawnList<MapItem> itemListSlice = Items.GetSpawnList(id);
            for (int jj = 0; jj < itemListSlice.Count; jj++)
                monsterHouseStep.Items.Add(new MapItem(itemListSlice.GetSpawn(jj)), itemListSlice.GetSpawnRate(jj));
            SpawnList<ItemTheme> itemThemeListSlice = ItemThemes.GetSpawnList(id);
            for (int jj = 0; jj < itemThemeListSlice.Count; jj++)
                monsterHouseStep.ItemThemes.Add(itemThemeListSlice.GetSpawn(jj).Copy(), itemThemeListSlice.GetSpawnRate(jj));
            SpawnList<MobSpawn> mobListSlice = Mobs.GetSpawnList(id);
            for (int jj = 0; jj < mobListSlice.Count; jj++)
            {
                MobSpawn newSpawn = mobListSlice.GetSpawn(jj).Copy();
                monsterHouseStep.Mobs.Add(newSpawn, mobListSlice.GetSpawnRate(jj));
            }
            SpawnList<MobTheme> mobThemeListSlice = MobThemes.GetSpawnList(id);
            for (int jj = 0; jj < mobThemeListSlice.Count; jj++)
                monsterHouseStep.MobThemes.Add(mobThemeListSlice.GetSpawn(jj).Copy(), mobThemeListSlice.GetSpawnRate(jj));

            queue.Enqueue(Priority, monsterHouseStep);

            return true;
        }
    }
}

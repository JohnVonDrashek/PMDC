using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Dungeon;
using RogueEssence;
using RogueEssence.LevelGen;
using PMDC.Dungeon;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Abstract base class for all monster house generation steps.
    /// Monster houses are trap rooms that spawn enemies when the player enters, containing items as bait.
    /// Subclasses implement specific monster house layouts and spawning strategies.
    /// </summary>
    /// <typeparam name="T">The map generation context type, constrained to <see cref="ListMapGenContext"/>.</typeparam>
    [Serializable]
    public abstract class MonsterHouseBaseStep<T> : GenStep<T>, IMonsterHouseBaseStep
        where T : ListMapGenContext
    {
        /// <summary>
        /// The odds (1 in N) that a spawned monster will have an alternate color skin.
        /// Value of 32 means a 1 in 32 chance (approximately 3.125%).
        /// </summary>
        public const int ALT_COLOR_ODDS = 32;

        /// <summary>
        /// Gets or sets the pool of items that can be found in the monster house.
        /// These items are spawned in addition to the items naturally found elsewhere on the map.
        /// Spawn rates determine the relative likelihood of each item appearing.
        /// </summary>
        public SpawnList<MapItem> Items { get; set; }

        /// <summary>
        /// Gets or sets the item themes used to filter the items that can spawn in the monster house.
        /// Each theme has a spawn weight that affects which themed items are more likely to appear.
        /// </summary>
        public SpawnList<ItemTheme> ItemThemes { get; set; }

        /// <summary>
        /// Gets or sets the pool of mobs (monsters) that can be found in the monster house.
        /// These mobs are spawned in addition to the mobs naturally found elsewhere on the map.
        /// Spawn rates determine the relative likelihood of each mob appearing.
        /// </summary>
        public SpawnList<MobSpawn> Mobs { get; set; }

        /// <summary>
        /// Gets or sets the mob themes used to filter the mobs that can spawn in the monster house.
        /// Each theme has a spawn weight that affects which themed mobs are more likely to appear.
        /// </summary>
        public SpawnList<MobTheme> MobThemes { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonsterHouseBaseStep{T}"/> class with empty spawn lists.
        /// </summary>
        public MonsterHouseBaseStep()
        {
            Items = new SpawnList<MapItem>();
            ItemThemes = new SpawnList<ItemTheme>();
            Mobs = new SpawnList<MobSpawn>();
            MobThemes = new SpawnList<MobTheme>();
        }

        /// <summary>
        /// Copy constructor that creates a deep copy of the specified monster house step.
        /// Clones all spawn lists and their contents to ensure the copy is independent from the original.
        /// </summary>
        /// <param name="other">The monster house step instance to copy from.</param>
        protected MonsterHouseBaseStep(MonsterHouseBaseStep<T> other) : this()
        {
            for (int ii = 0; ii < other.Items.Count; ii++)
                Items.Add(new MapItem(other.Items.GetSpawn(ii)), other.Items.GetSpawnRate(ii));
            for (int ii = 0; ii < other.ItemThemes.Count; ii++)
                ItemThemes.Add(other.ItemThemes.GetSpawn(ii).Copy(), other.ItemThemes.GetSpawnRate(ii));
            for (int ii = 0; ii < other.Mobs.Count; ii++)
                Mobs.Add(other.Mobs.GetSpawn(ii).Copy(), other.Mobs.GetSpawnRate(ii));
            for (int ii = 0; ii < other.MobThemes.Count; ii++)
                MobThemes.Add(other.MobThemes.GetSpawn(ii).Copy(), other.MobThemes.GetSpawnRate(ii));
        }

        /// <summary>
        /// Adds an intrusion check event to the map that triggers when the player enters the specified bounds.
        /// If an "intrusion_check" map status already exists, the check is added to its existing event list.
        /// Otherwise, a new map status is created and the check is added to it.
        /// </summary>
        /// <param name="map">The map generation context where the intrusion check will be added.</param>
        /// <param name="check">The <see cref="CheckIntrudeBoundsEvent"/> that defines the bounds and behavior when the player enters.</param>
        protected void AddIntrudeStep(T map, CheckIntrudeBoundsEvent check)
        {
            //TODO: remove this magic number
            string intrudeStatus = "intrusion_check";
            MapStatus status;
            if (map.Map.Status.TryGetValue(intrudeStatus, out status))
            {
                MapCheckState destChecks = status.StatusStates.GetWithDefault<MapCheckState>();
                destChecks.CheckEvents.Add(check);
            }
            else
            {
                status = new MapStatus(intrudeStatus);
                status.LoadFromData();
                MapCheckState checkState = status.StatusStates.GetWithDefault<MapCheckState>();
                checkState.CheckEvents.Add(check);
                map.Map.Status.Add(intrudeStatus, status);
            }
        }

        /// <summary>
        /// Creates a deep copy of this monster house step.
        /// Must be implemented by subclasses to ensure proper cloning with all subclass-specific properties.
        /// </summary>
        /// <returns>A new instance that is a deep copy of this monster house step.</returns>
        public abstract MonsterHouseBaseStep<T> CreateNew();

        /// <inheritdoc/>
        IMonsterHouseBaseStep IMonsterHouseBaseStep.CreateNew() { return CreateNew(); }
    }

    /// <summary>
    /// Interface for monster house generation steps, allowing type-agnostic access to monster house configuration.
    /// This interface enables different generic implementations of <see cref="MonsterHouseBaseStep{T}"/> to be handled uniformly.
    /// </summary>
    public interface IMonsterHouseBaseStep : IGenStep
    {
        /// <summary>
        /// Gets or sets the pool of items that can be found in the monster house.
        /// Spawn rates in this list determine the relative likelihood of each item appearing.
        /// </summary>
        SpawnList<MapItem> Items { get; set; }

        /// <summary>
        /// Gets or sets the item themes used to filter the items that can spawn in the monster house.
        /// Each theme's spawn rate controls how often items of that theme are selected.
        /// </summary>
        SpawnList<ItemTheme> ItemThemes { get; set; }

        /// <summary>
        /// Gets or sets the pool of mobs (monsters) that can be found in the monster house.
        /// Spawn rates in this list determine the relative likelihood of each mob appearing.
        /// </summary>
        SpawnList<MobSpawn> Mobs { get; set; }

        /// <summary>
        /// Gets or sets the mob themes used to filter the mobs that can spawn in the monster house.
        /// Each theme's spawn rate controls how often mobs of that theme are selected.
        /// </summary>
        SpawnList<MobTheme> MobThemes { get; set; }

        /// <summary>
        /// Creates a deep copy of this monster house step.
        /// The implementation should ensure all configuration properties are properly cloned.
        /// </summary>
        /// <returns>A new instance that is a deep copy of this step.</returns>
        IMonsterHouseBaseStep CreateNew();
    }
}

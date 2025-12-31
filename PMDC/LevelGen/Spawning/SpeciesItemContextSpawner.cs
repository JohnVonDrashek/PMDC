using RogueElements;
using System;
using System.IO;
using System.Collections.Generic;
using RogueEssence.Dungeon;
using RogueEssence.LevelGen;
using RogueEssence;
using RogueEssence.Data;
using System.Xml;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Spawns species-specific items on the floor by enumerating possible creatures from active team spawners.
    /// </summary>
    /// <remarks>
    /// This spawner determines which species are possible to spawn by examining the creatures
    /// that can be spawned on the current floor from all active team spawners. It then generates
    /// items appropriate for those species based on the configured rarity and amount parameters.
    /// </remarks>
    /// <typeparam name="TGenContext">The map generation context type that provides team spawner information.</typeparam>
    [Serializable]
    public class SpeciesItemContextSpawner<TGenContext> : SpeciesItemSpawner<TGenContext>
        where TGenContext : BaseMapGenContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpeciesItemContextSpawner{TGenContext}"/> class.
        /// </summary>
        public SpeciesItemContextSpawner()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeciesItemContextSpawner{TGenContext}"/> class with the specified rarity range and amount.
        /// </summary>
        /// <param name="rarity">The rarity range of items to spawn.</param>
        /// <param name="amount">The random range for the quantity of items to spawn.</param>
        public SpeciesItemContextSpawner(IntRange rarity, RandRange amount) : base(rarity, amount)
        {

        }

        /// <summary>
        /// Gets all possible species that can be spawned on the current floor by examining active team spawners.
        /// </summary>
        /// <remarks>
        /// This method iterates through all team spawners available in the map's team spawns collection,
        /// retrieves the possible creature spawns for the current floor, and yields the base species of each spawn.
        /// This ensures that only species that can actually appear on the floor are considered for item spawning.
        /// </remarks>
        /// <param name="map">The map generation context containing team spawner information.</param>
        /// <returns>An enumerable of species strings representing creatures that can spawn on the floor.</returns>
        public override IEnumerable<string> GetPossibleSpecies(TGenContext map)
        {
            foreach (TeamSpawner teamSpawn in map.TeamSpawns.EnumerateOutcomes())
            {
                SpawnList<MobSpawn> mobsAtFloor = teamSpawn.GetPossibleSpawns();
                foreach (MobSpawn mobSpawn in mobsAtFloor.EnumerateOutcomes())
                    yield return mobSpawn.BaseForm.Species;
            }
        }
    }
}

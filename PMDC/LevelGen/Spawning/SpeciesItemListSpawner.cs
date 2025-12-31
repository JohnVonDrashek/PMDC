using RogueElements;
using System;
using System.IO;
using System.Collections.Generic;
using RogueEssence.Dungeon;
using RogueEssence.LevelGen;
using RogueEssence;
using RogueEssence.Data;
using System.Xml;
using Newtonsoft.Json;
using RogueEssence.Dev;
using PMDC.Data;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Spawns items associated with a specific list of monster species.
    /// Uses the rarity table to determine which exclusive items can spawn for the given species.
    /// </summary>
    /// <typeparam name="TGenContext">The map generation context type.</typeparam>
    [Serializable]
    public class SpeciesItemListSpawner<TGenContext> : SpeciesItemSpawner<TGenContext>
        where TGenContext : BaseMapGenContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpeciesItemListSpawner{TGenContext}"/> class with an empty species list.
        /// </summary>
        public SpeciesItemListSpawner()
        {
            this.Species = new List<string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeciesItemListSpawner{TGenContext}"/> class with the specified rarity range, item amount range, and species list.
        /// </summary>
        /// <param name="rarity">The rarity level range to use when determining which exclusive items can spawn.</param>
        /// <param name="amount">The range of item quantities to spawn.</param>
        /// <param name="species">The monster species whose exclusive items can be spawned.</param>
        public SpeciesItemListSpawner(IntRange rarity, RandRange amount, params string[] species) : base(rarity, amount)
        {
            this.Species = new List<string>();
            this.Species.AddRange(species);
        }

        /// <summary>
        /// Gets or sets the list of monster species whose exclusive items can be spawned.
        /// </summary>
        [JsonConverter(typeof(MonsterListConverter))]
        [DataType(1, DataManager.DataType.Monster, false)]
        public List<string> Species { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<string> GetPossibleSpecies(TGenContext map)
        {
            foreach (string baseSpecies in Species)
                yield return baseSpecies;
        }
    }
}

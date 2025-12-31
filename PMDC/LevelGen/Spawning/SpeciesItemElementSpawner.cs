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
    /// Spawns items matching monsters of a specific element type, with optional exclusions.
    /// </summary>
    /// <remarks>
    /// This spawner filters the pool of available species to only include those that match
    /// the specified element (primary or secondary). Species can be explicitly excluded via
    /// the ExceptFor set.
    /// </remarks>
    /// <typeparam name="TGenContext">The map generation context type.</typeparam>
    [Serializable]
    public class SpeciesItemElementSpawner<TGenContext> : SpeciesItemSpawner<TGenContext>
        where TGenContext : BaseMapGenContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpeciesItemElementSpawner{TGenContext}"/> class.
        /// </summary>
        public SpeciesItemElementSpawner()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeciesItemElementSpawner{TGenContext}"/> class with rarity range, amount, element type, and exclusion list.
        /// </summary>
        /// <param name="rarity">The rarity range for items to spawn.</param>
        /// <param name="amount">The number of items to spawn per occurrence.</param>
        /// <param name="element">The element type to filter species by (primary or secondary).</param>
        /// <param name="exceptFor">A set of species IDs to exclude from the spawn pool.</param>
        public SpeciesItemElementSpawner(IntRange rarity, RandRange amount, string element, HashSet<string> exceptFor) : base(rarity, amount)
        {
            Element = element;
            ExceptFor = exceptFor;
        }

        /// <summary>
        /// Gets or sets the element type to filter species by.
        /// </summary>
        /// <remarks>
        /// Species are included if their primary or secondary element matches this value,
        /// or if this is set to the default element.
        /// </remarks>
        [DataType(0, DataManager.DataType.Element, false)]
        public string Element { get; set; }

        /// <summary>
        /// Gets or sets the set of species IDs to exclude from the spawn pool.
        /// </summary>
        [DataType(1, DataManager.DataType.Monster, false)]
        public HashSet<string> ExceptFor { get; set; }

        /// <summary>
        /// Gets the enumerable of species that match the element filter and are not excluded.
        /// </summary>
        /// <remarks>
        /// Iterates through all released monsters in the data manager and yields those whose
        /// primary or secondary element matches the configured element, excluding any species
        /// in the ExceptFor set.
        /// </remarks>
        /// <param name="map">The map generation context (unused).</param>
        /// <returns>An enumerable of species IDs that match the filter criteria.</returns>
        public override IEnumerable<string> GetPossibleSpecies(TGenContext map)
        {
            MonsterFeatureData feature = DataManager.Instance.UniversalData.Get<MonsterFeatureData>();
            //iterate all species that have that element, except for
            foreach (string key in DataManager.Instance.DataIndices[DataManager.DataType.Monster].GetOrderedKeys(true))
            {
                if (ExceptFor.Contains(key))
                    continue;
                EntrySummary summary = DataManager.Instance.DataIndices[DataManager.DataType.Monster].Get(key);
                if (!summary.Released)
                    continue;

                Dictionary<int, FormFeatureSummary> species;
                if (!feature.FeatureData.TryGetValue(key, out species))
                    continue;
                FormFeatureSummary form;
                if (!species.TryGetValue(0, out form))
                    continue;

                if (Element == DataManager.Instance.DefaultElement || form.Element1 == Element || form.Element2 == Element)
                    yield return key;
            }
        }
    }
}

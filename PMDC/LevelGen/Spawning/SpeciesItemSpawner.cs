using RogueElements;
using System;
using System.IO;
using System.Collections.Generic;
using RogueEssence.Dungeon;
using RogueEssence.LevelGen;
using RogueEssence;
using RogueEssence.Data;
using System.Xml;
using PMDC.Data;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Base class for spawning items associated with monster species.
    /// Uses the rarity table to determine which exclusive items can spawn based on species and rarity tier.
    /// </summary>
    /// <typeparam name="TGenContext">The map generation context type.</typeparam>
    [Serializable]
    public abstract class SpeciesItemSpawner<TGenContext> : IStepSpawner<TGenContext, MapItem>
        where TGenContext : BaseMapGenContext
    {
        /// <summary>
        /// Initializes a new instance of the SpeciesItemSpawner class with default values.
        /// </summary>
        public SpeciesItemSpawner()
        {
        }

        /// <summary>
        /// Initializes a new instance of the SpeciesItemSpawner class with the specified rarity range and item amount.
        /// </summary>
        /// <param name="rarity">The range of rarity tiers to include when looking up exclusive items.</param>
        /// <param name="amount">The number of items to spawn.</param>
        public SpeciesItemSpawner(IntRange rarity, RandRange amount)
        {
            this.Rarity = rarity;
            this.Amount = amount;
        }

        /// <summary>
        /// The range of rarity tiers to include when looking up exclusive items.
        /// </summary>
        public IntRange Rarity { get; set; }

        /// <summary>
        /// The number of items to spawn.
        /// </summary>
        public RandRange Amount { get; set; }

        /// <summary>
        /// Gets the species whose exclusive items should be considered for spawning.
        /// </summary>
        /// <param name="map">The map generation context.</param>
        /// <returns>An enumerable of species IDs.</returns>
        public abstract IEnumerable<string> GetPossibleSpecies(TGenContext map);

        /// <summary>
        /// Generates the list of items to spawn based on species and rarity settings.
        /// </summary>
        /// <param name="map">The map generation context.</param>
        /// <returns>A list of map items to be placed.</returns>
        public List<MapItem> GetSpawns(TGenContext map)
        {
            int chosenAmount = Amount.Pick(map.Rand);

            RarityData rarity = DataManager.Instance.UniversalData.Get<RarityData>();
            List<string> possibleItems = new List<string>();
            foreach (string baseSpecies in GetPossibleSpecies(map))
            {
                for (int ii = Rarity.Min; ii < Rarity.Max; ii++)
                {
                    Dictionary<int, List<string>> rarityTable;
                    if (rarity.RarityMap.TryGetValue(baseSpecies, out rarityTable))
                    {
                        if (rarityTable.ContainsKey(ii))
                        {
                            foreach (string item in rarityTable[ii])
                            {
                                EntrySummary summary = DataManager.Instance.DataIndices[DataManager.DataType.Item].Get(item);
                                if (summary.Released)
                                    possibleItems.Add(item);
                            }
                        }
                    }
                }
            }

            List<MapItem> results = new List<MapItem>();
            if (possibleItems.Count > 0)
            {
                for (int ii = 0; ii < chosenAmount; ii++)
                {
                    string chosenItem = possibleItems[map.Rand.Next(possibleItems.Count)];
                    results.Add(new MapItem(chosenItem));
                }
            }

            return results;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0}: Rarity:{1} Amt:{2}", this.GetType().GetFormattedTypeName(), this.Rarity.ToString(), this.Amount.ToString());
        }
    }
}

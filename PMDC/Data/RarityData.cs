using System;
using System.Collections.Generic;
using RogueElements;
using System.Drawing;
using RogueEssence;
using RogueEssence.Data;
using RogueEssence.Dungeon;
using System.IO;
using PMDC.Dungeon;

namespace PMDC.Data
{
    /// <summary>
    /// Indexed data class that maps items to monsters based on rarity and family membership.
    /// Used for determining which items are associated with specific monster species
    /// at different rarity tiers.
    /// </summary>
    [Serializable]
    public class RarityData : BaseData
    {
        /// <summary>
        /// The filename used when saving/loading this data index.
        /// </summary>
        public override string FileName => "Rarity";

        /// <summary>
        /// The data type that triggers re-indexing when modified (Item data).
        /// </summary>
        public override DataManager.DataType TriggerType => DataManager.DataType.Item;

        /// <summary>
        /// Dictionary mapping monster species ID to a dictionary of rarity tier to item IDs.
        /// Provides lookup of items associated with each monster at each rarity level.
        /// </summary>
        public Dictionary<string, Dictionary<int, List<string>>> RarityMap;

        /// <summary>
        /// Initializes a new instance of the RarityData class with an empty rarity map.
        /// </summary>
        public RarityData()
        {
            RarityMap = new Dictionary<string, Dictionary<int, List<string>>>();
        }

        /// <inheritdoc/>
        public override void ContentChanged(string idx)
        {
            //remove the index from its previous locations
            foreach (Dictionary<int, List<string>> rarityTable in RarityMap.Values)
            {
                foreach (List<string> items in rarityTable.Values)
                {
                    if (items.Remove(idx))
                        break;
                }
            }

            //check against deletion
            ItemData data = DataManager.LoadEntryData<ItemData>(idx, DataManager.DataType.Item.ToString());
            if (data != null)
            {
                computeSummary(idx, data);
            }
        }

        /// <inheritdoc/>
        public override void ReIndex()
        {
            RarityMap.Clear();

            string dataPath = DataManager.DATA_PATH + DataManager.DataType.Item.ToString() + "/";
            foreach (string dir in PathMod.GetModFiles(dataPath, "*" + DataManager.DATA_EXT))
            {
                string file = Path.GetFileNameWithoutExtension(dir);
                ItemData data = DataManager.LoadEntryData<ItemData>(file, DataManager.DataType.Item.ToString());
                if (data.Released)
                    computeSummary(file, data);
            }
        }

        /// <summary>
        /// Adds an item to the rarity map based on its family state and rarity value.
        /// Items with a FamilyState are added to each monster in the family.
        /// </summary>
        /// <param name="num">The item ID to add to the rarity map.</param>
        /// <param name="data">The item data containing family and rarity information.</param>
        private void computeSummary(string num, ItemData data)
        {
            FamilyState family;
            if (data.ItemStates.TryGet<FamilyState>(out family))
            {
                foreach (string monster in family.Members)
                {
                    if (!RarityMap.ContainsKey(monster))
                        RarityMap[monster] = new Dictionary<int, List<string>>();

                    if (!RarityMap[monster].ContainsKey(data.Rarity))
                        RarityMap[monster][data.Rarity] = new List<string>();

                    RarityMap[monster][data.Rarity].Add(num);
                }
            }
        }
    }

}
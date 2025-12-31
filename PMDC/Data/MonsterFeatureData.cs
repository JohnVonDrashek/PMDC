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
    /// Flags representing the evolutionary stage of a monster species.
    /// Used to categorize monsters based on their position in an evolution chain.
    /// </summary>
    [Flags]
    public enum EvoFlag
    {
        /// <summary>
        /// No evolution stage specified.
        /// </summary>
        None = 0,
        /// <summary>
        /// Monster has no evolutions (standalone species).
        /// </summary>
        NoEvo = 1,//^0
        /// <summary>
        /// Monster is the first stage in an evolution chain.
        /// </summary>
        FirstEvo = 2,//^1
        /// <summary>
        /// Monster is the final stage in an evolution chain.
        /// </summary>
        FinalEvo = 4,//^2
        /// <summary>
        /// Monster is a middle stage in an evolution chain.
        /// </summary>
        MidEvo = 8,//^3
        /// <summary>
        /// Includes all evolution stages.
        /// </summary>
        All = 15
    }

    /// <summary>
    /// Contains summarized feature data for a specific monster form.
    /// Includes evolutionary family information, elemental types, and stat analysis.
    /// </summary>
    [Serializable]
    public class FormFeatureSummary
    {
        /// <summary>
        /// The species ID of the base (first stage) monster in this evolutionary family.
        /// </summary>
        public string Family;

        /// <summary>
        /// The evolutionary stage of this monster (first, middle, final, or standalone).
        /// </summary>
        public EvoFlag Stage;

        /// <summary>
        /// The primary elemental type of this monster form.
        /// </summary>
        public string Element1;

        /// <summary>
        /// The secondary elemental type of this monster form. May be empty if mono-type.
        /// </summary>
        public string Element2;

        /// <summary>
        /// The stat category where this monster form has the highest base value.
        /// Set to None if there is a tie for highest stat.
        /// </summary>
        public Stat BestStat;

        /// <summary>
        /// The stat category where this monster form has the lowest base value.
        /// Set to None if there is a tie for lowest stat.
        /// </summary>
        public Stat WorstStat;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormFeatureSummary"/> class.
        /// All fields are initialized to their default values.
        /// </summary>
        public FormFeatureSummary() { }
    }


    /// <summary>
    /// Indexed data class that computes and stores feature summaries for all monster forms.
    /// Automatically updates when monster data changes and provides quick lookup of
    /// evolutionary family information, elemental types, and stat characteristics.
    /// </summary>
    [Serializable]
    public class MonsterFeatureData : BaseData
    {
        /// <summary>
        /// The filename used when saving/loading this data index.
        /// </summary>
        public override string FileName => "MonsterFeature";

        /// <summary>
        /// The data type that triggers re-indexing when modified (Monster data).
        /// </summary>
        public override DataManager.DataType TriggerType => DataManager.DataType.Monster;

        /// <summary>
        /// Dictionary mapping monster species ID to a dictionary of form index to feature summary.
        /// Provides quick lookup of computed features for any monster form.
        /// </summary>
        public Dictionary<string, Dictionary<int, FormFeatureSummary>> FeatureData;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonsterFeatureData"/> class with an empty feature dictionary.
        /// </summary>
        public MonsterFeatureData()
        {
            FeatureData = new Dictionary<string, Dictionary<int, FormFeatureSummary>>();
        }

        /// <summary>
        /// Updates the feature data for a specific monster when its data changes.
        /// Recomputes the feature summary for all forms of the specified monster.
        /// </summary>
        /// <param name="idx">The species ID of the monster that was modified.</param>
        public override void ContentChanged(string idx)
        {
            MonsterData data = DataManager.LoadEntryData<MonsterData>(idx, DataManager.DataType.Monster.ToString());
            Dictionary<int, FormFeatureSummary> formSummaries = computeSummary(idx, data);
            FeatureData[idx] = formSummaries;
        }

        /// <summary>
        /// Rebuilds the entire feature data index by scanning all monster data files.
        /// Computes feature summaries for every form of every monster species.
        /// </summary>
        public override void ReIndex()
        {
            FeatureData.Clear();

            string dataPath = DataManager.DATA_PATH + DataManager.DataType.Monster.ToString() + "/";
            foreach (string dir in PathMod.GetModFiles(dataPath, "*" + DataManager.DATA_EXT))
            {
                string file = Path.GetFileNameWithoutExtension(dir);
                MonsterData data = DataManager.LoadEntryData<MonsterData>(file, DataManager.DataType.Monster.ToString());
                Dictionary<int, FormFeatureSummary> formSummaries = computeSummary(file, data);
                FeatureData[file] = formSummaries;
            }
        }

        /// <summary>
        /// Computes feature summaries for all forms of a monster species.
        /// Determines evolutionary family, stage, elemental types, and best/worst stats for each form.
        /// The evolutionary family is traced by walking backwards through the evolution chain to find the base (first stage) species.
        /// </summary>
        /// <param name="num">The species ID of the monster whose forms are being summarized.</param>
        /// <param name="data">The monster data containing form information and evolution chain references.</param>
        /// <returns>A dictionary mapping form index to the computed <see cref="FormFeatureSummary"/> for that form.</returns>
        private Dictionary<int, FormFeatureSummary> computeSummary(string num, MonsterData data)
        {
            Dictionary<int, FormFeatureSummary> formFeatureData = new Dictionary<int, FormFeatureSummary>();
            string family = num;
            MonsterData preEvo = data;
            HashSet<string> traversed = new HashSet<string>();
            while (!String.IsNullOrEmpty(preEvo.PromoteFrom) && !traversed.Contains(preEvo.PromoteFrom))
            {
                traversed.Add(family);
                family = preEvo.PromoteFrom.ToString();
                preEvo = DataManager.LoadEntryData<MonsterData>(family, DataManager.DataType.Monster.ToString());
            }
            EvoFlag stage = EvoFlag.NoEvo;
            bool evolvedFrom = !String.IsNullOrEmpty(data.PromoteFrom);
            bool evolves = (data.Promotions.Count > 0);
            if (evolvedFrom && evolves)
                stage = EvoFlag.MidEvo;
            else if (evolvedFrom)
                stage = EvoFlag.FinalEvo;
            else if (evolves)
                stage = EvoFlag.FirstEvo;

            for (int ii = 0; ii < data.Forms.Count; ii++)
            {
                FormFeatureSummary summary = new FormFeatureSummary();
                summary.Family = family;
                summary.Stage = stage;

                MonsterFormData formData = data.Forms[ii] as MonsterFormData;
                summary.Element1 = formData.Element1;
                summary.Element2 = formData.Element2;

                Stat bestStat = Stat.HP;
                Stat worstStat = Stat.HP;

                for (int nn = 0; nn < (int)Stat.HitRate; nn++)
                {
                    if (bestStat != Stat.None)
                    {
                        if (formData.GetBaseStat((Stat)nn) > formData.GetBaseStat(bestStat))
                            bestStat = (Stat)nn;
                        else if (formData.GetBaseStat((Stat)nn) == formData.GetBaseStat(bestStat))
                            bestStat = Stat.None;
                    }
                    if (worstStat != Stat.None)
                    {
                        if (formData.GetBaseStat((Stat)nn) < formData.GetBaseStat(worstStat))
                            worstStat = (Stat)nn;
                        else if (formData.GetBaseStat((Stat)nn) == formData.GetBaseStat(worstStat))
                            worstStat = Stat.None;
                    }
                }
                summary.BestStat = bestStat;
                summary.WorstStat = worstStat;

                formFeatureData[ii] = summary;
            }
            return formFeatureData;
        }
    }

}
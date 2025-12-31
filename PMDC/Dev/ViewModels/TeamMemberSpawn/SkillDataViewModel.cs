using RogueEssence;
using RogueEssence.Data;
using RogueEssence.Dev.ViewModels;
using RogueEssence.Dungeon;

namespace PMDC.Dev.ViewModels
{
    /// <summary>
    /// View model for displaying skill (move) data in the team member spawn editor.
    /// Provides bindable properties for move stats, element, category, and descriptions.
    /// </summary>
    public class SkillDataViewModel : ViewModelBase
    {
        /// <summary>
        /// The summary data for this skill.
        /// </summary>
        private SkillDataSummary summary;

        /// <summary>
        /// Gets the index of this skill in the filtered list for selection tracking.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Indicates whether the currently selected monster can learn this skill.
        /// </summary>
        private bool _monsterLearns;

        /// <summary>
        /// Creates a new SkillDataViewModel for the specified skill.
        /// </summary>
        /// <param name="skillKey">The skill key identifier.</param>
        /// <param name="index">The index in the overall skill list.</param>
        public SkillDataViewModel(string skillKey, int index)
        {
            Index = index;
            summary = (SkillDataSummary) DataManager.Instance.DataIndices[DataManager.DataType.Skill].Get(skillKey);
        }

        /// <summary>
        /// Sets whether the currently selected monster can learn this skill.
        /// </summary>
        /// <param name="learns">True if the monster can learn this skill.</param>
        public void SetMonsterLearns(bool learns)
        {
            _monsterLearns = learns;
        }

        /// <summary>
        /// Gets whether the currently selected monster can learn this skill.
        /// </summary>
        public bool MonsterLearns
        {
            get { return _monsterLearns; }
        }

        /// <summary>
        /// Gets the localized display name of the skill.
        /// </summary>
        public string Name
        {
            get { return summary.Name.ToLocal();  }
        }

        /// <summary>
        /// Gets the element (type) key of the skill.
        /// </summary>
        public string Element
        {
            get { return summary.Element;  }
        }

        /// <summary>
        /// Gets the localized display name of the skill's element.
        /// </summary>
        public string ElementDisplay
        {
            get { return DataManager.Instance.GetElement(Element).Name.ToLocal(); }
        }

        /// <summary>
        /// Gets the skill category (Physical, Special, or Status).
        /// </summary>
        public BattleData.SkillCategory Category
        {
            get { return summary.Category; }
        }

        /// <summary>
        /// Gets the localized display name of the skill category.
        /// </summary>
        public string CategoryDisplay
        {
            get { return summary.Category.ToLocal();  }
        }

        /// <summary>
        /// Gets the base power of the skill.
        /// </summary>
        public int BasePower
        {
            get { return summary.BasePower; }
        }

        /// <summary>
        /// Gets the base PP (power points/charges) of the skill.
        /// </summary>
        public int BaseCharges
        {
            get { return summary.BaseCharges; }
        }

        /// <summary>
        /// Gets the accuracy (hit rate) of the skill.
        /// </summary>
        public int Accuracy
        {
            get { return summary.HitRate; }
        }

        /// <summary>
        /// Gets a description of the skill's targeting range.
        /// </summary>
        public string RangeDescription
        {
            get { return summary.RangeDescription; }
        }

        /// <summary>
        /// Gets the localized description of the skill's effects.
        /// </summary>
        public string Description
        {
            get { return summary.Description.ToLocal(); }
        }

        /// <summary>
        /// Gets whether this skill has been released for use in the game.
        /// </summary>
        public bool Released
        {
            get { return summary.Released; }
        }
    }
}
using RogueEssence.Data;
using RogueEssence.Dev.ViewModels;

namespace PMDC.Dev.ViewModels
{
    /// <summary>
    /// View model for displaying intrinsic (ability) data in the team member spawn editor.
    /// Provides bindable properties for ability name, description, and availability.
    /// </summary>
    public class IntrinsicViewModel : ViewModelBase
    {
        /// <summary>
        /// The underlying intrinsic data being wrapped.
        /// </summary>
        private IntrinsicData intrinsicData;

        /// <summary>
        /// Indicates whether the currently selected monster can have this ability.
        /// </summary>
        private bool _monsterLearns;

        /// <summary>
        /// Gets the index of this ability in the filtered list for selection tracking.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Creates a new IntrinsicViewModel for the specified ability.
        /// </summary>
        /// <param name="key">The ability key identifier.</param>
        /// <param name="index">The index in the overall ability list.</param>
        public IntrinsicViewModel(string key, int index)
        {
            intrinsicData = DataManager.Instance.GetIntrinsic(key);
            Index = index;
        }

        /// <summary>
        /// Gets whether the currently selected monster can have this ability.
        /// </summary>
        public bool MonsterLearns
        {
            get => _monsterLearns;
        }

        /// <summary>
        /// Sets whether the currently selected monster can have this ability.
        /// </summary>
        /// <param name="learns">True if the monster can have this ability.</param>
        public void SetMonsterLearns(bool learns)
        {
            _monsterLearns = learns;
        }

        /// <summary>
        /// Gets the localized display name of the ability.
        /// </summary>
        public string Name
        {
            get { return intrinsicData.Name.ToLocal(); }
        }

        /// <summary>
        /// Gets whether this ability has been released for use in the game.
        /// </summary>
        public bool Released
        {
            get { return intrinsicData.Released; }
        }

        /// <summary>
        /// Gets the localized description of the ability.
        /// </summary>
        public string Description
        {
            get { return intrinsicData.Desc.ToLocal(); }
        }
    }
}
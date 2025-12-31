using System;
using RogueElements;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Data;

namespace PMDC.Dungeon
{
    /// <summary>
    /// AI plan for thief-type characters that flee after stealing an item.
    /// The character will run away from enemies only after successfully stealing
    /// (when their held item changes from the original). If cornered, the plan aborts
    /// to allow fallback behavior. Used for hit-and-run thieves.
    /// </summary>
    [Serializable]
    public class ThiefPlan : AvoidPlan
    {
        /// <summary>
        /// The original item ID the character was holding at spawn.
        /// Used to detect when a theft has occurred (item changed).
        /// </summary>
        [NonSerialized]
        private string origItem;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThiefPlan"/> class.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        public ThiefPlan(AIFlags iq) : base(iq)
        {
            origItem = "";
        }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        protected ThiefPlan(ThiefPlan other) : base(other) { }

        /// <inheritdoc/>
        public override BasePlan CreateNew() { return new ThiefPlan(this); }

        /// <summary>
        /// Gets a value indicating whether the character should flee from allies.
        /// Thieves do not flee from allies, only from enemies.
        /// </summary>
        /// <inheritdoc/>
        protected override bool RunFromAllies { get { return false; } }

        /// <summary>
        /// Gets a value indicating whether the character should flee from foes.
        /// Thieves actively flee from enemies, especially after stealing.
        /// </summary>
        /// <inheritdoc/>
        protected override bool RunFromFoes { get { return true; } }

        /// <summary>
        /// Gets a value indicating whether the plan should be aborted if cornered.
        /// Thieves will abort if cornered, allowing fallback behavior.
        /// </summary>
        /// <inheritdoc/>
        protected override bool AbortIfCornered { get { return true; } }

        /// <summary>
        /// Initializes the plan by recording the character's original held item.
        /// </summary>
        /// <param name="controlledChar">The character being controlled.</param>
        public override void Initialize(Character controlledChar)
        {
            origItem = controlledChar.EquippedItem.ID;
            base.Initialize(controlledChar);
        }

        /// <summary>
        /// Flees from enemies only if the character has stolen an item (item changed).
        /// </summary>
        /// <param name="controlledChar">The character being controlled by this AI.</param>
        /// <param name="preThink">Whether this is a pre-think evaluation.</param>
        /// <param name="rand">Random number generator for decision-making.</param>
        /// <returns>The escape action if item was stolen, or null to defer to other plans.</returns>
        public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
        {
            if (controlledChar.EquippedItem.ID != origItem && !String.IsNullOrEmpty(controlledChar.EquippedItem.ID))//we have a held item that is different now
                return base.Think(controlledChar, preThink, rand);

            return null;
        }
    }
}

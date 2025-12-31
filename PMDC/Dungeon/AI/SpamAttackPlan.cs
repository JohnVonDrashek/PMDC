using System;
using RogueElements;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Data;

namespace PMDC.Dungeon
{
    /// <summary>
    /// AI plan that uses the first available skill every turn regardless of targets.
    /// The character will spam skills in order from slot 0 until one is usable.
    /// Used for characters that should continuously use abilities without targeting logic.
    /// </summary>
    /// <remarks>
    /// This plan iterates through the character's skill slots and returns the first skill that meets all availability conditions:
    /// - Has a valid skill number assigned
    /// - Has charges remaining
    /// - Is not sealed
    /// - Is enabled
    /// The returned action has no directional component, allowing the game engine to determine targeting.
    /// </remarks>
    [Serializable]
    public class SpamAttackPlan : AIPlan
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpamAttackPlan"/> class.
        /// </summary>
        public SpamAttackPlan() { }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        protected SpamAttackPlan(SpamAttackPlan other) : base(other) { }

        /// <inheritdoc/>
        public override BasePlan CreateNew() { return new SpamAttackPlan(this); }

        /// <summary>
        /// Determines the next action by selecting the first available skill from the character's skill list.
        /// </summary>
        /// <param name="controlledChar">The character being controlled by this AI.</param>
        /// <param name="preThink">Whether this is a pre-think evaluation phase. This plan uses the same logic regardless of phase.</param>
        /// <param name="rand">Random number generator for decision-making. Not used by this plan.</param>
        /// <returns>
        /// A <see cref="GameAction"/> representing a skill use action with the index of the first available skill,
        /// or <c>null</c> if no skills are available.
        /// </returns>
        public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
        {
            //need attack action check
            for (int ii = 0; ii < controlledChar.Skills.Count; ii++)
            {
                if (!String.IsNullOrEmpty(controlledChar.Skills[ii].Element.SkillNum) && controlledChar.Skills[ii].Element.Charges > 0 && !controlledChar.Skills[ii].Element.Sealed && controlledChar.Skills[ii].Element.Enabled)
                    return new GameAction(GameAction.ActionType.UseSkill, Dir8.None, ii);
            }
            return null;
        }
    }
}

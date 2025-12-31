using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Data;

namespace PMDC.Dungeon
{
    /// <summary>
    /// AI plan that causes the character to retreat when HP drops below a threshold.
    /// The character will flee from enemies and attempt to use Teleport if available.
    /// If cornered, the plan aborts to allow fallback to combat behavior.
    /// </summary>
    [Serializable]
    public class RetreaterPlan : AvoidPlan
    {
        /// <summary>
        /// The HP threshold factor. The plan activates when HP * Factor &lt; MaxHP.
        /// A factor of 2 means activation at 50% HP, factor of 4 means 25% HP, etc.
        /// </summary>
        public int Factor;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetreaterPlan"/> class.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        /// <param name="factor">HP threshold factor for activation.</param>
        public RetreaterPlan(AIFlags iq, int factor) : base(iq) { Factor = factor; }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        protected RetreaterPlan(RetreaterPlan other) : base(other) { Factor = other.Factor; }

        /// <inheritdoc/>
        public override BasePlan CreateNew() { return new RetreaterPlan(this); }

        /// <inheritdoc/>
        /// <remarks>The retreater does not flee from allies.</remarks>
        protected override bool RunFromAllies { get { return false; } }

        /// <inheritdoc/>
        /// <remarks>The retreater flees from foes when HP is low.</remarks>
        protected override bool RunFromFoes { get { return true; } }

        /// <inheritdoc/>
        /// <remarks>If cornered with no escape route, the plan aborts to allow fallback behavior.</remarks>
        protected override bool AbortIfCornered { get { return true; } }

        /// <summary>
        /// Retreats from enemies when HP is low, using Teleport if available.
        /// </summary>
        /// <param name="controlledChar">The character being controlled by this AI.</param>
        /// <param name="preThink">Whether this is a pre-think evaluation.</param>
        /// <param name="rand">Random number generator for decision-making.</param>
        /// <returns>The escape action, or null if HP is sufficient or cornered.</returns>
        public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
        {
            if (controlledChar.HP * Factor >= controlledChar.MaxHP)
                return null;


            List<Character> seenCharacters = controlledChar.GetSeenCharacters(Alignment.Foe);
            if (seenCharacters.Count == 0)
                return null;

            if (!controlledChar.CantInteract)//TODO: CantInteract doesn't always indicate forced attack, but this'll do for now.
            {
                for (int ii = 0; ii < controlledChar.Skills.Count; ii++)
                {
                    if (!String.IsNullOrEmpty(controlledChar.Skills[ii].Element.SkillNum) && controlledChar.Skills[ii].Element.Charges > 0 && !controlledChar.Skills[ii].Element.Sealed && controlledChar.Skills[ii].Element.Enabled)
                    {
                        if (controlledChar.Skills[ii].Element.SkillNum == "teleport")//Teleport; NOTE: specialized AI code!
                            return new GameAction(GameAction.ActionType.UseSkill, Dir8.None, ii);
                    }
                }
            }

            return base.Think(controlledChar, preThink, rand);
        }
    }
}

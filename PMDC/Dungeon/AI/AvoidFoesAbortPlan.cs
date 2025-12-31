using System;
using RogueElements;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Data;

namespace PMDC.Dungeon
{
    /// <summary>
    /// AI plan that causes the character to flee from enemy characters.
    /// Unlike <see cref="AvoidFoesPlan"/>, this plan will abort and defer to the next plan
    /// when the character is cornered, allowing for a fallback behavior like fighting back.
    /// </summary>
    /// <remarks>
    /// This plan inherits from <see cref="AvoidPlan"/> and provides a flexible avoidance behavior
    /// that respects situational constraints. When the character has no escape routes (cornered),
    /// the plan aborts and allows fallback plans to be executed, such as engaging in combat.
    /// </remarks>
    [Serializable]
    public class AvoidFoesCornerPlan : AvoidPlan
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AvoidFoesCornerPlan"/> class.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        public AvoidFoesCornerPlan(AIFlags iq) : base(iq) { }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        protected AvoidFoesCornerPlan(AvoidFoesCornerPlan other) : base(other) { }

        /// <summary>
        /// Creates a new instance of this plan.
        /// </summary>
        /// <returns>A new <see cref="AvoidFoesCornerPlan"/> instance copied from the current plan.</returns>
        public override BasePlan CreateNew() { return new AvoidFoesCornerPlan(this); }

        /// <summary>
        /// Gets a value indicating whether the character should flee from allied characters.
        /// </summary>
        /// <value>Always <c>false</c>, as this plan only flees from enemies.</value>
        protected override bool RunFromAllies { get { return false; } }

        /// <summary>
        /// Gets a value indicating whether the character should flee from enemy characters.
        /// </summary>
        /// <value>Always <c>true</c>, as this plan is designed to flee from foes.</value>
        protected override bool RunFromFoes { get { return true; } }

        /// <summary>
        /// Gets a value indicating whether this plan should abort if the character is cornered.
        /// </summary>
        /// <value>Always <c>true</c>, allowing fallback to other plans when escape routes are blocked.</value>
        protected override bool AbortIfCornered { get { return true; } }
    }
}
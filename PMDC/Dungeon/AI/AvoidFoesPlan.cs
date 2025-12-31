using System;
using RogueElements;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Data;

namespace PMDC.Dungeon
{
    /// <summary>
    /// AI plan that causes the character to flee from enemy characters only.
    /// When cornered, the character will wait rather than aborting to another plan.
    /// Use <see cref="AvoidFoesCornerPlan"/> if you want the character to fall through
    /// to another plan (like attacking) when cornered.
    /// </summary>
    [Serializable]
    public class AvoidFoesPlan : AvoidPlan
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AvoidFoesPlan"/> class.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        public AvoidFoesPlan(AIFlags iq) : base(iq) { }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        protected AvoidFoesPlan(AvoidFoesPlan other) : base(other) { }

        /// <inheritdoc/>
        public override BasePlan CreateNew() { return new AvoidFoesPlan(this); }

        /// <summary>
        /// Gets a value indicating whether the character should flee from allied characters.
        /// This plan does not flee from allies.
        /// </summary>
        /// <inheritdoc/>
        protected override bool RunFromAllies { get { return false; } }

        /// <summary>
        /// Gets a value indicating whether the character should flee from enemy characters.
        /// This plan flees from foes.
        /// </summary>
        /// <inheritdoc/>
        protected override bool RunFromFoes { get { return true; } }

        /// <summary>
        /// Gets a value indicating whether the character should abort the plan when cornered.
        /// When cornered, this plan will wait rather than falling through to another plan.
        /// </summary>
        /// <inheritdoc/>
        protected override bool AbortIfCornered { get { return false; } }
    }
}

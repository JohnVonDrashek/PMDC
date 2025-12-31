using System;
using RogueElements;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Data;

namespace PMDC.Dungeon
{
    /// <summary>
    /// AI plan that causes the character to flee from allied characters only.
    /// Useful for maintaining distance from teammates, such as spreading out for area coverage
    /// or avoiding friendly fire from splash damage attacks.
    /// When cornered, the character will wait rather than aborting to another plan.
    /// </summary>
    [Serializable]
    public class AvoidAlliesPlan : AvoidPlan
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AvoidAlliesPlan"/> class.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        public AvoidAlliesPlan(AIFlags iq) : base(iq) { }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        protected AvoidAlliesPlan(AvoidAlliesPlan other) : base(other) { }

        /// <inheritdoc/>
        public override BasePlan CreateNew() { return new AvoidAlliesPlan(this); }

        /// <summary>
        /// Gets a value indicating whether the character should flee from allied characters.
        /// </summary>
        /// <value>Always returns <c>true</c> for this plan.</value>
        /// <inheritdoc/>
        protected override bool RunFromAllies { get { return true; } }

        /// <summary>
        /// Gets a value indicating whether the character should flee from enemy characters.
        /// </summary>
        /// <value>Always returns <c>false</c> for this plan; enemies are ignored.</value>
        /// <inheritdoc/>
        protected override bool RunFromFoes { get { return false; } }

        /// <summary>
        /// Gets a value indicating whether the plan should abort if the character is cornered.
        /// </summary>
        /// <value>Always returns <c>false</c>; the character will wait instead of aborting.</value>
        /// <inheritdoc/>
        protected override bool AbortIfCornered { get { return false; } }
    }
}

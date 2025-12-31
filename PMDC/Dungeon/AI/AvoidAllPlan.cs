using System;
using RogueElements;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Data;

namespace PMDC.Dungeon
{
    /// <summary>
    /// AI plan that causes the character to flee from all other characters, both allies and foes.
    /// When cornered, the character will wait rather than aborting to another plan.
    /// Useful for characters that want to avoid all contact.
    /// </summary>
    [Serializable]
    public class AvoidAllPlan : AvoidPlan
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AvoidAllPlan"/> class.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        public AvoidAllPlan(AIFlags iq) : base(iq) { }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        protected AvoidAllPlan(AvoidAllPlan other) : base(other) { }

        /// <inheritdoc/>
        public override BasePlan CreateNew() { return new AvoidAllPlan(this); }

        /// <inheritdoc/>
        /// <remarks>
        /// Always true for AvoidAllPlan - the character will flee from all allies.
        /// </remarks>
        protected override bool RunFromAllies { get { return true; } }

        /// <inheritdoc/>
        /// <remarks>
        /// Always true for AvoidAllPlan - the character will flee from all foes.
        /// </remarks>
        protected override bool RunFromFoes { get { return true; } }

        /// <inheritdoc/>
        /// <remarks>
        /// Always false for AvoidAllPlan - when cornered with no escape route, the character will wait rather than abort to another plan.
        /// </remarks>
        protected override bool AbortIfCornered { get { return false; } }
    }
}

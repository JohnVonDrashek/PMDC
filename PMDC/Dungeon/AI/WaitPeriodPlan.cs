using System;
using RogueElements;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Data;

namespace PMDC.Dungeon
{
    /// <summary>
    /// AI plan that causes the character to wait on specific turn intervals.
    /// The character will skip their turn (Wait action) except on turns divisible by the specified period.
    /// Used for creating slow-acting or periodic behavior patterns.
    /// </summary>
    [Serializable]
    public class WaitPeriodPlan : AIPlan
    {
        /// <summary>
        /// The turn interval. The character acts normally only when MapTurns % Turns == 0.
        /// For example, Turns=2 means the character acts every other turn.
        /// </summary>
        public int Turns;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitPeriodPlan"/> class with default values.
        /// </summary>
        public WaitPeriodPlan() : base()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitPeriodPlan"/> class.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        /// <param name="turns">The turn interval for acting.</param>
        public WaitPeriodPlan(AIFlags iq, int turns) : base(iq)
        {
            Turns = turns;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitPeriodPlan"/> class with full configuration.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        /// <param name="turns">The turn interval for acting.</param>
        /// <param name="attackRange">Minimum range to target before considering attack moves.</param>
        /// <param name="statusRange">Minimum range to target before considering status moves.</param>
        /// <param name="selfStatusRange">Minimum range to target before considering self-targeting status moves.</param>
        /// <param name="restrictedMobilityTypes">Terrain types the AI will not enter.</param>
        /// <param name="restrictMobilityPassable">Whether to restrict movement on passable terrain.</param>
        public WaitPeriodPlan(AIFlags iq, int turns, int attackRange, int statusRange, int selfStatusRange, TerrainData.Mobility restrictedMobilityTypes, bool restrictMobilityPassable) : base(iq, attackRange, statusRange, selfStatusRange, restrictedMobilityTypes, restrictMobilityPassable)
        {
            Turns = turns;
        }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        protected WaitPeriodPlan(WaitPeriodPlan other) : base(other) { Turns = other.Turns; }

        /// <summary>
        /// Creates a copy of this plan instance.
        /// </summary>
        /// <returns>A new WaitPeriodPlan instance with the same configuration.</returns>
        public override BasePlan CreateNew() { return new WaitPeriodPlan(this); }

        /// <summary>
        /// Waits on non-action turns, defers to next plan on action turns.
        /// </summary>
        /// <param name="controlledChar">The character being controlled by this AI.</param>
        /// <param name="preThink">Whether this is a pre-think evaluation.</param>
        /// <param name="rand">Random number generator for decision-making.</param>
        /// <returns>Wait action on non-action turns, null on action turns to defer.</returns>
        public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
        {
            if (ZoneManager.Instance.CurrentMap.MapTurns % Turns == 0)
                return null;
            return new GameAction(GameAction.ActionType.Wait, Dir8.None);
        }
    }
}

using System;
using RogueElements;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Data;

namespace PMDC.Dungeon
{
    /// <summary>
    /// AI plan that causes the character to always wait and do nothing.
    /// The character will skip every turn without moving or attacking.
    /// Used as a fallback behavior or for completely passive entities.
    /// </summary>
    [Serializable]
    public class WaitPlan : AIPlan
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WaitPlan"/> class with default values.
        /// </summary>
        public WaitPlan() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitPlan"/> class.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        public WaitPlan(AIFlags iq) : base(iq) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitPlan"/> class with full configuration.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        /// <param name="attackRange">Minimum range to target before considering attack moves.</param>
        /// <param name="statusRange">Minimum range to target before considering status moves.</param>
        /// <param name="selfStatusRange">Minimum range to target before considering self-targeting status moves.</param>
        /// <param name="restrictedMobilityTypes">Terrain types the AI will not enter.</param>
        /// <param name="restrictMobilityPassable">Whether to restrict movement on passable terrain.</param>
        public WaitPlan(AIFlags iq, int attackRange, int statusRange, int selfStatusRange, TerrainData.Mobility restrictedMobilityTypes, bool restrictMobilityPassable) : base(iq, attackRange, statusRange, selfStatusRange, restrictedMobilityTypes, restrictMobilityPassable) { }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        protected WaitPlan(WaitPlan other) : base(other) { }

        /// <inheritdoc/>
        public override BasePlan CreateNew() { return new WaitPlan(this); }

        /// <summary>
        /// Always returns a Wait action.
        /// </summary>
        /// <param name="controlledChar">The character being controlled by this AI.</param>
        /// <param name="preThink">Whether this is a pre-think evaluation.</param>
        /// <param name="rand">Random number generator for decision-making.</param>
        /// <returns>Always returns a Wait action.</returns>
        public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
        {
            return new GameAction(GameAction.ActionType.Wait, Dir8.None);
        }
    }

    /// <summary>
    /// AI plan that causes the character to wait only when a leader is visible.
    /// If no higher-ranking team member is visible, the plan defers to the next behavior.
    /// Used for followers that should stay put when their leader is nearby.
    /// </summary>
    [Serializable]
    public class WaitWithLeaderPlan : AIPlan
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WaitWithLeaderPlan"/> class with default values.
        /// </summary>
        public WaitWithLeaderPlan() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitWithLeaderPlan"/> class.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        public WaitWithLeaderPlan(AIFlags iq) : base(iq) { }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        protected WaitWithLeaderPlan(WaitWithLeaderPlan other) : base(other) { }

        /// <inheritdoc/>
        public override BasePlan CreateNew() { return new WaitWithLeaderPlan(this); }

        /// <summary>
        /// Waits if a leader is visible, otherwise defers to the next plan.
        /// </summary>
        /// <param name="controlledChar">The character being controlled by this AI.</param>
        /// <param name="preThink">Whether this is a pre-think evaluation.</param>
        /// <param name="rand">Random number generator for decision-making.</param>
        /// <returns>Wait action if leader visible, null otherwise.</returns>
        public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
        {
            //check if there's an ally of higher rank than self visible, wait
            foreach (Character testChar in controlledChar.MemberTeam.IterateByRank())
            {
                //if we have gotten to this character, we could not find a leader
                if (testChar == controlledChar)
                    break;
                else if (controlledChar.IsInSightBounds(testChar.CharLoc))
                {
                    //if we saw our leader, we wait.
                    return new GameAction(GameAction.ActionType.Wait, Dir8.None);
                }
            }
            return null;
        }
    }
}

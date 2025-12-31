using System;
using RogueElements;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Data;

namespace PMDC.Dungeon
{
    /// <summary>
    /// AI plan for boss characters that unlocks all skills when health drops below 50%.
    /// Extends <see cref="AttackFoesPlan"/> with a rage mode that enables disabled skills
    /// when the boss becomes desperate, providing an escalating challenge.
    /// </summary>
    /// <remarks>
    /// This plan monitors the boss's health and automatically enables all disabled skills
    /// when the boss's HP drops to 50% or below. This creates a dynamic difficulty increase
    /// where the boss becomes more dangerous as the battle progresses, reflecting typical
    /// boss behavior where they unlock powerful attacks when threatened.
    /// </remarks>
    [Serializable]
    public class BossPlan : AttackFoesPlan
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BossPlan"/> class with full configuration.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        /// <param name="attackRange">Minimum range to target before considering attack moves.</param>
        /// <param name="statusRange">Minimum range to target before considering status moves.</param>
        /// <param name="selfStatusRange">Minimum range to target before considering self-targeting status moves.</param>
        /// <param name="attackPattern">Strategy for selecting attacks.</param>
        /// <param name="positionPattern">Strategy for positioning relative to targets.</param>
        /// <param name="restrictedMobilityTypes">Terrain types the AI will not enter.</param>
        /// <param name="restrictMobilityPassable">Whether to restrict movement on passable terrain.</param>
        public BossPlan(AIFlags iq, int attackRange, int statusRange, int selfStatusRange, AIPlan.AttackChoice attackPattern, PositionChoice positionPattern, TerrainData.Mobility restrictedMobilityTypes, bool restrictMobilityPassable) : base(iq, attackRange, statusRange, selfStatusRange, attackPattern, positionPattern, restrictedMobilityTypes, restrictMobilityPassable) { }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        protected BossPlan(BossPlan other) : base(other) { }

        /// <inheritdoc/>
        public override BasePlan CreateNew() { return new BossPlan(this); }

        /// <summary>
        /// Evaluates the current situation and determines the best action.
        /// When HP drops below 50%, enables all disabled skills to provide a "rage mode" challenge.
        /// </summary>
        /// <param name="controlledChar">The character being controlled by this AI.</param>
        /// <param name="preThink">Whether this is a pre-think evaluation.</param>
        /// <param name="rand">Random number generator for decision-making.</param>
        /// <returns>The action to take, or null to defer to the next plan.</returns>
        /// <remarks>
        /// This method first checks if the boss's current HP is at or below 50% of maximum.
        /// If so, it iterates through all skills and enables any that have a valid skill number.
        /// After enabling the skills, it delegates to the base <see cref="AttackFoesPlan.Think"/> method
        /// to select and execute the actual attack.
        /// </remarks>
        public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
        {
            if (controlledChar.HP * 2 < controlledChar.MaxHP)
            {
                //at half health, unlock the all moves
                for (int ii = 0; ii < controlledChar.Skills.Count; ii++)
                {
                    if (!String.IsNullOrEmpty(controlledChar.Skills[ii].Element.SkillNum))
                        controlledChar.Skills[ii].Element.Enabled = true;
                }
            }

            return base.Think(controlledChar, preThink, rand);
        }
    }
}

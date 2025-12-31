using System;
using RogueElements;
using System.Collections.Generic;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Data;
using Newtonsoft.Json;
using RogueEssence.Dev;

namespace PMDC.Dungeon
{
    /// <summary>
    /// AI plan that attacks from the current position without moving.
    /// The character will use attacks if enemies are in range, but will not pursue them.
    /// Useful for stationary defenders or turret-like behavior.
    /// </summary>
    [Serializable]
    public class PreparePlan : AIPlan
    {
        /// <summary>
        /// The strategy used to select which attack or skill to use.
        /// </summary>
        public AttackChoice AttackPattern;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreparePlan"/> class with default values.
        /// </summary>
        public PreparePlan() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreparePlan"/> class with full configuration.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        /// <param name="attackRange">Minimum range to target before considering attack moves.</param>
        /// <param name="statusRange">Minimum range to target before considering status moves.</param>
        /// <param name="selfStatusRange">Minimum range to target before considering self-targeting status moves.</param>
        /// <param name="attackPattern">Strategy for selecting attacks.</param>
        /// <param name="restrictedMobilityTypes">Terrain types the AI will not enter.</param>
        /// <param name="restrictMobilityPassable">Whether to restrict movement on passable terrain.</param>
        public PreparePlan(AIFlags iq, int attackRange, int statusRange, int selfStatusRange, AttackChoice attackPattern, TerrainData.Mobility restrictedMobilityTypes, bool restrictMobilityPassable) : base(iq, attackRange, statusRange, selfStatusRange, restrictedMobilityTypes, restrictMobilityPassable)
        {
            AttackPattern = attackPattern;
        }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        public PreparePlan(PreparePlan other) : base(other)
        {
            AttackPattern = other.AttackPattern;
        }

        /// <inheritdoc/>
        public override BasePlan CreateNew() { return new PreparePlan(this); }

        /// <inheritdoc/>
        public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
        {
            bool playerSense = (IQ & AIFlags.PlayerSense) != AIFlags.None;
            Character target = null;
            foreach (Character seenChar in GetAcceptableTargets(controlledChar))
            {
                target = seenChar;
                break;
            }

            //need attack action check
            if (target != null)
            {
                GameAction attackCommand = TryAttackChoice(rand, controlledChar, AttackPattern);
                if (attackCommand.Type != GameAction.ActionType.Wait)
                    return attackCommand;
                attackCommand = TryAttackChoice(rand, controlledChar, AttackChoice.StandardAttack);
                if (attackCommand.Type != GameAction.ActionType.Wait)
                    return attackCommand;
            }

            return null;
        }
    }

    /// <summary>
    /// AI plan that follows the leader but attacks enemies if no movement is needed.
    /// Combines leader-following behavior with opportunistic attacks.
    /// The character will attack enemies when already positioned near the leader.
    /// </summary>
    [Serializable]
    public class PrepareWithLeaderPlan : FollowLeaderPlan
    {
        /// <summary>
        /// The strategy used to select which attack or skill to use.
        /// </summary>
        public AttackChoice AttackPattern;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrepareWithLeaderPlan"/> class with default values.
        /// </summary>
        public PrepareWithLeaderPlan() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrepareWithLeaderPlan"/> class with full configuration.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        /// <param name="attackRange">Minimum range to target before considering attack moves.</param>
        /// <param name="statusRange">Minimum range to target before considering status moves.</param>
        /// <param name="selfStatusRange">Minimum range to target before considering self-targeting status moves.</param>
        /// <param name="attackPattern">Strategy for selecting attacks.</param>
        /// <param name="restrictedMobilityTypes">Terrain types the AI will not enter.</param>
        /// <param name="restrictMobilityPassable">Whether to restrict movement on passable terrain.</param>
        public PrepareWithLeaderPlan(AIFlags iq, int attackRange, int statusRange, int selfStatusRange, AttackChoice attackPattern, TerrainData.Mobility restrictedMobilityTypes, bool restrictMobilityPassable) : base(iq, attackRange, statusRange, selfStatusRange, restrictedMobilityTypes, restrictMobilityPassable)
        {
            AttackPattern = attackPattern;
        }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        public PrepareWithLeaderPlan(PrepareWithLeaderPlan other) : base(other)
        {
            AttackPattern = other.AttackPattern;
        }

        /// <inheritdoc/>
        public override BasePlan CreateNew() { return new PrepareWithLeaderPlan(this); }

        /// <inheritdoc/>
        public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
        {
            GameAction baseAction = base.Think(controlledChar, preThink, rand);

            //behave like followLeader normally
            if (baseAction != null && baseAction.Type != GameAction.ActionType.Wait)
                return baseAction;

            //but if we have no place to walk, try to attack a foe from where we are

            bool playerSense = (IQ & AIFlags.PlayerSense) != AIFlags.None;
            Character target = null;
            foreach (Character seenChar in GetAcceptableTargets(controlledChar))
            {
                target = seenChar;
                break;
            }

            //need attack action check
            if (target != null)
            {
                GameAction attackCommand = TryAttackChoice(rand, controlledChar, AttackPattern);
                if (attackCommand.Type != GameAction.ActionType.Wait)
                    return attackCommand;
                attackCommand = TryAttackChoice(rand, controlledChar, AttackChoice.StandardAttack);
                if (attackCommand.Type != GameAction.ActionType.Wait)
                    return attackCommand;
            }

            return baseAction;
        }

        /// <summary>
        /// Checks if the controlled character is close to the highest ranking member in sight.
        /// </summary>
        /// <param name="controlledChar">The character to check proximity for.</param>
        /// <returns>True if the character is adjacent and can walk to the highest ranking visible team member; otherwise false.</returns>
        private bool closestToHighestLeader(Character controlledChar)
        {
            foreach (Character testChar in controlledChar.MemberTeam.IterateByRank())
            {
                //no leader found?  don't be preparing.
                if (testChar == controlledChar)
                    return false;
                else if (controlledChar.IsInSightBounds(testChar.CharLoc))
                {
                    //only check the first leader that is within sight
                    //leader found; check if nearby
                    if (ZoneManager.Instance.CurrentMap.InRange(testChar.CharLoc, controlledChar.CharLoc, 1))
                    {
                        //check if able to walk there specifically
                        Dir8 dir = DirExt.GetDir(controlledChar.CharLoc, testChar.CharLoc);
                        if (!ZoneManager.Instance.CurrentMap.DirBlocked(dir, controlledChar.CharLoc, controlledChar.Mobility, 1, false, true))
                            return true;
                    }
                    //if any checks fail, return null
                    return false;
                }
            }
            //couldn't find the leader by some way
            return false;
        }

        /// <summary>
        /// Checks if the controlled character is transitively close to the team leader.
        /// Tests if there is a connected path of team members from the controlled character to the leader.
        /// Note: This method may not be used in the current implementation.
        /// </summary>
        /// <param name="controlledChar">The character to check path for.</param>
        /// <returns>True if a transitive path exists to the leader; otherwise false.</returns>
        private bool transitivelyTouchesLeader(Character controlledChar)
        {
            Team team = controlledChar.MemberTeam;

            //requires a valid target tile
            Grid.LocTest checkDiagBlock = (Loc testLoc) => {
                Character nextChar = ZoneManager.Instance.CurrentMap.GetCharAtLoc(testLoc);
                if (nextChar == null)
                    return true;
                if (nextChar.MemberTeam != team)
                    return true;

                //check to make sure you can actually walk this way
                return ZoneManager.Instance.CurrentMap.TileBlocked(testLoc, controlledChar.Mobility, true);
            };

            Grid.LocTest checkBlock = (Loc testLoc) => {

                Character nextChar = ZoneManager.Instance.CurrentMap.GetCharAtLoc(testLoc);
                if (nextChar == null)
                    return true;
                if (nextChar.MemberTeam != team)
                    return true;
                return false;
            };

            Loc mapStart = controlledChar.CharLoc - Character.GetSightDims();
            Loc mapSize = Character.GetSightDims() * 2 + new Loc(1);
            List<Loc> path = Grid.FindPath(mapStart, mapSize, controlledChar.CharLoc, team.Leader.CharLoc, checkBlock, checkDiagBlock);

            return (path[0] == team.Leader.CharLoc);
        }
    }


    /// <summary>
    /// AI plan that uses a status move before engaging, but only once per encounter.
    /// The character will use a status move first, then defer to other plans.
    /// Tracks whether the buff has been used via a status effect.
    /// </summary>
    [Serializable]
    public class PreBuffPlan : AIPlan
    {
        /// <summary>
        /// The status effect ID that indicates the first move has been used.
        /// Once this status is present, the plan defers to the next behavior.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        public string FirstMoveStatus;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreBuffPlan"/> class with default values.
        /// </summary>
        public PreBuffPlan() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreBuffPlan"/> class with full configuration.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        /// <param name="attackRange">Minimum range to target before considering attack moves.</param>
        /// <param name="statusRange">Minimum range to target before considering status moves.</param>
        /// <param name="selfStatusRange">Minimum range to target before considering self-targeting status moves.</param>
        /// <param name="firstMoveStatus">Status effect ID that tracks whether the buff has been used.</param>
        /// <param name="restrictedMobilityTypes">Terrain types the AI will not enter.</param>
        /// <param name="restrictMobilityPassable">Whether to restrict movement on passable terrain.</param>
        public PreBuffPlan(AIFlags iq, int attackRange, int statusRange, int selfStatusRange, string firstMoveStatus, TerrainData.Mobility restrictedMobilityTypes, bool restrictMobilityPassable) : base(iq, attackRange, statusRange, selfStatusRange, restrictedMobilityTypes, restrictMobilityPassable)
        {
            FirstMoveStatus = firstMoveStatus;
        }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        public PreBuffPlan(PreBuffPlan other) : base(other)
        {
            FirstMoveStatus = other.FirstMoveStatus;
        }

        /// <inheritdoc/>
        public override BasePlan CreateNew() { return new PreBuffPlan(this); }

        /// <inheritdoc/>
        public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
        {
            if (controlledChar.GetStatusEffect(FirstMoveStatus) != null)
                return null;

            bool playerSense = (IQ & AIFlags.PlayerSense) != AIFlags.None;
            Character target = null;
            foreach (Character seenChar in GetAcceptableTargets(controlledChar))
            {
                target = seenChar;
                break;
            }

            //need attack action check
            if (target != null)
            {
                GameAction attackCommand = TryAttackChoice(rand, controlledChar, AttackChoice.StatusAttack);
                if (attackCommand.Type != GameAction.ActionType.Wait)
                    return attackCommand;
            }

            return null;
        }
    }




    /// <summary>
    /// AI plan that uses the first skill slot before any other action.
    /// The character will use skill slot 0 once, then defer to other plans.
    /// Tracks whether the skill has been used via a status effect.
    /// </summary>
    [Serializable]
    public class LeadSkillPlan : AIPlan
    {
        /// <summary>
        /// The status effect ID that indicates the first skill has been used.
        /// Once this status is present, the plan defers to the next behavior.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        public string FirstMoveStatus;

        /// <summary>
        /// Initializes a new instance of the <see cref="LeadSkillPlan"/> class with default values.
        /// </summary>
        public LeadSkillPlan() { FirstMoveStatus = ""; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeadSkillPlan"/> class with full configuration.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        /// <param name="attackRange">Minimum range to target before considering attack moves.</param>
        /// <param name="statusRange">Minimum range to target before considering status moves.</param>
        /// <param name="selfStatusRange">Minimum range to target before considering self-targeting status moves.</param>
        /// <param name="firstMoveStatus">Status effect ID that tracks whether the skill has been used.</param>
        /// <param name="restrictedMobilityTypes">Terrain types the AI will not enter.</param>
        /// <param name="restrictMobilityPassable">Whether to restrict movement on passable terrain.</param>
        public LeadSkillPlan(AIFlags iq, int attackRange, int statusRange, int selfStatusRange, string firstMoveStatus, TerrainData.Mobility restrictedMobilityTypes, bool restrictMobilityPassable) : base(iq, attackRange, statusRange, selfStatusRange, restrictedMobilityTypes, restrictMobilityPassable)
        {
            FirstMoveStatus = firstMoveStatus;
        }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        public LeadSkillPlan(LeadSkillPlan other) : base(other)
        {
            FirstMoveStatus = other.FirstMoveStatus;
        }

        /// <inheritdoc/>
        public override BasePlan CreateNew() { return new LeadSkillPlan(this); }

        /// <inheritdoc/>
        public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
        {
            if (controlledChar.GetStatusEffect(FirstMoveStatus) != null)
                return null;

            if (controlledChar.CantInteract)//TODO: CantInteract doesn't always indicate forced attack, but this'll do for now.
                return null;

            //use the first attack
            if (IsSkillUsable(controlledChar, 0))
                return new GameAction(GameAction.ActionType.UseSkill, controlledChar.CharDir, 0);

            return null;
        }
    }
}

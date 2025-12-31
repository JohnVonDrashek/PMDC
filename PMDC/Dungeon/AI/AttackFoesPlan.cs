using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Data;

namespace PMDC.Dungeon
{
    /// <summary>
    /// AI plan that causes the character to seek out and attack hostile targets.
    /// Evaluates available skills, considers positioning strategy, and pathfinds toward enemies.
    /// This is the primary aggressive AI behavior used by most hostile dungeon characters.
    /// </summary>
    [Serializable]
    public class AttackFoesPlan : AIPlan
    {
        /// <summary>
        /// The strategy used to select which attack or skill to use against enemies.
        /// </summary>
        public AttackChoice AttackPattern;

        /// <summary>
        /// The positioning strategy to use when approaching or fighting enemies.
        /// </summary>
        public PositionChoice PositionPattern;
        /// <summary>
        /// The last known location of a target. Tracks the position where an enemy was last seen.
        /// Allows the character to pursue to the last location if the target is no longer visible,
        /// before eventually losing aggro.
        /// </summary>
        [NonSerialized]
        private Loc? targetLoc;

        /// <summary>
        /// History of locations visited during pursuit. Used to maintain pursuit pathfinding
        /// and avoid getting stuck in repetitive movement patterns.
        /// Limited to the last 10 locations to prevent unbounded memory growth.
        /// </summary>
        [NonSerialized]
        public List<Loc> LocHistory;

        /// <summary>
        /// The last character that was directly observed by this AI.
        /// Cached to enable intelligent pursuit behavior after target goes out of sight.
        /// </summary>
        [NonSerialized]
        private Character lastSeenChar;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttackFoesPlan"/> class with default values.
        /// </summary>
        public AttackFoesPlan() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttackFoesPlan"/> class with full configuration.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        /// <param name="attackRange">Minimum range to target before considering attack moves.</param>
        /// <param name="statusRange">Minimum range to target before considering status moves.</param>
        /// <param name="selfStatusRange">Minimum range to target before considering self-targeting status moves.</param>
        /// <param name="attackPattern">Strategy for selecting attacks.</param>
        /// <param name="positionPattern">Strategy for positioning relative to targets.</param>
        /// <param name="restrictedMobilityTypes">Terrain types the AI will not enter.</param>
        /// <param name="restrictMobilityPassable">Whether to restrict movement on passable terrain.</param>
        public AttackFoesPlan(AIFlags iq, int attackRange, int statusRange, int selfStatusRange, AttackChoice attackPattern, PositionChoice positionPattern, TerrainData.Mobility restrictedMobilityTypes, bool restrictMobilityPassable) : base(iq, attackRange, statusRange, selfStatusRange, restrictedMobilityTypes, restrictMobilityPassable)
        {
            AttackPattern = attackPattern;
            PositionPattern = positionPattern;
        }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        protected AttackFoesPlan(AttackFoesPlan other) : base(other)
        {
            AttackPattern = other.AttackPattern;
            PositionPattern = other.PositionPattern;
        }

        /// <inheritdoc/>
        public override BasePlan CreateNew()
        {
            return new AttackFoesPlan(this);
        }

        /// <inheritdoc/>
        public override void SwitchedIn(BasePlan currentPlan)
        {
            lastSeenChar = null;
            LocHistory = new List<Loc>();
            targetLoc = null;
            base.SwitchedIn(currentPlan);
        }

        /// <summary>
        /// Evaluates the current situation and determines the best action to take.
        /// Searches for enemies, calculates optimal positioning, and selects attacks.
        /// </summary>
        /// <param name="controlledChar">The character being controlled by this AI.</param>
        /// <param name="preThink">Whether this is a pre-think evaluation (before the character's actual turn).</param>
        /// <param name="rand">Random number generator for decision-making.</param>
        /// <returns>The action to take, or null to defer to the next plan in the tactic.</returns>
        public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
        {
            if (controlledChar.CantWalk)
            {
                GameAction attack = TryAttackChoice(rand, controlledChar, AttackPattern);
                if (attack.Type != GameAction.ActionType.Wait)
                    return attack;
                return null;
            }


            //past this point, using moves won't work, so try to find a path
            List<Character> seenCharacters = new List<Character>();
            foreach (Character seenChar in GetAcceptableTargets(controlledChar))
                seenCharacters.Add(seenChar);

            Character closestChar = null;
            Loc closestDiff = Loc.Zero;
            for (int ii = 0; ii < seenCharacters.Count; ii++)
            {
                if (closestChar == null)
                {
                    closestChar = seenCharacters[ii];
                    closestDiff = controlledChar.CharLoc - closestChar.CharLoc;
                }
                else
                {
                    Loc newDiff = controlledChar.CharLoc - seenCharacters[ii].CharLoc;
                    if (newDiff.DistSquared() < closestDiff.DistSquared())
                        closestChar = seenCharacters[ii];
                }
            }
            if (closestChar != null)
            {
                lastSeenChar = closestChar;
                targetLoc = closestChar.CharLoc;
            }

            //if we have another move we can make, take this turn to reposition
            int extraTurns = ZoneManager.Instance.CurrentMap.CurrentTurnMap.GetRemainingTurns(controlledChar) - 1;
            if (AttackPattern == AttackChoice.DumbAttack)
                extraTurns = 0;

            if (extraTurns <= 1)
            {
                //attempt to use a move
                GameAction attack = TryAttackChoice(rand, controlledChar, AttackPattern);
                if (attack.Type != GameAction.ActionType.Wait)
                    return attack;
            }


            //path to the closest enemy
            List<Loc> path = null;
            Character targetChar = null;
            Dictionary<Loc, RangeTarget> endHash = new Dictionary<Loc, RangeTarget>();
            Loc[] ends = null;
            bool hasSelfEnd = false;//the controlledChar's destination is included among ends
            bool aimForDistance = false; // determines if we are pathing directly to the target or to a tile we can hit the target from

            PositionChoice positioning = PositionPattern;
            if (extraTurns > 0)
                positioning = PositionChoice.Avoid;

            if (controlledChar.CantInteract)//TODO: CantInteract doesn't always indicate forced attack, but this'll do for now.
                positioning = PositionChoice.Approach;

            bool playerSense = (IQ & AIFlags.PlayerSense) != AIFlags.None;
            if (!playerSense)
            {
                //for dumb NPCs, if they have a status where they can't attack, treat it as a regular attack pattern so that they walk up to the player
                //only cringe does this right now...
                StatusEffect flinchStatus = controlledChar.GetStatusEffect("flinch"); //NOTE: specialized AI code!
                if (flinchStatus != null)
                    positioning = PositionChoice.Approach;
            }

            // If the Positionchoice is Avoid, take attack ranges into consideration
            // the end points should be all locations where one can attack the target
            // for projectiles, it should be the farthest point where they can attack:
            if (positioning != PositionChoice.Approach)
            {
                //get all move ranges and use all their ranges to denote destination tiles.
                FillRangeTargets(controlledChar, seenCharacters, endHash, positioning != PositionChoice.Avoid);
                List<Loc> endList = new List<Loc>();
                foreach (Loc endLoc in endHash.Keys)
                {
                    bool addLoc = false;
                    if (aimForDistance)
                    {
                        if (endHash[endLoc].Weight > 0)
                            addLoc = true;
                    }
                    else
                    {
                        if (endHash[endLoc].Weight > 0)
                        {
                            aimForDistance = true;
                            endList.Clear();
                        }
                        addLoc = true;
                    }
                    if (addLoc)
                    {
                        if (endLoc != controlledChar.CharLoc)//destination cannot be the current location (unless we have turns to spare)
                            endList.Add(endLoc);
                        else
                        {
                            if (extraTurns > 0)
                            {
                                endList.Add(endLoc);
                                hasSelfEnd = true;
                            }
                        }
                    }
                }
                ends = endList.ToArray();
            }
            else
            {
                ends = new Loc[seenCharacters.Count];
                for (int ii = 0; ii < seenCharacters.Count; ii++)
                {
                    endHash[seenCharacters[ii].CharLoc] = new RangeTarget(seenCharacters[ii], 0);
                    ends[ii] = seenCharacters[ii].CharLoc;
                }
            }

            //now actually decide the path to get there
            if (ends.Length > 0)
            {
                List<Loc>[] closestPaths = GetPaths(controlledChar, ends, !aimForDistance, !preThink, hasSelfEnd ? 2 : 1);
                int closestIdx = -1;
                for (int ii = 0; ii < ends.Length; ii++)
                {
                    if (closestPaths[ii] == null)//no path was found
                        continue;
                    if (closestPaths[ii][0] != ends[ii])//an incomplete path was found
                    {
                        if (endHash[ends[ii]].Origin.CharLoc != ends[ii]) // but only for pathing that goes to a tile to hit the target from
                            continue;
                    }

                    if (closestIdx == -1)
                        closestIdx = ii;
                    else
                    {
                        int cmp = comparePathValues(positioning, endHash[ends[ii]], endHash[ends[closestIdx]]);
                        if (cmp > 0)
                            closestIdx = ii;
                        else if (cmp == 0)
                        {
                            // among ties, the tile closest to the target wins
                            int curDiff = (ends[closestIdx] - endHash[ends[closestIdx]].Origin.CharLoc).DistSquared();
                            int newDiff = (ends[ii] - endHash[ends[ii]].Origin.CharLoc).DistSquared();
                            if (newDiff < curDiff)
                                closestIdx = ii;
                        }
                    }
                }

                if (closestIdx > -1)
                {
                    path = closestPaths[closestIdx];
                    targetChar = endHash[ends[closestIdx]].Origin;
                }
            }

            //update last-seen target location if we have a target, otherwise leave it alone
            if (targetChar != null)
            {
                targetLoc = targetChar.CharLoc;
                lastSeenChar = targetChar;
            }
            else if (targetLoc != null) // follow up on a previous targeted loc
            {
                if (preThink)
                {
                    // no currently seen target, check if the target loc is in sight to determine if we should keep last seen char
                    if (!controlledChar.CanSeeLoc(targetLoc.Value, controlledChar.GetCharSight()))
                        lastSeenChar = null;
                    if (lastSeenChar != null)
                        targetLoc = lastSeenChar.CharLoc;
                }
            }

            //update lochistory for potential movement in exploration
            if (LocHistory.Count == 0 || LocHistory[LocHistory.Count - 1] != controlledChar.CharLoc)
                LocHistory.Add(controlledChar.CharLoc);
            if (LocHistory.Count > 10)
                LocHistory.RemoveAt(0);

            if (path != null)
            {
                //pursue the enemy if one is located
                if (path[0] == targetChar.CharLoc)
                    path.RemoveAt(0);

                GameAction attack = null;
                if (path.Count <= 1 || path.Count > 3)//if it takes more than 2 steps to get into position (list includes the loc for start position, for a total of 3), try a local attack
                {
                    if (ZoneManager.Instance.CurrentMap.InRange(targetChar.CharLoc, controlledChar.CharLoc, 1))
                    {
                        attack = TryAttackChoice(rand, controlledChar, AttackPattern, true);
                        if (attack.Type != GameAction.ActionType.Wait)
                            return attack;
                    }
                    attack = TryAttackChoice(rand, controlledChar, AttackChoice.StandardAttack, true);
                    if (attack.Type != GameAction.ActionType.Wait)
                        return attack;
                }
                //move if the destination can be reached
                if (path.Count > 1)
                    return SelectChoiceFromPath(controlledChar, path);
                //lastly, try normal attack
                if (attack == null)
                    attack = TryAttackChoice(rand, controlledChar, AttackChoice.StandardAttack, true);
                if (attack.Type != GameAction.ActionType.Wait)
                    return attack;
                return new GameAction(GameAction.ActionType.Wait, Dir8.None);
            }
            else if (!playerSense && targetLoc.HasValue && targetLoc.Value != controlledChar.CharLoc)
            {
                //if no enemy is located, path to the location of the last seen enemy
                List<Loc>[] paths = GetPaths(controlledChar, new Loc[1] { targetLoc.Value }, false, !preThink);
                path = paths[0];
                if (path.Count > 1)
                    return SelectChoiceFromPath(controlledChar, path);
                else
                    targetLoc = null;
            }

            return null;
        }

        /// <summary>
        /// Compares two positioning options to determine which is more favorable.
        /// The comparison value depends on the current positioning strategy.
        /// For Avoid positioning, higher weight (longer distance) is better.
        /// For Close positioning, lower weight (shorter distance) is better.
        /// </summary>
        /// <param name="positioning">The positioning strategy to use for comparison.</param>
        /// <param name="newVal">The new positioning option being evaluated.</param>
        /// <param name="curBest">The current best positioning option.</param>
        /// <returns>
        /// 1 if newVal is better than curBest, -1 if worse, 0 if equal.
        /// </returns>
        private int comparePathValues(PositionChoice positioning, RangeTarget newVal, RangeTarget curBest)
        {
            if (newVal.Weight == curBest.Weight)
                return 0;

            switch (positioning)
            {
                case PositionChoice.Avoid:
                    if (newVal.Weight > curBest.Weight)
                        return 1;
                    break;
                case PositionChoice.Close:
                    if (newVal.Weight < curBest.Weight)
                        return 1;
                    break;
            }
            return -1;
        }
    }
}

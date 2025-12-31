using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Data;
using Newtonsoft.Json;

namespace PMDC.Dungeon
{
    /// <summary>
    /// AI plan that explores the dungeon only when players are (or are not) visible.
    /// Extends <see cref="ExplorePlan"/> with a visibility condition check.
    /// When the condition is not met, the plan defers to the next behavior in the tactic.
    /// </summary>
    [Serializable]
    public class ExploreIfSeenPlan : ExplorePlan
    {
        /// <summary>
        /// When true, explores only when players are NOT visible.
        /// When false, explores only when players ARE visible.
        /// </summary>
        public bool Negate;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExploreIfSeenPlan"/> class.
        /// </summary>
        /// <param name="negate">If true, explore when players are not visible; if false, explore when they are.</param>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        public ExploreIfSeenPlan(bool negate, AIFlags iq) : base(iq)
        {
            Negate = negate;
        }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        protected ExploreIfSeenPlan(ExploreIfSeenPlan other) : base(other)
        {
            Negate = other.Negate;
        }

        /// <inheritdoc/>
        public override BasePlan CreateNew() { return new ExploreIfSeenPlan(this); }

        /// <inheritdoc/>
        public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
        {
            //specifically check for players
            foreach (Character target in ZoneManager.Instance.CurrentMap.ActiveTeam.Players)
            {
                if (controlledChar.CanSeeCharacter(target, Map.SightRange.Clear) == Negate)
                {
                    //if a threat is in the vicinity (doesn't have to be seen), abort this plan
                    return null;
                }
            }

            return base.Think(controlledChar, preThink, rand);
        }
    }

    /// <summary>
    /// AI plan that causes the character to explore the dungeon by moving toward unexplored exits.
    /// Maintains a location history to avoid backtracking and prefers forward-facing paths.
    /// Used for NPCs that wander or patrol the dungeon.
    /// </summary>
    [Serializable]
    public class ExplorePlan : AIPlan
    {
        /// <summary>
        /// The current path being followed toward the goal destination.
        /// </summary>
        [NonSerialized]
        protected List<Loc> goalPath;

        /// <summary>
        /// History of recently visited locations, used to avoid backtracking during exploration.
        /// </summary>
        [NonSerialized]
        public List<Loc> LocHistory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExplorePlan"/> class with default values.
        /// </summary>
        public ExplorePlan() : base()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExplorePlan"/> class.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        public ExplorePlan(AIFlags iq) : base(iq)
        {
            goalPath = new List<Loc>();
            LocHistory = new List<Loc>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExplorePlan"/> class with full configuration.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        /// <param name="attackRange">Minimum range to target before considering attack moves.</param>
        /// <param name="statusRange">Minimum range to target before considering status moves.</param>
        /// <param name="selfStatusRange">Minimum range to target before considering self-targeting status moves.</param>
        /// <param name="restrictedMobilityTypes">Terrain types the AI will not enter.</param>
        /// <param name="restrictMobilityPassable">Whether to restrict movement on passable terrain.</param>
        public ExplorePlan(AIFlags iq, int attackRange, int statusRange, int selfStatusRange, TerrainData.Mobility restrictedMobilityTypes, bool restrictMobilityPassable) : base(iq, attackRange, statusRange, selfStatusRange, restrictedMobilityTypes, restrictMobilityPassable)
        {
            goalPath = new List<Loc>();
            LocHistory = new List<Loc>();
        }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        protected ExplorePlan(ExplorePlan other) : base(other) { }

        /// <inheritdoc/>
        public override BasePlan CreateNew() { return new ExplorePlan(this); }

        /// <inheritdoc/>
        public override void Initialize(Character controlledChar)
        {
            //create a pathfinding map?
        }

        /// <summary>
        /// Called when this plan is switched in from another plan.
        /// Initializes the path and location history, inheriting backtrack history from AttackFoesPlan if applicable.
        /// </summary>
        /// <param name="currentPlan">The plan being switched from.</param>
        public override void SwitchedIn(BasePlan currentPlan)
        {
            goalPath = new List<Loc>();
            LocHistory = new List<Loc>();
            if (currentPlan is AttackFoesPlan)
                LocHistory.AddRange(((AttackFoesPlan)currentPlan).LocHistory);
            base.SwitchedIn(currentPlan);
        }

        /// <summary>
        /// Evaluates the current situation and determines the best exploration action.
        /// Pathfinds toward unexplored exits while avoiding backtracking.
        /// </summary>
        /// <param name="controlledChar">The character being controlled by this AI.</param>
        /// <param name="preThink">Whether this is a pre-think evaluation.</param>
        /// <param name="rand">Random number generator for decision-making.</param>
        /// <returns>The action to take, or null to defer to the next plan.</returns>
        public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
        {
            if (controlledChar.CantWalk)
                return null;

            //remove all locs from the locHistory that are no longer on screen
            Loc seen = Character.GetSightDims();
            for (int ii = LocHistory.Count - 1; ii >= 0; ii--)
            {
                Loc diff = LocHistory[ii] - controlledChar.CharLoc;
                if (Math.Abs(diff.X) > seen.X || Math.Abs(diff.Y) > seen.Y || ii > 15)
                {
                    LocHistory.RemoveRange(0, ii);
                    break;
                }
            }
            Loc offset = controlledChar.CharLoc - seen;

            //CHECK FOR ADVANCE
            if (goalPath.Count > 1 && ZoneManager.Instance.CurrentMap.WrapLoc(goalPath[goalPath.Count-2]) == controlledChar.CharLoc)//check if we advanced since last time
                goalPath.RemoveAt(goalPath.Count-1);//remove our previous trail

            //check to see if the end loc is still valid... or, just check to see if *the next step* is still valid
            if (goalPath.Count > 1)
            {
                if (controlledChar.CharLoc == ZoneManager.Instance.CurrentMap.WrapLoc(goalPath[goalPath.Count - 1]))//check if on the trail
                {
                    if (!IsPathBlocked(controlledChar, goalPath[goalPath.Count - 2]) && !BlockedByObstacleChar(controlledChar, goalPath[goalPath.Count - 2]))//check to make sure the next step didn't suddely become blocked
                    {
                        //update current traversals
                        if (LocHistory.Count == 0 || LocHistory[LocHistory.Count - 1] != controlledChar.CharLoc)
                            LocHistory.Add(controlledChar.CharLoc);
                        if (!preThink)
                        {
                            Character destChar = ZoneManager.Instance.CurrentMap.GetCharAtLoc(goalPath[goalPath.Count - 2]);
                            // if there's a character there, and they're ordered before us
                            if (!canPassChar(controlledChar, destChar, false))
                                return new GameAction(GameAction.ActionType.Wait, Dir8.None);
                        }
                        GameAction act = TrySelectWalk(controlledChar, ZoneManager.Instance.CurrentMap.GetClosestDir8(goalPath[goalPath.Count - 1], goalPath[goalPath.Count - 2]));
                        //attempt to continue the path
                        //however, we can only verify that we continued on the path on the next loop, using the CHECK FOR ADVANCE block
                        return act;
                    }
                }
            }

            goalPath = new List<Loc>();
            //if it isn't find a new end loc
            List<Loc> seenExits = GetDestinations(controlledChar);

            if (seenExits.Count == 0)
                return null;
            //one element in the acceptable range will be randomly selected to be the one that drives the heuristic

            //later, rate the exits based on how far they are from the tail point of the lochistory
            //add them to a sorted list

            List<Loc> forwardFacingLocs = new List<Loc>();
            if (LocHistory.Count > 0)
            {
                Loc pastLoc = ZoneManager.Instance.CurrentMap.GetClosestUnwrappedLoc(controlledChar.CharLoc, LocHistory[0]);
                Loc pastDir = pastLoc - controlledChar.CharLoc;
                for (int ii = seenExits.Count - 1; ii >= 0; ii--)
                {
                    if (Loc.Dot(pastDir, (seenExits[ii] - controlledChar.CharLoc)) <= 0)
                    {
                        forwardFacingLocs.Add(seenExits[ii]);
                        seenExits.RemoveAt(ii);
                    }
                }
            }

            //if any of the tiles are reached in the search, they will be automatically chosen

            //Analysis:
            //if there is only one exit, and it's easily reached, the speed is the same - #1 fastest case
            //if there are many exits, and they're easily reached, the speed is the same - #1 fastest case
            //if there's one exit, and it's impossible, the speed is the same - #2 fastest case
            //if there's many exits, and they're all impossible, the speed is faster - #2 fastest case
            //if there's many exits, and only the backtrack is possible, the speed is faster - #2 fastest case

            //first attempt the ones that face forward
            if (forwardFacingLocs.Count > 0)
                goalPath = GetRandomPathPermissive(rand, controlledChar, forwardFacingLocs);

            //then attempt remaining locations
            if (goalPath.Count == 0)
                goalPath = GetRandomPathPermissive(rand, controlledChar, seenExits);

            if (goalPath.Count == 0)
                return null;

            if (LocHistory.Count == 0 || LocHistory[LocHistory.Count - 1] != controlledChar.CharLoc)
                LocHistory.Add(controlledChar.CharLoc);

            //TODO: we seldom ever run into other characters who obstruct our path, but if they do, try to wait courteously for them if they are earlier on the team list than us
            //check to make sure we aren't force-warping anyone from their position
            if (!preThink && goalPath.Count > 1)
            {
                Character destChar = ZoneManager.Instance.CurrentMap.GetCharAtLoc(goalPath[goalPath.Count - 2]);
                if (!canPassChar(controlledChar, destChar, false))
                    return new GameAction(GameAction.ActionType.Wait, Dir8.None);
            }
            return SelectChoiceFromPath(controlledChar, goalPath);
        }

        /// <summary>
        /// Gets the list of destination locations to explore.
        /// Override in subclasses to customize exploration targets.
        /// </summary>
        /// <param name="controlledChar">The character being controlled.</param>
        /// <returns>A list of locations to consider as exploration destinations.</returns>
        protected virtual List<Loc> GetDestinations(Character controlledChar)
        {
            return GetAreaExits(controlledChar);
        }
    }
}

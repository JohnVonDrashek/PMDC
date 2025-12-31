using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Data;

namespace PMDC.Dungeon
{
    /// <summary>
    /// AI plan that causes the character to orbit around their team leader within a set range.
    /// The character will move randomly while staying within MAX_RANGE tiles of the leader.
    /// Used for creating bodyguard-like behavior where allies stay close but don't cluster.
    /// </summary>
    [Serializable]
    public class OrbitLeaderPlan : AIPlan
    {
        /// <summary>
        /// Maximum distance in tiles the character will stay from the leader.
        /// </summary>
        const int MAX_RANGE = 3;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrbitLeaderPlan"/> class.
        /// </summary>
        public OrbitLeaderPlan() { }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        protected OrbitLeaderPlan(OrbitLeaderPlan other) : base(other) { }

        /// <summary>
        /// Creates a new instance of this plan as a copy of the current instance.
        /// </summary>
        /// <returns>A new <see cref="OrbitLeaderPlan"/> instance.</returns>
        /// <inheritdoc/>
        public override BasePlan CreateNew() { return new OrbitLeaderPlan(this); }

        /// <summary>
        /// Determines the next action for the controlled character to move randomly while staying within range of the leader.
        /// </summary>
        /// <remarks>
        /// This method finds the first visible team member (the leader) and checks if the character is within MAX_RANGE tiles.
        /// If within range, it selects a random adjacent direction or waits. If the new position would keep the character
        /// within range of the leader and is not blocked by terrain or characters, it attempts to move in that direction.
        /// </remarks>
        /// <param name="controlledChar">The character being controlled by this AI plan.</param>
        /// <param name="preThink">Whether this is a pre-think evaluation pass. When true, friendly characters do not block movement.</param>
        /// <param name="rand">The random number generator used to select random movement directions.</param>
        /// <returns>A <see cref="GameAction"/> representing a move or wait action, or null if no valid leader is visible or no valid moves are available.</returns>
        public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
        {
            if (controlledChar.CantWalk)
                return null;

            Loc targetLoc = new Loc(-1);

            foreach (Character chara in controlledChar.MemberTeam.EnumerateChars())
            {
                if (chara == controlledChar)
                    break;
                else if (controlledChar.IsInSightBounds(chara.CharLoc))
                    targetLoc = chara.CharLoc;
            }

            if (targetLoc == new Loc(-1))
                return null;

            //check to see if the end loc is still valid
            if (ZoneManager.Instance.CurrentMap.InRange(controlledChar.CharLoc, targetLoc, MAX_RANGE))
            {
                List<Dir8> dirs = new List<Dir8>();
                dirs.Add(Dir8.None);
                for (int ii = 0; ii < DirExt.DIR8_COUNT; ii++)
                    dirs.Add((Dir8)ii);
                //walk to random locations
                while (dirs.Count > 0)
                {
                    int randIndex = rand.Next(dirs.Count);
                    if (dirs[randIndex] == Dir8.None)
                        return new GameAction(GameAction.ActionType.Wait, Dir8.None);
                    else
                    {
                        Loc endLoc = controlledChar.CharLoc + ((Dir8)dirs[randIndex]).GetLoc();
                        if (ZoneManager.Instance.CurrentMap.InRange(endLoc, targetLoc, MAX_RANGE))
                        {

                            bool blocked = Grid.IsDirBlocked(controlledChar.CharLoc, (Dir8)dirs[randIndex],
                                (Loc testLoc) =>
                                {
                                    if (IsPathBlocked(controlledChar, testLoc))
                                        return true;

                                    if (!preThink && BlockedByChar(controlledChar, testLoc, Alignment.Friend | Alignment.Foe))
                                        return true;

                                    return false;
                                },
                                (Loc testLoc) =>
                                {
                                    return (ZoneManager.Instance.CurrentMap.TileBlocked(testLoc, controlledChar.Mobility, true));
                                },
                                1);

                            if (!blocked)
                                return TrySelectWalk(controlledChar, (Dir8)dirs[randIndex]);
                        }
                        dirs.RemoveAt(randIndex);
                    }
                }
            }
            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Data;

namespace PMDC.Dungeon
{
    /// <summary>
    /// AI plan that causes the character to wander randomly within a fixed range of their spawn point.
    /// The character will move to random valid directions while staying within MAX_RANGE tiles
    /// of their original position. Used for patrolling guards or territorial creatures.
    /// </summary>
    [Serializable]
    public class StayInRangePlan : AIPlan
    {
        /// <summary>
        /// Maximum distance in tiles the character can wander from their spawn point.
        /// </summary>
        const int MAX_RANGE = 4;

        /// <summary>
        /// The spawn location where the character started. Used as the reference point for range checks.
        /// </summary>
        [NonSerialized]
        private Loc targetLoc;

        /// <summary>
        /// Initializes a new instance of the <see cref="StayInRangePlan"/> class with default values.
        /// </summary>
        public StayInRangePlan() : base()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StayInRangePlan"/> class.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        public StayInRangePlan(AIFlags iq) : base(iq)
        {
            targetLoc = new Loc(-1);
        }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        protected StayInRangePlan(StayInRangePlan other) : base(other) { }

        /// <inheritdoc/>
        public override BasePlan CreateNew() { return new StayInRangePlan(this); }

        /// <summary>
        /// Initializes the plan by recording the character's current location as their spawn/home point.
        /// Subsequent movement decisions will keep the character within MAX_RANGE of this location.
        /// </summary>
        /// <param name="controlledChar">The character being controlled by this AI plan.</param>
        public override void Initialize(Character controlledChar)
        {
            targetLoc = controlledChar.CharLoc;
            base.Initialize(controlledChar);
        }

        /// <summary>
        /// Determines the next action by selecting a random direction to move while staying within MAX_RANGE of the spawn point.
        /// The character will walk to valid adjacent tiles that keep them in range, or wait if no valid moves exist.
        /// Movement is blocked by terrain, characters (during non-preThink evaluation), and out-of-range destinations.
        /// </summary>
        /// <param name="controlledChar">The character being controlled by this AI plan.</param>
        /// <param name="preThink">If true, only terrain blocks movement; if false, ally and enemy characters also block movement.</param>
        /// <param name="rand">Random number generator for selecting which direction to attempt.</param>
        /// <returns>A movement action toward a valid in-range direction, a wait action if no movement is possible, or null if the character cannot walk.</returns>
        public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
        {
            if (controlledChar.CantWalk)
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

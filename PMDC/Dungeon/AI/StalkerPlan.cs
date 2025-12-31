using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Dungeon;
using RogueEssence.Data;
using Newtonsoft.Json;
using RogueEssence.Dev;

namespace PMDC.Dungeon
{
    /// <summary>
    /// AI plan that causes the character to stalk players while staying in dark/covered terrain.
    /// The character remains in light-blocking terrain and follows players within a limited range.
    /// When hit (tracked via status), the character enters "suspicion mode" and pursues more aggressively.
    /// Used for creating ambush predator behavior.
    /// </summary>
    [Serializable]
    public class StalkerPlan : AIPlan
    {
        /// <summary>
        /// The status effect ID that tracks when the character was last attacked.
        /// Used to determine if the character should enter aggressive pursuit mode.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string StatusIndex;

        /// <summary>
        /// How many tiles away this mob can sense targets when stalking in the dark.
        /// Does not respect normal vision limitations.
        /// </summary>
        public int DarkRange;

        /// <summary>
        /// Initializes a new instance of the <see cref="StalkerPlan"/> class.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        /// <param name="status">The status effect ID for tracking when last attacked.</param>
        /// <param name="prescience">The range at which targets can be sensed in the dark.</param>
        public StalkerPlan(AIFlags iq, string status, int prescience) : base(iq)
        {
            StatusIndex = status;
            DarkRange = prescience;
        }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        protected StalkerPlan(StalkerPlan other) : base(other)
        {
            StatusIndex = other.StatusIndex;
            DarkRange = other.DarkRange;
        }

        /// <inheritdoc/>
        public override BasePlan CreateNew() { return new StalkerPlan(this); }

        /// <summary>
        /// Stalks targets while staying in dark terrain. Becomes aggressive when attacked.
        /// </summary>
        /// <param name="controlledChar">The character being controlled by this AI.</param>
        /// <param name="preThink">Whether this is a pre-think evaluation.</param>
        /// <param name="rand">Random number generator for decision-making.</param>
        /// <returns>The movement action toward targets, or Wait if already in position.</returns>
        public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
        {
            if (controlledChar.CantWalk)
                return null;

            bool suspicion = false;
            //check for being hit already.  if hit, and it's been less than 10 turns ago, flip on the suspicion flag
            StatusEffect lastHit = controlledChar.GetStatusEffect(StatusIndex);
            if (lastHit != null)
            {
                if (lastHit.StatusStates.Get<CountDownState>().Counter < 10)
                    suspicion = true;
            }

            //check for being already in the light.  if in the light, skip this plan
            if (!suspicion)
            {
                for (int xx = -1; xx <= 1; xx++)
                {
                    for (int yy = -1; yy <= 1; yy++)
                    {
                        // must stay in the dark
                        if (!TileBlocksLight(controlledChar.CharLoc + new Loc(xx, yy)))
                            return null;
                    }
                }
            }

            Character targetChar = null;
            int minRange = DarkRange + 1;
            if (suspicion)
                minRange = 80;
            foreach (Character testChar in ZoneManager.Instance.CurrentMap.ActiveTeam.IterateByRank())
            {
                int testDist = ZoneManager.Instance.CurrentMap.GetClosestDist8(testChar.CharLoc, controlledChar.CharLoc);
                if (testDist < minRange)
                {
                    targetChar = testChar;
                    minRange = testDist;
                }
            }

            //gravitate to the CLOSEST target.
            //iterate in increasing character indices
            GameAction result = null;
            if (targetChar != null)
            {
                //get the direction to that character
                Dir8 dirToChar = ZoneManager.Instance.CurrentMap.GetClosestDir8(controlledChar.CharLoc, targetChar.CharLoc);

                //is it possible to move in that direction?
                //if so, use it
                result = tryDir(controlledChar, targetChar, dirToChar, suspicion, !preThink);
                if (result != null)
                    return result;
                if (dirToChar.IsDiagonal())
                {
                    Loc diff = controlledChar.CharLoc - targetChar.CharLoc;
                    DirH horiz;
                    DirV vert;
                    dirToChar.Separate(out horiz, out vert);
                    //start with the one that covers the most distance
                    if (Math.Abs(diff.X) < Math.Abs(diff.Y))
                    {
                        result = tryDir(controlledChar, targetChar, vert.ToDir8(), suspicion, !preThink);
                        if (result != null)
                            return result;
                        result = tryDir(controlledChar, targetChar, horiz.ToDir8(), suspicion, !preThink);
                        if (result != null)
                            return result;
                    }
                    else
                    {
                        result = tryDir(controlledChar, targetChar, horiz.ToDir8(), suspicion, !preThink);
                        if (result != null)
                            return result;
                        result = tryDir(controlledChar, targetChar, vert.ToDir8(), suspicion, !preThink);
                        if (result != null)
                            return result;
                    }
                }
                else
                {
                    result = tryDir(controlledChar, targetChar, DirExt.AddAngles(dirToChar, Dir8.DownLeft), suspicion, !preThink);
                    if (result != null)
                        return result;
                    result = tryDir(controlledChar, targetChar, DirExt.AddAngles(dirToChar, Dir8.DownRight), suspicion, !preThink);
                    if (result != null)
                        return result;
                }
            }

            //if a path can't be found to anyone, just wait and stalk
            return new GameAction(GameAction.ActionType.Wait, Dir8.None);
        }

        /// <summary>
        /// Checks if a tile at the specified location blocks light.
        /// </summary>
        /// <param name="testLoc">The location to test for light-blocking terrain.</param>
        /// <returns>True if the tile blocks light or if the location is out of bounds; false otherwise.</returns>
        private bool TileBlocksLight(Loc testLoc)
        {
            Tile tile = ZoneManager.Instance.CurrentMap.GetTile(testLoc);
            if (tile == null)
                return true;

            TerrainData terrain = (TerrainData)tile.Data.GetData();
            if (terrain.BlockLight)
                return true;

            return false;
        }

        /// <summary>
        /// Attempts to move in a given direction toward the target character.
        /// Validates that the move is legal (not blocked) and maintains stalking behavior constraints.
        /// </summary>
        /// <param name="controlledChar">The character being controlled by this AI.</param>
        /// <param name="targetChar">The target character to move toward.</param>
        /// <param name="testDir">The direction to attempt movement in.</param>
        /// <param name="suspicion">If false, the character must stay in dark terrain; if true, relaxes this constraint.</param>
        /// <param name="respectPeers">If true, prevents moving through allied characters; if false, allows passing through allies.</param>
        /// <returns>A GameAction representing the move if valid, or null if the move is blocked or invalid.</returns>
        private GameAction tryDir(Character controlledChar, Character targetChar, Dir8 testDir, bool suspicion, bool respectPeers)
        {
            Loc endLoc = controlledChar.CharLoc + testDir.GetLoc();


            if (!suspicion)
            {
                //do not go even one tile near light-exposed tiles
                for (int xx = -1; xx <= 1; xx++)
                {
                    for (int yy = -1; yy <= 1; yy++)
                    {
                        // must stay in the dark
                        if (!TileBlocksLight(endLoc + new Loc(xx, yy)))
                            return null;
                    }
                }
            }

            //check to see if it's possible to move in this direction
            bool blocked = Grid.IsDirBlocked(controlledChar.CharLoc, testDir,
                (Loc testLoc) =>
                {
                    if (IsPathBlocked(controlledChar, testLoc))
                        return true;

                    if (ZoneManager.Instance.CurrentMap.WrapLoc(testLoc) != targetChar.CharLoc && respectPeers)
                    {
                        Character destChar = ZoneManager.Instance.CurrentMap.GetCharAtLoc(testLoc);
                        if (!canPassChar(controlledChar, destChar, true))
                            return true;
                    }

                    return false;
                },
                (Loc testLoc) =>
                {
                    return (ZoneManager.Instance.CurrentMap.TileBlocked(testLoc, controlledChar.Mobility, true));
                },
                1);

            //if that direction is good, send the command to move in that direction
            if (blocked)
                return null;

            //check to see if moving in this direction will get to the target char
            if (ZoneManager.Instance.CurrentMap.WrapLoc(endLoc) == targetChar.CharLoc)
                return new GameAction(GameAction.ActionType.Wait, Dir8.None);

            return TrySelectWalk(controlledChar, testDir);
        }
    }
}

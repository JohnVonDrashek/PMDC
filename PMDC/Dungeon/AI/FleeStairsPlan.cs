using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Data;
using RogueEssence.Dev;

namespace PMDC.Dungeon
{
    /// <summary>
    /// AI plan that causes the character to flee toward stairs when HP is low.
    /// The character will pathfind to the nearest visible stair tile and wait there.
    /// Used for enemies that try to escape to the next floor when injured.
    /// </summary>
    [Serializable]
    public class FleeStairsPlan : AIPlan
    {
        /// <summary>
        /// The set of tile IDs that are considered valid stair destinations.
        /// </summary>
        [DataType(0, DataManager.DataType.Tile, false)]
        public HashSet<string> StairIds;

        /// <summary>
        /// The HP threshold factor. The plan activates when HP * Factor &lt; MaxHP.
        /// A factor of 2 means activation at 50% HP, factor of 4 means 25% HP, etc.
        /// </summary>
        public int Factor;

        /// <summary>
        /// Whether the character can sense stairs anywhere on the map, not just within sight range.
        /// </summary>
        public bool Omniscient;

        /// <summary>
        /// Initializes a new instance of the <see cref="FleeStairsPlan"/> class.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        /// <param name="destLocations">The set of stair tile IDs to flee toward.</param>
        /// <param name="omniscient">Whether to sense stairs anywhere on the map.</param>
        /// <param name="factor">HP threshold factor for activation.</param>
        public FleeStairsPlan(AIFlags iq, HashSet<string> destLocations, bool omniscient = false, int factor = 1) : base(iq)
        {
            StairIds = destLocations;
            Omniscient = omniscient;
            Factor = factor;
        }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        protected FleeStairsPlan(FleeStairsPlan other) : base(other)
        {
            StairIds = other.StairIds;
            Omniscient = other.Omniscient;
            Factor = other.Factor;
        }

        /// <inheritdoc/>
        public override BasePlan CreateNew()
        {
            return new FleeStairsPlan(this);
        }

        /// <summary>
        /// Evaluates whether to flee to stairs based on current HP.
        /// </summary>
        /// <param name="controlledChar">The character being controlled by this AI.</param>
        /// <param name="preThink">Whether this is a pre-think evaluation.</param>
        /// <param name="rand">Random number generator for decision-making.</param>
        /// <returns>The action to take, or null if HP is sufficient or no stairs found.</returns>
        public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
        {
            if (controlledChar.HP * Factor >= controlledChar.MaxHP)
                return null;

            if (controlledChar.CantWalk)
                return null;

            Map map = ZoneManager.Instance.CurrentMap;

            Loc seen = Character.GetSightDims();

            Rect sightBounds = new Rect(Loc.Zero, controlledChar.MemberTeam.ContainingMap.Size);
            if (!Omniscient)
            {
                Rect.FromPoints(controlledChar.CharLoc - seen, controlledChar.CharLoc + seen + Loc.One);
                sightBounds = controlledChar.MemberTeam.ContainingMap.GetClampedSight(sightBounds);
            }
            
            // Get all the visible stairs within vision
            List<Loc> stairLocs = new List<Loc>();  
            for (int xx = sightBounds.X; xx < sightBounds.End.X; xx++)
            {
                for (int yy = sightBounds.Y; yy < sightBounds.End.Y; yy++) { 
                    
                    Loc loc = new Loc(xx, yy);
                    
                    Tile tile = map.GetTile(loc);
                    if (tile != null && tile.Effect.Revealed && StairIds.Contains(tile.Effect.ID) && 
                        (Omniscient || controlledChar.CanSeeLoc(loc, controlledChar.GetCharSight())))
                    {
                        //do nothing if positioned at the stairs
                        if (loc == controlledChar.CharLoc)
                        {
                            return new GameAction(GameAction.ActionType.Wait, Dir8.None);
                        }
                        stairLocs.Add(loc);   
                    };
                }
            }

            if (stairLocs.Count > 0)
            {
                List<Loc> path = GetEscapePath(controlledChar, stairLocs.ToArray());
                if (path.Count > 1)
                    return SelectChoiceFromPath(controlledChar, path);
            }

            return null;
        }

        /// <summary>
        /// Calculates the escape path to the nearest stair location.
        /// </summary>
        /// <param name="controlledChar">The character being controlled.</param>
        /// <param name="ends">Array of stair locations to pathfind toward.</param>
        /// <returns>The path to the nearest stair, or an empty path if none reachable.</returns>
        protected List<Loc> GetEscapePath(Character controlledChar, Loc[] ends)
        {
            Loc[] wrappedEnds = getWrappedEnds(controlledChar.CharLoc, ends);

            //requires a valid target tile
            Grid.LocTest checkDiagBlock = (Loc loc) => {
                return (ZoneManager.Instance.CurrentMap.TileBlocked(loc, controlledChar.Mobility, true));
                //enemy/ally blockings don't matter for diagonals
            };

            Grid.LocTest checkBlock = (Loc testLoc) => {

                if (IsPathBlocked(controlledChar, testLoc))
                    return true;

                if (BlockedByChar(controlledChar, testLoc, Alignment.Foe))
                    return true;

                return false;
            };

            Rect sightBounds = new Rect(Loc.Zero, controlledChar.MemberTeam.ContainingMap.Size);
            if (!Omniscient)
                sightBounds = new Rect(controlledChar.CharLoc - Character.GetSightDims(), Character.GetSightDims() * 2 + new Loc(1));
            List<Loc>[] paths = Grid.FindNPaths(sightBounds.Start, sightBounds.Size, controlledChar.CharLoc, wrappedEnds, checkBlock, checkDiagBlock, 1, false);
            return paths[0];
        }
    }
}

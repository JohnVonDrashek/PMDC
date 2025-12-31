using System;
using RogueElements;
using RogueEssence.Dungeon;
using RogueEssence;
using RogueEssence.LevelGen;
using PMDC.Dungeon;
using System.Collections.Generic;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Base class for generation steps that seal rooms with barriers.
    /// Provides common functionality for surrounding filtered rooms with walls and special tiles.
    /// Identifies rooms matching filter criteria and surrounds them with sealed borders, creating vault-like structures.
    /// </summary>
    /// <typeparam name="T">The map generation context type, must implement ListMapGenContext.</typeparam>
    [Serializable]
    public abstract class BaseSealStep<T> : GenStep<T> where T : ListMapGenContext
    {
        /// <summary>
        /// Defines the types of seals that can be applied to border tiles around sealed rooms.
        /// </summary>
        protected enum SealType
        {
            /// <summary>Tile should be made impassable, blocking movement.</summary>
            Blocked,
            /// <summary>Tile should be locked with a special barrier that requires a key.</summary>
            Locked,
            /// <summary>Tile should serve as the key/entrance point to bypass the seal.</summary>
            Key
        }

        /// <summary>
        /// Initializes a new instance of the BaseSealStep class with an empty filter list.
        /// </summary>
        public BaseSealStep()
        {
            this.Filters = new List<BaseRoomFilter>();
        }

        /// <summary>
        /// Gets or sets the list of room filters that determine which rooms should be sealed.
        /// Only rooms passing all filters will have barrier tiles placed around them.
        /// </summary>
        public List<BaseRoomFilter> Filters { get; set; }

        /// <summary>
        /// When overridden in a derived class, places the actual barrier tiles based on the calculated seal types.
        /// </summary>
        /// <param name="map">The map generation context containing room and tile information.</param>
        /// <param name="sealList">Dictionary mapping tile locations to their seal types (Blocked, Locked, or Key).</param>
        protected abstract void PlaceBorders(T map, Dictionary<Loc, SealType> sealList);

        /// <inheritdoc/>
        public override void Apply(T map)
        {
            //Iterate every room/hall and coat the ones filtered
            List<RoomHallIndex> spawningRooms = new List<RoomHallIndex>();
            Dictionary<Loc, SealType> sealList = new Dictionary<Loc, SealType>();

            for (int ii = 0; ii < map.RoomPlan.RoomCount; ii++)
            {
                if (!BaseRoomFilter.PassesAllFilters(map.RoomPlan.GetRoomPlan(ii), this.Filters))
                    continue;
                spawningRooms.Add(new RoomHallIndex(ii, false));
            }

            for (int ii = 0; ii < map.RoomPlan.HallCount; ii++)
            {
                if (!BaseRoomFilter.PassesAllFilters(map.RoomPlan.GetHallPlan(ii), this.Filters))
                    continue;
                spawningRooms.Add(new RoomHallIndex(ii, true));
            }

            if (spawningRooms.Count == 0)
                return;

            for (int ii = 0; ii < spawningRooms.Count; ii++)
            {
                IFloorRoomPlan plan = map.RoomPlan.GetRoomHall(spawningRooms[ii]);

                //seal the sides and note edge cases
                for (int xx = plan.RoomGen.Draw.X+1; xx < plan.RoomGen.Draw.End.X-1; xx++)
                {
                    sealBorderRay(map, sealList, plan, new LocRay8(xx, plan.RoomGen.Draw.Y, Dir8.Up), Dir8.Left, Dir8.Right);
                    sealBorderRay(map, sealList, plan, new LocRay8(xx, plan.RoomGen.Draw.End.Y-1, Dir8.Down), Dir8.Left, Dir8.Right);
                }

                for (int yy = plan.RoomGen.Draw.Y+1; yy < plan.RoomGen.Draw.End.Y-1; yy++)
                {
                    sealBorderRay(map, sealList, plan, new LocRay8(plan.RoomGen.Draw.X, yy, Dir8.Left), Dir8.Up, Dir8.Down);
                    sealBorderRay(map, sealList, plan, new LocRay8(plan.RoomGen.Draw.End.X-1, yy, Dir8.Right), Dir8.Up, Dir8.Down);
                }

                //seal edge cases
                sealCornerRay(map, sealList, plan, new LocRay8(plan.RoomGen.Draw.X, plan.RoomGen.Draw.Y, Dir8.UpLeft));
                sealCornerRay(map, sealList, plan, new LocRay8(plan.RoomGen.Draw.End.X - 1, plan.RoomGen.Draw.Y, Dir8.UpRight));
                sealCornerRay(map, sealList, plan, new LocRay8(plan.RoomGen.Draw.X, plan.RoomGen.Draw.End.Y-1, Dir8.DownLeft));
                sealCornerRay(map, sealList, plan, new LocRay8(plan.RoomGen.Draw.End.X - 1, plan.RoomGen.Draw.End.Y - 1, Dir8.DownRight));


                for (int xx = plan.RoomGen.Draw.X; xx < plan.RoomGen.Draw.End.X; xx++)
                {
                    for (int yy = plan.RoomGen.Draw.Y; yy < plan.RoomGen.Draw.End.Y; yy++)
                        map.GetPostProc(new Loc(xx, yy)).Status |= PostProcType.Terrain;
                }

            }

            PlaceBorders(map, sealList);
        }

        /// <summary>
        /// Analyzes a border tile and determines whether it should be sealed based on adjacent room filter conditions.
        /// Categorizes tiles as Blocked, Locked, or Key depending on whether adjacent rooms pass the seal filters.
        /// </summary>
        /// <param name="map">The map generation context containing tile and room information.</param>
        /// <param name="sealList">Dictionary to accumulate the seal types for border tiles.</param>
        /// <param name="plan">The floor room plan containing adjacency information.</param>
        /// <param name="locRay">A ray from the room border outward in a specific direction.</param>
        /// <param name="side1">The first perpendicular direction to check for additional sealing.</param>
        /// <param name="side2">The second perpendicular direction to check for additional sealing.</param>
        /// <returns>True if the tile outward from the room should remain accessible; false if sealed inward.</returns>
        private bool sealBorderRay(T map, Dictionary<Loc, SealType> sealList, IFloorRoomPlan plan, LocRay8 locRay, Dir8 side1, Dir8 side2)
        {
            Loc forthLoc = locRay.Loc + locRay.Dir.GetLoc();

            bool hasAdjacent = false;
            bool hasCondition = false;
            for (int ii = 0; ii < plan.Adjacents.Count; ii++)
            {
                IFloorRoomPlan adjacentPlan = map.RoomPlan.GetRoomHall(plan.Adjacents[ii]);
                if (map.RoomPlan.InBounds(adjacentPlan.RoomGen.Draw, forthLoc))
                {
                    hasAdjacent = true;
                    if (BaseRoomFilter.PassesAllFilters(adjacentPlan, this.Filters))
                    {
                        hasCondition = true;
                        break;
                    }
                }
            }

            if (!hasAdjacent)
            {
                //in the case where the extending tile is within no adjacents
                //  all normal walls shall be turned into impassables
                //  everything else is saved into the lock list
                sealBorderTile(map, sealList, SealType.Locked, forthLoc);

                return true;
            }
            else if (!hasCondition)
            {
                //in the case where the extending tile is within an adjacent and that adjacent DOESNT pass filter
                //  all normal walls for the INWARD border shall be turned into impassables
                //  everything else for the INWARD border shall be saved into a key list

                if (!map.TileBlocked(forthLoc))
                    sealBorderTile(map, sealList, SealType.Key, locRay.Loc);
                else
                    sealBorderTile(map, sealList, SealType.Locked, locRay.Loc);

                //when transitioning between inward and outward
                //-when transitioning from outward to inward, the previous outward tile needs an inward check
                //-when transitioning from inward to outward, the current outward tile needs a inward check

                //in the interest of trading redundancy for simplicity, an inward block will just block the tiles to the sides
                //regardless of if they've already been blocked
                //redundancy will be handled by hashsets
                if (side1 != Dir8.None)
                {
                    Loc sideLoc = locRay.Loc + side1.GetLoc();
                    sealBorderTile(map, sealList, SealType.Locked, sideLoc);
                }
                if (side2 != Dir8.None)
                {
                    Loc sideLoc = locRay.Loc + side2.GetLoc();
                    sealBorderTile(map, sealList, SealType.Locked, sideLoc);
                }
                return false;
            }
            else
            {
                //in the case where the extending tile is within an adjacent and that adjacent passes filter
                //  do nothing and skip these tiles
                return true;
            }
        }


        /// <summary>
        /// Seals a corner tile of a room by analyzing both horizontal and vertical directions.
        /// Ensures consistent sealing across diagonal room corners.
        /// </summary>
        /// <param name="map">The map generation context containing tile and room information.</param>
        /// <param name="sealList">Dictionary to accumulate the seal types for border tiles.</param>
        /// <param name="plan">The floor room plan containing adjacency information.</param>
        /// <param name="locRay">A ray from the corner of the room pointing diagonally outward.</param>
        private void sealCornerRay(T map, Dictionary<Loc, SealType> sealList, IFloorRoomPlan plan, LocRay8 locRay)
        {
            DirH dirH;
            DirV dirV;
            locRay.Dir.Separate(out dirH, out dirV);

            bool outwardsH = sealBorderRay(map, sealList, plan, new LocRay8(locRay.Loc, dirH.ToDir8()), dirV.ToDir8().Reverse(), Dir8.None);
            bool outwardsV = sealBorderRay(map, sealList, plan, new LocRay8(locRay.Loc, dirV.ToDir8()), dirH.ToDir8().Reverse(), Dir8.None);


            //when two directions of a corner tile face inward, or outward, or a combination of inward and outward
            //-both inward: needs to not be redundant across the two sides - handled by hashset, no action needed
            //-one inward and one outward: can coexist - no action needed
            //-both outward: needs to check the outward diagonal to see if it forces inward
            // -if it doesnt force inward, do an outward operation
            // -if it does, do an inward operation

            if (outwardsH && outwardsV)
                sealBorderRay(map, sealList, plan, locRay, Dir8.None, Dir8.None);
        }

        /// <summary>
        /// Registers or updates a border tile's seal type in the seal list.
        /// If the tile is already blocked, it remains Blocked. Otherwise, higher-priority seals override lower ones.
        /// </summary>
        /// <param name="map">The map generation context for checking if tiles are already blocked.</param>
        /// <param name="sealList">Dictionary to store the seal type for this tile location.</param>
        /// <param name="seal">The seal type to apply (Blocked, Locked, or Key).</param>
        /// <param name="loc">The location of the tile to seal.</param>
        private void sealBorderTile(T map, Dictionary<Loc, SealType> sealList, SealType seal, Loc loc)
        {
            if (map.TileBlocked(loc))
                sealList[loc] = SealType.Blocked;
            else
            {
                SealType curSeal;
                if (sealList.TryGetValue(loc, out curSeal))
                {
                    if (curSeal < seal)
                        sealList[loc] = seal;
                }
                else
                    sealList[loc] = seal;
            }
        }
    }
}

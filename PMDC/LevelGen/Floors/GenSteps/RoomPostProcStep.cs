using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Data;
using RogueEssence.Dungeon;
using RogueEssence.LevelGen;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Applies post-processing flags to tiles in selected rooms.
    /// This step marks tiles with PostProcType flags that affect later generation steps.
    /// </summary>
    /// <typeparam name="T">The map generation context type.</typeparam>
    [Serializable]
    public class RoomPostProcStep<T> : GenStep<T> where T : ListMapGenContext
    {
        /// <summary>
        /// The number of rooms to apply post-processing to.
        /// A random value is picked from this range for each generation.
        /// </summary>
        public RandRange Amount;

        /// <summary>
        /// Filters to determine which rooms are eligible for post-processing.
        /// All filters must be passed for a room to be considered.
        /// </summary>
        public List<BaseRoomFilter> Filters { get; set; }

        /// <summary>
        /// The post-processing flags to apply to eligible tiles.
        /// These flags affect how tiles are treated in later generation steps.
        /// </summary>
        public PostProcType PostProc { get; set; }

        /// <summary>
        /// Determines which tiles within selected rooms are eligible to receive post-processing flags.
        /// Only tiles that pass the stencil test will be marked.
        /// </summary>
        public ITerrainStencil<T> TerrainStencil { get; set; }

        /// <summary>
        /// Whether hall regions are eligible for post-processing.
        /// </summary>
        public bool IncludeHalls { get; set; }

        /// <summary>
        /// Whether room regions are eligible for post-processing.
        /// </summary>
        public bool IncludeRooms { get; set; }

        /// <summary>
        /// Initializes a new instance with default values.
        /// Filters list is empty and terrain stencil is set to the default implementation.
        /// </summary>
        public RoomPostProcStep()
        {
            this.Filters = new List<BaseRoomFilter>();
            this.TerrainStencil = new DefaultTerrainStencil<T>();
        }

        /// <summary>
        /// Initializes a new instance with the specified post-processing settings.
        /// </summary>
        /// <param name="postProc">The post-processing flags to apply to selected room tiles.</param>
        /// <param name="amount">The number of rooms to select and process.</param>
        /// <param name="includeRooms">Whether to include room regions in the selection.</param>
        /// <param name="includeHalls">Whether to include hall regions in the selection.</param>
        public RoomPostProcStep(PostProcType postProc, RandRange amount, bool includeRooms, bool includeHalls) : this()
        {
            PostProc = postProc;
            Amount = amount;
            IncludeRooms = includeRooms;
            IncludeHalls = includeHalls;
        }

        /// <summary>
        /// Applies post-processing flags to tiles in a randomly selected set of rooms.
        ///
        /// This method:
        /// 1. Determines how many rooms to process based on the Amount range
        /// 2. Collects eligible rooms and halls based on filters and inclusion settings
        /// 3. Randomly selects the specified number of rooms
        /// 4. Marks tiles in those rooms with the PostProc flags (limited to tiles passing the terrain stencil)
        /// </summary>
        /// <param name="map">The map generation context containing room plans and post-processing data.</param>
        /// <inheritdoc/>
        public override void Apply(T map)
        {
            int chosenAmount = Amount.Pick(map.Rand);
            if (chosenAmount == 0)
                return;

            List<RoomHallIndex> openRooms = new List<RoomHallIndex>();
            if (this.IncludeRooms)
            {
                for (int ii = 0; ii < map.RoomPlan.RoomCount; ii++)
                {
                    if (BaseRoomFilter.PassesAllFilters(map.RoomPlan.GetRoomPlan(ii), this.Filters))
                        openRooms.Add(new RoomHallIndex(ii, false));
                }
            }

            if (this.IncludeHalls)
            {
                for (int ii = 0; ii < map.RoomPlan.HallCount; ii++)
                {
                    if (!BaseRoomFilter.PassesAllFilters(map.RoomPlan.GetHallPlan(ii), this.Filters))
                        continue;
                    openRooms.Add(new RoomHallIndex(ii, true));
                }
            }

            for (int ii = 0; ii < chosenAmount; ii++)
            {
                if (openRooms.Count > 0)
                {
                    int randIndex = map.Rand.Next(openRooms.Count);
                    IFloorRoomPlan plan;
                    if (openRooms[randIndex].IsHall)
                        plan = map.RoomPlan.GetHallPlan(openRooms[randIndex].Index);
                    else
                        plan = map.RoomPlan.GetRoomPlan(openRooms[randIndex].Index);


                    for (int xx = plan.RoomGen.Draw.X - 1; xx < plan.RoomGen.Draw.End.X + 1; xx++)
                    {
                        for (int yy = plan.RoomGen.Draw.Y - 1; yy < plan.RoomGen.Draw.End.Y + 1; yy++)
                        {
                            Loc destLoc = new Loc(xx, yy);
                            if (this.TerrainStencil.Test(map, destLoc))
                            {
                                PostProcTile tile = map.GetPostProc(destLoc);
                                tile.Status |= PostProc;
                            }
                        }
                    }

                    openRooms.RemoveAt(randIndex);
                }
            }
            
        }

    }

}

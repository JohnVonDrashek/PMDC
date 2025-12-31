using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Data;
using RogueEssence.Dungeon;

namespace PMDC.LevelGen
{
    /// <summary>
    /// A generation step that paints terrain in a number of randomly selected rooms or halls with the specified tile.
    /// This step replaces eligible tiles within room/hall bounds with the configured terrain, subject to filter and stencil constraints.
    /// </summary>
    /// <remarks>
    /// The step operates by:
    /// 1. Collecting eligible rooms/halls that pass all configured filters
    /// 2. Randomly selecting a subset of those eligible candidates
    /// 3. For each selected room/hall, iterating through its perimeter and interior tiles
    /// 4. Using the terrain stencil to determine which tiles are actually replaceable
    /// 5. Setting replaceable tiles to the configured terrain
    /// </remarks>
    /// <typeparam name="T">The map generation context type, must implement <see cref="IFloorPlanGenContext"/>.</typeparam>
    [Serializable]
    public class RoomTerrainStep<T> : GenStep<T> where T : class, IFloorPlanGenContext
    {
        /// <summary>
        /// Gets or sets the range that determines how many rooms or halls to paint with terrain.
        /// The actual count is randomly selected from this range each time the step is applied.
        /// </summary>
        public RandRange Amount;

        /// <summary>
        /// Gets or sets the list of filters that determine which rooms and halls are eligible for terrain painting.
        /// Only rooms/halls that pass all filters are considered for selection.
        /// </summary>
        public List<BaseRoomFilter> Filters { get; set; }

        /// <summary>
        /// Gets or sets the terrain tile to paint with.
        /// This tile is copied and applied to all eligible locations within selected rooms/halls.
        /// </summary>
        public ITile Terrain { get; set; }

        /// <summary>
        /// Gets or sets the terrain stencil that determines which tiles within a room/hall are eligible to be painted.
        /// The stencil acts as a mask, allowing fine-grained control over which tile locations receive the terrain.
        /// </summary>
        public ITerrainStencil<T> TerrainStencil { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether halls are eligible for terrain painting.
        /// </summary>
        public bool IncludeHalls { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether rooms are eligible for terrain painting.
        /// </summary>
        public bool IncludeRooms { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoomTerrainStep{T}"/> class with default values.
        /// Sets up an empty filter list and a default terrain stencil.
        /// </summary>
        public RoomTerrainStep()
        {
            this.Filters = new List<BaseRoomFilter>();
            this.TerrainStencil = new DefaultTerrainStencil<T>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoomTerrainStep{T}"/> class with the specified terrain settings.
        /// </summary>
        /// <param name="terrain">The terrain tile to paint with.</param>
        /// <param name="amount">The range for the number of rooms or halls to paint.</param>
        /// <param name="includeRooms">Whether to include regular rooms in the selection.</param>
        /// <param name="includeHalls">Whether to include halls in the selection.</param>
        public RoomTerrainStep(ITile terrain, RandRange amount, bool includeRooms, bool includeHalls) : this()
        {
            Terrain = terrain;
            Amount = amount;
            IncludeRooms = includeRooms;
            IncludeHalls = includeHalls;
        }

        /// <summary>
        /// Applies the terrain painting step to the floor plan.
        /// Randomly selects eligible rooms/halls and paints the specified terrain on tiles that pass the stencil test.
        /// </summary>
        /// <param name="map">The floor plan generation context containing the map data and room/hall plans.</param>
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
                                map.TrySetTile(destLoc, this.Terrain.Copy());
                        }
                    }

                    openRooms.RemoveAt(randIndex);
                }
            }

        }

    }

}

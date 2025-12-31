using System;
using System.Collections.Generic;
using RogueElements;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Adds the entrance and exit to the floor in a room-conscious manner.
    /// The algorithm will try to place them within and outside of a certain specified range in tiles.
    /// </summary>
    /// <remarks>
    /// This step is a specialized version of the base stairs step that enforces a distance constraint
    /// between entrance and exit placements. It ensures entrances and exits are separated by a distance
    /// that falls within the specified tile distance range.
    /// </remarks>
    /// <typeparam name="TGenContext">The floor plan generation context type that provides room planning and placement capabilities.</typeparam>
    /// <typeparam name="TEntrance">The entrance type to be placed on the floor.</typeparam>
    /// <typeparam name="TExit">The exit type to be placed on the floor.</typeparam>
    [Serializable]
    public class FloorStairsDistanceStep<TGenContext, TEntrance, TExit> : BaseFloorStairsStep<TGenContext, TEntrance, TExit>
        where TGenContext : class, IFloorPlanGenContext, IPlaceableGenContext<TEntrance>, IPlaceableGenContext<TExit>
        where TEntrance : IEntrance
        where TExit : IExit
    {
        /// <inheritdoc/>
        public FloorStairsDistanceStep()
        {
        }

        /// <summary>
        /// Initializes a new instance with a single entrance and exit at the specified distance range.
        /// </summary>
        /// <param name="range">The minimum and maximum tile distance between entrance and exit. The range is start-inclusive, end-exclusive.</param>
        /// <param name="entrance">The entrance to place on the floor.</param>
        /// <param name="exit">The exit to place on the floor.</param>
        public FloorStairsDistanceStep(IntRange range, TEntrance entrance, TExit exit) : base(entrance, exit)
        {
            Distance = range;
        }

        /// <summary>
        /// Initializes a new instance with multiple entrances and exits at the specified distance range.
        /// </summary>
        /// <param name="range">The minimum and maximum tile distance between entrance and exit. The range is start-inclusive, end-exclusive.</param>
        /// <param name="entrances">The collection of entrances to place on the floor.</param>
        /// <param name="exits">The collection of exits to place on the floor.</param>
        public FloorStairsDistanceStep(IntRange range, List<TEntrance> entrances, List<TExit> exits) : base(entrances, exits)
        {
            Distance = range;
        }

        /// <summary>
        /// Gets or sets the range of tile distance that must be maintained between entrances and exits.
        /// </summary>
        /// <remarks>
        /// The distance is calculated using Manhattan distance (Dist4) between room centers.
        /// The range is start-inclusive and end-exclusive (e.g., IntRange(10, 20) allows distances 10-19).
        /// </remarks>
        /// <value>An IntRange specifying the minimum and maximum tile separation distance.</value>
        public IntRange Distance { get; set; }

        /// <summary>
        /// Finds an outlet location for placing an entrance or exit within a suitable room.
        /// </summary>
        /// <remarks>
        /// This method selects a room from the available free rooms and verifies that it maintains
        /// the required distance constraint from any previously used rooms. It returns a random tile
        /// within the selected room that is suitable for placement. Rooms that cannot satisfy the
        /// distance constraint are moved to the used list or removed from consideration.
        /// </remarks>
        /// <typeparam name="T">The type of placement entity (entrance or exit).</typeparam>
        /// <param name="map">The floor plan generation context providing room information and tile availability.</param>
        /// <param name="free_indices">A list of room indices that are available for consideration. Rooms are removed as they are processed.</param>
        /// <param name="used_indices">A list of room indices that have already been used for placement. This is checked to enforce distance constraints. May be null.</param>
        /// <returns>A location within a suitable room if one is found; null if no suitable rooms remain.</returns>
        protected override Loc? GetOutlet<T>(TGenContext map, List<int> free_indices, List<int> used_indices)
        {
            while (free_indices.Count > 0)
            {
                int roomIndex = map.Rand.Next() % free_indices.Count;
                int startRoom = free_indices[roomIndex];

                Rect startDraw = map.RoomPlan.GetRoom(startRoom).Draw;

                bool used = false;
                if (used_indices != null)
                {
                    foreach (int usedRoom in used_indices)
                    {
                        Rect usedDraw = map.RoomPlan.GetRoom(usedRoom).Draw;
                        if (!Distance.Contains((usedDraw.Start - startDraw.Start).Dist4()))
                        {
                            used = true;
                            break;
                        }
                    }
                }

                if (used)
                {
                    // if we're not on our backup list, move it to the backup list and continue on
                    if (used_indices != null)
                    {
                        free_indices.RemoveAt(roomIndex);
                        //TODO: come up with a third list for indices that can still be used in the final backup plan
                        //but must be avoided when checking existing used indices
                        //used_indices.Add(startRoom);
                        continue;
                    }
                }

                List<Loc> tiles = ((IPlaceableGenContext<T>)map).GetFreeTiles(startDraw);

                if (tiles.Count == 0)
                {
                    // this room is not suitable and never will be, remove it
                    free_indices.RemoveAt(roomIndex);
                    continue;
                }

                Loc start = tiles[map.Rand.Next(tiles.Count)];

                // if we have a used-list, transfer the index over
                if (used_indices != null)
                {
                    free_indices.RemoveAt(roomIndex);
                    used_indices.Add(startRoom);
                }

                return start;
            }

            return null;
        }
    }
}

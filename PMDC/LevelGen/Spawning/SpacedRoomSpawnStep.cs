using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Dungeon;
using PMDC.Dungeon;
using RogueEssence.LevelGen;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Spawns objects in randomly chosen rooms with spacing constraints.
    /// Once a room is chosen, it and all adjacent rooms cannot be chosen again.
    /// Large rooms have the same probability as small rooms.
    /// </summary>
    /// <typeparam name="TGenContext">The type of generation context that provides floor planning and placement capabilities.</typeparam>
    /// <typeparam name="TSpawnable">The type of objects to spawn (must implement ISpawnable).</typeparam>
    [Serializable]
    public class SpacedRoomSpawnStep<TGenContext, TSpawnable> : RoomSpawnStep<TGenContext, TSpawnable>
        where TGenContext : class, IFloorPlanGenContext, IPlaceableGenContext<TSpawnable>
        where TSpawnable : ISpawnable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpacedRoomSpawnStep{TGenContext, TSpawnable}"/> class.
        /// </summary>
        public SpacedRoomSpawnStep()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified spawner and hall inclusion setting.
        /// </summary>
        /// <param name="spawn">The spawner that generates objects to place in rooms.</param>
        /// <param name="includeHalls">If <c>true</c>, hallways are eligible for spawning; otherwise only rooms are used.</param>
        public SpacedRoomSpawnStep(IStepSpawner<TGenContext, TSpawnable> spawn, bool includeHalls = false)
            : base(spawn)
        {
            this.IncludeHalls = includeHalls;
        }

        /// <summary>
        /// Gets or sets a value indicating whether halls are eligible for spawning.
        /// </summary>
        /// <value><c>true</c> if hallways should be considered as valid spawn locations; otherwise <c>false</c>.</value>
        public bool IncludeHalls { get; set; }

        /// <summary>
        /// Distributes spawn objects across rooms with spacing constraints to ensure selected rooms and their adjacent rooms are spaced apart.
        /// </summary>
        /// <param name="map">The generation context containing the floor plan and placement system.</param>
        /// <param name="spawns">The list of objects to spawn. Items are removed as they are successfully placed.</param>
        /// <remarks>
        /// <para>This method implements a spacing algorithm that:</para>
        /// <list type="number">
        /// <item><description>Collects all valid rooms (and halls if enabled) that pass the configured filters.</description></item>
        /// <item><description>Randomly selects rooms from the available pool and attempts to spawn one object per room.</description></item>
        /// <item><description>After a successful spawn, marks the selected room and all adjacent rooms as taken.</description></item>
        /// <item><description>Continues until all spawns are placed or no more valid rooms are available.</description></item>
        /// <item><description>Falls back to the base class method to spawn remaining objects in any available rooms.</description></item>
        /// </list>
        /// </remarks>
        public override void DistributeSpawns(TGenContext map, List<TSpawnable> spawns)
        {
            HashSet<RoomHallIndex> takenRooms = new HashSet<RoomHallIndex>();

            // random per room, not per-tile
            var spawningRooms = new SpawnList<RoomHallIndex>();
            var remainingRooms = new SpawnList<RoomHallIndex>();

            for (int ii = 0; ii < map.RoomPlan.RoomCount; ii++)
            {
                if (!BaseRoomFilter.PassesAllFilters(map.RoomPlan.GetRoomPlan(ii), this.Filters))
                    continue;
                spawningRooms.Add(new RoomHallIndex(ii, false), 10);
            }

            if (this.IncludeHalls)
            {
                for (int ii = 0; ii < map.RoomPlan.HallCount; ii++)
                {
                    if (!BaseRoomFilter.PassesAllFilters(map.RoomPlan.GetHallPlan(ii), this.Filters))
                        continue;
                    spawningRooms.Add(new RoomHallIndex(ii, true), 10);
                }
            }

            while (spawningRooms.Count > 0 && spawns.Count > 0)
            {
                int randIndex = spawningRooms.PickIndex(map.Rand);
                RoomHallIndex roomIndex = spawningRooms.GetSpawn(randIndex);

                if (takenRooms.Contains(roomIndex))
                {
                    spawningRooms.RemoveAt(randIndex);
                    remainingRooms.Add(roomIndex, 10);
                    continue;
                }

                // try to spawn the item
                if (this.SpawnInRoom(map, roomIndex, spawns[spawns.Count - 1]))
                {
                    GenContextDebug.DebugProgress("Placed Object");

                    // remove the item spawn
                    spawns.RemoveAt(spawns.Count - 1);

                    spawningRooms.RemoveAt(randIndex);
                    takenRooms.Add(roomIndex);

                    //add adjacents to the takenRooms
                    List<RoomHallIndex> adjacent = map.RoomPlan.GetRoomHall(roomIndex).Adjacents;
                    for (int ii = 0; ii < adjacent.Count; ii++)
                        takenRooms.Add(adjacent[ii]);

                    if (!roomIndex.IsHall)
                    {
                        List<int> adjacentRooms = map.RoomPlan.GetAdjacentRooms(roomIndex.Index);
                        for (int ii = 0; ii < adjacentRooms.Count; ii++)
                            takenRooms.Add(new RoomHallIndex(ii, false));
                    }
                }
                else
                {
                    spawningRooms.RemoveAt(randIndex);
                }
            }

            //backup plan; spawn in remaining rooms
            this.SpawnRandInCandRooms(map, spawningRooms, spawns, 100);
        }
    }
}

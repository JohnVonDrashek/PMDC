using System;
using RogueElements;
using RogueEssence.Dungeon;
using RogueEssence;
using RogueEssence.LevelGen;
using PMDC.Dungeon;
using System.Collections.Generic;
using Newtonsoft.Json;
using RogueEssence.Dev;
using RogueEssence.Data;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Generates a temporary tile on the floor that counts down and disappears based on a map status effect.
    /// </summary>
    /// <remarks>
    /// This generation step places a single temporary effect tile on the map at a randomly selected free location.
    /// The tile's lifetime is determined by a countdown status whose duration is proportional to the Manhattan distance
    /// from the dungeon entrance. The tile and countdown status are configurable, and room/hall filters determine valid
    /// placement locations.
    /// </remarks>
    /// <typeparam name="T">The map generation context type, must be a ListMapGenContext.</typeparam>
    [Serializable]
    public class TempTileStep<T> : GenStep<T> where T : ListMapGenContext
    {
        /// <summary>
        /// Gets or sets the temporary effect tile to place on the map.
        /// </summary>
        /// <remarks>
        /// This is a random picker that selects which effect tile to place. The picked tile is cloned
        /// when placed on the map.
        /// </remarks>
        public IRandPicker<EffectTile> TempTile;

        /// <summary>
        /// Gets or sets the map status ID that tracks the countdown timer for the temporary tile.
        /// </summary>
        /// <remarks>
        /// The status is loaded from the game data and its countdown state is initialized based on
        /// the distance between the entrance and the tile placement location. The status must have
        /// MapCountDownState and MapLocState components.
        /// </remarks>
        [JsonConverter(typeof(MapStatusConverter))]
        [DataType(0, DataManager.DataType.MapStatus, false)]
        public string TempStatus;

        /// <summary>
        /// Gets or sets the room and hall filters that determine which locations are valid for tile placement.
        /// </summary>
        /// <remarks>
        /// Only rooms and halls that pass all filters in this list are considered for tile placement.
        /// If no valid locations exist after filtering, the step does nothing.
        /// </remarks>
        public List<BaseRoomFilter> TileFilters { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TempTileStep{T}"/> class with default values.
        /// </summary>
        public TempTileStep()
        {
            TileFilters = new List<BaseRoomFilter>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TempTileStep{T}"/> class with the specified tile and countdown status.
        /// </summary>
        /// <param name="tempTile">The random picker for the temporary effect tile to place.</param>
        /// <param name="tempStatus">The map status ID used to track the countdown timer for the tile.</param>
        public TempTileStep(IRandPicker<EffectTile> tempTile, string tempStatus) : base()
        {
            TempTile = tempTile;
            TempStatus = tempStatus;
            TileFilters = new List<BaseRoomFilter>();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This method performs the following steps:
        /// 1. Collects all free tiles from rooms and halls that pass the configured filters.
        /// 2. Randomly selects one of these free tiles as the placement location.
        /// 3. Places a cloned temporary effect tile at the selected location.
        /// 4. Initializes a countdown status at that location, with duration proportional to the Manhattan distance from the dungeon entrance.
        /// The countdown duration is calculated as (distance * 3 / 10 + 1) * 10, rounding distance-based time to the nearest 10 turns.
        /// </remarks>
        /// <param name="map">The map generation context on which to place the temporary tile.</param>
        public override void Apply(T map)
        {
            //all free tiles to place switches, by room
            List<Loc> freeTiles = new List<Loc>();

            for (int ii = 0; ii < map.RoomPlan.RoomCount; ii++)
            {
                FloorRoomPlan plan = map.RoomPlan.GetRoomPlan(ii);
                if (!BaseRoomFilter.PassesAllFilters(plan, this.TileFilters))
                    continue;
                freeTiles.AddRange(((IPlaceableGenContext<EffectTile>)map).GetFreeTiles(plan.RoomGen.Draw));
            }
            for (int ii = 0; ii < map.RoomPlan.HallCount; ii++)
            {
                FloorHallPlan plan = map.RoomPlan.GetHallPlan(ii);
                if (!BaseRoomFilter.PassesAllFilters(plan, this.TileFilters))
                    continue;
                freeTiles.AddRange(((IPlaceableGenContext<EffectTile>)map).GetFreeTiles(plan.RoomGen.Draw));
            }

            if (freeTiles.Count == 0)
                return;

            int randTileIndex = map.Rand.Next(freeTiles.Count);
            Loc destLoc = freeTiles[randTileIndex];
            EffectTile switchTile = TempTile.Pick(map.Rand);

            ((IPlaceableGenContext<EffectTile>)map).PlaceItem(destLoc, new EffectTile(switchTile));
            map.GetPostProc(destLoc).Status |= (PostProcType.Panel | PostProcType.Item | PostProcType.Terrain);

            Loc entranceLoc = ((IViewPlaceableGenContext<MapGenEntrance>)map).GetLoc(0);
            int manhattanDistance = (entranceLoc - destLoc).Dist4();

            MapStatus tempStatus = new MapStatus(TempStatus);
            tempStatus.LoadFromData();
            MapLocState locState = tempStatus.StatusStates.GetWithDefault<MapLocState>();
            locState.Target = destLoc;
            MapCountDownState countdown = tempStatus.StatusStates.GetWithDefault<MapCountDownState>();
            //the player gets a time of 3x the distance rounded up to the 10.
            countdown.Counter = (manhattanDistance * 3 / 10 + 1) * 10;
            map.Map.Status.Add(TempStatus, tempStatus);
        }


    }
}

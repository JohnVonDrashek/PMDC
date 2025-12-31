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
    /// Adds an extra room to the layout that can only be accessed by pushing a switch.
    /// Creates one or more detour rooms sealed by a tile, and places a switch tile that opens them with an optional time limit.
    /// </summary>
    /// <typeparam name="T">The map generation context type, must derive from <see cref="BaseMapGenContext"/>.</typeparam>
    [Serializable]
    public class SwitchDetourStep<T> : BaseDetourStep<T> where T : BaseMapGenContext
    {
        /// <summary>
        /// The tile ID used to seal/lock the entrance to the detour room.
        /// </summary>
        [JsonConverter(typeof(TileConverter))]
        [DataType(0, DataManager.DataType.Tile, false)]
        public string SealedTile;


        /// <summary>
        /// The tile ID that serves as the switch to open the sealed door.
        /// </summary>
        [JsonConverter(typeof(TileConverter))]
        [DataType(0, DataManager.DataType.Tile, false)]
        public string SwitchTile;

        /// <summary>
        /// Determines whether a time limit is triggered when the switch is activated.
        /// </summary>
        public bool TimeLimit;

        /// <summary>
        /// The range of detour rooms to create. The actual count will be randomly selected within this range.
        /// </summary>
        public RandRange EntranceCount;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public SwitchDetourStep()
        { }

        /// <summary>
        /// Initializes a new instance with the specified configuration.
        /// </summary>
        /// <param name="sealedTile">The tile ID used to seal the detour room entrances.</param>
        /// <param name="switchTile">The tile ID of the switch that opens the sealed doors.</param>
        /// <param name="entranceCount">The range for the number of detour rooms to create.</param>
        /// <param name="timeLimit">Whether activating the switch triggers a countdown timer for accessing the detour rooms.</param>
        public SwitchDetourStep(string sealedTile, string switchTile, RandRange entranceCount, bool timeLimit) : this()
        {
            SealedTile = sealedTile;
            SwitchTile = switchTile;
            EntranceCount = entranceCount;
            TimeLimit = timeLimit;
        }

        /// <summary>
        /// Applies the switch detour generation step to the map.
        /// Creates sealed detour rooms and places a switch tile that can open them, optionally with a time limit.
        /// </summary>
        /// <param name="map">The map generation context to apply the detour to.</param>
        /// <inheritdoc/>
        public override void Apply(T map)
        {
            //first get all free tiles suitable for the switch
            List<Loc> freeSwitchTiles = ((IPlaceableGenContext<EffectTile>)map).GetAllFreeTiles();
            if (freeSwitchTiles.Count == 0)
                return;

            Grid.LocTest checkGround = (Loc testLoc) =>
            {
                return (!map.TileBlocked(testLoc) && !map.HasTileEffect(testLoc));
            };
            Grid.LocTest checkBlock = (Loc testLoc) =>
            {
                return map.WallTerrain.TileEquivalent(map.GetTile(testLoc));
            };

            List<LocRay4> rays = Detection.DetectWalls(((IViewPlaceableGenContext<MapGenEntrance>)map).GetLoc(0), new Rect(0, 0, map.Width, map.Height), checkBlock, checkGround);

            EffectTile effect = new EffectTile(SealedTile, true);

            List<Loc> freeTiles = new List<Loc>();
            List<LocRay4> createdEntrances = new List<LocRay4>();

            int amount = EntranceCount.Pick(map.Rand);

            for (int ii = 0; ii < amount; ii++)
            {
                LocRay4? ray = PlaceRoom(map, rays, effect, freeTiles);

                if (ray != null)
                    createdEntrances.Add(ray.Value);
            }

            if (createdEntrances.Count > 0)
            {
                PlaceEntities(map, freeTiles);

                EffectTile switchTile = new EffectTile(SwitchTile, true);

                switchTile.TileStates.Set(new DangerState(TimeLimit));

                TileListState state = new TileListState();
                for (int mm = 0; mm < createdEntrances.Count; mm++)
                    state.Tiles.Add(new Loc(createdEntrances[mm].Loc));
                switchTile.TileStates.Set(state);

                int randIndex = map.Rand.Next(freeSwitchTiles.Count);
                
                ((IPlaceableGenContext<EffectTile>)map).PlaceItem(freeSwitchTiles[randIndex], switchTile);
            }
        }

    }
}

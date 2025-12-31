using System;
using RogueElements;
using System.Collections.Generic;
using RogueEssence.LevelGen;
using RogueEssence.Dev;
using RogueEssence.Data;
using RogueEssence.Dungeon;
using PMDC.Dungeon;
using Newtonsoft.Json;

namespace PMDC.LevelGen
{
    /// <summary>
    /// A generation step that orients all already-placed compass tiles to point to points of interest.
    /// This step searches for compass tiles in the floor, identifies eligible destination tiles,
    /// and configures each compass tile's state to track these destinations.
    /// </summary>
    /// <typeparam name="T">The map generation context type, must inherit from StairsMapGenContext.</typeparam>
    [Serializable]
    public class SetCompassStep<T> : GenStep<T>
        where T : StairsMapGenContext
    {
        /// <summary>
        /// Gets or sets the ID of the tile to use as a compass.
        /// This tile should have a CompassEvent in its InteractWithTiles to define eligible destinations.
        /// </summary>
        [JsonConverter(typeof(TileConverter))]
        [DataType(0, DataManager.DataType.Tile, false)]
        public string CompassTile;

        /// <summary>
        /// Initializes a new instance of the SetCompassStep class with default values.
        /// </summary>
        public SetCompassStep()
        {
        }

        /// <summary>
        /// Initializes a new instance of the SetCompassStep class with the specified compass tile ID.
        /// </summary>
        /// <param name="tile">The ID of the tile to use as a compass.</param>
        public SetCompassStep(string tile)
        {
            CompassTile = tile;
        }

        /// <summary>
        /// Applies the compass orientation step to the provided map.
        /// Locates all compass tiles and eligible destination tiles, then configures each compass
        /// tile to point to all eligible destinations and map exits.
        /// </summary>
        /// <param name="map">The map generation context to apply the step to.</param>
        public override void Apply(T map)
        {
            List<Tile> compassTiles = new List<Tile>();
            List<Loc> endpointTiles = new List<Loc>();
            TileData tileData = DataManager.Instance.GetTile(CompassTile);
            CompassEvent compassEvent = null;
            foreach (SingleCharEvent effect in tileData.InteractWithTiles.EnumerateInOrder())
            {
                compassEvent = effect as CompassEvent;
                if (effect != null)
                    break;
            }

            for (int xx = 0; xx < map.Width; xx++)
            {
                for (int yy = 0; yy < map.Height; yy++)
                {
                    Loc tileLoc = new Loc(xx, yy);
                    Tile tile = map.Map.GetTile(tileLoc);
                    if (tile.Effect.ID == CompassTile)
                        compassTiles.Add(tile);
                    else if (compassEvent.EligibleTiles.Contains(tile.Effect.ID))
                        endpointTiles.Add(tileLoc);
                }
            }
            foreach (Tile compass in compassTiles)
            {
                TileListState destState = new TileListState();
                foreach (Loc loc in endpointTiles)
                    destState.Tiles.Add(loc);
                foreach (MapGenExit exit in map.GenExits)
                    destState.Tiles.Add(exit.Loc);
                compass.Effect.TileStates.Set(destState);
            }
        }
    }
}

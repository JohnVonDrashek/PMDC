using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Data;
using RogueEssence.Dungeon;
using RogueEssence.LevelGen;

namespace PMDC.LevelGen
{
    /// <summary>
    /// A generation step that places effect tiles across the map based on a terrain stencil filter.
    /// </summary>
    /// <remarks>
    /// This step iterates through all map positions and places a copy of the specified effect tile
    /// at each location where the terrain stencil evaluates to true. The stencil determines which
    /// tiles are eligible to receive the effect tile based on the current map state.
    /// </remarks>
    /// <typeparam name="T">The map generation context type. Must derive from <see cref="BaseMapGenContext"/>.</typeparam>
    [Serializable]
    public class MapTileStep<T> : GenStep<T> where T : BaseMapGenContext
    {
        /// <summary>
        /// Gets or sets the effect tile to place on matching positions.
        /// </summary>
        /// <remarks>
        /// A copy of this tile is placed at each location that passes the terrain stencil test.
        /// </remarks>
        public EffectTile Tile { get; set; }

        /// <summary>
        /// Gets or sets the terrain stencil that determines which tiles are eligible to receive the effect tile.
        /// </summary>
        /// <remarks>
        /// The stencil is evaluated at each map position to determine whether the effect tile should be placed there.
        /// If the stencil test returns true for a position, a copy of <see cref="Tile"/> is placed at that location.
        /// </remarks>
        public ITerrainStencil<T> TerrainStencil { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapTileStep{T}"/> class with default values.
        /// </summary>
        /// <remarks>
        /// The terrain stencil is initialized to <see cref="DefaultTerrainStencil{T}"/>, which accepts all valid tile positions.
        /// </remarks>
        public MapTileStep()
        {
            this.TerrainStencil = new DefaultTerrainStencil<T>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapTileStep{T}"/> class with the specified effect tile.
        /// </summary>
        /// <param name="tile">The effect tile to place at matching positions throughout the map.</param>
        /// <remarks>
        /// The terrain stencil is initialized to <see cref="DefaultTerrainStencil{T}"/>, which accepts all valid tile positions.
        /// </remarks>
        public MapTileStep(EffectTile tile) : this()
        {
            Tile = tile;
        }

        /// <summary>
        /// Applies this generation step to the specified map context.
        /// </summary>
        /// <param name="map">The map generation context to apply this step to.</param>
        /// <remarks>
        /// This method iterates through every position on the map and places a copy of <see cref="Tile"/>
        /// at each position where the <see cref="TerrainStencil"/> test returns true.
        /// </remarks>
        /// <inheritdoc/>
        public override void Apply(T map)
        {
            for (int xx = 0; xx < map.Width; xx++)
            {
                for (int yy = 0; yy < map.Height; yy++)
                {
                    Loc destLoc = new Loc(xx, yy);
                    if (this.TerrainStencil.Test(map, destLoc))
                        ((IPlaceableGenContext<EffectTile>)map).PlaceItem(destLoc, new EffectTile(Tile));
                }
            }
        }

    }

}

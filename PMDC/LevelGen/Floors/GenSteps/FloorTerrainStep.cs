using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Data;
using RogueEssence.Dungeon;

namespace PMDC.LevelGen
{
    /// <summary>
    /// A generation step that sets terrain tiles across the entire floor based on a stencil filter.
    /// </summary>
    /// <remarks>
    /// This step iterates through every tile on the floor and applies the terrain to those tiles
    /// that pass the terrain stencil test. The stencil allows selective painting of terrain based
    /// on custom criteria (e.g., only on walkable tiles, only in specific regions).
    /// </remarks>
    /// <typeparam name="T">The tiled generation context type containing floor information.</typeparam>
    [Serializable]
    public class FloorTerrainStep<T> : GenStep<T> where T : class, ITiledGenContext
    {
        /// <summary>
        /// Gets or sets the tile template representing the terrain to paint across the floor.
        /// </summary>
        /// <remarks>
        /// The terrain tile will be copied for each location it is applied to, ensuring each
        /// position gets its own independent instance of the terrain tile.
        /// </remarks>
        public ITile Terrain { get; set; }

        /// <summary>
        /// Gets or sets the stencil that determines which tiles are eligible to receive the terrain.
        /// </summary>
        /// <remarks>
        /// The stencil's Test method is called for each tile position to determine if the terrain
        /// should be applied. This allows for flexible, conditional terrain painting logic.
        /// </remarks>
        public ITerrainStencil<T> TerrainStencil { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FloorTerrainStep{T}"/> class with a default terrain stencil.
        /// </summary>
        /// <remarks>
        /// The default terrain stencil is created as <see cref="DefaultTerrainStencil{T}"/>.
        /// The terrain tile must be set separately after construction.
        /// </remarks>
        public FloorTerrainStep()
        {
            this.TerrainStencil = new DefaultTerrainStencil<T>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FloorTerrainStep{T}"/> class with the specified terrain tile.
        /// </summary>
        /// <param name="terrain">The terrain tile template to paint across eligible positions on the floor.</param>
        /// <remarks>
        /// This constructor calls the parameterless constructor to set up the default terrain stencil,
        /// then sets the terrain tile to the provided value.
        /// </remarks>
        public FloorTerrainStep(ITile terrain) : this()
        {
            Terrain = terrain;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Iterates through all positions on the floor. For each position that passes the terrain stencil test,
        /// applies a copy of the terrain tile to that location. Uses <see cref="ITiledGenContext.TrySetTile"/>
        /// to safely set the terrain, which may fail if the location is invalid.
        /// </remarks>
        public override void Apply(T map)
        {
            for (int xx = 0; xx < map.Width; xx++)
            {
                for (int yy = 0; yy < map.Height; yy++)
                {
                    Loc destLoc = new Loc(xx, yy);
                    if (this.TerrainStencil.Test(map, destLoc))
                        map.TrySetTile(destLoc, this.Terrain.Copy());
                }
            }
        }

    }

}

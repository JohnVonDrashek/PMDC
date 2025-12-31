using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Data;
using RogueEssence.Dungeon;

namespace PMDC.LevelGen
{
    /// <summary>
    /// A generation step that places terrain tiles in specified patterns on a floor.
    /// </summary>
    /// <remarks>
    /// This step extends <see cref="PatternPlacerStep{TGenContext}"/> to distribute a specific terrain type
    /// across designated pattern locations. It respects terrain stencils when applying the terrain,
    /// allowing fine control over placement rules. Commonly used for placing water, grass, or other
    /// environmental terrain features during floor generation.
    /// </remarks>
    /// <typeparam name="TGenContext">The generation context type, must support tiled generation and floor planning.</typeparam>
    [Serializable]
    public class PatternTerrainStep<TGenContext> : PatternPlacerStep<TGenContext>
        where TGenContext : class, ITiledGenContext, IFloorPlanGenContext
    {

        /// <summary>
        /// Gets or sets the tile representing the terrain to paint in the pattern.
        /// </summary>
        /// <remarks>
        /// This tile will be copied and placed at each valid location determined by the pattern and stencil.
        /// </remarks>
        public ITile Terrain { get; set; }

        /// <inheritdoc/>
        public PatternTerrainStep() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PatternTerrainStep{TGenContext}"/> class with the specified terrain tile.
        /// </summary>
        /// <param name="terrain">The terrain tile to paint in the pattern. This tile will be copied for each placement.</param>
        public PatternTerrainStep(ITile terrain) : base()
        {
            Terrain = terrain;
        }

        /// <summary>
        /// Applies the terrain tile to each location in the provided list that passes the terrain stencil test.
        /// </summary>
        /// <remarks>
        /// For each location in <paramref name="drawLocs"/>, this method tests whether the location passes
        /// the <see cref="PatternPlacerStep{TGenContext}.TerrainStencil"/> check. If it does, a copy of
        /// <see cref="Terrain"/> is placed at that location via <see cref="ITiledGenContext.TrySetTile(Loc, ITile)"/>.
        /// </remarks>
        /// <param name="map">The generation context containing the floor being modified.</param>
        /// <param name="drawLocs">The list of locations where terrain should be applied.</param>
        protected override void DrawOnLocs(TGenContext map, List<Loc> drawLocs)
        {
            ITile tile = Terrain;
            foreach (Loc destLoc in drawLocs)
            {
                if (this.TerrainStencil.Test(map, destLoc))
                    map.TrySetTile(destLoc, tile.Copy());
            }
        }

    }
}

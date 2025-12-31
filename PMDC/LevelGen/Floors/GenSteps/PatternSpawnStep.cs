using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Data;
using RogueEssence.Dungeon;

namespace PMDC.LevelGen
{
    /// <summary>
    /// A generation step that places spawnable objects (such as items or enemies) on a floor in specified patterns.
    /// </summary>
    /// <remarks>
    /// This step places spawnables at specified locations determined by the pattern, using the spawner system
    /// to pick which spawnables to place. It respects terrain stencils and only places items where allowed.
    /// </remarks>
    /// <typeparam name="TGenContext">The generation context type, which must support spawning, placeable placement, and floor planning.</typeparam>
    /// <typeparam name="TSpawnable">The type of spawnable object to place, must implement ISpawnable.</typeparam>
    [Serializable]
    public class PatternSpawnStep<TGenContext, TSpawnable> : PatternPlacerStep<TGenContext>
        where TGenContext : class, ISpawningGenContext<TSpawnable>, IPlaceableGenContext<TSpawnable>, IFloorPlanGenContext
        where TSpawnable : ISpawnable
    {
        /// <summary>
        /// Initializes a new instance of the PatternSpawnStep class.
        /// </summary>
        public PatternSpawnStep() : base()
        {

        }

        /// <summary>
        /// Places spawnables at the specified locations using the map's spawner system.
        /// </summary>
        /// <remarks>
        /// Picks a single spawnable from the map's spawner and places it at each valid location in drawLocs.
        /// Placement is only performed if:
        /// 1. The spawner has available spawnables to pick
        /// 2. The terrain stencil allows placement at the location
        /// 3. The map allows item placement at the location
        /// </remarks>
        /// <param name="map">The generation context containing the floor map, spawner, and placement rules.</param>
        /// <param name="drawLocs">The list of locations where spawnables should be placed.</param>
        protected override void DrawOnLocs(TGenContext map, List<Loc> drawLocs)
        {
            if (!map.Spawner.CanPick)
                return;

            TSpawnable spawn = map.Spawner.Pick(map.Rand);
            foreach (Loc destLoc in drawLocs)
            {
                if (this.TerrainStencil.Test(map, destLoc) && map.CanPlaceItem(destLoc))
                    map.PlaceItem(destLoc, spawn);
            }
        }

    }
}

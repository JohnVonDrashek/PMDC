// <copyright file="BlobWaterStep.cs" company="Audino">
// Copyright (c) Audino
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence;
using RogueEssence.Data;
using RogueEssence.Dungeon;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Creates patterns of water by loading pre-designed map files and placing them randomly across the dungeon floor.
    /// </summary>
    /// <remarks>
    /// This water step uses a blob stencil to determine valid placement locations and attempts to place patterns
    /// at multiple random positions. Patterns can be transposed during placement for variety.
    /// </remarks>
    /// <typeparam name="T">The tiled generation context type, must implement <see cref="ITiledGenContext"/>.</typeparam>
    [Serializable]
    public class PatternWaterStep<T> : WaterStep<T>, IPatternWaterStep
        where T : class, ITiledGenContext
    {
        /// <inheritdoc/>
        public PatternWaterStep()
            : base()
        {
            this.BlobStencil = new DefaultBlobStencil<T>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PatternWaterStep{T}"/> class with the specified parameters.
        /// </summary>
        /// <param name="amount">The random range for the number of patterns to place.</param>
        /// <param name="terrain">The tile type to use for the water terrain.</param>
        /// <param name="stencil">The terrain stencil that defines valid placement areas.</param>
        /// <param name="blobStencil">The blob stencil that validates pattern placement based on neighborhood conditions.</param>
        public PatternWaterStep(RandRange amount, ITile terrain, ITerrainStencil<T> stencil, IBlobStencil<T> blobStencil)
            : base(terrain, stencil)
        {
            this.Amount = amount;
            this.BlobStencil = blobStencil;
        }

        /// <summary>
        /// Gets or sets the number of patterns to place on the floor.
        /// </summary>
        /// <value>A <see cref="RandRange"/> specifying the random number of patterns to place.</value>
        public RandRange Amount { get; set; }

        /// <summary>
        /// Gets or sets the collection of map file paths to choose from when placing water patterns.
        /// </summary>
        /// <value>A <see cref="SpawnList{T}"/> of map file names. Files are loaded from the "Map/" data folder.</value>
        [RogueEssence.Dev.DataFolder(1, "Map/")]
        public SpawnList<string> Maps;

        /// <summary>
        /// Gets or sets the blob stencil used to validate pattern placement.
        /// </summary>
        /// <remarks>
        /// The blob stencil operates on an all-or-nothing basis: if the stencil test passes for all valid positions
        /// within the pattern boundary, the entire pattern is drawn. Otherwise, no part of it is placed.
        /// </remarks>
        /// <value>An <see cref="IBlobStencil{T}"/> that determines if a pattern blob can be placed.</value>
        public IBlobStencil<T> BlobStencil { get; set; }

        /// <summary>
        /// Applies the water pattern generation step to the dungeon floor.
        /// </summary>
        /// <remarks>
        /// For each pattern to be placed, this method attempts up to 30 random locations until one passes the blob stencil test.
        /// Patterns are randomly selected from the <see cref="Maps"/> list and may be transposed before placement.
        /// </remarks>
        /// <param name="map">The tiled generation context representing the dungeon floor.</param>
        public override void Apply(T map)
        {
            int chosenAmount = Amount.Pick(map.Rand);
            if (chosenAmount == 0 || Maps.Count == 0)
                return;

            Dictionary<string, Map> mapCache = new Dictionary<string, Map>();

            for (int ii = 0; ii < chosenAmount; ii++)
            {
                // attempt to place in 30 locations
                for (int jj = 0; jj < 30; jj++)
                {
                    string chosenPattern = Maps.Pick(map.Rand);

                    Map placeMap;
                    if (!mapCache.TryGetValue(chosenPattern, out placeMap))
                    {
                        placeMap = DataManager.Instance.GetMap(chosenPattern);
                        mapCache[chosenPattern] = placeMap;
                    }

                    // TODO: instead of transpose, just flipV and flipH with 50% for each?
                    bool transpose = (map.Rand.Next(2) == 0);
                    Loc size = placeMap.Size;
                    if (transpose)
                        size = size.Transpose();

                    int maxWidth = Math.Max(1, map.Width - size.X);
                    int maxHeight = Math.Max(1, map.Height - size.Y);
                    Loc offset = new Loc(map.Rand.Next(0, maxWidth), map.Rand.Next(0, maxHeight));
                    bool placed = this.AttemptBlob(map, placeMap, offset);

                    if (placed)
                        break;
                }
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0}: Amt:{1} Maps:{2}", this.GetType().GetFormattedTypeName(), this.Amount.ToString(), this.Maps.Count.ToString());
        }

        /// <summary>
        /// Attempts to place a pattern blob at the specified offset on the map.
        /// </summary>
        /// <remarks>
        /// This method checks if the pattern passes the blob stencil test at the given offset.
        /// If valid, it draws the blob to the map using the inherited <c>DrawBlob</c> method.
        /// The blob is considered valid where the pattern's terrain differs from the floor's room terrain.
        /// </remarks>
        /// <param name="map">The tiled generation context representing the dungeon floor.</param>
        /// <param name="placeMap">The map containing the pattern to place.</param>
        /// <param name="offset">The location where the pattern should be placed on the floor.</param>
        /// <returns><c>true</c> if the pattern blob was successfully placed; <c>false</c> if the stencil test failed.</returns>
        protected virtual bool AttemptBlob(T map, Map placeMap, Loc offset)
        {
            bool IsBlobValid(Loc loc)
            {
                Loc srcLoc = loc - offset;
                if (Collision.InBounds(new Rect(Loc.Zero, placeMap.Size), srcLoc))
                    return !map.RoomTerrain.TileEquivalent(placeMap.Tiles[srcLoc.X][srcLoc.Y]);
                return false;
            }

            if (!this.BlobStencil.Test(map, new Rect(offset, placeMap.Size), IsBlobValid))
                return false;

            this.DrawBlob(map, new Rect(offset, placeMap.Size), IsBlobValid);
            return true;
        }
    }
}

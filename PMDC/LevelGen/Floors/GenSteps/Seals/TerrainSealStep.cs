using System;
using RogueElements;
using RogueEssence.Dungeon;
using RogueEssence;
using RogueEssence.LevelGen;
using PMDC.Dungeon;
using System.Collections.Generic;
using RogueEssence.Dev;
using RogueEssence.Data;
using Newtonsoft.Json;

namespace PMDC.LevelGen
{
    /// <summary>
    /// One part of several steps used to create a room sealed by terrain, or several thereof.
    /// This step takes the target rooms and surrounds them with the selected walls, with one key block used to unlock them.
    /// </summary>
    /// <remarks>
    /// This generation step seals target rooms using terrain tiles as barriers. The sealed rooms will have:
    /// - A seal terrain tile blocking the main entrance
    /// - Border terrain tiles surrounding the room perimeter
    /// - One key block to unlock the seal (handled by base class)
    /// </remarks>
    /// <typeparam name="T">The map context type, must inherit from <see cref="ListMapGenContext"/>.</typeparam>
    [Serializable]
    public class TerrainSealStep<T> : BaseSealStep<T> where T : ListMapGenContext
    {
        /// <summary>
        /// The terrain type that is used to block off the main entrance to the room.
        /// </summary>
        /// <remarks>
        /// This terrain tile is placed at locations marked as <see cref="SealType.Locked"/> or <see cref="SealType.Key"/>
        /// in the seal list. It serves as the primary barrier to accessing the sealed room.
        /// </remarks>
        [DataType(0, DataManager.DataType.Terrain, false)]
        public string SealTerrain;

        /// <summary>
        /// The terrain type that is used to border the room perimeter.
        /// </summary>
        /// <remarks>
        /// This terrain tile is placed at locations marked as <see cref="SealType.Blocked"/> in the seal list.
        /// It forms the secondary barrier that surrounds the sealed room.
        /// </remarks>
        [DataType(0, DataManager.DataType.Terrain, false)]
        public string BorderTerrain;

        /// <summary>
        /// Initializes a new instance of the <see cref="TerrainSealStep{T}"/> class with empty terrain types.
        /// </summary>
        /// <remarks>
        /// The terrain types must be set after construction before the step is applied.
        /// </remarks>
        public TerrainSealStep()
        {
            SealTerrain = "";
            BorderTerrain = "";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TerrainSealStep{T}"/> class with the specified terrain types.
        /// </summary>
        /// <param name="sealedTerrain">The terrain type for the main entrance seal.</param>
        /// <param name="borderTerrain">The terrain type for room borders.</param>
        public TerrainSealStep(string sealedTerrain, string borderTerrain) : base()
        {
            SealTerrain = sealedTerrain;
            BorderTerrain = borderTerrain;
        }

        /// <summary>
        /// Places the seal and border terrain tiles at the specified locations on the map.
        /// </summary>
        /// <remarks>
        /// This method replaces tiles at the seal locations with the appropriate terrain type based on the seal type:
        /// - <see cref="SealType.Blocked"/> locations get <see cref="BorderTerrain"/>
        /// - <see cref="SealType.Locked"/> and <see cref="SealType.Key"/> locations get <see cref="SealTerrain"/>
        /// - Locations with unbreakable terrain are skipped and left unchanged
        /// </remarks>
        /// <param name="map">The map context to place borders on.</param>
        /// <param name="sealList">A dictionary mapping locations to their seal types, determining which terrain to place.</param>
        protected override void PlaceBorders(T map, Dictionary<Loc, SealType> sealList)
        {
            foreach (Loc loc in sealList.Keys)
            {
                //Do nothing for unbreakables
                if (map.UnbreakableTerrain.TileEquivalent(map.GetTile(loc)))
                    continue;

                switch (sealList[loc])
                {
                    //lay down the blocks
                    case SealType.Blocked:
                        map.SetTile(loc, new Tile(BorderTerrain));
                        break;
                    case SealType.Locked:
                    case SealType.Key:
                        map.SetTile(loc, new Tile(SealTerrain));
                        break;
                }
            }
        }

    }
}

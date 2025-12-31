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
    /// Adds an extra room to the layout that can only be accessed by using a key item.
    /// This detour creates an inaccessible branch of the dungeon protected by a locked tile that requires a specific key item to unlock.
    /// </summary>
    /// <typeparam name="T">The map generation context type, must inherit from <see cref="BaseMapGenContext"/>.</typeparam>
    [Serializable]
    public class KeyDetourStep<T> : BaseDetourStep<T> where T : BaseMapGenContext
    {
        /// <summary>
        /// Gets or sets the tile ID used to lock the detour room, preventing access until the key item is obtained.
        /// </summary>
        [JsonConverter(typeof(TileConverter))]
        [DataType(0, DataManager.DataType.Tile, false)]
        public string LockedTile;

        /// <summary>
        /// Gets or sets the item ID required to unlock the detour room.
        /// When a character possesses this item, they can pass through the locked tile.
        /// </summary>
        [JsonConverter(typeof(ItemConverter))]
        [DataType(0, DataManager.DataType.Item, false)]
        public string KeyItem;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyDetourStep{T}"/> class with default values.
        /// The key item is initialized to an empty string.
        /// </summary>
        public KeyDetourStep()
        { KeyItem = ""; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyDetourStep{T}"/> class with the specified locked tile and key item.
        /// </summary>
        /// <param name="sealedTile">The tile ID used to lock the detour room.</param>
        /// <param name="keyItem">The item ID required to unlock the detour room.</param>
        public KeyDetourStep(string sealedTile, string keyItem) : this()
        {
            LockedTile = sealedTile;
            KeyItem = keyItem;
        }

        /// <inheritdoc/>
        public override void Apply(T map)
        {
            Grid.LocTest checkGround = (Loc testLoc) =>
            {
                return (!map.TileBlocked(testLoc) && !map.HasTileEffect(testLoc));
            };
            Grid.LocTest checkBlock = (Loc testLoc) =>
            {
                return map.WallTerrain.TileEquivalent(map.GetTile(testLoc));
            };

            List<LocRay4> rays = Detection.DetectWalls(((IViewPlaceableGenContext<MapGenEntrance>)map).GetLoc(0), new Rect(0, 0, map.Width, map.Height), checkBlock, checkGround);

            EffectTile effect = new EffectTile(LockedTile, true);
            TileListState state = new TileListState();
            effect.TileStates.Set(state);
            effect.TileStates.Set(new UnlockState(KeyItem));

            List<Loc> freeTiles = new List<Loc>();
            LocRay4? ray = PlaceRoom(map, rays, effect, freeTiles);

            if (ray != null)
                PlaceEntities(map, freeTiles);

        }

    }
}

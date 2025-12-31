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
    /// One part of several steps used to create a boss room.
    /// This step takes an already-placed boss room with an already-placed summoning tile
    /// and fills it with data on which tiles to lock down before summoning the boss.
    /// </summary>
    /// <typeparam name="T">The map generation context type that extends ListMapGenContext.</typeparam>
    [Serializable]
    public class BossSealStep<T> : BaseSealStep<T> where T : ListMapGenContext
    {
        /// <summary>
        /// The tile type used to seal the room borders.
        /// </summary>
        [JsonConverter(typeof(TileConverter))]
        [DataType(0, DataManager.DataType.Tile, false)]
        public string SealedTile;

        /// <summary>
        /// The tile type used to summon the boss battle.
        /// </summary>
        [JsonConverter(typeof(TileConverter))]
        [DataType(0, DataManager.DataType.Tile, false)]
        public string BossTile;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public BossSealStep()
        {
            BossFilters = new List<BaseRoomFilter>();
        }

        /// <summary>
        /// Initializes a new instance with the specified seal and boss tiles.
        /// </summary>
        /// <param name="sealedTile">The tile ID used to seal the room borders.</param>
        /// <param name="bossTile">The tile ID used to trigger the boss fight.</param>
        public BossSealStep(string sealedTile, string bossTile) : base()
        {
            SealedTile = sealedTile;
            BossTile = bossTile;
            BossFilters = new List<BaseRoomFilter>();
        }

        /// <summary>
        /// Gets or sets the filters used to identify the boss room for this sealing process.
        /// </summary>
        public List<BaseRoomFilter> BossFilters { get; set; }

        /// <summary>
        /// Places seal tiles around the boss room and sets up the boss effect event state.
        /// Finds the boss room using the configured filters, then seals the boundaries
        /// and locks certain tiles. The boss tile is linked to an OpenVaultEvent that
        /// unlocks the sealed tiles when triggered.
        /// </summary>
        /// <param name="map">The map generation context to modify.</param>
        /// <param name="sealList">A dictionary mapping locations to their seal types.
        /// Blocked seals are replaced with unbreakable terrain.
        /// Other seal types are locked with sealed tiles and tracked for the vault event.</param>
        /// <inheritdoc/>
        protected override void PlaceBorders(T map, Dictionary<Loc, SealType> sealList)
        {
            Rect? bossRect = null;

            for (int ii = 0; ii < map.RoomPlan.RoomCount; ii++)
            {
                FloorRoomPlan plan = map.RoomPlan.GetRoomPlan(ii);
                if (!BaseRoomFilter.PassesAllFilters(plan, this.BossFilters))
                    continue;
                bossRect = plan.RoomGen.Draw;
                break;
            }

            //if there's no way to open the door, there cannot be a door; give the player the treasure unguarded
            if (bossRect == null)
                return;

            EffectTile bossEffect = null;

            for (int xx = bossRect.Value.Start.X; xx < bossRect.Value.End.X; xx++)
            {
                for (int yy = bossRect.Value.Start.Y; yy < bossRect.Value.End.Y; yy++)
                {
                    Tile tile = (Tile)map.GetTile(new Loc(xx, yy));
                    if (tile.Effect.ID == BossTile)
                    {
                        bossEffect = tile.Effect;
                        break;
                    }
                }
                if (bossEffect != null)
                    break;
            }

            if (bossEffect == null)
                return;


            List<Loc> lockList = new List<Loc>();

            foreach (Loc loc in sealList.Keys)
            {
                switch (sealList[loc])
                {
                    case SealType.Blocked:
                        map.SetTile(loc, map.UnbreakableTerrain.Copy());
                        break;
                    default:
                        lockList.Add(loc);
                        break;
                }
            }

            foreach (Loc loc in lockList)
            {
                map.SetTile(loc, map.UnbreakableTerrain.Copy());
                EffectTile newEffect = new EffectTile(SealedTile, true, loc);
                ((IPlaceableGenContext<EffectTile>)map).PlaceItem(loc, newEffect);
            }

            ResultEventState resultEvent = new ResultEventState();
            resultEvent.ResultEvents.Add(new OpenVaultEvent(lockList));
            bossEffect.TileStates.Set(resultEvent);
        }

    }
}

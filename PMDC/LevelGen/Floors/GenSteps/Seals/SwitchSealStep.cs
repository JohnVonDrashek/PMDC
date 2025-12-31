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
    /// A floor generation step that creates switch-activated sealed rooms or vaults.
    /// This step surrounds target rooms with unbreakable walls and places one or more switch tiles
    /// that unlock them when activated. The switch placement is constrained by configurable room filters.
    /// </summary>
    /// <typeparam name="T">The context type that provides room and tile placement functionality. Must be a <see cref="ListMapGenContext"/>.</typeparam>
    [Serializable]
    public class SwitchSealStep<T> : BaseSealStep<T> where T : ListMapGenContext
    {
        /// <summary>
        /// The tile type that blocks off sealed rooms.
        /// These tiles are removed from the map when the corresponding switch is activated.
        /// </summary>
        [JsonConverter(typeof(TileConverter))]
        [DataType(0, DataManager.DataType.Tile, false)]
        public string SealedTile;

        /// <summary>
        /// The tile type that represents the switch mechanism.
        /// When stepped on or activated, this tile removes all sealed tiles in the linked vault.
        /// </summary>
        [JsonConverter(typeof(TileConverter))]
        [DataType(0, DataManager.DataType.Tile, false)]
        public string SwitchTile;

        /// <summary>
        /// The number of switch tiles to place during floor generation.
        /// If there are multiple switches, all must be activated to unlock the sealed vault.
        /// </summary>
        public int Amount;

        /// <summary>
        /// Indicates whether switch tiles should be visible to the player.
        /// When <c>true</c>, switches are revealed on the map. When <c>false</c>, they are hidden.
        /// </summary>
        public bool Revealed;

        /// <summary>
        /// Indicates whether activating the switch triggers a time limit on the current floor.
        /// When <c>true</c>, pressing the switch starts a countdown timer.
        /// </summary>
        public bool TimeLimit;

        /// <summary>
        /// A list of filters that determine which rooms are eligible for switch placement.
        /// Only rooms and halls that pass all filters can have switches placed in them.
        /// </summary>
        public List<BaseRoomFilter> SwitchFilters { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SwitchSealStep{T}"/> class with default values.
        /// </summary>
        public SwitchSealStep()
        {
            SwitchFilters = new List<BaseRoomFilter>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SwitchSealStep{T}"/> class with the specified configuration.
        /// </summary>
        /// <param name="sealedTile">The tile type to use for sealing off the vault or room.</param>
        /// <param name="switchTile">The tile type for the switch that unlocks the vault.</param>
        /// <param name="amount">The number of switch tiles to place. If greater than 1, all switches must be activated.</param>
        /// <param name="revealed">Whether the switch tiles should be visible on the map.</param>
        /// <param name="timeLimit">Whether activating the switch should trigger a time limit on the floor.</param>
        public SwitchSealStep(string sealedTile, string switchTile, int amount, bool revealed, bool timeLimit) : base()
        {
            SealedTile = sealedTile;
            SwitchTile = switchTile;
            Amount = amount;
            Revealed = revealed;
            TimeLimit = timeLimit;
            SwitchFilters = new List<BaseRoomFilter>();
        }

        /// <summary>
        /// Places sealed barriers and switch tiles on the floor to create locked vaults.
        /// This method:
        /// 1. Identifies rooms and halls eligible for switch placement based on filters.
        /// 2. Surrounds sealed rooms with unbreakable terrain.
        /// 3. Places the specified number of switches in eligible rooms, with each switch linked to all sealed tiles.
        /// 4. Configures switches with optional time limit and reveal settings.
        /// </summary>
        /// <param name="map">The floor generation context providing tile and room access.</param>
        /// <param name="sealList">A dictionary mapping tile locations to seal types, indicating which tiles should be sealed or locked.</param>
        /// <remarks>
        /// If no eligible rooms exist for switch placement, the method returns early and no seals are applied,
        /// allowing the floor to be generated without the sealed vault mechanic.
        /// Multiple switches placed in the same vault are tracked via a <see cref="TileReqListState"/> to ensure
        /// all switches must be pressed before the vault opens.
        /// </remarks>
        protected override void PlaceBorders(T map, Dictionary<Loc, SealType> sealList)
        {
            //all free tiles to place switches, by room
            List<List<Loc>> roomSwitchTiles = new List<List<Loc>>();

            for (int ii = 0; ii < map.RoomPlan.RoomCount; ii++)
            {
                FloorRoomPlan plan = map.RoomPlan.GetRoomPlan(ii);
                if (!BaseRoomFilter.PassesAllFilters(plan, this.SwitchFilters))
                    continue;
                List<Loc> freeTiles = ((IPlaceableGenContext<EffectTile>)map).GetFreeTiles(plan.RoomGen.Draw);
                if (freeTiles.Count > 0)
                    roomSwitchTiles.Add(freeTiles);
            }
            for (int ii = 0; ii < map.RoomPlan.HallCount; ii++)
            {
                FloorHallPlan plan = map.RoomPlan.GetHallPlan(ii);
                if (!BaseRoomFilter.PassesAllFilters(plan, this.SwitchFilters))
                    continue;
                List<Loc> freeTiles = ((IPlaceableGenContext<EffectTile>)map).GetFreeTiles(plan.RoomGen.Draw);
                if (freeTiles.Count > 0)
                    roomSwitchTiles.Add(freeTiles);
            }

            //if there's no way to open the door, there cannot be a door; give the player the treasure unguarded
            if (roomSwitchTiles.Count == 0)
                return;

            List <Loc> lockList = new List<Loc>();

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

            List<Loc> chosenLocs = new List<Loc>();
            for (int ii = 0; ii < Amount; ii++)
            {
                EffectTile switchTile = new EffectTile(SwitchTile, true);

                switchTile.TileStates.Set(new DangerState(TimeLimit));

                TileListState state = new TileListState();
                state.Tiles = lockList;
                switchTile.TileStates.Set(state);

                int randIndex = map.Rand.Next(roomSwitchTiles.Count);
                List<Loc> freeSwitchTiles = roomSwitchTiles[randIndex];

                int randTileIndex = map.Rand.Next(freeSwitchTiles.Count);
                chosenLocs.Add(map.Map.WrapLoc(freeSwitchTiles[randTileIndex]));

                freeSwitchTiles.RemoveAt(randTileIndex);

                //don't use this list anymore if it's empty
                //don't choose the same room for multiple switches
                if (freeSwitchTiles.Count == 0 || Amount - ii <= roomSwitchTiles.Count)
                    roomSwitchTiles.RemoveAt(randIndex);
            }

            foreach (Loc chosenLoc in chosenLocs)
            {
                EffectTile switchTile = new EffectTile(SwitchTile, Revealed);

                switchTile.TileStates.Set(new DangerState(TimeLimit));

                TileListState state = new TileListState();
                state.Tiles = lockList;
                switchTile.TileStates.Set(state);

                TileReqListState reqState = new TileReqListState();
                reqState.Tiles.AddRange(chosenLocs);
                switchTile.TileStates.Set(reqState);

                ((IPlaceableGenContext<EffectTile>)map).PlaceItem(chosenLoc, switchTile);
                map.GetPostProc(chosenLoc).Status |= (PostProcType.Panel | PostProcType.Item | PostProcType.Terrain);
            }
        }

    }
}

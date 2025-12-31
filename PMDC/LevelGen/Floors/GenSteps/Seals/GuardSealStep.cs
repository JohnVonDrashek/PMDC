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
    /// A floor generation step that creates sealed key rooms surrounded by unbreakable walls with guard monsters.
    /// When all guard monsters are defeated, the sealed room becomes accessible.
    /// </summary>
    /// <typeparam name="T">The map generation context type, must derive from <see cref="ListMapGenContext"/>.</typeparam>
    [Serializable]
    public class GuardSealStep<T> : BaseSealStep<T> where T : ListMapGenContext
    {
        /// <summary>
        /// Gets or sets the picker for selecting guard monster spawns that unlock the sealed room when defeated.
        /// </summary>
        public IMultiRandPicker<MobSpawn> Guards;

        /// <summary>
        /// Initializes a new instance of the <see cref="GuardSealStep{T}"/> class.
        /// </summary>
        public GuardSealStep()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GuardSealStep{T}"/> class with the specified guard picker.
        /// </summary>
        /// <param name="guards">The picker for selecting guard monster spawns.</param>
        public GuardSealStep(IMultiRandPicker<MobSpawn> guards) : base()
        {
            Guards = guards;
        }

        /// <summary>
        /// Places unbreakable walls around sealed rooms and spawns guard monsters at key positions.
        /// </summary>
        /// <param name="map">The map generation context containing the tiles and spawn information.</param>
        /// <param name="sealList">A dictionary mapping tile locations to their seal types (Blocked, Locked, or Key).</param>
        /// <inheritdoc/>
        protected override void PlaceBorders(T map, Dictionary<Loc, SealType> sealList)
        {
            List<Loc> guardLocList = new List<Loc>();

            foreach (Loc loc in sealList.Keys)
            {
                switch (sealList[loc])
                {
                    //lay down the blocks
                    case SealType.Blocked:
                        map.SetTile(loc, map.UnbreakableTerrain.Copy());
                        break;
                    case SealType.Locked:
                        {
                            if (!Grid.IsChokePoint(loc - Loc.One, Loc.One * 3, loc,
                                map.TileBlocked, (Loc testLoc) => { return true; }))
                                map.SetTile(loc, map.UnbreakableTerrain.Copy());
                        }
                        break;
                    case SealType.Key:
                        guardLocList.Add(loc);
                        break;
                }
            }

            List<MobSpawn> spawns = Guards.Roll(map.Rand);

            foreach (MobSpawn spawn in spawns)
            {
                Loc baseLoc = guardLocList[map.Rand.Next(guardLocList.Count)];
                Loc? destLoc = map.Map.GetClosestTileForChar(null, baseLoc);
                if (destLoc.HasValue)
                {
                    MonsterTeam team = new MonsterTeam();
                    Character newChar = spawn.Spawn(team, map);
                    ((IGroupPlaceableGenContext<TeamSpawn>)map).PlaceItems(new TeamSpawn(team, false), new Loc[1] { destLoc.Value });
                }
            }
        }

    }
}

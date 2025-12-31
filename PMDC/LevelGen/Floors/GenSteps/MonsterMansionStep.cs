using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Dungeon;
using RogueEssence;
using RogueEssence.LevelGen;
using PMDC.Dungeon;
using RogueEssence.Data;

namespace PMDC.LevelGen
{
    /// <summary>
    /// A monster house that consists of the entire floor, creating a mansion-style ambush.
    /// When it activates, all enemies become visible on the map and can see the player.
    /// This step treats the whole map as the monster house bounds.
    /// </summary>
    /// <typeparam name="T">The map generation context type.</typeparam>
    [Serializable]
    public class MonsterMansionStep<T> : MonsterHouseBaseStep<T> where T : ListMapGenContext
    {
        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MonsterMansionStep() : base() { }

        /// <summary>
        /// Copy constructor for cloning.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        public MonsterMansionStep(MonsterMansionStep<T> other) : base(other) { }

        /// <summary>
        /// Creates a new instance of this step by cloning the current instance.
        /// </summary>
        /// <returns>A new <see cref="MonsterMansionStep{T}"/> instance that is a copy of this instance.</returns>
        public override MonsterHouseBaseStep<T> CreateNew() { return new MonsterMansionStep<T>(this); }

        /// <summary>
        /// Applies the monster mansion step to the given map context.
        /// Converts the entire floor into a monster mansion by:
        /// 1. Identifying available item and mob spawn locations
        /// 2. Selecting random item and mob themes
        /// 3. Distributing themed items across the floor
        /// 4. Creating a monitor event that covers the entire map bounds
        /// 5. Spawning themed monsters and applying monster mansion status effects
        /// </summary>
        /// <param name="map">The map generation context to apply the step to.</param>
        public override void Apply(T map)
        {
            if (!ItemThemes.CanPick)
                return;

            if (!MobThemes.CanPick)
                return;

            Rect bounds = new Rect(0, 0, map.Width, map.Height);

            //determine the number of free tiles to put items on; trim the maximum item spawn accordingly (maximum <= 1/2 of free tiles)
            //determine the number of free tiles to put mobs on; trim the maximum mob spawn accordingly (maximum <= 1/2 of free tiles)
            List<Loc> itemTiles = new List<Loc>();
            int mobSpace = 0;
            for (int x = bounds.X; x < bounds.X + bounds.Size.X; x++)
            {
                for (int y = bounds.Y; y < bounds.Y + bounds.Size.Y; y++)
                {
                    Loc testLoc = new Loc(x, y);
                    if (!map.TileBlocked(testLoc))
                    {
                        if (!map.HasTileEffect(new Loc(x, y)) && (map.GetPostProc(testLoc).Status & (PostProcType.Panel | PostProcType.Item)) == PostProcType.None)
                        {
                            bool hasItem = false;
                            foreach (MapItem item in map.Items)
                            {
                                if (item.TileLoc == testLoc)
                                {
                                    hasItem = true;
                                    break;
                                }
                            }
                            if (!hasItem)
                                itemTiles.Add(testLoc);
                        }
                        bool hasMob = false;
                        foreach (Team team in map.AllyTeams)
                        {
                            foreach (Character testChar in team.EnumerateChars())
                            {
                                if (testChar.CharLoc == testLoc)
                                {
                                    hasMob = true;
                                    break;
                                }
                            }
                        }
                        foreach (Team team in map.MapTeams)
                        {
                            foreach (Character testChar in team.EnumerateChars())
                            {
                                if (testChar.CharLoc == testLoc)
                                {
                                    hasMob = true;
                                    break;
                                }
                            }
                        }
                        if (!hasMob)
                            mobSpace++;
                    }
                }
            }

            //choose which item theme to work with
            ItemTheme chosenItemTheme = ItemThemes.Pick(map.Rand);

            //the item spawn list in this class dictates the items available for spawning
            //it will be queried for items that match the theme selected
            List<MapItem> chosenItems = chosenItemTheme.GenerateItems(map, Items);
            
            //place the items
            for (int ii = 0; ii < chosenItems.Count; ii++)
            {
                if (itemTiles.Count > 0)
                {
                    MapItem item = new MapItem(chosenItems[ii]);
                    int randIndex = map.Rand.Next(itemTiles.Count);
                    ((IPlaceableGenContext<MapItem>)map).PlaceItem(itemTiles[randIndex], item);
                    itemTiles.RemoveAt(randIndex);
                }
            }



            //the mob theme will be selected randomly
            MobTheme chosenMobTheme = MobThemes.Pick(map.Rand);

            //the mobs in this class are the ones that would be available when the game wants to spawn things outside of the floor's spawn list
            //it will be queried for monsters that match the theme provided
            List<MobSpawn> chosenMobs = chosenMobTheme.GenerateMobs(map, Mobs);

            //cover the room in a check that holds all of the monsters, and covers the room's bounds
            CheckIntrudeBoundsEvent check = new CheckIntrudeBoundsEvent();
            check.Bounds = bounds;
            {
                RevealAllEvent reveal = new RevealAllEvent();
                check.Effects.Add(reveal);

                GiveMapStatusSingleEvent statusEvent = new GiveMapStatusSingleEvent("monster_mansion", 0);
                check.Effects.Add(statusEvent);

                MonsterHouseMapEvent house = new MonsterHouseMapEvent();
                house.Bounds = bounds;

                foreach (MobSpawn mob in chosenMobs)
                {
                    MobSpawn copyMob = mob.Copy();
                    if (map.Rand.Next(ALT_COLOR_ODDS) == 0)
                    {
                        SkinTableState table = DataManager.Instance.UniversalEvent.UniversalStates.GetWithDefault<SkinTableState>();
                        copyMob.BaseForm.Skin = table.AltColor;
                    }
                    house.Mobs.Add(copyMob);
                }
                check.Effects.Add(house);
            }

            AddIntrudeStep(map, check);
        }
    }

}

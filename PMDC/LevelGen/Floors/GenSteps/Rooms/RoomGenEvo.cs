using System;
using RogueElements;
using RogueEssence.Dungeon;
using System.Collections.Generic;
using RogueEssence;
using RogueEssence.LevelGen;
using RogueEssence.Dev;
using RogueEssence.Data;
using RogueEssence.Content;
using RogueEssence.Ground;
using PMDC.Dungeon;
using Newtonsoft.Json;

namespace PMDC.LevelGen
{
    /// <summary>
    /// THIS CLASS IS DEPRECATED. Use <see cref="RoomGenLoadEvo{T}"/> with a custom map using this shape instead.
    /// Generates a hardcoded evolution room layout of 7x6 in size with specific terrain patterns.
    /// The room features platform areas and wall placements optimized for evolution gameplay.
    /// </summary>
    /// <typeparam name="T">The context type, must implement <see cref="ITiledGenContext"/>,
    /// <see cref="IPostProcGenContext"/>, and <see cref="IPlaceableGenContext{EffectTile}"/>.</typeparam>
    [Serializable]
    public class RoomGenEvo<T> : RoomGen<T> where T : ITiledGenContext, IPostProcGenContext, IPlaceableGenContext<EffectTile>
    {
        //?#####?
        //.#---#.
        //..---..
        //.......
        //.......
        //.......

        /// <summary>
        /// The Y-axis offset where the main room area begins.
        /// </summary>
        const int ROOM_OFFSET = 1;

        /// <summary>
        /// The width of the evolution room in tiles.
        /// </summary>
        const int MAP_WIDTH = 7;

        /// <summary>
        /// The height of the evolution room in tiles.
        /// </summary>
        const int MAP_HEIGHT = 6;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoomGenEvo{T}"/> class.
        /// </summary>
        public RoomGenEvo() { }

        /// <inheritdoc/>
        public override RoomGen<T> Copy() { return new RoomGenEvo<T>(); }

        /// <inheritdoc/>
        public override Loc ProposeSize(IRandom rand)
        {
            return new Loc(MAP_WIDTH, MAP_HEIGHT);
        }

        /// <inheritdoc/>
        protected override void PrepareFulfillableBorders(IRandom rand)
        {
            if (Draw.Width != MAP_WIDTH || Draw.Height != MAP_HEIGHT)
            {
                foreach (Dir4 dir in DirExt.VALID_DIR4)
                {
                    for (int jj = 0; jj < FulfillableBorder[dir].Length; jj++)
                        FulfillableBorder[dir][jj] = true;
                }
            }
            else
            {
                for (int ii = 0; ii < Draw.Width; ii++)
                {
                    FulfillableBorder[Dir4.Up][ii] = ii == 0 || ii == Draw.Width-1;
                    FulfillableBorder[Dir4.Down][ii] = true;
                }

                for (int ii = 0; ii < Draw.Height; ii++)
                {
                    if (ii > 0)
                    {
                        FulfillableBorder[Dir4.Left][ii] = true;
                        FulfillableBorder[Dir4.Right][ii] = true;
                    }
                }
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Draws the evolution room with hardcoded layout including:
        /// - Room terrain filling the main area below the offset
        /// - Central evolution platform with specific dimensions
        /// - Wall placements at fixed locations
        /// - Border tunnels connecting to adjacent rooms based on connection requirements
        /// Falls back to default map drawing if room dimensions don't match expected size.
        /// </remarks>
        public override void DrawOnMap(T map)
        {
            if (MAP_WIDTH != Draw.Width || MAP_HEIGHT != Draw.Height)
            {
                DrawMapDefault(map);
                return;
            }


            for (int x = 0; x < Draw.Width; x++)
            {
                for (int y = ROOM_OFFSET; y < Draw.Height; y++)
                    map.SetTile(new Loc(Draw.X + x, Draw.Y + y), map.RoomTerrain.Copy());
            }
            int platWidth = 3;
            int platHeight = 2;
            Loc platStart = Draw.Start + new Loc(2, ROOM_OFFSET);
            map.PlaceItem(new Loc(platStart.X + 1, platStart.Y), new EffectTile("tile_evo", true));
            //TODO: when it's possible to specify the border digging, this entire class can be deprecated
            for (int x = 0; x < platWidth; x++)
            {
                for (int y = 0; y < platHeight; y++)
                {
                    map.GetPostProc(new Loc(platStart.X + x, platStart.Y + y)).Status |= PostProcType.Panel;
                    map.GetPostProc(new Loc(platStart.X + x, platStart.Y + y)).Status |= PostProcType.Terrain;
                }
            }
            map.SetTile(new Loc(Draw.X + 1, Draw.Y + ROOM_OFFSET), map.WallTerrain.Copy());
            map.GetPostProc(new Loc(Draw.X + 1, Draw.Y + ROOM_OFFSET)).Status |= PostProcType.Terrain;
            map.SetTile(new Loc(Draw.X + 5, Draw.Y + ROOM_OFFSET), map.WallTerrain.Copy());
            map.GetPostProc(new Loc(Draw.X + 5, Draw.Y + ROOM_OFFSET)).Status |= PostProcType.Terrain;


            //dig tunnels within this room to hook up to the incoming demands
            List<IntRange> upReq = RoomSideReqs[Dir4.Up];
            bool left = false;
            bool right = false;
            for (int ii = 0; ii < upReq.Count; ii++)
            {
                bool hasLeft = upReq[ii].Contains(Draw.Start.X) && BorderToFulfill[Dir4.Up][0];
                bool hasRight = upReq[ii].Contains(Draw.End.X - 1) && BorderToFulfill[Dir4.Up][Draw.Width - 1];
                if (hasLeft && hasRight)
                {
                    if (map.Rand.Next(2) == 0)
                        left = true;
                    else
                        right = true;
                }
                else
                {
                    left |= hasLeft;
                    right |= hasRight;
                }
            }
            if (left)
                DigAtBorder(map, Dir4.Up, Draw.Start.X);
            if (right)
                DigAtBorder(map, Dir4.Up, Draw.End.X - 1);

            SetRoomBorders(map);
        }

        /// <summary>
        /// Returns a formatted string representation of this room generator.
        /// </summary>
        /// <returns>The formatted type name of this room generator.</returns>
        public override string ToString()
        {
            return string.Format("{0}", this.GetType().GetFormattedTypeName());
        }
    }


    /// <summary>
    /// THIS CLASS IS DEPRECATED. Use <see cref="RoomGenLoadEvo{T}"/> with a custom map using this shape instead.
    /// Generates a smaller hardcoded evolution room layout of 5x6 in size.
    /// Similar to <see cref="RoomGenEvo{T}"/> but with a more compact design suitable for smaller dungeons.
    /// </summary>
    /// <typeparam name="T">The context type, must implement <see cref="ITiledGenContext"/>,
    /// <see cref="IPostProcGenContext"/>, and <see cref="IPlaceableGenContext{EffectTile}"/>.</typeparam>
    [Serializable]
    public class RoomGenEvoSmall<T> : PermissiveRoomGen<T> where T : ITiledGenContext, IPostProcGenContext, IPlaceableGenContext<EffectTile>
    {
        //.....
        //..#..
        //.---.
        //.---.
        //.#.#.
        //.....

        /// <summary>
        /// The width of the small evolution room in tiles.
        /// </summary>
        const int MAP_WIDTH = 5;

        /// <summary>
        /// The height of the small evolution room in tiles.
        /// </summary>
        const int MAP_HEIGHT = 6;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoomGenEvoSmall{T}"/> class.
        /// </summary>
        public RoomGenEvoSmall() { }

        /// <inheritdoc/>
        public override RoomGen<T> Copy() { return new RoomGenEvoSmall<T>(); }

        /// <inheritdoc/>
        public override Loc ProposeSize(IRandom rand)
        {
            return new Loc(MAP_WIDTH, MAP_HEIGHT);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Draws the small evolution room with hardcoded layout including:
        /// - Room terrain filling most of the space
        /// - Central evolution platform with specific dimensions
        /// - Wall placements at fixed locations for a more compact layout
        /// Falls back to default map drawing if room dimensions don't match expected size.
        /// </remarks>
        public override void DrawOnMap(T map)
        {
            if (MAP_WIDTH != Draw.Width || MAP_HEIGHT != Draw.Height)
            {
                DrawMapDefault(map);
                return;
            }

            for (int x = 0; x < Draw.Width; x++)
            {
                for (int y = 0; y < Draw.Height; y++)
                    map.SetTile(new Loc(Draw.X + x, Draw.Y + y), map.RoomTerrain.Copy());
            }
            int platWidth = 3;
            int platHeight = 2;
            Loc platStart = Draw.Start + new Loc(1, 2);
            map.PlaceItem(new Loc(platStart.X + 1, platStart.Y), new EffectTile("tile_evo", true));

            for (int x = 0; x < platWidth; x++)
            {
                for (int y = 0; y < platHeight; y++)
                {
                    map.GetPostProc(new Loc(platStart.X + x, platStart.Y + y)).Status |= PostProcType.Panel;
                    map.GetPostProc(new Loc(platStart.X + x, platStart.Y + y)).Status |= PostProcType.Terrain;
                }
            }
            map.SetTile(new Loc(Draw.X + 2, Draw.Y + 1), map.WallTerrain.Copy());
            map.GetPostProc(new Loc(Draw.X + 2, Draw.Y + 1)).Status |= PostProcType.Terrain;
            map.SetTile(new Loc(Draw.X + 1, Draw.Y + 4), map.WallTerrain.Copy());
            map.GetPostProc(new Loc(Draw.X + 1, Draw.Y + 4)).Status |= PostProcType.Terrain;
            map.SetTile(new Loc(Draw.X + 3, Draw.Y + 4), map.WallTerrain.Copy());
            map.GetPostProc(new Loc(Draw.X + 3, Draw.Y + 4)).Status |= PostProcType.Terrain;

            SetRoomBorders(map);
        }

        /// <summary>
        /// Returns a formatted string representation of this room generator.
        /// </summary>
        /// <returns>The formatted type name of this room generator.</returns>
        public override string ToString()
        {
            return string.Format("{0}", this.GetType().GetFormattedTypeName());
        }
    }



    /// <summary>
    /// Generates an evolution room by loading a custom map file as the room layout.
    /// Loads all content from a predefined map including tiles, items, enemies, and spawn points.
    /// Automatically configures borders based on walkable tiles and applies post-processing masks
    /// to non-standard terrain and the evolution platform area.
    /// </summary>
    /// <typeparam name="T">The context type, must inherit from <see cref="BaseMapGenContext"/>.</typeparam>
    [Serializable]
    public class RoomGenLoadEvo<T> : RoomGenLoadMapBase<T> where T : BaseMapGenContext
    {
        /// <summary>
        /// The width of the evolution platform in tiles.
        /// </summary>
        const int PLAT_WIDTH = 3;

        /// <summary>
        /// The height of the evolution platform in tiles.
        /// </summary>
        const int PLAT_HEIGHT = 2;

        /// <summary>
        /// The X-offset of the platform start position relative to the trigger tile.
        /// </summary>
        const int PLAT_START_X = -1;

        /// <summary>
        /// The Y-offset of the platform start position relative to the trigger tile.
        /// </summary>
        const int PLAT_START_Y = 0;

        /// <summary>
        /// The ID of the tile used to mark the evolution platform location.
        /// This tile ID is searched within the loaded map to identify where the
        /// evolution platform should be protected from terrain changes.
        /// </summary>
        [JsonConverter(typeof(TileConverter))]
        [DataType(0, DataManager.DataType.Tile, false)]
        public string TriggerTile;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoomGenLoadEvo{T}"/> class.
        /// </summary>
        public RoomGenLoadEvo()
        {
            TriggerTile = "";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoomGenLoadEvo{T}"/> class by copying another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected RoomGenLoadEvo(RoomGenLoadEvo<T> other) : base(other)
        {
            this.TriggerTile = other.TriggerTile;
        }

        /// <inheritdoc/>
        public override RoomGen<T> Copy() { return new RoomGenLoadEvo<T>(this); }

        /// <inheritdoc/>
        /// <remarks>
        /// Draws the evolution room from the loaded map. Applies all map content and then:
        /// - Marks all non-standard terrain with post-processing restrictions
        /// - Marks the 3x2 evolution platform area around the trigger tile with post-processing restrictions
        /// Falls back to default map drawing if room dimensions don't match the loaded map size.
        /// </remarks>
        public override void DrawOnMap(T map)
        {
            if (this.Draw.Width != this.roomMap.Width || this.Draw.Height != this.roomMap.Height)
            {
                this.DrawMapDefault(map);
                return;
            }

            //no copying is needed here since the map is disposed of after use

            DrawTiles(map);

            DrawDecorations(map);

            DrawItems(map);

            DrawMobs(map);

            DrawEntrances(map);

            this.FulfillRoomBorders(map, false);

            this.SetRoomBorders(map);

            for (int xx = 0; xx < Draw.Width; xx++)
            {
                for (int yy = 0; yy < Draw.Height; yy++)
                {
                    if (this.roomMap.Tiles[xx][yy].Data.ID != DataManager.Instance.GenFloor)
                        map.GetPostProc(new Loc(Draw.X + xx, Draw.Y + yy)).AddMask(new PostProcTile(PreventChanges));
                    if (this.roomMap.Tiles[xx][yy].Effect.ID == TriggerTile)
                    {
                        for (int x2 = 0; x2 < PLAT_WIDTH; x2++)
                        {
                            for (int y2 = 0; y2 < PLAT_HEIGHT; y2++)
                            {
                                Loc dest = new Loc(xx + PLAT_START_X + x2, yy + PLAT_START_Y + y2);
                                map.GetPostProc(Draw.Start + dest).AddMask(new PostProcTile(PreventChanges));
                            }
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Prepares the borders that can be modified for connecting to adjacent rooms.
        /// Uses the loaded map's tile data to determine where connections can be made:
        /// - First, marks all walkable floor tiles as potential connection points
        /// - Then, marks non-unbreakable tiles as backup connection points for borders that have no walkable options
        /// This two-pass approach ensures at least some connection possibility for each border.
        /// Note: Because the context is unavailable during border preparation, the tile ID
        /// representing an opening (walkable floor) must be predefined as GenFloor.
        /// </remarks>
        protected override void PrepareFulfillableBorders(IRandom rand)
        {
            // NOTE: Because the context is not passed in when preparing borders,
            // the tile ID representing an opening must be specified on this class instead.
            if (this.Draw.Width != this.roomMap.Width || this.Draw.Height != this.roomMap.Height)
            {
                foreach (Dir4 dir in DirExt.VALID_DIR4)
                {
                    for (int jj = 0; jj < this.FulfillableBorder[dir].Length; jj++)
                        this.FulfillableBorder[dir][jj] = true;
                }
            }
            else
            {
                HashSet<Dir4> satisfiedBorders = new HashSet<Dir4>();
                for (int ii = 0; ii < this.Draw.Width; ii++)
                {
                    if (this.roomMap.Tiles[ii][0].Data.ID == DataManager.Instance.GenFloor)
                    {
                        this.FulfillableBorder[Dir4.Up][ii] = true;
                        satisfiedBorders.Add(Dir4.Up);
                    }
                    if (this.roomMap.Tiles[ii][this.Draw.Height - 1].Data.ID == DataManager.Instance.GenFloor)
                    {
                        this.FulfillableBorder[Dir4.Down][ii] = true;
                        satisfiedBorders.Add(Dir4.Down);
                    }
                }

                for (int ii = 0; ii < this.Draw.Height; ii++)
                {
                    if (this.roomMap.Tiles[0][ii].Data.ID == DataManager.Instance.GenFloor)
                    {
                        this.FulfillableBorder[Dir4.Left][ii] = true;
                        satisfiedBorders.Add(Dir4.Left);
                    }
                    if (this.roomMap.Tiles[this.Draw.Width - 1][ii].Data.ID == DataManager.Instance.GenFloor)
                    {
                        this.FulfillableBorder[Dir4.Right][ii] = true;
                        satisfiedBorders.Add(Dir4.Right);
                    }
                }

                //backup plan: permit any borders that do not have unbreakables
                for (int ii = 0; ii < this.Draw.Width; ii++)
                {
                    if (!satisfiedBorders.Contains(Dir4.Up) && this.roomMap.Tiles[ii][0].Data.ID != DataManager.Instance.GenUnbreakable)
                        this.FulfillableBorder[Dir4.Up][ii] = true;

                    if (!satisfiedBorders.Contains(Dir4.Down) && this.roomMap.Tiles[ii][this.Draw.Height - 1].Data.ID != DataManager.Instance.GenUnbreakable)
                        this.FulfillableBorder[Dir4.Down][ii] = true;
                }

                for (int ii = 0; ii < this.Draw.Height; ii++)
                {
                    if (!satisfiedBorders.Contains(Dir4.Left) && this.roomMap.Tiles[0][ii].Data.ID != DataManager.Instance.GenUnbreakable)
                        this.FulfillableBorder[Dir4.Left][ii] = true;

                    if (!satisfiedBorders.Contains(Dir4.Right) && this.roomMap.Tiles[this.Draw.Width - 1][ii].Data.ID != DataManager.Instance.GenUnbreakable)
                        this.FulfillableBorder[Dir4.Right][ii] = true;
                }
            }
        }
    }
}

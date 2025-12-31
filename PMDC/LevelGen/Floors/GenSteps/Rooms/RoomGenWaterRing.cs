using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Dungeon;
using RogueEssence.LevelGen;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Generates a room containing a ring of water encircling treasure on a central island.
    /// </summary>
    /// <remarks>
    /// This room generator creates a layout with a central island surrounded by a water ring, with treasure items
    /// placed on the island. The room includes padding around the water ring and ensures the entire structure fits
    /// within the available space. If insufficient space exists, it falls back to default room generation.
    /// </remarks>
    /// <typeparam name="T">The context type, which must support post-processing, tiled generation, and item placement.</typeparam>
    [Serializable]
    public class RoomGenWaterRing<T> : PermissiveRoomGen<T> where T : IPostProcGenContext, ITiledGenContext, IPlaceableGenContext<MapItem>
    {
        /// <summary>
        /// The extra width of the room added to the area occupied by the water ring.
        /// </summary>
        /// <remarks>
        /// This value is randomly selected from the specified range and determines the padding to the left and right
        /// of the water ring structure. A minimum of 2 is enforced.
        /// </remarks>
        public RandRange PadWidth;

        /// <summary>
        /// The extra height of the room added to the area occupied by the water ring.
        /// </summary>
        /// <remarks>
        /// This value is randomly selected from the specified range and determines the padding above and below
        /// the water ring structure. A minimum of 2 is enforced.
        /// </remarks>
        public RandRange PadHeight;

        /// <summary>
        /// The number of items to spawn on the central island.
        /// </summary>
        /// <remarks>
        /// This value also determines the minimum size of the island, which is sized to accommodate at least this many items.
        /// </remarks>
        public int ItemAmount;

        /// <summary>
        /// The pool of treasure items that can be randomly selected and placed on the island.
        /// </summary>
        /// <remarks>
        /// Each spawn entry has an associated spawn rate, which determines the relative probability of selection
        /// when items are being placed.
        /// </remarks>
        public SpawnList<MapItem> Treasures;

        /// <summary>
        /// The terrain type used to render the water ring surrounding the island.
        /// </summary>
        public ITile WaterTerrain;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoomGenWaterRing{T}"/> class with default values.
        /// </summary>
        /// <remarks>
        /// The default constructor creates an empty treasure spawn list. All other properties should be set before drawing.
        /// </remarks>
        public RoomGenWaterRing()
        {
            Treasures = new SpawnList<MapItem>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoomGenWaterRing{T}"/> class by copying another instance.
        /// </summary>
        /// <remarks>
        /// This copy constructor creates a deep copy of the provided instance, including all treasures and the water terrain.
        /// </remarks>
        /// <param name="other">The instance to copy.</param>
        protected RoomGenWaterRing(RoomGenWaterRing<T> other)
        {
            PadWidth = other.PadWidth;
            PadHeight = other.PadHeight;
            ItemAmount = other.ItemAmount;
            Treasures = new SpawnList<MapItem>();
            for (int ii = 0; ii < other.Treasures.Count; ii++)
                Treasures.Add(new MapItem(other.Treasures.GetSpawn(ii)), other.Treasures.GetSpawnRate(ii));
            WaterTerrain = other.WaterTerrain.Copy();
        }

        /// <inheritdoc/>
        public override RoomGen<T> Copy() { return new RoomGenWaterRing<T>(this); }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoomGenWaterRing{T}"/> class with the specified parameters.
        /// </summary>
        /// <remarks>
        /// This constructor allows for complete initialization of the room generator with all necessary parameters.
        /// The treasure spawn list is initialized but remains empty; treasures can be added afterward.
        /// </remarks>
        /// <param name="waterTerrain">The terrain tile to use for the water ring surrounding the island.</param>
        /// <param name="padWidth">The extra width padding around the water ring structure.</param>
        /// <param name="padHeight">The extra height padding around the water ring structure.</param>
        /// <param name="itemAmount">The number of items to spawn on the island.</param>
        public RoomGenWaterRing(ITile waterTerrain, RandRange padWidth, RandRange padHeight, int itemAmount)
        {
            WaterTerrain = waterTerrain;
            PadWidth = padWidth;
            PadHeight = padHeight;
            ItemAmount = itemAmount;
            Treasures = new SpawnList<MapItem>();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// The proposed size includes the island size (calculated to fit the specified item count) plus a one-tile water ring
        /// on all sides, plus the random padding. The island is sized to be as square as possible while accommodating the
        /// required number of items.
        /// </remarks>
        public override Loc ProposeSize(IRandom rand)
        {
            Loc isleSize = new Loc(1);
            while (isleSize.X * isleSize.Y < ItemAmount)
            {
                if (isleSize.X > isleSize.Y)
                    isleSize.Y++;
                else
                    isleSize.X++;
            }
            Loc ringSize = isleSize + new Loc(2);
            Loc pad = new Loc(Math.Max(PadWidth.Pick(rand), 2), Math.Max(PadHeight.Pick(rand), 2));

            return new Loc(ringSize.X + pad.X, ringSize.Y + pad.Y);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This method draws the room layout as follows:
        /// 1. Fills the entire room with the default terrain
        /// 2. Randomly positions a water ring within the available space
        /// 3. Fills the interior of the ring (island) with default terrain
        /// 4. Randomly places the specified number of items on the island
        /// 5. Marks island tiles with Panel and Item post-processing flags
        ///
        /// If the room is too small to contain the required water ring with surrounding padding, it falls back
        /// to default room generation.
        /// </remarks>
        public override void DrawOnMap(T map)
        {
            Loc isleSize = new Loc(1);
            while (isleSize.X * isleSize.Y < ItemAmount)
            {
                if (isleSize.X > isleSize.Y)
                    isleSize.Y++;
                else
                    isleSize.X++;
            }

            //require at least a rectangle that can contain a ring of land around the ring of water
            if (isleSize.X + 4 > Draw.Size.X || isleSize.Y + 4 > Draw.Size.Y)
            {
                DrawMapDefault(map);
                return;
            }

            Loc ringSize = isleSize + new Loc(2);
            //size of room should be between size of cave + 2 and max
            for (int x = 0; x < Draw.Size.X; x++)
            {
                for (int y = 0; y < Draw.Size.Y; y++)
                    map.SetTile(new Loc(Draw.X + x, Draw.Y + y), map.RoomTerrain.Copy());
            }

            List<Loc> freeTiles = new List<Loc>();
            Loc blockStart = new Loc(Draw.X + 1 + map.Rand.Next(Draw.Size.X - ringSize.X - 1), Draw.Y + 1 + map.Rand.Next(Draw.Size.Y - ringSize.Y - 1));
            for (int x = 0; x < ringSize.X; x++)
            {
                for (int y = 0; y < ringSize.Y; y++)
                {
                    Loc targetLoc = new Loc(blockStart.X + x, blockStart.Y + y);
                    if (x == 0 || x == ringSize.X - 1 || y == 0 || y == ringSize.Y - 1)
                        map.SetTile(targetLoc, WaterTerrain.Copy());
                    else
                    {
                        freeTiles.Add(targetLoc);
                        map.GetPostProc(targetLoc).Status |= PostProcType.Panel;
                        map.GetPostProc(targetLoc).Status |= PostProcType.Item;
                    }
                }
            }
            if (Treasures.Count > 0)
            {
                for (int ii = 0; ii < ItemAmount; ii++)
                {
                    MapItem item = new MapItem(Treasures.Pick(map.Rand));
                    int randIndex = map.Rand.Next(freeTiles.Count);
                    map.PlaceItem(freeTiles[randIndex], item);
                    freeTiles.RemoveAt(randIndex);
                }
            }

            //hall restrictions
            SetRoomBorders(map);
        }
    }
}

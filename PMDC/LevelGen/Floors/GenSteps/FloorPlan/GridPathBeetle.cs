using System;
using RogueElements;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Populates the empty floor plan of a map by creating a beetle-shaped layout.
    /// This consists of one large central room (the body) with smaller rooms attached like legs.
    /// </summary>
    /// <typeparam name="T">The room grid generation context type.</typeparam>
    [Serializable]
    public class GridPathBeetle<T> : GridPathStartStepGeneric<T>
        where T : class, IRoomGridGenContext
    {
        /// <summary>
        /// Gets or sets a value indicating whether to use a horizontal or vertical orientation for the beetle body.
        /// When true, the body extends vertically; when false, it extends horizontally.
        /// </summary>
        public bool Vertical;

        /// <summary>
        /// Gets or sets the percentage chance (0-100) for each position to spawn a leg room attached to the main body.
        /// Higher values result in more leg rooms being created.
        /// </summary>
        public int LegPercent;

        /// <summary>
        /// Gets or sets the percentage chance (0-100) for adjacent leg rooms to be connected to each other.
        /// This creates cross-connections between neighboring legs, adding complexity to the layout.
        /// </summary>
        public int ConnectPercent;

        /// <summary>
        /// Gets or sets a value indicating whether the main body can be placed in a corner instead of being centered.
        /// When true, the body can spawn at either end of the available space; when false, it spawns in the middle.
        /// </summary>
        public bool FromCorners;

        /// <summary>
        /// Gets or sets the room types that can be used for the giant central room in the layout.
        /// If no giant hall generator is specified, falls back to generic rooms.
        /// </summary>
        public SpawnList<RoomGen<T>> GiantHallGen;

        /// <summary>
        /// Gets or sets the components that the giant central room will be labeled with.
        /// These components define special properties or behaviors of the large body room.
        /// </summary>
        public ComponentCollection LargeRoomComponents { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridPathBeetle{T}"/> class with default values.
        /// </summary>
        public GridPathBeetle()
            : base()
        {
            GiantHallGen = new SpawnList<RoomGen<T>>();
            LargeRoomComponents = new ComponentCollection();
        }

        /// <inheritdoc/>
        public override void ApplyToPath(IRandom rand, GridPlan floorPlan)
        {
            int gapLength = Vertical ? floorPlan.GridHeight : floorPlan.GridWidth;
            int sideLength = Vertical ? floorPlan.GridWidth : floorPlan.GridHeight;

            if (gapLength < 3 || sideLength < 2)
            {
                CreateErrorPath(rand, floorPlan);
                return;
            }

            //add the body
            int chosenTier = FromCorners ? (rand.Next(2) * gapLength - 1) : rand.Next(1, gapLength - 1);

            RoomGen<T> roomGen = GiantHallGen.Pick(rand);
            if (roomGen == null)
                roomGen = GenericRooms.Pick(rand);
            floorPlan.AddRoom(new Rect(Vertical ? 0 : chosenTier, Vertical ? chosenTier : 0, Vertical ? sideLength : 1, Vertical ? 1 : sideLength), roomGen, this.LargeRoomComponents.Clone());

            GenContextDebug.DebugProgress("Center Room");

            //add the legs
            for (int ii = 0; ii < sideLength; ii++)
            {
                if (chosenTier > 0)
                {
                    if (rand.Next(100) < LegPercent)
                    {
                        int roomTier = rand.Next(0, chosenTier);
                        floorPlan.AddRoom(new Loc(Vertical ? ii : roomTier, Vertical ? roomTier : ii), GenericRooms.Pick(rand), this.RoomComponents.Clone());
                        for(int jj = roomTier; jj < chosenTier; jj++)
                            SafeAddHall(new LocRay4(new Loc(Vertical ? ii : jj, Vertical ? jj : ii), Vertical ? Dir4.Down : Dir4.Right),
                                floorPlan, GenericHalls.Pick(rand), GetDefaultGen(), this.RoomComponents, this.HallComponents, true);

                        GenContextDebug.DebugProgress("Add Leg");

                        int hasRoom = -1;
                        for (int jj = ii - 1; jj >= 0; jj--)
                        {
                            if (floorPlan.GetRoomPlan(new Loc(Vertical ? jj : roomTier, Vertical ? roomTier : jj)) != null)
                            {
                                hasRoom = jj;
                                break;
                            }
                        }
                        if (ii > 0 && hasRoom > -1)
                        {
                            if (rand.Next(100) < ConnectPercent)
                            {
                                for (int jj = ii; jj > hasRoom; jj--)
                                {
                                    SafeAddHall(new LocRay4(new Loc(Vertical ? jj : roomTier, Vertical ? roomTier : jj), Vertical ? Dir4.Left : Dir4.Up),
                                        floorPlan, GenericHalls.Pick(rand), GetDefaultGen(), this.RoomComponents, this.HallComponents, true);

                                    GenContextDebug.DebugProgress("Connect Leg");
                                }
                            }
                        }
                    }
                }
                if (chosenTier < gapLength - 1)
                {
                    if (rand.Next(100) < LegPercent)
                    {
                        int roomTier = rand.Next(chosenTier + 1, gapLength);
                        floorPlan.AddRoom(new Loc(Vertical ? ii : roomTier, Vertical ? roomTier : ii), GenericRooms.Pick(rand), this.RoomComponents.Clone());
                        for (int jj = chosenTier; jj < roomTier; jj++)
                            SafeAddHall(new LocRay4(new Loc(Vertical ? ii : jj, Vertical ? jj : ii), Vertical ? Dir4.Down : Dir4.Right),
                                floorPlan, GenericHalls.Pick(rand), GetDefaultGen(), this.RoomComponents, this.HallComponents, true);

                        GenContextDebug.DebugProgress("Add Leg");

                        int hasRoom = -1;
                        for (int jj = ii - 1; jj >= 0; jj--)
                        {
                            if (floorPlan.GetRoomPlan(new Loc(Vertical ? jj : roomTier, Vertical ? roomTier : jj)) != null)
                            {
                                hasRoom = jj;
                                break;
                            }
                        }
                        if (ii > 0 && hasRoom > -1)
                        {
                            if (rand.Next(100) < ConnectPercent)
                            {
                                for (int jj = ii; jj > hasRoom; jj--)
                                {
                                    SafeAddHall(new LocRay4(new Loc(Vertical ? jj : roomTier, Vertical ? roomTier : jj), Vertical ? Dir4.Left : Dir4.Up),
                                        floorPlan, GenericHalls.Pick(rand), GetDefaultGen(), this.RoomComponents, this.HallComponents, true);

                                    GenContextDebug.DebugProgress("Connect Leg");
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns a string representation of this floor plan generator's configuration.
        /// Includes orientation, leg spawn percentage, and connection percentage.
        /// </summary>
        /// <returns>A formatted string describing the beetle layout parameters.</returns>
        public override string ToString()
        {
            return string.Format("{0}: Vert:{1} Leg:{2}% Connect:{3}%", this.GetType().GetFormattedTypeName(), this.Vertical, this.LegPercent, this.ConnectPercent);
        }
    }
}

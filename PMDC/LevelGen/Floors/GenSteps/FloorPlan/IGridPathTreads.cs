using System;
using System.Collections.Generic;
using RogueElements;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Interface for grid path generators that create tread-style layouts with large rooms on the sides.
    /// </summary>
    public interface IGridPathTreads
    {
        /// <summary>
        /// Gets or sets a value indicating whether the layout is oriented vertically (large rooms on top/bottom) or horizontally (large rooms on left/right).
        /// </summary>
        bool Vertical { get; set; }

        /// <summary>
        /// Gets or sets the range of small room spawn density as a percentage of possible room locations.
        /// </summary>
        RandRange RoomPercent { get; set; }

        /// <summary>
        /// Gets or sets the range of hallway connections between adjacent small rooms as a percentage of possible connections.
        /// </summary>
        RandRange ConnectPercent { get; set; }

        /// <summary>
        /// Gets or sets the components that will be applied to the large "tread" rooms.
        /// </summary>
        ComponentCollection LargeRoomComponents { get; set; }
    }
    
    /// <summary>
    /// Creates a grid plan with two large "tread" rooms along the sides and a set of rooms in the middle.
    /// Inverse of GridPathBeetle. The large rooms form the main pathways, with smaller rooms branching off between them.
    /// </summary>
    /// <typeparam name="T">The context type implementing <see cref="IRoomGridGenContext"/> for room grid generation.</typeparam>
    [Serializable]
    public class GridPathTreads<T> : GridPathStartStepGeneric<T>, IGridPathTreads
        where T : class, IRoomGridGenContext
    {
        /// <summary>
        /// Gets or sets a value indicating whether the layout is oriented vertically (large rooms on top/bottom) or horizontally (large rooms on left/right).
        /// </summary>
        public bool Vertical { get; set; }

        /// <summary>
        /// Gets or sets the range for small room spawn density as a percentage of possible room locations (0-100).
        /// </summary>
        public RandRange RoomPercent { get; set; }

        /// <summary>
        /// Gets or sets the range for hallway connections between adjacent small rooms as a percentage of possible connections (0-100).
        /// </summary>
        public RandRange ConnectPercent { get; set; }

        /// <summary>
        /// Gets or sets the weighted spawn list of room generators used to create the two large tread rooms.
        /// </summary>
        public SpawnList<RoomGen<T>> GiantRoomsGen;

        /// <summary>
        /// Gets or sets the components that will be applied to the large tread rooms.
        /// </summary>
        public ComponentCollection LargeRoomComponents { get; set; }
        

        /// <summary>
        /// Initializes a new instance of the <see cref="GridPathTreads{T}"/> class.
        /// </summary>
        public GridPathTreads()
            : base()
        {
            GiantRoomsGen = new SpawnList<RoomGen<T>>();
            LargeRoomComponents = new ComponentCollection();
        }

        /// <inheritdoc/>
        public override void ApplyToPath(IRandom rand, GridPlan floorPlan)
        {
            int mainLength = Vertical ? floorPlan.GridHeight : floorPlan.GridWidth;
            int sideLength = Vertical ? floorPlan.GridWidth : floorPlan.GridHeight;

            if (mainLength < 3 || sideLength < 2)
            {
                CreateErrorPath(rand, floorPlan);
                return;
            }

            //add the two edge rooms
            int firstRoomIndex = 0;
            int secondRoomIndex = mainLength - 1;

            RoomGen<T> roomGen1 = GiantRoomsGen.Pick(rand);
            if (roomGen1 == null)
                roomGen1 = GenericRooms.Pick(rand);
            floorPlan.AddRoom(new Rect(Vertical ? 0 : firstRoomIndex, Vertical ? firstRoomIndex : 0, Vertical ? sideLength : 1, Vertical ? 1 : sideLength), roomGen1, this.LargeRoomComponents.Clone());

            
            RoomGen<T> roomGen2 = GiantRoomsGen.Pick(rand);
            if (roomGen2 == null)
                roomGen2 = GenericRooms.Pick(rand);
            floorPlan.AddRoom(new Rect(Vertical ? 0 : secondRoomIndex, Vertical ? secondRoomIndex : 0, Vertical ? sideLength : 1, Vertical ? 1 : sideLength), roomGen2, this.LargeRoomComponents.Clone());
            
            GenContextDebug.DebugProgress("Side Rooms");

            //add the rooms in the middle
            SpawnList<Loc> possibleRoomLocs = new SpawnList<Loc>(true);
            List<Loc> rooms = new List<Loc>();

            for (int i = 1; i < mainLength - 1; i++)
            {
                for (int j = 0; j < sideLength; j++)
                {
                    possibleRoomLocs.Add(Vertical ? new Loc(j, i) : new Loc(i, j), 1);
                }
            }

            int numMiddleRooms = (int)(possibleRoomLocs.Count * (RoomPercent.Pick(rand) / 100f));

            if (numMiddleRooms < 1)
            {
                numMiddleRooms = 1;
            }

            for (int i = 0; i < numMiddleRooms; i++)
            {
                rooms.Add(possibleRoomLocs.Pick(rand));
            }
            
            foreach (Loc room in rooms)
            {
                floorPlan.AddRoom(room, GenericRooms.Pick(rand), this.RoomComponents.Clone());
            }

            //Make hallway connections after all rooms have spawned
            foreach (Loc room in rooms)
            {
                AddHallwayConnections(rand, floorPlan, room, mainLength);
            }
            
            //Add additional side hallway connections based on the component connection
            AddAdditionalHallwayConnections(rand, floorPlan, rooms);
        }

        /// <summary>
        /// Returns a string representation of the grid path tread configuration.
        /// </summary>
        /// <returns>A formatted string containing the class name, orientation, room percentage, and connection percentage.</returns>
        public override string ToString()
        {
            return string.Format("{0}: Vert:{1} Room:{2}% Connect:{3}%", this.GetType().GetFormattedTypeName(), this.Vertical, this.RoomPercent, this.ConnectPercent);
        }

        /// <summary>
        /// Creates hallway connections from a middle room upward and downward to the large tread rooms.
        /// Attempts to connect the specified room to both the top and bottom tread rooms via hallways.
        /// </summary>
        /// <param name="rand">The random number generator for probabilistic room/hallway generation.</param>
        /// <param name="floorPlan">The grid plan to modify with hallway connections.</param>
        /// <param name="room">The middle room location to create connections from.</param>
        /// <param name="mainLength">The length of the main axis (height if vertical, width if horizontal).</param>
        protected void AddHallwayConnections(IRandom rand, GridPlan floorPlan, Loc room, int mainLength)
        {
            int roomTier = Vertical ? room.Y : room.X;
            int roomSideIndex = Vertical ? room.X : room.Y;
            
            //Connect up
            int hasRoom = -1;
            for (int jj = roomTier - 1; jj >= 0; jj--)
            {
                if (floorPlan.GetRoomPlan(new Loc(Vertical ? roomSideIndex : jj, Vertical ? jj : roomSideIndex)) != null)
                {
                    hasRoom = jj;
                    break;
                }
            }
            if (roomTier > 0 && hasRoom > -1)
            {
                for (int jj = roomTier; jj > hasRoom; jj--)
                {
                    Loc curLoc = new Loc(Vertical ? roomSideIndex : jj, Vertical ? jj : roomSideIndex);

                    if (jj != roomTier && floorPlan.GetRoomPlan(curLoc) == null)
                    {
                        floorPlan.AddRoom(curLoc,this.GenericHalls.Pick(rand), this.HallComponents.Clone(), true);
                    }
                    SafeAddHall(new LocRay4(curLoc, Vertical ? Dir4.Up : Dir4.Left), 
                        floorPlan, GenericHalls.Pick(rand), GetDefaultGen(), this.RoomComponents, this.HallComponents,
                        true);
                }
            }
            
            GenContextDebug.DebugProgress("Connect Leg Up");
            
            //Connect down with the bottom room if possible
            
             hasRoom = -1;
            for (int jj = roomTier + 1; jj < mainLength; jj++)
            {
                if (floorPlan.GetRoomPlan(new Loc(Vertical ? roomSideIndex : jj, Vertical ? jj : roomSideIndex)) != null)
                {
                    hasRoom = jj;
                    break;
                }
            }
            if (roomTier > 0 && hasRoom == (mainLength - 1))
            {
                for (int jj = roomTier; jj < hasRoom; jj++)
                {
                    Loc curLoc = new Loc(Vertical ? roomSideIndex : jj, Vertical ? jj : roomSideIndex);

                    if (jj != roomTier && floorPlan.GetRoomPlan(curLoc) == null)
                    {
                        floorPlan.AddRoom(curLoc,this.GenericHalls.Pick(rand), this.HallComponents.Clone(), true);
                    }

                    SafeAddHall(new LocRay4(curLoc, Vertical ? Dir4.Down : Dir4.Right), 
                        floorPlan, GenericHalls.Pick(rand), GetDefaultGen(), this.RoomComponents, this.HallComponents,
                        true);

                }
            }
            
            GenContextDebug.DebugProgress("Connect Leg Down");
        }

        /// <summary>
        /// Creates additional lateral hallway connections between middle rooms based on the connection percentage.
        /// Rooms that can reach a neighboring room on the opposite side will have side hallways connecting them.
        /// </summary>
        /// <param name="rand">The random number generator for probabilistic selection of connections.</param>
        /// <param name="floorPlan">The grid plan to modify with additional hallway connections.</param>
        /// <param name="rooms">The list of middle room locations to consider for side connections.</param>
        protected void AddAdditionalHallwayConnections(IRandom rand, GridPlan floorPlan, List<Loc> rooms)
        {
            //This stores a list of locs that can generate a left or up migration for a side hallway connection.
            SpawnList<Tuple<Loc, int>> possibleSideHallwaySources = new SpawnList<Tuple<Loc, int>>(true);
            List<Tuple<Loc, int>> sideHallwaySources = new List<Tuple<Loc, int>>();
            
            foreach (Loc room in rooms)
            {
                int hasRoom = -1;
                int roomTier = Vertical ? room.Y : room.X;
                int roomSideIndex = Vertical ? room.X : room.Y;

                if (roomSideIndex == 0)
                {
                    //Do not try to form connections from the leftmost rooms
                    continue;
                }
                
                for (int i = roomSideIndex - 1; i >= 0; i--)
                {
                    if (floorPlan.GetRoomPlan(new Loc(Vertical ? i : roomTier, Vertical ? roomTier : i)) !=
                        null)
                    {
                        hasRoom = i;
                        break;
                    }
                }

                if (hasRoom > -1)
                {
                    possibleSideHallwaySources.Add(new Tuple<Loc, int>(room, hasRoom), 1);
                }
            }

            int totalNumPossibleConnections = possibleSideHallwaySources.Count;

            int numConnections = (int)(totalNumPossibleConnections * (ConnectPercent.Pick(rand) / 100f));

            for (int i = 0; i < numConnections; i++)
            {
                sideHallwaySources.Add(possibleSideHallwaySources.Pick(rand));
            }
            
            foreach (Tuple<Loc, int> sideHallwaySource in sideHallwaySources)
            {
                Loc sourceLoc = sideHallwaySource.Item1;
                int targetSideIndex = sideHallwaySource.Item2;
                
                int sourceRoomTier = Vertical ? sourceLoc.Y : sourceLoc.X;
                int sourceRoomSideIndex = Vertical ? sourceLoc.X : sourceLoc.Y;
                
                for (int jj = sourceRoomSideIndex; jj > targetSideIndex; jj--)
                {
                    Loc curLoc = new Loc(Vertical ? jj : sourceRoomTier, Vertical ? sourceRoomTier : jj);

                    if (jj != sourceRoomSideIndex && floorPlan.GetRoomPlan(curLoc) == null)
                    {
                        floorPlan.AddRoom(curLoc,this.GenericHalls.Pick(rand), this.HallComponents.Clone(), true);
                    }
                    SafeAddHall(new LocRay4(curLoc, Vertical ? Dir4.Left : Dir4.Up), 
                        floorPlan, GenericHalls.Pick(rand), GetDefaultGen(), this.RoomComponents, this.HallComponents,
                        true);
                }
                GenContextDebug.DebugProgress("Connect Side Hallway");
            }
        }
    }
}
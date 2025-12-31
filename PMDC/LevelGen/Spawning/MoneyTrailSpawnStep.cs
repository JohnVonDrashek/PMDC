// <copyright file="MoneyTrailSpawnStep.cs" company="Audino">
// Copyright (c) Audino
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.LevelGen;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Spawns money in a trail leading up to an item at the endpoint.
    /// </summary>
    /// <remarks>
    /// This step distributes spawnable items across selected rooms and creates money trails
    /// leading away from those items. Each trail moves in a random direction with bouncing off
    /// map edges, spawning progressively more valuable money as it approaches the endpoint.
    /// </remarks>
    /// <typeparam name="TGenContext">The generation context type, must support room grid operations, item placement, and money spawning.</typeparam>
    /// <typeparam name="TSpawnable">The type of spawnable item.</typeparam>
    [Serializable]
    public class MoneyTrailSpawnStep<TGenContext, TSpawnable> : RoomSpawnStep<TGenContext, TSpawnable>
        where TGenContext : class, IRoomGridGenContext, IPlaceableGenContext<TSpawnable>, IPlaceableGenContext<MoneySpawn>, ISpawningGenContext<MoneySpawn>
        where TSpawnable : ISpawnable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MoneyTrailSpawnStep{TGenContext, TSpawnable}"/> class with default values.
        /// </summary>
        public MoneyTrailSpawnStep()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MoneyTrailSpawnStep{TGenContext, TSpawnable}"/> class with specified parameters.
        /// </summary>
        /// <param name="spawn">The spawner responsible for picking items to place.</param>
        /// <param name="trailLength">The range of lengths for money trails in tiles.</param>
        /// <param name="placementValue">The range of money values to place at each position along the trail.</param>
        public MoneyTrailSpawnStep(IStepSpawner<TGenContext, TSpawnable> spawn, RandRange trailLength, IntRange placementValue)
            : base(spawn)
        {
            this.TrailLength = trailLength;
            this.PlacementValue = placementValue;
        }

        /// <summary>
        /// The length of the money trail in tiles.
        /// </summary>
        public RandRange TrailLength;

        /// <summary>
        /// The range of money values to place at each tile, interpolating from min to max along the trail.
        /// </summary>
        public IntRange PlacementValue;

        /// <summary>
        /// Distributes the spawnable items across randomly selected rooms and creates money trails for each.
        /// </summary>
        /// <remarks>
        /// This method attempts to place each spawnable item in a random room that passes the configured filters,
        /// then creates money trails from those rooms using a proportional share of the total available money budget.
        /// </remarks>
        /// <param name="map">The map generation context containing the grid plan and random number generator.</param>
        /// <param name="spawns">The list of spawnable items to distribute across rooms.</param>
        public override void DistributeSpawns(TGenContext map, List<TSpawnable> spawns)
        {
            List<int> spawnedRooms = new List<int>();
            // Choose randomly
            for (int nn = 0; nn < spawns.Count; nn++)
            {
                for (int ii = 0; ii < 10; ii++)
                {
                    int randIdx = map.Rand.Next(map.GridPlan.RoomCount);
                    if (!BaseRoomFilter.PassesAllFilters(map.GridPlan.GetRoomPlan(randIdx), this.Filters))
                        continue;

                    if (spawnItemInRoom(map, randIdx, spawns[nn]))
                    {
                        spawnedRooms.Add(randIdx);
                        break;
                    }
                }
            }


            MoneySpawn total = map.Spawner.Pick(map.Rand);
            int chosenDiv = Math.Min(total.Amount, Math.Max(1, spawnedRooms.Count));
            int avgAmount = total.Amount / chosenDiv;
            for (int ii = 0; ii < chosenDiv; ii++)
            {
                int budget = avgAmount;
                while (budget > 0)
                    this.spawnWithTrail(map, spawnedRooms[ii], map.Rand.Next(360), ref budget);
            }
        }

        /// <summary>
        /// Creates a money trail starting from a specific room and moving in a random direction.
        /// </summary>
        /// <remarks>
        /// This method spawns money along a trail path, with values interpolating from the minimum to maximum
        /// PlacementValue range. The trail moves forward in the specified direction with random bouncing off
        /// map edges. The total cost is deducted from the money budget.
        /// </remarks>
        /// <param name="map">The map generation context.</param>
        /// <param name="startIdx">The index of the starting room.</param>
        /// <param name="startDegrees">The initial direction in degrees (clockwise from down).</param>
        /// <param name="moneyToSpawn">Reference to the remaining money budget, updated after spawning.</param>
        private void spawnWithTrail(TGenContext map, int startIdx, int startDegrees, ref int moneyToSpawn)
        {
            int avgCost = (PlacementValue.Min + PlacementValue.Max) / 2;
            int allowedLength = moneyToSpawn / avgCost;
            int chosenLength = Math.Min(TrailLength.Pick(map.Rand), allowedLength);
            int totalCost = chosenLength * avgCost;
            if (moneyToSpawn - totalCost < avgCost)
                totalCost = moneyToSpawn;

            List<MoneySpawn> toSpawns = new List<MoneySpawn>();
            int currentCost = 0;
            for (int ii = 0; ii < chosenLength; ii++)
            {
                if (ii == chosenLength - 1)
                {
                    toSpawns.Add(new MoneySpawn(totalCost - currentCost));
                    currentCost = totalCost;
                }
                else
                {
                    int added = MathUtils.Interpolate(PlacementValue.Min, PlacementValue.Max, ii, chosenLength - 1);
                    toSpawns.Add(new MoneySpawn(added));
                    currentCost += added;
                }
            }

            int curRotation = startDegrees;
            Loc roomLoc = map.GridPlan.GetRoomPlan(startIdx).Bounds.Start;
            for (int ii = chosenLength - 1; ii >= 0; ii--)
            {
                //attempt to move forward and place 3 times
                bool spawned = false;
                for (int jj = 0; jj < 3; jj++)
                {
                    int roomIdx = map.GridPlan.GetRoomIndex(roomLoc);
                    if (roomIdx > -1)
                    {
                        GridRoomPlan curPlan = map.GridPlan.GetRoomPlan(roomIdx);
                        if (!curPlan.PreferHall)
                        {
                            if (spawnInMoneyRoom(map, roomIdx, toSpawns[ii]))
                                spawned = true;
                        }
                    }
                    //move forward
                    roomLoc = moveForward(map, roomLoc, ref curRotation);

                    if (spawned)
                        break;
                }
            }

            moneyToSpawn -= totalCost;
        }
        /// <summary>
        /// Moves the trail forward in the current rotation direction, bouncing off map edges.
        /// </summary>
        /// <param name="map">The map generation context.</param>
        /// <param name="inLoc">The current location.</param>
        /// <param name="degreeRotation">Rotation is treated as clockwise from down. Updated with random variation.</param>
        /// <returns>The new location after moving forward.</returns>
        private Loc moveForward(TGenContext map, Loc inLoc, ref int degreeRotation)
        {
            //move in the chosen direction
            Dir8 moveDir = getRandDirFromDegree(map.Rand, degreeRotation);

            Loc moveLoc = moveDir.GetLoc();
            //check against horizontal border cross
            if (inLoc.X + moveLoc.X < 0 || inLoc.X + moveLoc.X >= map.GridPlan.Size.X)
            {
                //if so, reverse the moveLoc X and reflect degreeRotation
                moveLoc.X = -moveLoc.X;
                degreeRotation = (360 * 2 - degreeRotation) % 360;
            }

            //check against vertical border cross
            if (inLoc.Y + moveLoc.Y < 0 || inLoc.Y + moveLoc.Y >= map.GridPlan.Size.Y)
            {
                //if so, reverse the moveLoc Y and reflect degreeRotation
                moveLoc.Y = -moveLoc.Y;
                degreeRotation = ((360 - (90 + degreeRotation)) % 360 + 270) % 360;
            }

            Loc outLoc = inLoc + moveLoc;

            //modify the rotation
            degreeRotation = (degreeRotation + map.Rand.Next(360-45, 360+45+1)) % 360;

            return outLoc;
        }

        /// <summary>
        /// Converts a degree rotation to a cardinal/diagonal direction with randomization.
        /// </summary>
        /// <param name="rand">Random number generator.</param>
        /// <param name="degreeRotation">Rotation in degrees, treated as clockwise from down.</param>
        /// <returns>The corresponding 8-way direction.</returns>
        private Dir8 getRandDirFromDegree(IRandom rand, int degreeRotation)
        {
            int dir = degreeRotation / 45;
            int remainder = degreeRotation % 45;
            if (rand.Next(45) < remainder)
                dir = (dir + 1) % 8;
            return (Dir8)dir;
        }



        /// <summary>
        /// Attempts to place a spawnable item in a random free tile within a room.
        /// </summary>
        /// <param name="map">The map generation context.</param>
        /// <param name="roomIdx">The index of the room to spawn the item in.</param>
        /// <param name="spawn">The spawnable item to place.</param>
        /// <returns>True if the item was successfully placed, false if no free tiles were available.</returns>
        private bool spawnItemInRoom(TGenContext map, int roomIdx, TSpawnable spawn)
        {
            IRoomGen room = map.GridPlan.GetRoom(roomIdx);
            List<Loc> freeTiles = ((IPlaceableGenContext<TSpawnable>)map).GetFreeTiles(room.Draw);

            if (freeTiles.Count > 0)
            {
                int randIndex = map.Rand.Next(freeTiles.Count);
                map.PlaceItem(freeTiles[randIndex], spawn);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to place money in a random free tile within a room.
        /// </summary>
        /// <param name="map">The map generation context.</param>
        /// <param name="roomIdx">The index of the room to spawn money in.</param>
        /// <param name="spawn">The money spawn object containing the amount to place.</param>
        /// <returns>True if the money was successfully placed, false if no free tiles were available.</returns>
        private bool spawnInMoneyRoom(TGenContext map, int roomIdx, MoneySpawn spawn)
        {
            IRoomGen room = map.GridPlan.GetRoom(roomIdx);
            List<Loc> freeTiles = ((IPlaceableGenContext<MoneySpawn>)map).GetFreeTiles(room.Draw);

            if (freeTiles.Count > 0)
            {
                int randIndex = map.Rand.Next(freeTiles.Count);
                map.PlaceItem(freeTiles[randIndex], spawn);
                return true;
            }

            return false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using RogueElements;
using System.Diagnostics;
using RogueEssence.LevelGen;
using RogueEssence.Dungeon;
using RogueEssence.Data;
using PMDC.Dungeon;

namespace MapGenTest
{
    /// <summary>
    /// Debug visualization and step-through functionality for procedural map generation.
    /// Hooks into the RogueElements generation pipeline to provide interactive debugging
    /// with visual representations of grid layouts, room plans, and tile maps.
    /// </summary>
    public static class ExampleDebug
    {
        /// <summary>
        /// Key to step into nested generation steps.
        /// </summary>
        const ConsoleKey STEP_IN_KEY = ConsoleKey.F5;

        /// <summary>
        /// Key to step out of the current generation level.
        /// </summary>
        const ConsoleKey STEP_OUT_KEY = ConsoleKey.F6;

        /// <summary>
        /// Controls print depth. -1 disables printing, 0+ enables printing up to that depth.
        /// </summary>
        public static int Printing;

        /// <summary>
        /// When true, automatically steps into the next generation step.
        /// </summary>
        public static bool SteppingIn;

        /// <summary>
        /// Stack of generation step names for breadcrumb navigation.
        /// </summary>
        static List<string> stepStack;

        /// <summary>
        /// Debug state stack for grid-based room layouts.
        /// </summary>
        static List<DebugState> gridDebugString;

        /// <summary>
        /// Debug state stack for list-based room layouts.
        /// </summary>
        static List<DebugState> listDebugString;

        /// <summary>
        /// Debug state stack for tile-based map visualization.
        /// </summary>
        static List<DebugState> tileDebugString;

        /// <summary>
        /// Current nesting depth in the generation pipeline.
        /// </summary>
        static int currentDepth;

        /// <summary>
        /// The current map context being debugged.
        /// </summary>
        static IGenContext curMap;

        /// <summary>
        /// Stores any exception that occurred during generation for later reporting.
        /// </summary>
        public static Exception Error;

        /// <summary>
        /// Initializes the debug state for a new map generation session.
        /// Called at the start of each map generation to reset state stacks.
        /// </summary>
        /// <param name="newMap">The generation context to debug.</param>
        public static void Init(IGenContext newMap)
        {
            curMap = newMap;
            currentDepth = 0;
            stepStack = new List<string>();
            gridDebugString = new List<DebugState>();
            listDebugString = new List<DebugState>();
            tileDebugString = new List<DebugState>();

            stepStack.Add("");
            gridDebugString.Add(new DebugState());
            listDebugString.Add(new DebugState());
            tileDebugString.Add(new DebugState());
            Error = null;
        }

        /// <summary>
        /// Called when entering a nested generation step.
        /// Pushes a new debug state onto all stacks and handles step-in breakpoints.
        /// </summary>
        /// <param name="msg">The name/description of the step being entered.</param>
        public static void StepIn(string msg)
        {
            currentDepth++;
            stepStack.Add(msg);
            gridDebugString.Add(new DebugState(gridDebugString[gridDebugString.Count - 1].MapString));
            listDebugString.Add(new DebugState(listDebugString[listDebugString.Count - 1].MapString));
            tileDebugString.Add(new DebugState(tileDebugString[tileDebugString.Count - 1].MapString));

            if (SteppingIn)
            {
                //SteppingIn = false;
                Printing = Math.Max(Printing, currentDepth + 1);
                //Console.Clear();
                //Console.WriteLine(createStackString()+">");
                //ConsoleKeyInfo key = Console.ReadKey();

                ////step further in F5, continue, step out F4, escape ESC
                //if (key.Key == STEP_IN_KEY)
                //    SteppingIn = true;
                //else if (key.Key == STEP_OUT_KEY)
                //    Printing--;
                //else if (key.Key == ConsoleKey.Escape)
                //    Printing = 0;

            }

        }

        /// <summary>
        /// Called when exiting a nested generation step.
        /// Pops debug state from all stacks and prints the exit state if within print depth.
        /// </summary>
        public static void StepOut()
        {
            currentDepth--;
            string stepOutName = stepStack[stepStack.Count-1];
            stepStack.RemoveAt(stepStack.Count - 1);
            DebugState gridState = gridDebugString[gridDebugString.Count - 1];
            gridDebugString.RemoveAt(gridDebugString.Count - 1);
            DebugState listState = listDebugString[listDebugString.Count - 1];
            listDebugString.RemoveAt(listDebugString.Count - 1);
            DebugState tileState = tileDebugString[tileDebugString.Count - 1];
            tileDebugString.RemoveAt(tileDebugString.Count - 1);

            Printing = Math.Min(Printing, currentDepth + 1);

            //print within printing
            printStep(createStackString() + "<" + stepOutName + "<");
        }

        /// <summary>
        /// Called during a generation step to report progress.
        /// Prints the current state if within the configured print depth.
        /// </summary>
        /// <param name="msg">The step progress message.</param>
        public static void OnStep(string msg)
        {
            printStep(createStackString() + ">" + msg);
        }

        /// <summary>
        /// Prints the current generation state using all available visualization modes.
        /// Handles user input for stepping, continuing, or escaping.
        /// </summary>
        /// <param name="msg">The message to display with the visualization.</param>
        public static void printStep(string msg)
        {
            //SteppingIn = false;
            bool printDebug = false;
            bool printViewer = false;
            if (Printing == -1)
                return;

            if (currentDepth < Printing)
            {
                printDebug = true;
                printViewer = true;
            }
            if (currentDepth == 0)
                printDebug = true;


            ConsoleKey key = ConsoleKey.Enter;
            {
                ConsoleKey newKey = PrintGridRoomHalls(curMap, msg, printDebug, printViewer);
                if (key == ConsoleKey.Enter)
                    key = newKey;
            }

            {
                ConsoleKey newKey = PrintListRoomHalls(curMap, msg, printDebug, printViewer);
                if (key == ConsoleKey.Enter)
                    key = newKey;
            }

            {
                ConsoleKey newKey = PrintTiles(curMap, msg, printDebug, printViewer, false);
                if (key == ConsoleKey.Enter)
                    key = newKey;
            }
            if (key == STEP_IN_KEY)
                SteppingIn = true;
            else if (key == STEP_OUT_KEY)
                Printing--;
            else if (key == ConsoleKey.Escape)
                Printing = 0;

        }


        /// <summary>
        /// Prints a tile-based visualization of the generated map.
        /// Shows terrain, items, enemies, stairs, and traps using ASCII characters.
        /// Supports interactive cursor navigation to inspect individual tiles.
        /// </summary>
        /// <param name="map">The generation context containing the map.</param>
        /// <param name="msg">The header message to display.</param>
        /// <param name="printDebug">Whether to output to debug console.</param>
        /// <param name="printViewer">Whether to display interactive viewer.</param>
        /// <param name="forcePrint">Whether to print even if map hasn't changed.</param>
        /// <returns>The key pressed to exit the viewer.</returns>
        public static ConsoleKey PrintTiles(IGenContext map, string msg, bool printDebug, bool printViewer, bool forcePrint)
        {
            BaseMapGenContext context = map as BaseMapGenContext;
            StairsMapGenContext stairsContext = map as StairsMapGenContext;
            if (context == null)
                return ConsoleKey.Enter;
            if (!context.TilesInitialized)
                return ConsoleKey.Enter;

            StringBuilder str = new StringBuilder();

            for (int yy = 0; yy < context.Height; yy++)
            {
                if (yy > 0)
                    str.Append('\n');
                for (int xx = 0; xx < context.Width; xx++)
                {
                    Loc loc = new Loc(xx, yy);
                    char tileChar = ' ';
                    Tile tile = (Tile)context.GetTile(loc);
                    TerrainData terrainData = (TerrainData)tile.Data.GetData();
                    if (terrainData.BlockType == TerrainData.Mobility.Passable)//floor
                    {
                        if (context.RoomTerrain.TileEquivalent(tile))
                            tileChar = '.';
                        else
                            tileChar = ',';
                    }
                    else if (terrainData.BlockType == TerrainData.Mobility.Impassable)//unbreakable
                        tileChar = 'X';
                    else if (terrainData.BlockType == TerrainData.Mobility.Block)//wall
                        tileChar = '#';
                    else if (terrainData.BlockType == TerrainData.Mobility.Water)//water
                        tileChar = '~';
                    else if (terrainData.BlockType == TerrainData.Mobility.Lava)//lava
                        tileChar = '^';
                    else if (terrainData.BlockType == TerrainData.Mobility.Abyss)//abyss
                        tileChar = '_';
                    else
                        tileChar = '?';
                    
                    if (!String.IsNullOrEmpty(tile.Effect.ID))//traps always override
                        tileChar = '=';

                    if (stairsContext != null)
                    {
                        for (int ii = 0; ii < ((IViewPlaceableGenContext<MapGenEntrance>)stairsContext).Count; ii++)
                        {
                            if (((IViewPlaceableGenContext<MapGenEntrance>)stairsContext).GetLoc(ii) == loc)
                            {
                                tileChar = '<';
                                break;
                            }
                        }
                        for (int ii = 0; ii < ((IViewPlaceableGenContext<MapGenExit>)stairsContext).Count; ii++)
                        {
                            if (((IViewPlaceableGenContext<MapGenExit>)stairsContext).GetLoc(ii) == loc)
                            {
                                tileChar = '>';
                                break;
                            }
                        }
                    }

                    foreach (MapItem item in context.Map.Items)
                    {
                        if (item.TileLoc == loc)
                        {
                            if (item.IsMoney)
                                tileChar = '*';
                            else
                            {
                                ItemData itemEntry = DataManager.Instance.GetItem(item.Value);
                                if (itemEntry.ItemStates.Contains<FoodState>())
                                    tileChar = ';';
                                else if (itemEntry.ItemStates.Contains<EdibleState>())
                                    tileChar = '!';
                                else if (itemEntry.ItemStates.Contains<OrbState>() || itemEntry.ItemStates.Contains<WandState>())
                                    tileChar = '/';
                                else if (itemEntry.ItemStates.Contains<RecruitState>())
                                    tileChar = '?';
                                else if (itemEntry.UsageType == ItemData.UseType.Learn || itemEntry.ItemStates.Contains<UtilityState>() || itemEntry.ItemStates.Contains<MachineState>())
                                    tileChar = '%';
                                else
                                    tileChar = '$';
                            }
                            break;
                        }
                    }

                    foreach (Team team in context.Map.MapTeams)
                    {
                        foreach (Character character in team.Players)
                        {
                            if (character.CharLoc == loc)
                            {
                                tileChar = character.Name[0];
                                break;
                            }
                        }
                    }
                    str.Append(tileChar);
                }
            }


            string newStr = str.ToString();
            if (tileDebugString[currentDepth].MapString == newStr && !forcePrint)
                return ConsoleKey.Enter;

            tileDebugString[currentDepth].MapString = newStr;

            if (printDebug)
            {
                Debug.WriteLine(msg);
                Debug.Print(newStr);
            }

            if (printViewer)
            {
                //TODO: print with highlighting (use the bounds variable)
                //TODO: print with color
                SteppingIn = false;
                Console.Clear();
                Console.WriteLine(msg);
                Loc start = new Loc(Console.CursorLeft, Console.CursorTop);
                Console.Write(newStr);
                Loc end = new Loc(Console.CursorLeft, Console.CursorTop+1);
                Console.SetCursorPosition(start.X, start.Y);
                int prevFarthestPrint = end.Y;

                while (true)
                {
                    int farthestPrint = end.Y;
                    Loc mapLoc = new Loc(Console.CursorLeft, Console.CursorTop) - start;
                    rewriteLine(farthestPrint, String.Format("X:{0}  Y:{1}", mapLoc.X.ToString("D3"), mapLoc.Y.ToString("D3")));
                    farthestPrint++;
                    Tile tile = context.Tiles[mapLoc.X][mapLoc.Y];
                    rewriteLine(farthestPrint, String.Format("Terrain {0}: {1}", tile.Data.GetID(), !String.IsNullOrEmpty(tile.Data.GetID()) ? ((TerrainData)tile.Data.GetData()).Name.ToLocal() : "---"));
                    farthestPrint++;
                    rewriteLine(farthestPrint, String.Format("Tile {0}: {1}", tile.Effect.GetID(), !String.IsNullOrEmpty(tile.Effect.ID) ? ((TileData)tile.Effect.GetData()).Name.ToLocal() : "---"));
                    farthestPrint++;
                    for(int ii = 0; ii < context.Map.EntryPoints.Count; ii++)
                    {
                        if (context.Map.EntryPoints[ii].Loc == mapLoc)
                        {
                            rewriteLine(farthestPrint, String.Format("***Entrance {0}***", ii));
                            farthestPrint++;
                        }
                    }
                    foreach (MapItem item in context.Map.Items)
                    {
                        if (item.TileLoc == mapLoc)
                        {
                            if (item.IsMoney)
                                rewriteLine(farthestPrint, String.Format("Money: {0}", item.HiddenValue));
                            else
                                rewriteLine(farthestPrint, String.Format("Item: {0}", item.GetDungeonName().Replace("\u000D7", "(X)")));
                            farthestPrint++;
                        }
                    }

                    foreach (Team team in context.Map.MapTeams)
                    {
                        foreach (Character character in team.Players)
                        {
                            if (character.CharLoc == mapLoc)
                            {
                                rewriteLine(farthestPrint, String.Format("Monster:"));
                                farthestPrint++;
                                string nameString = String.Format("    Lv.{0} {1} ", character.Level, character.BaseName);
                                foreach (string status in character.StatusEffects.Keys)
                                    nameString += String.Format("[{0}]", ((StatusData)character.StatusEffects[status].GetData()).Name.ToLocal());

                                rewriteLine(farthestPrint, nameString);
                                farthestPrint++;
                                rewriteLine(farthestPrint, String.Format("    @{0} *{1} ?{2}", !String.IsNullOrEmpty(character.EquippedItem.ID) ? ((ItemData)character.EquippedItem.GetData()).Name.ToLocal() : "---",
                                    !String.IsNullOrEmpty(character.Intrinsics[0].Element.ID) ? ((IntrinsicData)character.Intrinsics[0].Element.GetData()).Name.ToLocal() : "---", character.Tactic.Name));
                                farthestPrint++;
                                rewriteLine(farthestPrint, String.Format("    {0}{1} {2}{3} {4}{5} {6}{7}",
                                    character.Skills[0].Element.Enabled ? "+" : "-", !string.IsNullOrEmpty(character.BaseSkills[0].SkillNum) ? DataManager.Instance.GetSkill(character.Skills[0].Element.SkillNum).Name.ToLocal() : "---",
                                    character.Skills[1].Element.Enabled ? "+" : "-", !string.IsNullOrEmpty(character.BaseSkills[1].SkillNum) ? DataManager.Instance.GetSkill(character.Skills[1].Element.SkillNum).Name.ToLocal() : "---",
                                    character.Skills[2].Element.Enabled ? "+" : "-", !string.IsNullOrEmpty(character.BaseSkills[2].SkillNum) ? DataManager.Instance.GetSkill(character.Skills[2].Element.SkillNum).Name.ToLocal() : "---",
                                    character.Skills[3].Element.Enabled ? "+" : "-", !string.IsNullOrEmpty(character.BaseSkills[3].SkillNum) ? DataManager.Instance.GetSkill(character.Skills[3].Element.SkillNum).Name.ToLocal() : "---"));
                                farthestPrint++;
                            }
                        }
                    }

                    for (int ii = farthestPrint; ii < prevFarthestPrint; ii++)
                        clearLine(ii);
                    prevFarthestPrint = farthestPrint;
                    Console.SetCursorPosition(start.X + mapLoc.X, start.Y + mapLoc.Y);


                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.UpArrow)
                        Console.SetCursorPosition(Console.CursorLeft, Math.Max(start.Y, Console.CursorTop - 1));
                    else if (key.Key == ConsoleKey.DownArrow)
                        Console.SetCursorPosition(Console.CursorLeft, Math.Min(Console.CursorTop + 1, end.Y - 1));
                    else if (key.Key == ConsoleKey.LeftArrow)
                        Console.SetCursorPosition(Math.Max(start.X, Console.CursorLeft - 1), Console.CursorTop);
                    else if (key.Key == ConsoleKey.RightArrow)
                        Console.SetCursorPosition(Math.Min(Console.CursorLeft + 1, end.X - 1), Console.CursorTop);
                    else
                        return key.Key;
                }
            }
            else
                return ConsoleKey.Enter;
        }

        /// <summary>
        /// Prints a visualization of the floor plan showing rooms and halls as overlapping rectangles.
        /// Rooms are labeled A-Z, halls are labeled a-z, overlaps shown with special characters.
        /// Supports interactive cursor navigation to inspect room/hall details.
        /// </summary>
        /// <param name="map">The generation context containing the floor plan.</param>
        /// <param name="msg">The header message to display.</param>
        /// <param name="printDebug">Whether to output to debug console.</param>
        /// <param name="printViewer">Whether to display interactive viewer.</param>
        /// <returns>The key pressed to exit the viewer.</returns>
        public static ConsoleKey PrintListRoomHalls(IGenContext map, string msg, bool printDebug, bool printViewer)
        {
            IFloorPlanGenContext context = map as IFloorPlanGenContext;
            if (context == null)
                return ConsoleKey.Enter;

            StringBuilder str = new StringBuilder();
            FloorPlan plan = context.RoomPlan;
            if (plan == null)
                return ConsoleKey.Enter;

            for (int yy = 0; yy < plan.DrawRect.Bottom; yy++)
            {
                for (int xx = 0; xx < plan.DrawRect.Right; xx++)
                {
                    str.Append(' ');
                }
            }

            for (int ii = 0; ii < plan.RoomCount; ii++)
            {
                char chosenChar = '@';
                //if (ii < 26)
                chosenChar = (char)('A' + ii % 26);
                IRoomGen gen = plan.GetRoom(ii);
                for (int xx = gen.Draw.Left; xx < gen.Draw.Right; xx++)
                {
                    for (int yy = gen.Draw.Top; yy < gen.Draw.Bottom; yy++)
                    {
                        Loc wrapLoc = Loc.Wrap(new Loc(xx, yy), plan.Size);
                        int index = wrapLoc.Y * plan.DrawRect.Right + wrapLoc.X;

                        if (str[index] == ' ')
                            str[index] = chosenChar;
                        else
                            str[index] = '!';
                    }
                }
            }
            for (int ii = 0; ii < plan.HallCount; ii++)
            {
                char chosenChar = '#';
                //if (ii < 26)
                chosenChar = (char)('a' + ii % 26);

                IRoomGen gen = plan.GetHall(ii);

                for (int xx = gen.Draw.Left; xx < gen.Draw.Right; xx++)
                {
                    for (int yy = gen.Draw.Top; yy < gen.Draw.Bottom; yy++)
                    {
                        Loc wrapLoc = Loc.Wrap(new Loc(xx, yy), plan.Size);
                        int index = wrapLoc.Y * plan.DrawRect.Right + wrapLoc.X;

                        if (str[index] == ' ')
                            str[index] = chosenChar;
                        else if (str[index] >= 'a' && str[index] <= 'z' || str[index] == '#')
                            str[index] = '+';
                        else
                            str[index] = '!';
                    }
                }
            }

            for (int yy = plan.DrawRect.Bottom - 1; yy > 0; yy--)
                str.Insert(plan.DrawRect.Right * yy, '\n');


            string newStr = str.ToString();
            if (listDebugString[currentDepth].MapString == newStr)
                return ConsoleKey.Enter;

            listDebugString[currentDepth].MapString = newStr;


            if (printDebug)
            {
                Debug.WriteLine(msg);
                Debug.Print(newStr);
            }

            if (printViewer)
            {
                SteppingIn = false;
                Console.Clear();
                Console.WriteLine(msg);
                Loc start = new Loc(Console.CursorLeft, Console.CursorTop);
                Console.Write(newStr);
                Loc end = new Loc(Console.CursorLeft, Console.CursorTop + 1);
                Console.SetCursorPosition(start.X, start.Y);
                int prevFarthestPrint = end.Y;

                while (true)
                {
                    int farthestPrint = end.Y;
                    Loc mapLoc = new Loc(Console.CursorLeft, Console.CursorTop) - start;
                    rewriteLine(farthestPrint, String.Format("X:{0}  Y:{1}", mapLoc.X.ToString("D3"), mapLoc.Y.ToString("D3")));
                    farthestPrint++;

                    for (int ii = 0; ii < plan.RoomCount; ii++)
                    {
                        FloorRoomPlan roomPlan = plan.GetRoomPlan(ii);
                        if (roomPlan.RoomGen.Draw.Contains(mapLoc))
                        {
                            //stats
                            string roomString = String.Format("Room #{0}: {1}x{2} {3}", ii, roomPlan.RoomGen.Draw.X, roomPlan.RoomGen.Draw.Y, roomPlan.RoomGen.ToString());
                            rewriteLine(farthestPrint, roomString);
                            farthestPrint++;
                            string componentString = String.Format("Components: {0}", String.Join(", ", roomPlan.Components));
                            rewriteLine(farthestPrint, componentString);
                            farthestPrint++;
                            //borders
                            StringBuilder lineString = new StringBuilder(" ");
                            for (int xx = 0; xx < roomPlan.RoomGen.Draw.Width; xx++)
                                lineString.Append(roomPlan.RoomGen.GetFulfillableBorder(Dir4.Up, xx) ? "^" : " ");
                            rewriteLine(farthestPrint, lineString.ToString());
                            farthestPrint++;
                            for (int yy = 0; yy < roomPlan.RoomGen.Draw.Height; yy++)
                            {
                                lineString = new StringBuilder(roomPlan.RoomGen.GetFulfillableBorder(Dir4.Left, yy) ? "<" : " ");
                                for (int xx = 0; xx < roomPlan.RoomGen.Draw.Width; xx++)
                                    lineString.Append("#");
                                lineString.Append(roomPlan.RoomGen.GetFulfillableBorder(Dir4.Right, yy) ? ">" : " ");
                                rewriteLine(farthestPrint, lineString.ToString());
                                farthestPrint++;
                            }
                            lineString = new StringBuilder(" ");
                            for (int xx = 0; xx < roomPlan.RoomGen.Draw.Width; xx++)
                                lineString.Append(roomPlan.RoomGen.GetFulfillableBorder(Dir4.Down, xx) ? "V" : " ");
                            rewriteLine(farthestPrint, lineString.ToString());
                            farthestPrint++;
                        }
                    }
                    for (int ii = 0; ii < plan.HallCount; ii++)
                    {
                        FloorHallPlan hallPlan = plan.GetHallPlan(ii);
                        if (hallPlan.RoomGen.Draw.Contains(mapLoc))
                        {
                            string roomString = String.Format("Hall #{0}: {1}x{2} {3}", ii, hallPlan.RoomGen.Draw.X, hallPlan.RoomGen.Draw.Y, hallPlan.RoomGen.ToString());
                            rewriteLine(farthestPrint, roomString);
                            farthestPrint++;
                            string componentString = String.Format("Components: {0}", String.Join(", ", hallPlan.Components));
                            rewriteLine(farthestPrint, componentString);
                            farthestPrint++;
                        }
                    }


                    for (int ii = farthestPrint; ii < prevFarthestPrint; ii++)
                        clearLine(ii);
                    prevFarthestPrint = farthestPrint;
                    Console.SetCursorPosition(start.X + mapLoc.X, start.Y + mapLoc.Y);


                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.UpArrow)
                        Console.SetCursorPosition(Console.CursorLeft, Math.Max(start.Y, Console.CursorTop - 1));
                    else if (key.Key == ConsoleKey.DownArrow)
                        Console.SetCursorPosition(Console.CursorLeft, Math.Min(Console.CursorTop + 1, end.Y - 1));
                    else if (key.Key == ConsoleKey.LeftArrow)
                        Console.SetCursorPosition(Math.Max(start.X, Console.CursorLeft - 1), Console.CursorTop);
                    else if (key.Key == ConsoleKey.RightArrow)
                        Console.SetCursorPosition(Math.Min(Console.CursorLeft + 1, end.X - 1), Console.CursorTop);
                    else
                        return key.Key;
                }
            }
            else
                return ConsoleKey.Enter;
        }

        /// <summary>
        /// Prints a grid-based visualization showing room placement and hall connections.
        /// Rooms are labeled A-Z at grid positions, halls shown as # connecting rooms.
        /// Supports interactive cursor navigation to inspect room/hall details.
        /// </summary>
        /// <param name="map">The generation context containing the grid plan.</param>
        /// <param name="msg">The header message to display.</param>
        /// <param name="printDebug">Whether to output to debug console.</param>
        /// <param name="printViewer">Whether to display interactive viewer.</param>
        /// <returns>The key pressed to exit the viewer.</returns>
        public static ConsoleKey PrintGridRoomHalls(IGenContext map, string msg, bool printDebug, bool printViewer)
        {
            IRoomGridGenContext context = map as IRoomGridGenContext;
            if (context == null)
                return ConsoleKey.Enter;

            StringBuilder str = new StringBuilder();
            GridPlan plan = context.GridPlan;
            if (plan == null)
                return ConsoleKey.Enter;

            for (int yy = 0; yy < plan.GridHeight; yy++)
            {
                if (yy > 0)
                    str.Append('\n');

                for (int xx = 0; xx < plan.GridWidth; xx++)
                {
                    int roomIndex = plan.GetRoomIndex(new Loc(xx, yy));
                    if (roomIndex == -1)
                        str.Append('0');
                    else// if (roomIndex < 26)
                        str.Append((char)('A' + roomIndex % 26));
                    //else
                    //    str.Append('@');

                    if (xx < plan.GridWidth - 1)
                    {
                        if (plan.GetHall(new LocRay4(xx, yy, Dir4.Right)) != null)
                            str.Append('#');
                        else
                            str.Append('.');
                    }
                }

                if (yy < plan.GridHeight - 1)
                {
                    str.Append('\n');
                    for (int xx = 0; xx < plan.GridWidth; xx++)
                    {
                        if (plan.GetHall(new LocRay4(xx, yy, Dir4.Down)) != null)
                            str.Append('#');
                        else
                            str.Append('.');

                        if (xx < plan.GridWidth - 1)
                        {
                            str.Append(' ');
                        }
                    }
                }
            }


            string newStr = str.ToString();
            if (gridDebugString[currentDepth].MapString == newStr)
                return ConsoleKey.Enter;

            gridDebugString[currentDepth].MapString = newStr;


            if (printDebug)
            {
                Debug.WriteLine(msg);
                Debug.Print(newStr);
            }

            if (printViewer)
            {
                SteppingIn = false;
                Console.Clear();
                Console.WriteLine(msg);
                Loc start = new Loc(Console.CursorLeft, Console.CursorTop);
                Console.Write(newStr);
                Loc end = new Loc(Console.CursorLeft, Console.CursorTop + 1);
                Console.SetCursorPosition(start.X, start.Y);
                int prevFarthestPrint = end.Y;

                while (true)
                {
                    int farthestPrint = end.Y;
                    Loc gridLoc = new Loc(Console.CursorLeft, Console.CursorTop) - start;
                    Loc mapLoc = gridLoc / 2;
                    rewriteLine(farthestPrint, String.Format("X:{0:0.0}  Y:{1:0.0}", ((float)gridLoc.X / 2), ((float)gridLoc.Y / 2)));
                    farthestPrint++;

                    bool alignX = gridLoc.X % 2 == 0;
                    bool alignY = gridLoc.Y % 2 == 0;

                    if (alignX && alignY)
                    {
                        int index = plan.GetRoomIndex(mapLoc);
                        GridRoomPlan roomPlan = plan.GetRoomPlan(mapLoc);
                        if (roomPlan != null)
                        {
                            string roomString = String.Format("Room #{0}: {1}", index, roomPlan.RoomGen.ToString());
                            if (roomPlan.PreferHall)
                                roomString += " [Hall]";
                            rewriteLine(farthestPrint, roomString);
                            farthestPrint++;
                            string componentString = String.Format("Components: {0}", String.Join(", ", roomPlan.Components));
                            rewriteLine(farthestPrint, componentString);
                            farthestPrint++;
                        }
                    }
                    else if (alignX)
                    {
                        GridHallPlan hall = plan.GetHall(new LocRay4(mapLoc, Dir4.Down));
                        if (hall != null)
                        {
                            rewriteLine(farthestPrint, "Hall: " + hall.RoomGen.ToString());
                            farthestPrint++;
                            string componentString = String.Format("Components: {0}", String.Join(", ", hall.Components));
                            rewriteLine(farthestPrint, componentString);
                            farthestPrint++;
                        }
                    }
                    else if (alignY)
                    {
                        GridHallPlan hall = plan.GetHall(new LocRay4(mapLoc, Dir4.Right));
                        if (hall != null)
                        {
                            rewriteLine(farthestPrint, "Hall: " + hall.RoomGen.ToString());
                            farthestPrint++;
                        }
                    }



                    for (int ii = farthestPrint; ii < prevFarthestPrint; ii++)
                        clearLine(ii);
                    prevFarthestPrint = farthestPrint;
                    Console.SetCursorPosition(start.X + gridLoc.X, start.Y + gridLoc.Y);


                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.UpArrow)
                        Console.SetCursorPosition(Console.CursorLeft, Math.Max(start.Y, Console.CursorTop - 1));
                    else if (key.Key == ConsoleKey.DownArrow)
                        Console.SetCursorPosition(Console.CursorLeft, Math.Min(Console.CursorTop + 1, end.Y - 1));
                    else if (key.Key == ConsoleKey.LeftArrow)
                        Console.SetCursorPosition(Math.Max(start.X, Console.CursorLeft - 1), Console.CursorTop);
                    else if (key.Key == ConsoleKey.RightArrow)
                        Console.SetCursorPosition(Math.Min(Console.CursorLeft + 1, end.X - 1), Console.CursorTop);
                    else
                        return key.Key;
                }
            }
            else
                return ConsoleKey.Enter;
        }


        /// <summary>
        /// Clears a console line by overwriting it with spaces.
        /// </summary>
        /// <param name="lineNum">The line number to clear.</param>
        private static void clearLine(int lineNum)
        {
            Console.SetCursorPosition(0, lineNum);
            Console.Write(new string(' ', Console.WindowWidth - Console.CursorLeft));
        }

        /// <summary>
        /// Writes a message to a console line, clearing any remaining content.
        /// </summary>
        /// <param name="lineNum">The line number to write to.</param>
        /// <param name="msg">The message to write.</param>
        private static void rewriteLine(int lineNum, string msg)
        {
            Console.SetCursorPosition(0, lineNum);
            Console.Write(msg);
            Console.Write(new string(' ', Console.WindowWidth - Console.CursorLeft));
        }

        /// <summary>
        /// Creates a breadcrumb string from the current step stack.
        /// </summary>
        /// <returns>A string like "Step1>Step2>Step3" showing the navigation path.</returns>
        private static string createStackString()
        {
            StringBuilder str = new StringBuilder();
            for (int ii = 0; ii < stepStack.Count; ii++)
            {
                if (ii > 0)
                    str.Append(">");
                str.Append(stepStack[ii]);
            }
            return str.ToString();
        }

        /// <summary>
        /// Called when an error occurs during generation.
        /// Stores the exception for later reporting after generation completes.
        /// </summary>
        /// <param name="ex">The exception that occurred.</param>
        public static void OnError(Exception ex)
        {
            Error = ex;
        }
    }
}

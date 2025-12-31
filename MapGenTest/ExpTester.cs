using PMDC.Dungeon;
using RogueEssence.Content;
using RogueEssence;
using RogueEssence.Data;
using RogueEssence.Dungeon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Joins;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using RogueElements;
using System.Xml.Linq;

namespace MapGenTest
{
    /// <summary>
    /// Test harness for comparing experience point distribution formulas.
    /// Loads experience logs from gameplay sessions and simulates how different
    /// exp handout algorithms would affect leveling progression.
    /// </summary>
    public static class ExpTester
    {
        /// <summary>
        /// Directory path containing experience log CSV files.
        /// </summary>
        public static string EXP_DIR;

        /// <summary>
        /// Loaded experience logs from gameplay sessions.
        /// </summary>
        private static List<ExpLog> Logs;

        /// <summary>
        /// List of experience handout formulas to compare, with descriptive names.
        /// </summary>
        private static List<(HandoutExpEvent expEvent, string name)> Handouts;

        /// <summary>
        /// Initializes the list of experience handout formulas to test.
        /// Each formula represents a different approach to scaling exp gains
        /// based on level differences between the player and defeated enemies.
        /// </summary>
        public static void InitHandouts()
        {
            Handouts = new List<(HandoutExpEvent expEvent, string name)>();
            {
                Handouts.Add((new HandoutRelativeExpEvent(1, 7, 10, 2), String.Format("ORIGINAL")));
            }
            {
                HandoutExpEvent low = new HandoutStackExpEvent(1, 7, 5);
                HandoutExpEvent high = new HandoutHarmonicExpEvent(1, 7, 5);

                Handouts.Add((new HandoutPiecewiseExpEvent(5, 0, low, high), String.Format("STACK HARMONIC")));
            }
            {
                HandoutExpEvent low = new HandoutStackExpEvent(1, 7, 7);
                HandoutExpEvent high = new HandoutHarmonicExpEvent(1, 7, 7);

                Handouts.Add((new HandoutPiecewiseExpEvent(5, 0, low, high), String.Format("STACK HARMONIC GRADUAL")));
            }
            {
                HandoutExpEvent low = new HandoutStackExpEvent(1, 7, 5);
                HandoutExpEvent high = new HandoutHarmonicExpEvent(1, 7, 5);

                Handouts.Add((new HandoutPiecewiseExpEvent(5, 5, low, high), String.Format("STACK HARMONIC OFFSET")));
            }
            {
                HandoutExpEvent low = new HandoutRelativeExpEvent(1, 7, 5, 2);
                HandoutExpEvent high = new HandoutRelativeExpEvent(1, 7, 5, 4);

                Handouts.Add((new HandoutPiecewiseExpEvent(5, 0, low, high), String.Format("HIGHER POWER OVERLEVEL")));
            }
            //{
            //    HandoutExpEvent low = new HandoutRelativeExpEvent(1, 7, 5, 2);
            //    HandoutExpEvent high = new HandoutRelativeExpEvent(1, 7, 0, 4);

            //    Handouts.Add((new HandoutPiecewiseExpEvent(5, 0, low, high), String.Format("HIGHER POWER NO BUFFER OVERLEVEL")));
            //}
            {
                HandoutExpEvent low = new HandoutRelativeExpEvent(1, 7, 10, 2);
                HandoutExpEvent high = new HandoutRelativeExpEvent(1, 15, 10, 2);

                Handouts.Add((new HandoutPiecewiseExpEvent(5, 0, low, high), String.Format("HIGHER DIV OVERLEVEL")));
            }
            //{
            //    HandoutExpEvent low = new HandoutRelativeExpEvent(1, 7, 10, 0);
            //    HandoutExpEvent high = new HandoutRelativeExpEvent(1, 15, 10, 0);

            //    Handouts.Add((new HandoutPiecewiseExpEvent(5, 0, low, high), String.Format("HIGHER DIV OVERLEVEL UNSCALED")));
            //}
            {
                HandoutExpEvent low = new HandoutRelativeExpEvent(1, 7, 10, 2);
                HandoutExpEvent high = new HandoutRelativeExpEvent(1, 15, 10, 4);

                Handouts.Add((new HandoutPiecewiseExpEvent(5, 0, low, high), String.Format("HIGHER DIV + POWER OVERLEVEL")));
            }
            //{
            //    HandoutExpEvent low = new HandoutRelativeExpEvent(1, 7, 10, 2);
            //    HandoutExpEvent high = new HandoutRelativeExpEvent(1, 15, 0, 4);

            //    Handouts.Add((new HandoutPiecewiseExpEvent(5, 0, low, high), String.Format("HIGHER DIV + POWER OVERLEVEL NO BUFFER")));
            //}
            //{
            //    HandoutExpEvent low = new HandoutRelativeExpEvent(1, 7, 0, 3);
            //    HandoutExpEvent high = new HandoutRelativeExpEvent(1, 7, 0, 3);

            //    Handouts.Add((new HandoutPiecewiseExpEvent(5, 5, low, high), String.Format("5-LEVEL OVERLEVEL, OLD POWER NO BUFFER")));
            //}
            {
                HandoutExpEvent low = new HandoutRelativeExpEvent(1, 7, 0, 2);
                HandoutExpEvent high = new HandoutRelativeExpEvent(1, 7, 0, 4);

                Handouts.Add((new HandoutPiecewiseExpEvent(5, 5, low, high), String.Format("5-LEVEL OVERLEVEL, POWER NO BUFFER")));
            }
            {
                HandoutExpEvent low = new HandoutRelativeExpEvent(1, 7, 0, 2);
                HandoutExpEvent high = new HandoutRelativeExpEvent(1, 15, 0, 4);

                Handouts.Add((new HandoutPiecewiseExpEvent(5, 5, low, high), String.Format("5-LEVEL OVERLEVEL, DIV + POWER OVERLEVEL NO BUFFER")));
            }
            {
                HandoutExpEvent low = new HandoutRelativeExpEvent(1, 7, 10, 2);
                HandoutExpEvent high = new HandoutHarmonicExpEvent(1, 7, 5);

                Handouts.Add((new HandoutPiecewiseExpEvent(5, 0, low, high), String.Format("POWER HARMONIC")));
            }
            //{
            //    HandoutExpEvent low = new HandoutRelativeExpEvent(1, 7, 0, 2);
            //    HandoutExpEvent high = new HandoutHarmonicExpEvent(1, 7, 5);

            //    Handouts.Add((new HandoutPiecewiseExpEvent(5, 0, low, high), String.Format("POWER HARMONIC NO BUFFER")));
            //}
        }

        /// <summary>
        /// Main entry point for the experience testing mode.
        /// Loads experience logs and displays an interactive menu for comparing formulas.
        /// </summary>
        public static void Run()
        {
            Logs = new List<ExpLog>();
            List<int> uniqueIDs = new List<int>();
            int maxUniqueIDs = 0;
            foreach (string dir in Directory.GetFiles(EXP_DIR + "/0.7.20"))
            {
                HashSet<int> curUniqueIDs = new HashSet<int>();
                ExpLog log = new ExpLog();
                string file = Path.GetFileNameWithoutExtension(dir);
                log.Name = file;
                using (StreamReader reader = new StreamReader(dir))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        string[] cols = line.Split(',');
                        string zone = cols[0];
                        int segment = Int32.Parse(cols[1]);
                        int id = Int32.Parse(cols[2]);
                        int expYield = Int32.Parse(cols[3]);
                        int level = Int32.Parse(cols[4]);
                        SegLoc loc = new SegLoc(segment, id);
                        if (!log.Zone.IsValid())
                            log.Zone = new ZoneLoc(zone, loc);
                        log.Exps.Add((loc, expYield, level));

                        if (segment == 0)
                            curUniqueIDs.Add(id);
                    }
                }
                if (log.Zone.ID == "forsaken_desert")
                {
                    Logs.Add(log);
                    uniqueIDs.Add(curUniqueIDs.Count);
                    if (curUniqueIDs.Count > maxUniqueIDs)
                        maxUniqueIDs = curUniqueIDs.Count;
                }
            }

            for (int ii = uniqueIDs.Count - 1; ii >= 0; ii--)
            {
                if (uniqueIDs[ii] < maxUniqueIDs)
                {
                    Logs.RemoveAt(ii);
                    uniqueIDs.RemoveAt(ii);
                }
            }

            InitHandouts();

            int offset = 0;
            string state = "Logs";
            while (true)
            {
                Console.Clear();
                Console.WriteLine(state);
                Console.WriteLine("Choose a log|ESC=Exit");

                int longestWidth = 0;
                for (int ii = offset; ii < Logs.Count; ii++)
                {
                    string label = GetSelectionString(ii, String.Format("{0}: {1}", Logs[ii].Zone.ID, Logs[ii].Name));
                    if (label.Length > longestWidth)
                        longestWidth = label.Length;
                }
                int cols = Math.Min(3, MathUtils.DivUp(Console.WindowWidth, longestWidth));
                int rows = Math.Max(Math.Min(12, Logs.Count - offset), MathUtils.DivUp(Logs.Count - offset, cols));

                for (int ii = 0; ii < rows; ii++)
                {
                    string choiceStr = "";
                    List<string> choiceList = new List<string>();
                    for (int jj = 0; jj < cols; jj++)
                    {
                        int index = ii + rows * jj;
                        if (index + offset < Logs.Count)
                        {
                            choiceStr += "{" + jj + "," + "-" + longestWidth + "}  ";
                            choiceList.Add(GetSelectionString(index, String.Format("{0}: {1}", Logs[index + offset].Zone.ID, Logs[index + offset].Name)));
                        }
                    }
                    Console.WriteLine(String.Format(choiceStr, choiceList.ToArray()));
                }

                int zoneIndex = -1;
                if (zoneIndex < 0)
                {
                    ConsoleKeyInfo key = Console.ReadKey();
                    if (key.Key == ConsoleKey.Escape)
                        break;

                    if (key.KeyChar >= '0' && key.KeyChar <= '9')
                        zoneIndex = key.KeyChar - '0' + offset;
                    if (key.KeyChar >= 'a' && key.KeyChar <= 'z')
                        zoneIndex = key.KeyChar - 'a' + 10 + offset;
                    if (key.Key == ConsoleKey.UpArrow)
                        offset -= 10;
                    if (key.Key == ConsoleKey.DownArrow)
                        offset += 10;
                }
                if (zoneIndex >= 0)
                {
                    ComparisonMenu(state, Logs[zoneIndex]);
                }
            }
        }

        /// <summary>
        /// Displays a comparison of all exp formulas for a selected log.
        /// Shows final levels across all growth groups for each formula.
        /// </summary>
        /// <param name="state">The navigation state string.</param>
        /// <param name="log">The experience log to analyze.</param>
        public static void ComparisonMenu(string state, ExpLog log)
        {
            state = log.Name;
            int levelDiff = 0;
            while (true)
            {
                Console.Clear();
                Console.WriteLine(state);
                Console.WriteLine("Showing with all exp curves offset {0}|ESC=Exit", levelDiff);

                TestExp(log, levelDiff);

                Console.WriteLine();

                List<string> growthKeys = DataManager.Instance.DataIndices[DataManager.DataType.GrowthGroup].GetOrderedKeys(false);
                for (int ii = 0; ii < growthKeys.Count; ii++)
                    Console.WriteLine(String.Format("{0}) {1}", ii, growthKeys[ii]));

                int growthIndex = -1;

                ConsoleKeyInfo key = Console.ReadKey();
                if (key.Key == ConsoleKey.Escape)
                    break;

                if (key.KeyChar >= '0' && key.KeyChar <= '9')
                    growthIndex = key.KeyChar - '0';

                if (growthIndex >= 0 && growthIndex < growthKeys.Count)
                {
                    LevelLogMenu(state, log, growthKeys[growthIndex]);
                }

                if (key.Key == ConsoleKey.UpArrow)
                    levelDiff += 5;
                else if (key.Key == ConsoleKey.DownArrow)
                    levelDiff -= 5;
            }
        }

        /// <summary>
        /// Displays a floor-by-floor level progression for a specific growth group.
        /// Compares all exp formulas side-by-side showing level at each floor.
        /// </summary>
        /// <param name="state">The navigation state string.</param>
        /// <param name="log">The experience log to analyze.</param>
        /// <param name="growth">The growth group ID to simulate.</param>
        public static void LevelLogMenu(string state, ExpLog log, string growth)
        {
            state = growth;
            int levelDiff = 0;
            while (true)
            {
                Console.Clear();
                Console.WriteLine(state);
                Console.WriteLine("Showing full history for one growth|ESC=Exit");

                Console.WriteLine();

                List<List<int>> floorList = new List<List<int>>();
                {
                    List<string> indices = new List<string>();
                    for (int ii = 0; ii < Handouts.Count; ii++)
                    {
                        Console.WriteLine(String.Format("{0}) {1}", ii, Handouts[ii].name));
                        List<int> levelLog = GetLevelLog(Handouts[ii].expEvent, log, growth, levelDiff);
                        floorList.Add(levelLog);
                        indices.Add(String.Format("{0,4}", ii.ToString()));
                    }
                    Console.WriteLine(" F|" + String.Join("|", indices.ToArray()));
                }

                for (int ii = 0; ii < floorList[0].Count; ii++)
                {
                    List<string> indices = new List<string>();
                    for (int jj = 0; jj < floorList.Count; jj++)
                        indices.Add(String.Format("{0,4}", floorList[jj][ii].ToString()));
                    Console.WriteLine(String.Format("{0,2}|", ii) + String.Join("|", indices.ToArray()));
                }

                ConsoleKeyInfo key = Console.ReadKey();
                if (key.Key == ConsoleKey.UpArrow)
                    levelDiff += 5;
                else if (key.Key == ConsoleKey.DownArrow)
                    levelDiff -= 5;
                else if (key.Key == ConsoleKey.Escape)
                    break;

            }
        }

        /// <summary>
        /// Tests all exp formulas against all growth groups and prints a comparison table.
        /// </summary>
        /// <param name="log">The experience log to analyze.</param>
        /// <param name="addLevel">Level offset to add to the starting level.</param>
        public static void TestExp(ExpLog log, int addLevel)
        {
            Console.WriteLine(String.Format("{0,4}|{1,4}|{2,4}|{3,4}|{4,4}|{5,4}", "err", "fast", "fluc", "mfst", "mslo", "slow"));

            foreach ((HandoutExpEvent expEvent, string name) handout in Handouts)
            {
                List<int> growthResults = new List<int>();
                foreach (string growth in DataManager.Instance.DataIndices[DataManager.DataType.GrowthGroup].GetOrderedKeys(false))
                {
                    List<int> levelLog = GetLevelLog(handout.expEvent, log, growth, addLevel);
                    growthResults.Add(levelLog[levelLog.Count - 1]);
                }
                Console.WriteLine(String.Format("{0,4}|{1,4}|{2,4}|{3,4}|{4,4}|{5,4}  //{6}", growthResults[0], growthResults[1], growthResults[2], growthResults[3], growthResults[4], growthResults[5], handout.name));
            }
        }

        /// <summary>
        /// Simulates level progression through an experience log using a specific formula and growth group.
        /// </summary>
        /// <param name="expEvent">The exp handout formula to use.</param>
        /// <param name="log">The experience log containing enemy defeats.</param>
        /// <param name="growth">The growth group ID for exp-to-level calculations.</param>
        /// <param name="addLevel">Level offset to add to the starting level.</param>
        /// <returns>A list of levels, one entry per floor in the log.</returns>
        public static List<int> GetLevelLog(HandoutExpEvent expEvent, ExpLog log, string growth, int addLevel)
        {
            List<int> levelLog = new List<int>();

            string zoneId = log.Zone.ID;
            ZoneEntrySummary zoneSummary = (ZoneEntrySummary)DataManager.Instance.DataIndices[DataManager.DataType.Zone].Get(zoneId);

            int recipientLv = zoneSummary.Level + addLevel;
            int expPercent = zoneSummary.ExpPercent;
            GrowthData growthData = DataManager.Instance.GetGrowth(growth);
            int gainedExp = 0;

            SegLoc currentLoc = log.Exps[0].segLoc;
            foreach ((SegLoc segLoc, int expYield, int level) exp in log.Exps)
            {
                if (!exp.segLoc.Equals(currentLoc))
                {
                    levelLog.Add(recipientLv);
                    currentLoc = exp.segLoc;
                }
                int gain = expEvent.GetExp(exp.expYield, exp.level, recipientLv);
                gainedExp += (gain * expPercent / 100);

                while (recipientLv < DataManager.Instance.Start.MaxLevel && gainedExp >= growthData.GetExpTo(recipientLv, recipientLv + 1))
                {
                    gainedExp -= growthData.GetExpTo(recipientLv, recipientLv + 1);
                    recipientLv++;
                }
            }
            levelLog.Add(recipientLv);

            return levelLog;
        }


        /// <summary>
        /// Formats a menu item with its selection key (0-9, then A-Z).
        /// </summary>
        /// <param name="index">The zero-based index of the menu item.</param>
        /// <param name="str">The description text for the menu item.</param>
        /// <returns>A formatted string like "0) Description" or "A) Description".</returns>
        public static string GetSelectionString(int index, string str)
        {
            char select = (char)(index > 9 ? 'A' + index - 10 : '0' + index);
            return select.ToString() + ") " + str;
        }
    }

    /// <summary>
    /// Represents a logged gameplay session with experience point gains.
    /// Parsed from CSV files containing zone, floor, exp yield, and enemy level data.
    /// </summary>
    public class ExpLog
    {
        /// <summary>
        /// The name/identifier of this log (typically the source filename).
        /// </summary>
        public string Name;

        /// <summary>
        /// The zone location where this gameplay session took place.
        /// </summary>
        public ZoneLoc Zone;

        /// <summary>
        /// List of experience gains, each containing the floor location,
        /// base exp yield of the defeated enemy, and its level.
        /// </summary>
        public List<(SegLoc segLoc, int expYield, int level)> Exps;

        /// <summary>
        /// Initializes a new empty experience log.
        /// </summary>
        public ExpLog()
        {
            Zone = ZoneLoc.Invalid;
            Exps = new List<(SegLoc segLoc, int expYield, int level)>();
        }
    }
}

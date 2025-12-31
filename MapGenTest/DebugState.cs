using System;
using System.Collections.Generic;
using System.Text;
using RogueElements;
using System.Diagnostics;
using RogueEssence.LevelGen;
using RogueEssence.Dungeon;
using RogueEssence.Data;

namespace MapGenTest
{
    /// <summary>
    /// Stores a snapshot of map visualization state for debugging purposes.
    /// Used to track changes between generation steps and avoid redundant console output.
    /// </summary>
    public class DebugState
    {
        /// <summary>
        /// The string representation of the map at this debug state.
        /// Used to detect changes between steps and avoid printing unchanged maps.
        /// </summary>
        public string MapString;

        /// <summary>
        /// Initializes a new debug state with an empty map string.
        /// </summary>
        public DebugState()
        {
            MapString = "";
        }

        /// <summary>
        /// Initializes a new debug state with the specified map string.
        /// </summary>
        /// <param name="str">The initial map string representation.</param>
        public DebugState(string str)
        {
            MapString = str;
        }

    }
}

using RogueElements;
using System;

namespace PMDC.LevelGen
{
    /// <summary>
    /// A room component that marks rooms as disconnected from the main path.
    /// Used to identify rooms that should not be connected to the rest of the dungeon layout.
    /// </summary>
    [Serializable]
    public class NoConnectRoom : RoomComponent
    {
        /// <inheritdoc/>
        public override RoomComponent Clone() { return new NoConnectRoom(); }

        /// <summary>
        /// Returns a string representation of this room component.
        /// </summary>
        /// <returns>The string "NoConnect".</returns>
        public override string ToString()
        {
            return "NoConnect";
        }
    }
}

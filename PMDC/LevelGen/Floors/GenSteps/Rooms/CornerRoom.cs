using RogueElements;
using System;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Room component that marks a room as being positioned in a corner of the floor layout.
    /// Used to identify rooms at cardinal extremes for special placement logic.
    /// </summary>
    [Serializable]
    public class CornerRoom : RoomComponent
    {
        /// <summary>
        /// Creates a deep copy of this corner room component.
        /// </summary>
        /// <returns>A new instance of <see cref="CornerRoom"/> with identical state.</returns>
        public override RoomComponent Clone() { return new CornerRoom(); }

        /// <summary>
        /// Returns a string representation of this corner room component.
        /// </summary>
        /// <returns>The string "CardinalRoom" identifying this component type.</returns>
        public override string ToString()
        {
            return "CardinalRoom";
        }
    }
}

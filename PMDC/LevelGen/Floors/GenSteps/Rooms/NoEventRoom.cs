using RogueElements;
using System;

namespace PMDC.LevelGen
{
    /// <summary>
    /// A room component that marks rooms where no events should take place.
    /// Prevents event spawning, monster houses, and other dynamic content from appearing in this room.
    /// </summary>
    [Serializable]
    public class NoEventRoom : RoomComponent
    {
        /// <inheritdoc/>
        public override RoomComponent Clone() { return new NoEventRoom(); }

        /// <summary>
        /// Returns a string representation of this room component.
        /// </summary>
        /// <returns>The string "NoEvent".</returns>
        public override string ToString()
        {
            return "NoEvent";
        }
    }
}

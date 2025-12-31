using RogueElements;
using System;

namespace PMDC.LevelGen
{
    /// <summary>
    /// A room component that marks rooms as ineligible for vault entrance attachment.
    /// Prevents the level generator from connecting vault rooms to this room.
    /// </summary>
    [Serializable]
    public class UnVaultableRoom : RoomComponent
    {
        /// <inheritdoc/>
        public override RoomComponent Clone() { return new UnVaultableRoom(); }

        /// <summary>
        /// Returns the string representation of this room component.
        /// </summary>
        /// <returns>The string "UnVaultable".</returns>
        public override string ToString()
        {
            return "UnVaultable";
        }
    }
}

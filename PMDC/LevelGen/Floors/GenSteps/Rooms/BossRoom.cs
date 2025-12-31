using RogueElements;
using System;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Room component that marks a room as a boss room.
    /// This component is used during level generation to identify and flag rooms that are designated for boss encounters.
    /// Rooms marked with this component will receive special handling during floor generation, such as prioritization for placing boss monsters and appropriate environmental setup.
    /// </summary>
    [Serializable]
    public class BossRoom : RoomComponent
    {
        /// <summary>
        /// Creates a deep copy of this BossRoom component.
        /// </summary>
        /// <returns>A new BossRoom instance that is a clone of this component.</returns>
        /// <inheritdoc/>
        public override RoomComponent Clone() { return new BossRoom(); }

        /// <summary>
        /// Returns a string representation of this component.
        /// </summary>
        /// <returns>The string "BossRoom".</returns>
        /// <inheritdoc/>
        public override string ToString()
        {
            return "BossRoom";
        }
    }
}

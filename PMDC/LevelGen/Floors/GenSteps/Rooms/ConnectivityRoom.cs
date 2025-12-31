using RogueElements;
using System;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Room component that identifies a room's connectivity status within the dungeon.
    /// Used to track whether a room is on the main path, disconnected, or part of a vault.
    /// </summary>
    [Serializable]
    public class ConnectivityRoom : RoomComponent
    {
        /// <summary>
        /// Flags indicating the connectivity status of a room.
        /// </summary>
        [Flags]
        public enum Connectivity
        {
            /// <summary>
            /// No connectivity flags set.
            /// </summary>
            None = 0,
            /// <summary>
            /// Room is on the main path of the dungeon.
            /// </summary>
            Main = 1,
            /// <summary>
            /// Room is disconnected from the main dungeon structure.
            /// </summary>
            Disconnected = 2,
            /// <summary>
            /// Room is part of a vault that requires a switch to access.
            /// </summary>
            SwitchVault = 4,
            /// <summary>
            /// Room is part of a vault that requires a key to access.
            /// </summary>
            KeyVault = 8,
            /// <summary>
            /// Room is locked behind a boss encounter.
            /// </summary>
            BossLocked = 16,
            /// <summary>
            /// Room is part of a vault that requires a block/obstacle to access.
            /// </summary>
            BlockVault = 32
        }

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public ConnectivityRoom()
        { }

        /// <summary>
        /// Initializes a new instance with the specified connectivity type.
        /// </summary>
        /// <param name="connectivity">The connectivity status for this room.</param>
        public ConnectivityRoom(Connectivity connectivity)
        {
            Connection = connectivity;
        }

        /// <summary>
        /// Copy constructor for cloning.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        public ConnectivityRoom(ConnectivityRoom other)
        {
            Connection = other.Connection;
        }

        /// <summary>
        /// Gets or sets the connectivity type of this room.
        /// </summary>
        public Connectivity Connection { get; set; }

        /// <inheritdoc/>
        public override RoomComponent Clone() { return new ConnectivityRoom(this); }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "ConnectType: " + Connection;
        }
    }
}

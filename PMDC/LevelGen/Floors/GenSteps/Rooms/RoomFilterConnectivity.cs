using System;
using RogueElements;

namespace PMDC.LevelGen
{
    /// <summary>
    /// A room filter that selects rooms based on their connectivity type.
    /// Matches rooms that have any of the specified connectivity flags set.
    /// </summary>
    [Serializable]
    public class RoomFilterConnectivity : BaseRoomFilter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RoomFilterConnectivity"/> class.
        /// </summary>
        public RoomFilterConnectivity()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoomFilterConnectivity"/> class with the specified connectivity.
        /// </summary>
        /// <param name="connectivity">The connectivity types to filter for.</param>
        public RoomFilterConnectivity(ConnectivityRoom.Connectivity connectivity)
        {
            this.Connection = connectivity;
        }

        /// <summary>
        /// The connectivity types to filter for.
        /// </summary>
        public ConnectivityRoom.Connectivity Connection;

        /// <inheritdoc/>
        public override bool PassesFilter(IRoomPlan plan)
        {
            ConnectivityRoom.Connectivity testConnection = ConnectivityRoom.Connectivity.None;
            ConnectivityRoom component;
            if (plan.Components.TryGet<ConnectivityRoom>(out component))
                testConnection = component.Connection;

            return (testConnection & Connection) != ConnectivityRoom.Connectivity.None;
        }

        /// <summary>
        /// Returns a string representation of this room filter.
        /// </summary>
        /// <returns>A formatted string containing the filter type name and connectivity flags.</returns>
        public override string ToString()
        {
            return string.Format("{0}: {1}", this.GetType().GetFormattedTypeName(), this.Connection.ToString());
        }
    }
}

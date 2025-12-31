// <copyright file="SetGridEdgeComponentStep.cs" company="Audino">
// Copyright (c) Audino
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using RogueElements;
using System;
using System.Collections.Generic;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Takes all rooms in the INSIDE of a map's grid plan and gives them a specified component.
    /// These components can be used to identify the room in some way for future filtering.
    /// </summary>
    /// <typeparam name="T">The floor generation context type that implements <see cref="IRoomGridGenContext"/>.</typeparam>
    [Serializable]
    public class SetGridInnerComponentStep<T> : GenStep<T>
        where T : class, IRoomGridGenContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SetGridInnerComponentStep{T}"/> class.
        /// </summary>
        public SetGridInnerComponentStep()
        {
            this.Components = new ComponentCollection();
        }

        /// <summary>
        /// Gets or sets the room components to apply to inner rooms and halls.
        /// The components are added to all rooms and halls that are contained within the inner grid boundary.
        /// </summary>
        public ComponentCollection Components { get; set; }

        /// <summary>
        /// Applies the configured components to all inner rooms and halls in the grid plan.
        /// Inner rooms are those contained within a boundary starting at (1, 1), excluding the outer edge.
        /// All vertical and horizontal halls connecting inner rooms are also marked with these components.
        /// </summary>
        /// <param name="map">The floor generation context containing the grid plan to modify.</param>
        public override void Apply(T map)
        {
            Rect innerRect = new Rect(1, 1, map.GridPlan.GridWidth, map.GridPlan.GridHeight);
            for (int ii = 0; ii < map.GridPlan.RoomCount; ii++)
            {
                GridRoomPlan plan = map.GridPlan.GetRoomPlan(ii);
                if (innerRect.Contains(plan.Bounds))
                {
                    foreach (RoomComponent component in this.Components)
                        plan.Components.Set(component.Clone());
                }
            }

            for (int xx = innerRect.X; xx < innerRect.End.X; xx++)
            {
                for (int yy = innerRect.Y; yy < innerRect.End.Y; yy++)
                {
                    if (yy < innerRect.End.Y - 1)
                    {
                        GridHallPlan plan = map.GridPlan.GetHall(new LocRay4(new Loc(xx, yy), Dir4.Down));
                        if (plan != null)
                        {
                            foreach (RoomComponent component in this.Components)
                                plan.Components.Set(component.Clone());
                        }
                    }

                    if (xx < innerRect.End.X - 1)
                    {
                        GridHallPlan plan = map.GridPlan.GetHall(new LocRay4(new Loc(xx, yy), Dir4.Right));
                        if (plan != null)
                        {
                            foreach (RoomComponent component in this.Components)
                                plan.Components.Set(component.Clone());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns a string representation of this step, showing the type name and component count.
        /// </summary>
        /// <returns>A formatted string containing the class name and the number of components.</returns>
        public override string ToString()
        {
            return string.Format("{0}[{1}]", this.GetType().GetFormattedTypeName(), this.Components.Count);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using RogueEssence.Content;
using RogueEssence.Dungeon;
using RogueEssence.Data;
using System.Drawing;
using RogueElements;
using Avalonia.Controls;
using RogueEssence.Dev.Views;
using System.Collections;
using Avalonia;
using System.Reactive.Subjects;
using PMDC.LevelGen;

namespace RogueEssence.Dev
{
    /// <summary>
    /// Editor for <see cref="FloorNameDropZoneStep"/> objects that display floor names when entering a dungeon segment.
    /// Provides custom display strings for the editor UI, allowing easy visualization of zone step configurations.
    /// </summary>
    public class FloorNameDropZoneStepEditor : Editor<FloorNameDropZoneStep>
    {
        /// <summary>
        /// Gets a display string for the floor name zone step showing the configured name.
        /// </summary>
        /// <remarks>
        /// Formats the display as "Show Floor Name: '[name]'" to provide a user-friendly representation
        /// of the zone step in the editor UI.
        /// </remarks>
        /// <param name="obj">The <see cref="FloorNameDropZoneStep"/> object to describe.</param>
        /// <param name="type">The type of the object being edited.</param>
        /// <param name="attributes">Custom attributes on the member being edited.</param>
        /// <returns>A formatted string showing the floor name configuration (e.g., "Show Floor Name: 'Level 1'").</returns>
        public override string GetString(FloorNameDropZoneStep obj, Type type, object[] attributes)
        {
            return String.Format("{0}: '{1}'", "Show Floor Name", obj.Name);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The display name "Show Floor Name" for this editor type.</returns>
        public override string GetTypeString()
        {
            return "Show Floor Name";
        }
    }
}

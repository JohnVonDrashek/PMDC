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
    /// Editor for <see cref="SpreadHouseZoneStep"/> objects that generate monster or item houses across dungeon floors.
    /// </summary>
    /// <remarks>
    /// This editor provides UI representation for zone-level steps that spread specialized monster or item houses
    /// throughout the floors of a dungeon structure. It determines whether to display "Monster Houses" or "Item Houses"
    /// based on the configured contents of the zone step.
    /// </remarks>
    public class SpreadHouseZoneStepEditor : Editor<SpreadHouseZoneStep>
    {
        /// <summary>
        /// Gets a display string describing the type of house being spread (Monster or Item).
        /// </summary>
        /// <remarks>
        /// The display string is determined by examining the <see cref="SpreadHouseZoneStep"/> contents:
        /// If the step contains mobs, it displays "Spread Monster Houses". If it contains items, it displays
        /// "Spread Item Houses". If neither are configured, it displays "Spread  Houses" (with an empty house type).
        /// </remarks>
        /// <param name="obj">The spread house zone step to describe.</param>
        /// <param name="type">The type of the object being described.</param>
        /// <param name="attributes">Custom attributes applied to the member being edited.</param>
        /// <returns>A formatted string describing the house type, in the format "Spread [Type] Houses".</returns>
        public override string GetString(SpreadHouseZoneStep obj, Type type, object[] attributes)
        {
            string housePrefix = "";

            if (obj.Mobs.Count > 0)
            {
                housePrefix = "Monster";
            }
            else if (obj.Items.Count > 0)
            {
                housePrefix = "Item";
            }

            return String.Format("Spread {0} Houses", housePrefix);
        }

        /// <inheritdoc/>
        public override string GetTypeString()
        {
            return "Spread Houses";
        }
    }
}

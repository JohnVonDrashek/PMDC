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
    /// Editor for <see cref="SpreadVaultZoneStep"/> objects that generate monster or item vaults across dungeon floors.
    /// </summary>
    /// <remarks>
    /// This editor provides a visual representation of vault spreading configuration in the dev UI,
    /// allowing developers to identify and edit vaults that are distributed across multiple floors in a zone.
    /// </remarks>
    public class SpreadVaultZoneStepEditor : Editor<SpreadVaultZoneStep>
    {
        /// <summary>
        /// Gets a display string describing the type of vault being spread (Monster or Item).
        /// </summary>
        /// <param name="obj">The spread vault zone step to describe.</param>
        /// <param name="type">The type of the object.</param>
        /// <param name="attributes">Custom attributes on the member.</param>
        /// <returns>A formatted string in the form "Spread [VaultType] Vaults", where VaultType is either "Monster", "Item", or empty if neither type has entries.</returns>
        public override string GetString(SpreadVaultZoneStep obj, Type type, object[] attributes)
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

            return String.Format("Spread {0} Vaults", housePrefix);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The constant string "Spread Vaults" as the type display name for this editor.</returns>
        public override string GetTypeString()
        {
            return "Spread Vaults";
        }
    }
}

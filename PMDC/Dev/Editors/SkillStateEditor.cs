using System;
using System.Collections.Generic;
using System.Text;
using RogueEssence.Content;
using RogueEssence.Dungeon;
using PMDC.Dungeon;
using RogueEssence.Data;
using System.Drawing;
using RogueElements;
using Avalonia.Controls;
using RogueEssence.Dev.Views;
using System.Collections;
using Avalonia;
using System.Reactive.Subjects;

namespace RogueEssence.Dev
{
    /// <summary>
    /// Editor for <see cref="BasePowerState"/> objects that define a skill's base power value.
    /// </summary>
    /// <remarks>
    /// This editor provides a display representation of the base power stat for skills,
    /// allowing them to be viewed and edited in the development environment.
    /// </remarks>
    public class BasePowerStateEditor : Editor<BasePowerState>
    {
        /// <inheritdoc/>
        /// <remarks>
        /// Returns a formatted string displaying the power value in the format "Power: {value}".
        /// </remarks>
        public override string GetString(BasePowerState obj, Type type, object[] attributes)
        {
            return String.Format("Power: {0}", obj.Power);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Returns "Power" as the display name for this editor type.
        /// </remarks>
        public override string GetTypeString()
        {
            return "Power";
        }
    }

    /// <summary>
    /// Editor for <see cref="AdditionalEffectState"/> objects that define a skill's secondary effect chance.
    /// </summary>
    /// <remarks>
    /// This editor provides a display representation of the additional effect chance stat for skills,
    /// allowing the probability of secondary effects to be viewed and edited in the development environment.
    /// </remarks>
    public class AdditionalEffectStateEditor : Editor<AdditionalEffectState>
    {
        /// <inheritdoc/>
        /// <remarks>
        /// Returns a formatted string displaying the effect chance percentage in the format "Effect Chance: {value}%".
        /// </remarks>
        public override string GetString(AdditionalEffectState obj, Type type, object[] attributes)
        {
            return String.Format("Effect Chance: {0}%", obj.EffectChance);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Returns "Effect Chance" as the display name for this editor type.
        /// </remarks>
        public override string GetTypeString()
        {
            return "Effect Chance";
        }
    }
}

using System;
using RogueElements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using RogueEssence.Content;
using RogueEssence.Dev;

namespace PMDC.Dev
{
    /// <summary>
    /// Character sheet operation that collapses and optimizes sprite offsets across all animations.
    /// </summary>
    /// <remarks>
    /// This operation consolidates offset data to reduce redundancy in the character sheet by calling
    /// the Collapse method on the CharSheet with both offset and frame optimization enabled.
    /// This is a global operation that affects all animations in the character sheet rather than
    /// specific animation indices.
    /// </remarks>
    [Serializable]
    public class CharSheetCollapseOffsetsOp : CharSheetOp
    {
        /// <summary>
        /// Gets the animation indices this operation applies to.
        /// </summary>
        /// <returns>An empty array, indicating this operation applies globally to all animations.</returns>
        public override int[] Anims { get { return new int[0]; } }

        /// <summary>
        /// Gets the display name of this operation.
        /// </summary>
        /// <returns>The string "Collapse Offsets".</returns>
        public override string Name { get { return "Collapse Offsets"; } }

        /// <summary>
        /// Applies offset collapsing to the character sheet, optimizing both offsets and frame data.
        /// </summary>
        /// <remarks>
        /// This method calls the Collapse method on the provided CharSheet with both offset optimization
        /// (first parameter: true) and frame data optimization (second parameter: true) enabled.
        /// The animation index parameter is not used since this is a global operation.
        /// </remarks>
        /// <param name="sheet">The character sheet to modify.</param>
        /// <param name="anim">The animation index. This parameter is unused for this global operation.</param>
        public override void Apply(CharSheet sheet, int anim)
        {
            sheet.Collapse(true, true);
        }
    }

}

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
    /// Character sheet operation that standardizes the alignment of sprite animations.
    /// Adjusts frame offsets and shadow positions to match a standard center point.
    /// </summary>
    /// <remarks>
    /// This operation normalizes sprite alignment across all animation sequences, ensuring
    /// consistent positioning of character sprites within the game. It skips animation indices
    /// 4 and 6, which are typically reserved for special animations.
    /// </remarks>
    [Serializable]
    public class CharSheetAlignOp : CharSheetOp
    {
        /// <summary>
        /// The standard center point for sprite alignment.
        /// </summary>
        /// <remarks>
        /// Coordinates are specified as (X: 0, Y: 4), representing the normalized anchor point
        /// that all animation frames should align to.
        /// </remarks>
        private static Loc StandardCenter = new Loc(0, 4);

        /// <summary>
        /// Gets the animation indices this operation applies to (all except indices 4 and 6).
        /// </summary>
        /// <returns>
        /// An array of animation indices from 0 to <see cref="GraphicsManager.Actions"/>.Count,
        /// excluding indices 4 and 6.
        /// </returns>
        /// <remarks>
        /// Indices 4 and 6 are excluded because they typically represent special or reserved
        /// animation states that should not be automatically aligned.
        /// </remarks>
        public override int[] Anims
        {
            get
            {
                List<int> all = new List<int>();
                for (int ii = 0; ii < GraphicsManager.Actions.Count; ii++)
                {
                    if (ii != 4 && ii != 6)
                        all.Add(ii);
                }
                return all.ToArray();
            }
        }

        /// <summary>
        /// Gets the display name of this operation.
        /// </summary>
        /// <returns>A string describing this operation for display in the UI.</returns>
        public override string Name { get => "Standardize Alignment"; }

        /// <summary>
        /// Applies alignment standardization to the specified animation, adjusting all frame
        /// offsets and shadow positions to match the standard center point.
        /// </summary>
        /// <param name="sheet">The character sheet to modify.</param>
        /// <param name="anim">The animation index to process.</param>
        /// <remarks>
        /// This method iterates through all sequences and frames in the specified animation.
        /// For each sequence, it calculates the offset difference between the standard center
        /// and the first frame's shadow offset, then applies this difference to all frames
        /// in the sequence to achieve consistent alignment. After processing all frames,
        /// it collapses the sheet to optimize the frame data.
        /// </remarks>
        public override void Apply(CharSheet sheet, int anim)
        {
            for (int ii = 0; ii < sheet.AnimData[anim].Sequences.Count; ii++)
            {
                CharAnimFrame firstFrame = sheet.AnimData[anim].Sequences[ii].Frames[0];
                Loc diff = StandardCenter - firstFrame.ShadowOffset;
                for (int jj = 0; jj < sheet.AnimData[anim].Sequences[ii].Frames.Count; jj++)
                {
                    CharAnimFrame origFrame = sheet.AnimData[anim].Sequences[ii].Frames[jj];
                    CharAnimFrame newFrame = new CharAnimFrame(origFrame);
                    newFrame.Offset = newFrame.Offset + diff;
                    newFrame.ShadowOffset = newFrame.ShadowOffset + diff;
                    sheet.AnimData[anim].Sequences[ii].Frames[jj] = newFrame;
                }
            }
            sheet.Collapse(false, true);
        }
    }

}

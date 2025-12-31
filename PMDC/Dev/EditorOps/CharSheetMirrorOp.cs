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
    /// Editor operation that mirrors character sprite animations horizontally.
    /// Copies animation frames from one direction to its opposite, flipping offsets and shadows
    /// to create symmetric animations. Can mirror from left-to-right or right-to-left.
    /// </summary>
    [Serializable]
    public class CharSheetMirrorOp : CharSheetOp
    {
        /// <summary>
        /// Determines the direction of the mirror operation.
        /// When <c>true</c>, mirrors from right-to-left; when <c>false</c>, mirrors from left-to-right.
        /// </summary>
        public bool StartRight;

        /// <summary>
        /// Gets the animation indices this operation applies to.
        /// </summary>
        /// <returns>An array containing all animation indices available in the graphics manager.</returns>
        public override int[] Anims
        {
            get
            {
                List<int> all = new List<int>();
                for (int ii = 0; ii < GraphicsManager.Actions.Count; ii++)
                    all.Add(ii);
                return all.ToArray();
            }
        }

        /// <summary>
        /// Gets the display name of this operation based on the mirror direction.
        /// </summary>
        /// <returns>"Mirror Right->Left" if <see cref="StartRight"/> is true; otherwise "Mirror Left->Right".</returns>
        public override string Name { get { return StartRight ? "Mirror Right->Left" : "Mirror Left->Right"; } }

        /// <inheritdoc/>
        /// <remarks>
        /// Mirrors the specified animation by copying frames from one direction to its opposite direction,
        /// with horizontally-flipped offsets and shadow offsets. This creates symmetric animations when
        /// the source direction matches the <see cref="StartRight"/> setting.
        /// </remarks>
        public override void Apply(CharSheet sheet, int anim)
        {
            for (int ii = 0; ii < sheet.AnimData[anim].Sequences.Count; ii++)
            {
                DirH dirH;
                DirV dirV;
                DirExt.Separate((Dir8)ii, out dirH, out dirV);
                if (dirH == DirH.Left && !StartRight || dirH == DirH.Right && StartRight)
                {
                    dirH = dirH.Reverse();
                    Dir8 flipDir = DirExt.Combine(dirH, dirV);
                    List<CharAnimFrame> frames = new List<CharAnimFrame>();
                    for (int jj = 0; jj < sheet.AnimData[anim].Sequences[ii].Frames.Count; jj++)
                    {
                        CharAnimFrame origFrame = sheet.AnimData[anim].Sequences[ii].Frames[jj];
                        CharAnimFrame newFrame = new CharAnimFrame(origFrame);
                        newFrame.Flip = !newFrame.Flip;
                        newFrame.Offset = new Loc(-newFrame.Offset.X, newFrame.Offset.Y);
                        newFrame.ShadowOffset = new Loc(-newFrame.ShadowOffset.X, newFrame.ShadowOffset.Y);
                        frames.Add(newFrame);
                    }
                    sheet.AnimData[anim].Sequences[(int)flipDir].Frames = frames;
                }
            }
            sheet.Collapse(false, true);
        }
    }

}

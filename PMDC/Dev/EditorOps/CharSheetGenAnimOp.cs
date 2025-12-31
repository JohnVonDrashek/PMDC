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
    /// Character sheet operation that generates simple procedural animations from the idle pose.
    /// Creates swing, double-hit, and rotation animations using predefined offset patterns.
    /// This operation generates three animation types (indices 40-42) by transforming idle frames.
    /// </summary>
    [Serializable]
    public class CharSheetGenAnimOp : CharSheetOp
    {
        /// <summary>
        /// Predefined frame offsets for swing animation across all 8 directions.
        /// Array dimensions: [8 directions][9 animation frames].
        /// Each offset represents pixel displacement (X, Y) for the sprite frame.
        /// </summary>
        private static Loc[,] swing_offsets = new Loc[8, 9] {
            { new Loc(0, 0), new Loc(6, 3), new Loc(8, 9), new Loc(7, 18), new Loc(0, 22), new Loc(-7, 18), new Loc(-8, 9), new Loc(-6, 3), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(-11, 0), new Loc(-21, 3), new Loc(-26, 10), new Loc(-20, 18), new Loc(-11, 19), new Loc(-3, 15), new Loc(0, 7), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(-5, -5), new Loc(-13, -6), new Loc(-20, -5), new Loc(-23, 0), new Loc(-20, 4), new Loc(-14, 7), new Loc(-6, 5), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(1, -6), new Loc(-4, -17), new Loc(-15, -22), new Loc(-21, -21), new Loc(-24, -13), new Loc(-19, -4), new Loc(-9, 0), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(-8, -4), new Loc(-9, -12), new Loc(-7, -20), new Loc(0, -22), new Loc(7, -20), new Loc(9, -10), new Loc(8, -4), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(-1, -6), new Loc(4, -17), new Loc(15, -22), new Loc(21, -21), new Loc(24, -13), new Loc(19, -4), new Loc(9, 0), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(5, -5), new Loc(13, -6), new Loc(20, -5), new Loc(23, 0), new Loc(20, 4), new Loc(14, 7), new Loc(6, 5), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(11, 0), new Loc(21, 3), new Loc(26, 10), new Loc(20, 18), new Loc(11, 19), new Loc(3, 15), new Loc(0, 7), new Loc(0, 0)}
        };

        /// <summary>
        /// Predefined shadow offsets for swing animation across all 8 directions.
        /// Array dimensions: [8 directions][9 animation frames].
        /// Each offset represents pixel displacement (X, Y) for the shadow sprite frame.
        /// </summary>
        private static Loc[,] swing_shadows = new Loc[8, 9] {
            { new Loc(0, 0), new Loc(6, 3), new Loc(8, 9), new Loc(7, 18), new Loc(0, 22), new Loc(-7, 18), new Loc(-8, 9), new Loc(-6, 3), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(-11, 0), new Loc(-21, 3), new Loc(-26, 10), new Loc(-20, 18), new Loc(-11, 19), new Loc(-3, 15), new Loc(0, 7), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(-5, -5), new Loc(-13, -6), new Loc(-20, -5), new Loc(-23, 0), new Loc(-20, 4), new Loc(-14, 7), new Loc(-6, 5), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(1, -6), new Loc(-4, -17), new Loc(-15, -22), new Loc(-21, -21), new Loc(-24, -13), new Loc(-19, -4), new Loc(-9, 0), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(-8, -4), new Loc(-9, -12), new Loc(-7, -20), new Loc(0, -22), new Loc(7, -20), new Loc(9, -10), new Loc(8, -4), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(-1, -6), new Loc(4, -17), new Loc(15, -22), new Loc(21, -21), new Loc(24, -13), new Loc(19, -4), new Loc(9, 0), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(5, -5), new Loc(13, -6), new Loc(20, -5), new Loc(23, 0), new Loc(20, 4), new Loc(14, 7), new Loc(6, 5), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(11, 0), new Loc(21, 3), new Loc(26, 10), new Loc(20, 18), new Loc(11, 19), new Loc(3, 15), new Loc(0, 7), new Loc(0, 0)}
        };

        /// <summary>
        /// Frame durations for each frame of the swing animation in game ticks.
        /// Length: 9 frames. Determines how long each frame displays during animation playback.
        /// </summary>
        private static int[] swing_durations = new int[] { 2, 1, 2, 2, 3, 2, 2, 1, 1 };


        /// <summary>
        /// Predefined frame offsets for double-hit animation across all 8 directions.
        /// Array dimensions: [8 directions][16 animation frames].
        /// Each offset represents pixel displacement (X, Y) for the sprite frame during the double-hit attack.
        /// </summary>
        private static Loc[,] double_offsets = new Loc[8, 16] {
            { new Loc(0, 0), new Loc(6, 0), new Loc(-6, 0), new Loc(10, 0), new Loc(-10, 0), new Loc(12, 0), new Loc(-12, 0), new Loc(13, 0), new Loc(-13, 0), new Loc(12, 0), new Loc(-12, 0), new Loc(10, 0), new Loc(-10, 0), new Loc(6, 0), new Loc(-6, 0), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(-6, -6), new Loc(6, 6), new Loc(-10, -10), new Loc(10, 10), new Loc(-12, -12), new Loc(12, 12), new Loc(-13, -13), new Loc(13, 13), new Loc(-12, -12), new Loc(12, 12), new Loc(-10, -10), new Loc(10, 10), new Loc(-6, -6), new Loc(6, 6), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(0, -6), new Loc(0, 6), new Loc(0, -10), new Loc(0, 10), new Loc(0, -12), new Loc(0, 12), new Loc(0, -13), new Loc(0, 13), new Loc(0, -12), new Loc(0, 12), new Loc(0, -10), new Loc(0, 10), new Loc(0, -6), new Loc(0, 6), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(6, -6), new Loc(-6, 6), new Loc(10, -10), new Loc(-10, 10), new Loc(12, -12), new Loc(-12, 12), new Loc(13, -13), new Loc(-13, 13), new Loc(12, -12), new Loc(-12, 12), new Loc(10, -10), new Loc(-10, 10), new Loc(6, -6), new Loc(-6, 6), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(6, 0), new Loc(-6, 0), new Loc(10, 0), new Loc(-10, 0), new Loc(12, 0), new Loc(-12, 0), new Loc(13, 0), new Loc(-13, 0), new Loc(12, 0), new Loc(-12, 0), new Loc(10, 0), new Loc(-10, 0), new Loc(6, 0), new Loc(-6, 0), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(-6, -6), new Loc(6, 6), new Loc(-10, -10), new Loc(10, 10), new Loc(-12, -12), new Loc(12, 12), new Loc(-13, -13), new Loc(13, 13), new Loc(-12, -12), new Loc(12, 12), new Loc(-10, -10), new Loc(10, 10), new Loc(-6, -6), new Loc(6, 6), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(0, -6), new Loc(0, 6), new Loc(0, -10), new Loc(0, 10), new Loc(0, -12), new Loc(0, 12), new Loc(0, -13), new Loc(0, 13), new Loc(0, -12), new Loc(0, 12), new Loc(0, -10), new Loc(0, 10), new Loc(0, -6), new Loc(0, 6), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(6, -6), new Loc(-6, 6), new Loc(10, -10), new Loc(-10, 10), new Loc(12, -12), new Loc(-12, 12), new Loc(13, -13), new Loc(-13, 13), new Loc(12, -12), new Loc(-12, 12), new Loc(10, -10), new Loc(-10, 10), new Loc(6, -6), new Loc(-6, 6), new Loc(0, 0)},
        };

        /// <summary>
        /// Predefined shadow offsets for double-hit animation across all 8 directions.
        /// Array dimensions: [8 directions][16 animation frames].
        /// Each offset represents pixel displacement (X, Y) for the shadow sprite frame during the double-hit attack.
        /// </summary>
        private static Loc[,] double_shadows = new Loc[8, 16] {
            { new Loc(0, 0), new Loc(6, 0), new Loc(-6, 0), new Loc(10, 0), new Loc(-10, 0), new Loc(12, 0), new Loc(-12, 0), new Loc(13, 0), new Loc(-13, 0), new Loc(12, 0), new Loc(-12, 0), new Loc(10, 0), new Loc(-10, 0), new Loc(6, 0), new Loc(-6, 0), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(-6, -6), new Loc(6, 6), new Loc(-10, -10), new Loc(10, 10), new Loc(-12, -12), new Loc(12, 12), new Loc(-13, -13), new Loc(13, 13), new Loc(-12, -12), new Loc(12, 12), new Loc(-10, -10), new Loc(10, 10), new Loc(-6, -6), new Loc(6, 6), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(0, -6), new Loc(0, 6), new Loc(0, -10), new Loc(0, 10), new Loc(0, -12), new Loc(0, 12), new Loc(0, -13), new Loc(0, 13), new Loc(0, -12), new Loc(0, 12), new Loc(0, -10), new Loc(0, 10), new Loc(0, -6), new Loc(0, 6), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(6, -6), new Loc(-6, 6), new Loc(10, -10), new Loc(-10, 10), new Loc(12, -12), new Loc(-12, 12), new Loc(13, -13), new Loc(-13, 13), new Loc(12, -12), new Loc(-12, 12), new Loc(10, -10), new Loc(-10, 10), new Loc(6, -6), new Loc(-6, 6), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(6, 0), new Loc(-6, 0), new Loc(10, 0), new Loc(-10, 0), new Loc(12, 0), new Loc(-12, 0), new Loc(13, 0), new Loc(-13, 0), new Loc(12, 0), new Loc(-12, 0), new Loc(10, 0), new Loc(-10, 0), new Loc(6, 0), new Loc(-6, 0), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(-6, -6), new Loc(6, 6), new Loc(-10, -10), new Loc(10, 10), new Loc(-12, -12), new Loc(12, 12), new Loc(-13, -13), new Loc(13, 13), new Loc(-12, -12), new Loc(12, 12), new Loc(-10, -10), new Loc(10, 10), new Loc(-6, -6), new Loc(6, 6), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(0, -6), new Loc(0, 6), new Loc(0, -10), new Loc(0, 10), new Loc(0, -12), new Loc(0, 12), new Loc(0, -13), new Loc(0, 13), new Loc(0, -12), new Loc(0, 12), new Loc(0, -10), new Loc(0, 10), new Loc(0, -6), new Loc(0, 6), new Loc(0, 0)},
            { new Loc(0, 0), new Loc(6, -6), new Loc(-6, 6), new Loc(10, -10), new Loc(-10, 10), new Loc(12, -12), new Loc(-12, 12), new Loc(13, -13), new Loc(-13, 13), new Loc(12, -12), new Loc(-12, 12), new Loc(10, -10), new Loc(-10, 10), new Loc(6, -6), new Loc(-6, 6), new Loc(0, 0)},
        };

        /// <summary>
        /// Frame durations for each frame of the double-hit animation in game ticks.
        /// Length: 16 frames. Determines how long each frame displays during animation playback.
        /// </summary>
        private static int[] double_durations = new int[] { 2, 2, 2, 2, 2, 2, 3, 3, 3, 2, 3, 2, 2, 2, 2, 2 };


        /// <summary>
        /// Gets the animation indices this operation can generate.
        /// </summary>
        /// <value>Array containing animation indices: 40 (swing), 41 (double-hit), 42 (rotate).</value>
        public override int[] Anims { get { return new int[3] { 40, 41, 42 }; } }

        /// <summary>
        /// Gets the display name of this editor operation.
        /// </summary>
        /// <value>The localized name displayed in the character sheet editor UI.</value>
        public override string Name { get { return "Generate Simple Anim"; } }

        /// <summary>
        /// Generates a procedural animation based on the idle pose frames.
        /// Creates one of three animation types: swing attack (40), double-hit (41), or rotation (42).
        /// Each animation is generated for all 8 directional variants by applying predefined offset transformations to idle frames.
        /// </summary>
        /// <param name="sheet">The character sheet whose animation data will be modified.</param>
        /// <param name="anim">The animation index to generate. Valid values: 40 (swing), 41 (double-hit), 42 (rotate).</param>
        public override void Apply(CharSheet sheet, int anim)
        {
            CharAnimGroup charAnim = new CharAnimGroup();
            if (anim == 40)
            {
                charAnim.InternalIndex = 8;
                charAnim.HitFrame = 5;
                charAnim.ReturnFrame = 5;
                for (int ii = 0; ii < sheet.AnimData[GraphicsManager.IdleAction].Sequences.Count; ii++)
                {
                    CharAnimSequence newSequence = new CharAnimSequence();
                    int curEndTime = 0;
                    for (int jj = 0; jj < DirExt.DIR8_COUNT + 1; jj++)
                    {
                        curEndTime += swing_durations[jj];
                        int dirIdx = (ii + jj) % DirExt.DIR8_COUNT;
                        if (ii > 0 && ii < 4)
                            dirIdx = (ii + DirExt.DIR8_COUNT - jj) % DirExt.DIR8_COUNT;
                        CharAnimSequence sequence = sheet.AnimData[GraphicsManager.IdleAction].Sequences[dirIdx];
                        CharAnimFrame frame = new CharAnimFrame(sequence.Frames[0]);
                        frame.Offset = frame.Offset + swing_offsets[ii, jj];
                        frame.ShadowOffset = frame.ShadowOffset + swing_shadows[ii, jj];
                        frame.EndTime = curEndTime;
                        newSequence.Frames.Add(frame);
                    }
                    charAnim.Sequences.Add(newSequence);
                }
            }
            else if (anim == 41)
            {
                charAnim.InternalIndex = 9;
                for (int ii = 0; ii < sheet.AnimData[GraphicsManager.IdleAction].Sequences.Count; ii++)
                {
                    CharAnimSequence sequence = sheet.AnimData[GraphicsManager.IdleAction].Sequences[ii];
                    CharAnimSequence newSequence = new CharAnimSequence();
                    int curEndTime = 0;
                    for (int jj = 0; jj < double_durations.Length; jj++)
                    {
                        curEndTime += double_durations[jj];
                        CharAnimFrame frame = new CharAnimFrame(sequence.Frames[0]);
                        frame.Offset = frame.Offset + double_offsets[ii, jj];
                        frame.ShadowOffset = frame.ShadowOffset + double_shadows[ii, jj];
                        frame.EndTime = curEndTime;
                        newSequence.Frames.Add(frame);
                    }
                    charAnim.Sequences.Add(newSequence);
                }
            }
            else if (anim == 42)
            {
                charAnim.InternalIndex = 12;
                charAnim.HitFrame = 8;
                for (int ii = 0; ii < sheet.AnimData[GraphicsManager.IdleAction].Sequences.Count; ii++)
                {
                    CharAnimSequence newSequence = new CharAnimSequence();
                    int curEndTime = 0;
                    for (int jj = 0; jj < DirExt.DIR8_COUNT + 1; jj++)
                    {
                        curEndTime += 2;
                        CharAnimSequence sequence = sheet.AnimData[GraphicsManager.IdleAction].Sequences[(ii + jj) % DirExt.DIR8_COUNT];
                        CharAnimFrame frame = new CharAnimFrame(sequence.Frames[0]);
                        frame.EndTime = curEndTime;
                        newSequence.Frames.Add(frame);
                    }
                    charAnim.Sequences.Add(newSequence);
                }
            }
            sheet.AnimData[anim] = charAnim;
        }
    }

}

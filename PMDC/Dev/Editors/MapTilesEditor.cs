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

namespace RogueEssence.Dev
{
    /// <summary>
    /// Editor for 2D tile arrays that provides a text-based map editing interface.
    /// Uses ASCII characters to represent different tile types for easy visual editing.
    /// Supports the following ASCII representations:
    /// - '.' for floor tiles
    /// - 'X' for unbreakable tiles
    /// - '#' for wall tiles
    /// - '~' for water tiles
    /// - '^' for lava tiles
    /// - '_' for pit tiles
    /// - '?' for unknown tile types
    /// </summary>
    public class MapTilesEditor : Editor<ITile[][]>
    {
        /// <summary>
        /// Gets whether this editor uses subgrouping by default.
        /// </summary>
        /// <value>Always returns <c>true</c> to enable subgroup organization in the editor.</value>
        public override bool DefaultSubgroup => true;

        /// <summary>
        /// Gets whether this editor uses decoration by default.
        /// </summary>
        /// <value>Always returns <c>false</c> to disable decorative elements in the editor.</value>
        public override bool DefaultDecoration => false;

        /// <summary>
        /// <inheritdoc/>
        /// Loads the tile map into a text box control using ASCII representation.
        /// Converts a 2D array of tiles into a multi-line text representation where each character
        /// represents a tile type. The text box is configured with monospace font for proper alignment.
        /// </summary>
        /// <param name="control">The parent <see cref="StackPanel"/> control to add the text box to.</param>
        /// <param name="parent">The name of the parent object that contains this member.</param>
        /// <param name="parentType">The type of the parent object.</param>
        /// <param name="name">The name of the member being edited.</param>
        /// <param name="type">The type of the member being edited.</param>
        /// <param name="attributes">Custom attributes applied to the member, if any.</param>
        /// <param name="member">The 2D tile array to load and display in the text box. May be null.</param>
        /// <param name="subGroupStack">The type stack for nested subgroups, used for organizational purposes.</param>
        public override void LoadWindowControls(StackPanel control, string parent, Type parentType, string name, Type type, object[] attributes, ITile[][] member, Type[] subGroupStack)
        {
            //for strings, use an edit textbox
            TextBox txtValue = new TextBox();
            txtValue.Height = 100;
            txtValue.AcceptsReturn = true;
            StringBuilder str = new StringBuilder();
            Tile floor = new Tile(DataManager.Instance.GenFloor);
            Tile impassable = new Tile("unbreakable");
            Tile wall = new Tile("wall");
            Tile water = new Tile("water");
            Tile lava = new Tile("lava");
            Tile pit = new Tile("pit");
            if (member != null && member.Length > 0)
            {
                for (int yy = 0; yy < member[0].Length; yy++)
                {
                    for (int xx = 0; xx < member.Length; xx++)
                    {
                        ITile tile = member[xx][yy];
                        if (tile.TileEquivalent(floor))
                            str.Append('.');
                        else if (tile.TileEquivalent(impassable))
                            str.Append('X');
                        else if (tile.TileEquivalent(wall))
                            str.Append('#');
                        else if (tile.TileEquivalent(water))
                            str.Append('~');
                        else if (tile.TileEquivalent(lava))
                            str.Append('^');
                        else if (tile.TileEquivalent(pit))
                            str.Append('_');
                        else
                            str.Append('?');
                    }
                    if (yy < member[0].Length - 1)
                        str.Append('\n');
                }
            }

            txtValue.Text = str.ToString();
            txtValue.FontFamily = new Avalonia.Media.FontFamily("Courier New");
            control.Children.Add(txtValue);
        }


        /// <summary>
        /// <inheritdoc/>
        /// Saves the text box content back to a tile array, parsing ASCII characters to tile types.
        /// Converts the multi-line text representation back into a 2D array of tiles by reading each character
        /// and creating the appropriate tile type. Unrecognized characters default to floor tiles.
        /// </summary>
        /// <param name="control">The parent <see cref="StackPanel"/> control containing the text box.</param>
        /// <param name="name">The name of the member being saved.</param>
        /// <param name="type">The type of the member being saved.</param>
        /// <param name="attributes">Custom attributes applied to the member, if any.</param>
        /// <param name="subGroupStack">The type stack for nested subgroups, used for organizational purposes.</param>
        /// <returns>A 2D array of <see cref="ITile"/> objects parsed from the text box content.
        /// The array dimensions are [width][height], where width is the length of the first line
        /// and height is the number of lines in the text box.</returns>
        public override ITile[][] SaveWindowControls(StackPanel control, string name, Type type, object[] attributes, Type[] subGroupStack)
        {
            int controlIndex = 0;

            TextBox txtValue = (TextBox)control.Children[controlIndex];
            string[] level = txtValue.Text.Split('\n');

            Tile[][] tiles = new Tile[level[0].Length][];
            for (int xx = 0; xx < level[0].Length; xx++)
            {
                tiles[xx] = new Tile[level.Length];
                for (int yy = 0; yy < level.Length; yy++)
                {
                    if (level[yy][xx] == 'X')
                        tiles[xx][yy] = new Tile("unbreakable");
                    else if (level[yy][xx] == '#')
                        tiles[xx][yy] = new Tile("wall");
                    else if (level[yy][xx] == '~')
                        tiles[xx][yy] = new Tile("water");
                    else if (level[yy][xx] == '^')
                        tiles[xx][yy] = new Tile("lava");
                    else if (level[yy][xx] == '_')
                        tiles[xx][yy] = new Tile("pit");
                    else
                        tiles[xx][yy] = new Tile(DataManager.Instance.GenFloor);
                }
            }

            return tiles;
        }
    }
}

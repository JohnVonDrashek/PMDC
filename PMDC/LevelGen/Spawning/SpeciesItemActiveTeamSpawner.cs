using RogueElements;
using System;
using System.IO;
using System.Collections.Generic;
using RogueEssence.Dungeon;
using RogueEssence.LevelGen;
using RogueEssence;
using RogueEssence.Data;
using System.Xml;
using Newtonsoft.Json;
using RogueEssence.Dev;
using PMDC.Data;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Spawner that yields items specific to the species of active team members from the current save file.
    /// </summary>
    /// <remarks>
    /// This spawner retrieves species from the current party/team in the game progress and uses them to determine
    /// which items to spawn. It supports an exception list allowing certain species to be replaced with a fallback species.
    /// </remarks>
    /// <typeparam name="TGenContext">The map generation context type, must inherit from BaseMapGenContext.</typeparam>
    [Serializable]
    public class SpeciesItemActiveTeamSpawner<TGenContext> : SpeciesItemSpawner<TGenContext>
        where TGenContext : BaseMapGenContext
    {
        /// <inheritdoc/>
        public SpeciesItemActiveTeamSpawner()
        {
        }

        /// <summary>
        /// Initializes a new instance with rarity range, amount, exception list, and fallback species.
        /// </summary>
        /// <param name="rarity">The rarity range for item selection.</param>
        /// <param name="amount">The random range for the number of items to spawn.</param>
        /// <param name="exceptFor">A set of species IDs that should use the fallback species instead of themselves.</param>
        /// <param name="exceptInstead">The species ID to use as a fallback when a team member's species is in the exception list.</param>
        public SpeciesItemActiveTeamSpawner(IntRange rarity, RandRange amount, HashSet<string> exceptFor, string exceptInstead) : base(rarity, amount)
        {
            ExceptFor = exceptFor;
            ExceptInstead = exceptInstead;
        }

        /// <summary>
        /// Gets or sets the species IDs that should use the fallback species instead of themselves.
        /// </summary>
        [DataType(1, DataManager.DataType.Monster, false)]
        public HashSet<string> ExceptFor { get; set; }

        /// <summary>
        /// Gets or sets the species ID to use when a team member's species is in the ExceptFor set.
        /// </summary>
        [DataType(0, DataManager.DataType.Monster, false)]
        public string ExceptInstead { get; set; }

        /// <summary>
        /// Returns the species of each active team member, substituting exceptions as needed.
        /// </summary>
        /// <remarks>
        /// Iterates through all players in the currently active team from the save file.
        /// For each player, yields either the fallback species (if the player's species is in ExceptFor)
        /// or the player's actual species. If there is no active game progress or team, yields nothing.
        /// </remarks>
        /// <param name="map">The map generation context (unused).</param>
        /// <returns>An enumerable of species IDs from the active team members.</returns>
        public override IEnumerable<string> GetPossibleSpecies(TGenContext map)
        {
            GameProgress progress = DataManager.Instance.Save;
            if (progress != null && progress.ActiveTeam != null)
            {
                foreach (Character chara in progress.ActiveTeam.Players)
                {
                    if (ExceptFor.Contains(chara.BaseForm.Species))
                        yield return ExceptInstead;
                    else
                        yield return chara.BaseForm.Species;

                }
            }
        }
    }
}

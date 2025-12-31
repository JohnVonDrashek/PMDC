// <copyright file="IPatternWaterStep.cs" company="Audino">
// Copyright (c) Audino
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using RogueElements;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Interface for water generation steps that place water patterns loaded from map files.
    /// </summary>
    /// <remarks>
    /// This interface extends <see cref="IWaterStep"/> to provide a contract for water placement strategies
    /// that use predefined patterns or templates. Implementations use this interface to specify how many
    /// instances of a water pattern should be placed during the floor generation process.
    /// </remarks>
    public interface IPatternWaterStep : IWaterStep
    {
        /// <summary>
        /// Gets or sets the range of water patterns to place on the map during floor generation.
        /// </summary>
        /// <value>
        /// A <see cref="RandRange"/> that specifies the minimum and maximum number of water patterns
        /// to place. The actual number of patterns placed will be randomly determined within this range.
        /// </value>
        RandRange Amount { get; set; }
    }
}
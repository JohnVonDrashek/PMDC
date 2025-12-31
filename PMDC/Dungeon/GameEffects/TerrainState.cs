using System;
using RogueEssence.LevelGen;
using System.Collections.Generic;
using RogueElements;
using RogueEssence;
using RogueEssence.Dev;
using RogueEssence.Dungeon;
using Newtonsoft.Json;

namespace PMDC.Dungeon
{
    /// <summary>
    /// Terrain state indicating a water tile.
    /// Water tiles can be traversed by characters with water mobility.
    /// </summary>
    [Serializable]
    public class WaterTerrainState : TerrainState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WaterTerrainState"/> class.
        /// </summary>
        public WaterTerrainState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new WaterTerrainState(); }
    }

    /// <summary>
    /// Terrain state indicating a lava tile.
    /// Lava tiles can be traversed by characters with lava mobility (typically Fire types).
    /// </summary>
    [Serializable]
    public class LavaTerrainState : TerrainState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LavaTerrainState"/> class.
        /// </summary>
        public LavaTerrainState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new LavaTerrainState(); }
    }

    /// <summary>
    /// Terrain state indicating an abyss/void tile.
    /// Abyss tiles can typically only be traversed by flying characters.
    /// </summary>
    [Serializable]
    public class AbyssTerrainState : TerrainState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbyssTerrainState"/> class.
        /// </summary>
        public AbyssTerrainState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new AbyssTerrainState(); }
    }

    /// <summary>
    /// Terrain state indicating a wall tile.
    /// Wall tiles block movement for most characters except those with wall-pass abilities (Ghost types).
    /// </summary>
    [Serializable]
    public class WallTerrainState : TerrainState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WallTerrainState"/> class.
        /// </summary>
        public WallTerrainState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new WallTerrainState(); }
    }

    /// <summary>
    /// Terrain state indicating a foliage/grass tile.
    /// Foliage tiles may provide concealment or special effects for certain character types.
    /// </summary>
    [Serializable]
    public class FoliageTerrainState : TerrainState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FoliageTerrainState"/> class.
        /// </summary>
        public FoliageTerrainState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new FoliageTerrainState(); }
    }
}

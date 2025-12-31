using System;
using RogueEssence.Dungeon;
using RogueEssence.Data;
using RogueElements;
using RogueEssence.LevelGen;
using PMDC.Data;
using System.Collections.Generic;
using RogueEssence.Dev;
using RogueEssence.Script;
using NLua;
using System.Linq;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Spawns the mob if the player's random seed has a specific remainder when divided by a specific number.
    /// </summary>
    [Serializable]
    public class MobCheckVersionDiff : MobSpawnCheck
    {
        /// <summary>
        /// The number to divide the player's seed by.
        /// </summary>
        public int Div;

        /// <summary>
        /// The remainder to check for when dividing the player's seed.
        /// </summary>
        public int Remainder;

        /// <summary>
        /// Initializes a new instance of <see cref="MobCheckVersionDiff"/>.
        /// </summary>
        public MobCheckVersionDiff()
        {

        }

        /// <summary>
        /// Initializes a new instance with the specified remainder and divisor.
        /// </summary>
        /// <param name="remainder">The remainder to check for when dividing the player's seed.</param>
        /// <param name="div">The number to divide the player's seed by.</param>
        public MobCheckVersionDiff(int remainder, int div)
        {
            Div = div;
            Remainder = remainder;
        }
        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="other">The <see cref="MobCheckVersionDiff"/> instance to copy.</param>
        protected MobCheckVersionDiff(MobCheckVersionDiff other) : this()
        {
            Remainder = other.Remainder;
            Div = other.Div;
        }

        /// <inheritdoc/>
        public override MobSpawnCheck Copy() { return new MobCheckVersionDiff(this); }

        /// <inheritdoc/>
        public override bool CanSpawn()
        {
            return DataManager.Instance.Save.Rand.FirstSeed % (ulong)Div == (ulong)Remainder;
        }

    }

    /// <summary>
    /// Spawns the mob if the player's savevar matches the specified status.
    /// </summary>
    [Serializable]
    public class MobCheckSaveVar : MobSpawnCheck
    {
        /// <summary>
        /// The savevar to query.
        /// </summary>
        public string SaveVar;

        /// <summary>
        /// The status to compare to in order to allow the spawn to occur.
        /// If set to <c>true</c>, the mob spawns only if the savevar is set to <c>true</c>.
        /// If set to <c>false</c>, the mob spawns only if the savevar is set to <c>false</c>.
        /// </summary>
        public bool Status;

        /// <summary>
        /// Initializes a new instance of <see cref="MobCheckSaveVar"/>.
        /// </summary>
        public MobCheckSaveVar()
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified savevar name and status to match.
        /// </summary>
        /// <param name="saveVar">The name of the savevar to query.</param>
        /// <param name="status">The status to match for the spawn to occur.</param>
        public MobCheckSaveVar(string saveVar, bool status)
        {
            SaveVar = saveVar;
            Status = status;
        }
        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="other">The <see cref="MobCheckSaveVar"/> instance to copy.</param>
        protected MobCheckSaveVar(MobCheckSaveVar other) : this()
        {
            SaveVar = other.SaveVar;
            Status = other.Status;
        }

        /// <inheritdoc/>
        public override MobSpawnCheck Copy() { return new MobCheckSaveVar(this); }

        /// <inheritdoc/>
        public override bool CanSpawn()
        {
            object obj = LuaEngine.Instance.LuaState[LuaEngine.SCRIPT_VARS_NAME + "." + SaveVar];
            return object.Equals(true, obj) == Status;
        }

    }

    /// <summary>
    /// Spawns the mob if the map hasn't started yet. This check is currently not implemented.
    /// </summary>
    [Serializable]
    public class MobCheckMapStart : MobSpawnCheck
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MobCheckMapStart"/>.
        /// </summary>
        public MobCheckMapStart()
        {
        }

        /// <inheritdoc/>
        public override MobCheckMapStart Copy() { return new MobCheckMapStart(); }

        /// <inheritdoc/>
        public override bool CanSpawn()
        {
            throw new NotImplementedException();
        }

    }

    /// <summary>
    /// Spawns the mob if the current time of day matches the specified time. This check is currently not implemented.
    /// </summary>
    [Serializable]
    public class MobCheckTimeOfDay : MobSpawnCheck
    {
        /// <summary>
        /// The time of day to check for.
        /// </summary>
        public TimeOfDay Time;

        /// <summary>
        /// Initializes a new instance of <see cref="MobCheckTimeOfDay"/>.
        /// </summary>
        public MobCheckTimeOfDay()
        {

        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="other">The <see cref="MobCheckTimeOfDay"/> instance to copy.</param>
        protected MobCheckTimeOfDay(MobCheckTimeOfDay other) : this()
        {
            Time = other.Time;
        }
        /// <inheritdoc/>
        public override MobSpawnCheck Copy() { return new MobCheckTimeOfDay(this); }

        /// <inheritdoc/>
        public override bool CanSpawn()
        {
            return DataManager.Instance.Save.Time == Time;
        }

    }
}

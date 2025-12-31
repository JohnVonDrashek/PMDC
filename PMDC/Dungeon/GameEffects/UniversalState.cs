using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using RogueEssence.Data;
using RogueEssence.Dev;
using RogueEssence.Dungeon;

namespace PMDC.Dungeon
{
    /// <summary>
    /// Universal state storing the type effectiveness chart for elemental matchups.
    /// Used to calculate damage multipliers based on attacking and defending types.
    /// </summary>
    [Serializable]
    public class ElementTableState : UniversalState
    {
        /// <summary>
        /// 2D array of type matchup multipliers. TypeMatchup[attackType][defendType] gives the effectiveness.
        /// </summary>
        public int[][] TypeMatchup;

        /// <summary>
        /// Array of effectiveness values corresponding to damage multiplier tiers.
        /// </summary>
        public int[] Effectiveness;

        /// <summary>
        /// Maps element string IDs to their numeric indices in the matchup table.
        /// </summary>
        public Dictionary<string, int> TypeMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementTableState"/> class with empty tables.
        /// </summary>
        public ElementTableState() { TypeMatchup = new int[0][]; Effectiveness = new int[0]; TypeMap = new Dictionary<string, int>(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementTableState"/> class by copying another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected ElementTableState(ElementTableState other)
        {
            TypeMatchup = new int[other.TypeMatchup.Length][];
            for (int ii = 0; ii < TypeMatchup.Length; ii++)
            {
                TypeMatchup[ii] = new int[other.TypeMatchup[ii].Length];
                other.TypeMatchup[ii].CopyTo(TypeMatchup[ii], 0);
            }
            Effectiveness = new int[other.Effectiveness.Length];
            other.Effectiveness.CopyTo(Effectiveness, 0);
            TypeMap = new Dictionary<string, int>();
            foreach (string key in other.TypeMap.Keys)
                TypeMap[key] = other.TypeMap[key];
        }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new ElementTableState(this); }

        /// <summary>
        /// Gets the type effectiveness multiplier for an attacking type against a defending type.
        /// </summary>
        /// <param name="attacking">The attacking element type ID.</param>
        /// <param name="defending">The defending element type ID.</param>
        /// <returns>The effectiveness value from the matchup table.</returns>
        public int GetMatchup(string attacking, string defending)
        {
            int attackIdx = TypeMap[attacking];
            int defendIdx = TypeMap[defending];
            return TypeMatchup[attackIdx][defendIdx];
        }



        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (TypeMap.Count == 0)
            {
                TypeMap["none"] = 0;
                TypeMap["bug"] = 1;
                TypeMap["dark"] = 2;
                TypeMap["dragon"] = 3;
                TypeMap["electric"] = 4;
                TypeMap["fairy"] = 5;
                TypeMap["fighting"] = 6;
                TypeMap["fire"] = 7;
                TypeMap["flying"] = 8;
                TypeMap["ghost"] = 9;
                TypeMap["grass"] = 10;
                TypeMap["ground"] = 11;
                TypeMap["ice"] = 12;
                TypeMap["normal"] = 13;
                TypeMap["poison"] = 14;
                TypeMap["psychic"] = 15;
                TypeMap["rock"] = 16;
                TypeMap["steel"] = 17;
                TypeMap["water"] = 18;
            }
        }
    }

    /// <summary>
    /// Universal state storing attack and defense stat stage multiplier tables.
    /// Used to calculate stat modifiers based on boost/drop levels.
    /// </summary>
    [Serializable]
    public class AtkDefLevelTableState : UniversalState
    {
        /// <summary>
        /// Minimum attack stage level (typically -6).
        /// </summary>
        public int MinAtk;

        /// <summary>
        /// Maximum attack stage level (typically +6).
        /// </summary>
        public int MaxAtk;

        /// <summary>
        /// Minimum defense stage level (typically -6).
        /// </summary>
        public int MinDef;

        /// <summary>
        /// Maximum defense stage level (typically +6).
        /// </summary>
        public int MaxDef;

        /// <summary>
        /// Base value for attack stage calculations.
        /// </summary>
        public int AtkBase;

        /// <summary>
        /// Base value for defense stage calculations.
        /// </summary>
        public int DefBase;

        /// <summary>
        /// Calculates the attack stat after applying stage modifiers.
        /// </summary>
        /// <param name="stat">The base attack stat value.</param>
        /// <param name="level">The current attack stage level.</param>
        /// <returns>The modified attack stat.</returns>
        public int AtkLevelMult(int stat, int level)
        {
            int bound_level = Math.Min(Math.Max(MinAtk, level), MaxAtk);
            if (bound_level < 0)
                return stat * AtkBase / (AtkBase - level);
            else if (bound_level > 0)
                return stat * (AtkBase + level) / AtkBase;
            else
                return stat;
        }

        /// <summary>
        /// Calculates the defense stat after applying stage modifiers.
        /// </summary>
        /// <param name="stat">The base defense stat value.</param>
        /// <param name="level">The current defense stage level.</param>
        /// <returns>The modified defense stat.</returns>
        public int DefLevelMult(int stat, int level)
        {
            int bound_level = Math.Min(Math.Max(MinDef, level), MaxDef);
            if (bound_level < 0)
                return stat * DefBase / (DefBase - level);
            else if (bound_level > 0)
                return stat * (DefBase + level) / DefBase;
            else
                return stat;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AtkDefLevelTableState"/> class with default values.
        /// </summary>
        public AtkDefLevelTableState() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AtkDefLevelTableState"/> class with specified stage limits and base values.
        /// </summary>
        /// <param name="minAtk">The minimum attack stage level.</param>
        /// <param name="maxAtk">The maximum attack stage level.</param>
        /// <param name="minDef">The minimum defense stage level.</param>
        /// <param name="maxDef">The maximum defense stage level.</param>
        /// <param name="atkBase">The base value for attack calculations.</param>
        /// <param name="defBase">The base value for defense calculations.</param>
        public AtkDefLevelTableState(int minAtk, int maxAtk, int minDef, int maxDef, int atkBase, int defBase)
        {
            MinAtk = minAtk;
            MaxAtk = maxAtk;
            MinDef = minDef;
            MaxDef = maxDef;

            AtkBase = atkBase;
            DefBase = defBase;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AtkDefLevelTableState"/> class by copying another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected AtkDefLevelTableState(AtkDefLevelTableState other)
        {
            MinAtk = other.MinAtk;
            MaxAtk = other.MaxAtk;
            MinDef = other.MinDef;
            MaxDef = other.MaxDef;

            AtkBase = other.AtkBase;
            DefBase = other.DefBase;
        }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new AtkDefLevelTableState(this); }
    }

    /// <summary>
    /// Universal state storing critical hit rate chances based on crit stage levels.
    /// Used to calculate the probability of landing a critical hit.
    /// </summary>
    [Serializable]
    public class CritRateLevelTableState : UniversalState
    {
        /// <summary>
        /// Array of critical hit chances for each crit stage level.
        /// Higher indices correspond to higher crit stages with better chances.
        /// </summary>
        public int[] CritLevels;

        /// <summary>
        /// Gets the critical hit chance for a given crit stage level.
        /// </summary>
        /// <param name="level">The crit stage level.</param>
        /// <returns>The critical hit chance value.</returns>
        public int GetCritChance(int level)
        {
            int bound_level = Math.Min(Math.Max(0, level), CritLevels.Length - 1);
            return CritLevels[bound_level];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CritRateLevelTableState"/> class with an empty levels array.
        /// </summary>
        public CritRateLevelTableState() { CritLevels = new int[0]; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CritRateLevelTableState"/> class by copying another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected CritRateLevelTableState(CritRateLevelTableState other)
        {
            CritLevels = new int[other.CritLevels.Length];
            other.CritLevels.CopyTo(CritLevels, 0);
        }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new CritRateLevelTableState(this); }
    }

    /// <summary>
    /// Universal state storing accuracy and evasion stage multiplier tables.
    /// Used to calculate hit rate modifiers based on stat stages.
    /// </summary>
    [Serializable]
    public class HitRateLevelTableState : UniversalState
    {
        /// <summary>
        /// Multiplier values for each accuracy stage level.
        /// </summary>
        public int[] AccuracyLevels;

        /// <summary>
        /// Multiplier values for each evasion stage level.
        /// </summary>
        public int[] EvasionLevels;

        /// <summary>
        /// Minimum accuracy stage level.
        /// </summary>
        public int MinAccuracy;

        /// <summary>
        /// Maximum accuracy stage level.
        /// </summary>
        public int MaxAccuracy;

        /// <summary>
        /// Minimum evasion stage level.
        /// </summary>
        public int MinEvasion;

        /// <summary>
        /// Maximum evasion stage level.
        /// </summary>
        public int MaxEvasion;

        /// <summary>
        /// Applies accuracy stage modifier to a base accuracy value.
        /// </summary>
        /// <param name="baseAcc">The base accuracy value.</param>
        /// <param name="statStage">The current accuracy stage.</param>
        /// <returns>The modified accuracy value.</returns>
        public int ApplyAccuracyMod(int baseAcc, int statStage)
        {
            int bound_level = Math.Min(Math.Max(0, statStage - MinAccuracy), AccuracyLevels.Length - 1);
            return baseAcc * AccuracyLevels[bound_level];
        }

        /// <summary>
        /// Applies evasion stage modifier to a base accuracy value.
        /// </summary>
        /// <param name="baseAcc">The base accuracy value.</param>
        /// <param name="statStage">The current evasion stage.</param>
        /// <returns>The modified accuracy value.</returns>
        public int ApplyEvasionMod(int baseAcc, int statStage)
        {
            int bound_level = Math.Min(Math.Max(0, statStage - MinEvasion), EvasionLevels.Length - 1);
            return baseAcc * EvasionLevels[bound_level];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HitRateLevelTableState"/> class with empty levels arrays.
        /// </summary>
        public HitRateLevelTableState() { AccuracyLevels = new int[0]; EvasionLevels = new int[0]; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HitRateLevelTableState"/> class with specified stage limits.
        /// </summary>
        /// <param name="minAcc">The minimum accuracy stage level.</param>
        /// <param name="maxAcc">The maximum accuracy stage level.</param>
        /// <param name="minEvade">The minimum evasion stage level.</param>
        /// <param name="maxEvade">The maximum evasion stage level.</param>
        public HitRateLevelTableState(int minAcc, int maxAcc, int minEvade, int maxEvade) : this()
        {
            MinAccuracy = minAcc;
            MaxAccuracy = maxAcc;
            MinEvasion = minEvade;
            MaxEvasion = maxEvade;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HitRateLevelTableState"/> class by copying another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        public HitRateLevelTableState(HitRateLevelTableState other)
        {
            AccuracyLevels = new int[other.AccuracyLevels.Length];
            other.AccuracyLevels.CopyTo(AccuracyLevels, 0);
            EvasionLevels = new int[other.EvasionLevels.Length];
            other.EvasionLevels.CopyTo(EvasionLevels, 0);
            MinAccuracy = other.MinAccuracy;
            MaxAccuracy = other.MaxAccuracy;
            MinEvasion = other.MinEvasion;
            MaxEvasion = other.MaxEvasion;
        }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new HitRateLevelTableState(this); }
    }

    /// <summary>
    /// Universal state storing configuration for alternate character skins/colors.
    /// Used for shiny variants and special challenge mode appearances.
    /// </summary>
    [Serializable]
    public class SkinTableState : UniversalState
    {
        /// <summary>
        /// The odds (1 in N) of encountering an alternate color variant.
        /// </summary>
        public int AltColorOdds;

        /// <summary>
        /// The skin ID for the alternate color variant (e.g., shiny).
        /// </summary>
        [DataType(0, DataManager.DataType.Skin, false)]
        public string AltColor;

        /// <summary>
        /// The skin ID for challenge mode characters.
        /// </summary>
        [DataType(0, DataManager.DataType.Skin, false)]
        public string Challenge;

        /// <summary>
        /// Initializes a new instance of the <see cref="SkinTableState"/> class with default empty skin IDs.
        /// </summary>
        public SkinTableState() { AltColor = ""; Challenge = ""; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkinTableState"/> class with specified values.
        /// </summary>
        /// <param name="odds">The odds (1 in N) of encountering an alternate color variant.</param>
        /// <param name="altColor">The skin ID for the alternate color variant.</param>
        /// <param name="challenge">The skin ID for challenge mode characters.</param>
        public SkinTableState(int odds, string altColor, string challenge) { AltColorOdds = odds; AltColor = altColor; Challenge = challenge; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkinTableState"/> class by copying another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected SkinTableState(SkinTableState other)
        {
            AltColorOdds = other.AltColorOdds;
            AltColor = other.AltColor;
            Challenge = other.Challenge;
        }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new SkinTableState(this); }



        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (String.IsNullOrEmpty(AltColor))
                AltColor = "shiny";
            if (String.IsNullOrEmpty(Challenge))
                Challenge = "shiny_square";
        }
    }

    /// <summary>
    /// Universal state storing configuration for monster house encounters and warnings.
    /// Controls visual indicators, ambush mechanics, and spawn restrictions for monster houses.
    /// </summary>
    [Serializable]
    public class MonsterHouseTableState : UniversalState
    {
        /// <summary>
        /// If this is set, this tile will be used to display a warning for monster houses.
        /// </summary>
        [DataType(0, DataManager.DataType.Tile, false)]
        public string MonsterHouseWarningTile;

        /// <summary>
        /// If this is set, this tile will be used to replace the chest in a chest ambush.
        /// </summary>
        [DataType(0, DataManager.DataType.Tile, false)]
        public string ChestAmbushWarningTile;

        /// <summary>
        /// If this is set to true, monster halls will never appear on tiles where you can't see the warning tile.
        /// </summary>
        public bool NoMonsterHallOnBlockLightTiles;

        /// <summary>
        /// If this is set to true, you will not be able to spawn into a monster house upon entering a floor.
        /// </summary>
        public bool NoMonsterHouseEntrances;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonsterHouseTableState"/> class with null tiles and disabled restrictions.
        /// </summary>
        public MonsterHouseTableState()
        {
            MonsterHouseWarningTile = null;
            ChestAmbushWarningTile = null;
            NoMonsterHallOnBlockLightTiles = false;
            NoMonsterHouseEntrances = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonsterHouseTableState"/> class with specified values.
        /// </summary>
        /// <param name="monsterHouseWarningTile">The tile ID for monster house warnings.</param>
        /// <param name="chestAmbushWarningTile">The tile ID for chest ambush warnings.</param>
        /// <param name="noMonsterHallOnBlockLightTiles">Whether to restrict monster halls on block-light tiles.</param>
        /// <param name="noMonsterHouseEntrances">Whether to prevent monster house spawns on floor entry.</param>
        public MonsterHouseTableState(string monsterHouseWarningTile, string chestAmbushWarningTile, bool noMonsterHallOnBlockLightTiles, bool noMonsterHouseEntrances)
        {
            MonsterHouseWarningTile = monsterHouseWarningTile;
            ChestAmbushWarningTile = chestAmbushWarningTile;
            NoMonsterHallOnBlockLightTiles = noMonsterHallOnBlockLightTiles;
            NoMonsterHouseEntrances = noMonsterHouseEntrances;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonsterHouseTableState"/> class by copying another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected MonsterHouseTableState(MonsterHouseTableState other)
        {
            MonsterHouseWarningTile = other.MonsterHouseWarningTile;
            ChestAmbushWarningTile = other.ChestAmbushWarningTile;
            NoMonsterHouseEntrances = other.NoMonsterHouseEntrances;
            NoMonsterHallOnBlockLightTiles = other.NoMonsterHallOnBlockLightTiles;
        }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new MonsterHouseTableState(this); }
    }
}

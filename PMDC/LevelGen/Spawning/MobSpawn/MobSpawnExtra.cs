using System;
using RogueEssence.Dungeon;
using RogueEssence.Data;
using RogueElements;
using RogueEssence;
using RogueEssence.LevelGen;
using PMDC.Data;
using System.Collections.Generic;
using RogueEssence.Dev;
using RogueEssence.Script;
using NLua;
using System.Linq;
using PMDC.Dungeon;
using System.Runtime.Serialization;
using System.Diagnostics;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Spawns the mob with a 35% fullness and 50% PP.
    /// </summary>
    [Serializable]
    public class MobSpawnWeak : MobSpawnExtra
    {
        /// <summary>
        /// Creates a copy of this mob spawn feature.
        /// </summary>
        /// <returns>A new instance of MobSpawnWeak with the same configuration.</returns>
        public override MobSpawnExtra Copy() { return new MobSpawnWeak(); }

        /// <summary>
        /// Applies reduced fullness and skill charges to the spawned character.
        /// Sets fullness to 35% and all skill charges to 50% of their base values.
        /// </summary>
        /// <param name="map">The mob spawn map context containing spawn information.</param>
        /// <param name="newChar">The character to apply the feature to.</param>
        public override void ApplyFeature(IMobSpawnMap map, Character newChar)
        {
            //set the newChar's uses to 50% (ceiling), hunger to 35%
            newChar.Fullness = 35;
            for (int ii = 0; ii < newChar.Skills.Count; ii++)
            {
                if (!String.IsNullOrEmpty(newChar.Skills[ii].Element.SkillNum))
                {
                    EntryDataIndex idx = DataManager.Instance.DataIndices[DataManager.DataType.Skill];
                    SkillDataSummary summary = (SkillDataSummary)idx.Get(newChar.Skills[ii].Element.SkillNum);
                    newChar.SetSkillCharges(ii, MathUtils.DivUp(summary.BaseCharges, 2));
                }
            }
        }

        /// <summary>
        /// Returns the string representation of this mob spawn feature.
        /// </summary>
        /// <returns>The formatted type name of this class.</returns>
        public override string ToString()
        {
            return string.Format("{0}", this.GetType().GetFormattedTypeName());
        }
    }

    /// <summary>
    /// Spawns the mob with a custom shiny chance.
    /// </summary>
    [Serializable]
    public class MobSpawnAltColor : MobSpawnExtra
    {
        /// <summary>
        /// Fractional chance of occurrence.
        /// </summary>
        [FractionLimit(0, 0, 0)]
        public Multiplier Chance;

        /// <summary>
        /// OBSOLETE - Use Chance instead.
        /// </summary>
        [NonEdited]
        public int Odds;

        /// <summary>
        /// Initializes a new instance of MobSpawnAltColor with default values.
        /// </summary>
        public MobSpawnAltColor() { }

        /// <summary>
        /// Initializes a new instance of MobSpawnAltColor with the specified odds.
        /// </summary>
        /// <param name="odds">The denominator for the spawn chance (1 in odds).</param>
        public MobSpawnAltColor(int odds)
        {
            Chance = new Multiplier(1, odds);
        }

        /// <summary>
        /// Initializes a new instance of MobSpawnAltColor by copying another instance.
        /// </summary>
        /// <param name="other">The MobSpawnAltColor instance to copy.</param>
        public MobSpawnAltColor(MobSpawnAltColor other)
        {
            Chance = other.Chance;
        }

        /// <inheritdoc/>
        public override MobSpawnExtra Copy() { return new MobSpawnAltColor(this); }

        /// <summary>
        /// Applies an alternate color (shiny variant) to the spawned character with the configured probability.
        /// </summary>
        /// <param name="map">The mob spawn map context containing spawn information.</param>
        /// <param name="newChar">The character to apply the feature to.</param>
        public override void ApplyFeature(IMobSpawnMap map, Character newChar)
        {
            if (Chance.Denominator > 0 && map.Rand.Next(Chance.Denominator) < Chance.Numerator)
            {
                SkinTableState table = DataManager.Instance.UniversalEvent.UniversalStates.GetWithDefault<SkinTableState>();
                newChar.BaseForm.Skin = table.AltColor;
            }
            else
                newChar.BaseForm.Skin = DataManager.Instance.DefaultSkin;
            newChar.CurrentForm = newChar.BaseForm;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0}: {1}", this.GetType().GetFormattedTypeName(), Chance);
        }

        /// <summary>
        /// Called during deserialization to handle backward compatibility with older versions.
        /// </summary>
        /// <param name="context">The streaming context.</param>
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            //TODO: Remove in v1.1
            if (Serializer.OldVersion < new Version(0, 7, 15))
            {
                Chance = new Multiplier(1, Odds);
            }
        }
    }

    /// <summary>
    /// Spawns the mob with moves turned off.
    /// </summary>
    [Serializable]
    public class MobSpawnMovesOff : MobSpawnExtra
    {
        /// <summary>
        /// The move index to start turning moves off.
        /// </summary>
        public int StartAt;

        /// <summary>
        /// Remove the moves entirely. If false, only disables them.
        /// </summary>
        public bool Remove;

        /// <summary>
        /// Initializes a new instance of MobSpawnMovesOff with default values.
        /// </summary>
        public MobSpawnMovesOff() { }

        /// <summary>
        /// Initializes a new instance of MobSpawnMovesOff with the specified starting index.
        /// </summary>
        /// <param name="startAt">The skill slot index to start disabling from.</param>
        public MobSpawnMovesOff(int startAt)
        {
            StartAt = startAt;
        }

        /// <summary>
        /// Initializes a new instance of MobSpawnMovesOff by copying another instance.
        /// </summary>
        /// <param name="other">The MobSpawnMovesOff instance to copy.</param>
        public MobSpawnMovesOff(MobSpawnMovesOff other)
        {
            StartAt = other.StartAt;
            Remove = other.Remove;
        }

        /// <inheritdoc/>
        public override MobSpawnExtra Copy() { return new MobSpawnMovesOff(this); }

        /// <summary>
        /// Applies the move disabling feature to the spawned character.
        /// Either removes moves starting from StartAt or disables them depending on the Remove flag.
        /// </summary>
        /// <param name="map">The mob spawn map context containing spawn information.</param>
        /// <param name="newChar">The character to apply the feature to.</param>
        public override void ApplyFeature(IMobSpawnMap map, Character newChar)
        {
            if (Remove)
            {
                for (int ii = StartAt; ii < Character.MAX_SKILL_SLOTS; ii++)
                    newChar.DeleteSkill(StartAt, false);
            }
            else
            {
                for (int ii = StartAt; ii < Character.MAX_SKILL_SLOTS; ii++)
                    newChar.Skills[ii].Element.Enabled = false;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0}: {1}+", this.GetType().GetFormattedTypeName(), StartAt);
        }
    }

    /// <summary>
    /// Spawn the mob with stat boosts (vitamin boosts).
    /// </summary>
    [Serializable]
    public class MobSpawnBoost : MobSpawnExtra
    {
        /// <summary>
        /// The bonus to apply to maximum HP stat.
        /// </summary>
        public int MaxHPBonus;

        /// <summary>
        /// The bonus to apply to attack stat.
        /// </summary>
        public int AtkBonus;

        /// <summary>
        /// The bonus to apply to defense stat.
        /// </summary>
        public int DefBonus;

        /// <summary>
        /// The bonus to apply to special attack stat.
        /// </summary>
        public int SpAtkBonus;

        /// <summary>
        /// The bonus to apply to special defense stat.
        /// </summary>
        public int SpDefBonus;

        /// <summary>
        /// The bonus to apply to speed stat.
        /// </summary>
        public int SpeedBonus;

        /// <summary>
        /// Initializes a new instance of MobSpawnBoost with default values.
        /// </summary>
        public MobSpawnBoost() { }

        /// <summary>
        /// Initializes a new instance of MobSpawnBoost by copying another instance.
        /// </summary>
        /// <param name="other">The MobSpawnBoost instance to copy.</param>
        public MobSpawnBoost(MobSpawnBoost other)
        {
            MaxHPBonus = other.MaxHPBonus;
            AtkBonus = other.AtkBonus;
            DefBonus = other.DefBonus;
            SpAtkBonus = other.SpAtkBonus;
            SpDefBonus = other.SpDefBonus;
            SpeedBonus = other.SpeedBonus;
        }

        /// <inheritdoc/>
        public override MobSpawnExtra Copy() { return new MobSpawnBoost(this); }

        /// <summary>
        /// Applies stat bonuses to the spawned character, clamping values to the maximum allowed stat boost.
        /// Also restores the character's HP to its maximum value.
        /// </summary>
        /// <param name="map">The mob spawn map context containing spawn information.</param>
        /// <param name="newChar">The character to apply the feature to.</param>
        public override void ApplyFeature(IMobSpawnMap map, Character newChar)
        {
            newChar.MaxHPBonus = Math.Min(MaxHPBonus, MonsterFormData.MAX_STAT_BOOST);
            newChar.AtkBonus = Math.Min(AtkBonus, MonsterFormData.MAX_STAT_BOOST);
            newChar.DefBonus = Math.Min(DefBonus, MonsterFormData.MAX_STAT_BOOST);
            newChar.MAtkBonus = Math.Min(SpAtkBonus, MonsterFormData.MAX_STAT_BOOST);
            newChar.MDefBonus = Math.Min(SpDefBonus, MonsterFormData.MAX_STAT_BOOST);
            newChar.SpeedBonus = Math.Min(SpeedBonus, MonsterFormData.MAX_STAT_BOOST);
            newChar.HP = newChar.MaxHP;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0}", this.GetType().GetFormattedTypeName());
        }
    }

    /// <summary>
    /// Spawn the mob with stat boosts (vitamin boosts) that scale based on its level.
    /// </summary>
    [Serializable]
    public class MobSpawnScaledBoost : MobSpawnExtra
    {
        /// <summary>
        /// The range of levels used to scale the stat boosts.
        /// </summary>
        public IntRange LevelRange;

        /// <summary>
        /// The range of maximum HP bonuses based on character level.
        /// </summary>
        public IntRange MaxHPBonus;

        /// <summary>
        /// The range of attack bonuses based on character level.
        /// </summary>
        public IntRange AtkBonus;

        /// <summary>
        /// The range of defense bonuses based on character level.
        /// </summary>
        public IntRange DefBonus;

        /// <summary>
        /// The range of special attack bonuses based on character level.
        /// </summary>
        public IntRange SpAtkBonus;

        /// <summary>
        /// The range of special defense bonuses based on character level.
        /// </summary>
        public IntRange SpDefBonus;

        /// <summary>
        /// The range of speed bonuses based on character level.
        /// </summary>
        public IntRange SpeedBonus;

        /// <summary>
        /// Initializes a new instance of MobSpawnScaledBoost with default values.
        /// </summary>
        public MobSpawnScaledBoost() { }

        /// <summary>
        /// Initializes a new instance of MobSpawnScaledBoost with the specified level range.
        /// </summary>
        /// <param name="range">The range of levels to scale stats across.</param>
        public MobSpawnScaledBoost(IntRange range)
        {
            LevelRange = range;
        }

        /// <summary>
        /// Initializes a new instance of MobSpawnScaledBoost by copying another instance.
        /// </summary>
        /// <param name="other">The MobSpawnScaledBoost instance to copy.</param>
        public MobSpawnScaledBoost(MobSpawnScaledBoost other)
        {
            LevelRange = other.LevelRange;
            MaxHPBonus = other.MaxHPBonus;
            AtkBonus = other.AtkBonus;
            DefBonus = other.DefBonus;
            SpAtkBonus = other.SpAtkBonus;
            SpDefBonus = other.SpDefBonus;
            SpeedBonus = other.SpeedBonus;
        }

        /// <inheritdoc/>
        public override MobSpawnExtra Copy() { return new MobSpawnScaledBoost(this); }

        /// <summary>
        /// Applies scaled stat bonuses to the spawned character based on its level.
        /// Linearly interpolates stat bonuses within the configured level range and clamps to maximum allowed values.
        /// Also restores the character's HP to its maximum value.
        /// </summary>
        /// <param name="map">The mob spawn map context containing spawn information.</param>
        /// <param name="newChar">The character to apply the feature to.</param>
        public override void ApplyFeature(IMobSpawnMap map, Character newChar)
        {
            int clampedLevel = Math.Clamp(newChar.Level, LevelRange.Min, LevelRange.Max);
            newChar.MaxHPBonus = Math.Min(MaxHPBonus.Min + MaxHPBonus.Length * (clampedLevel - LevelRange.Min) / LevelRange.Length, MonsterFormData.MAX_STAT_BOOST);
            newChar.AtkBonus = Math.Min(AtkBonus.Min + AtkBonus.Length * (clampedLevel - LevelRange.Min) / LevelRange.Length, MonsterFormData.MAX_STAT_BOOST);
            newChar.DefBonus = Math.Min(DefBonus.Min + DefBonus.Length * (clampedLevel - LevelRange.Min) / LevelRange.Length, MonsterFormData.MAX_STAT_BOOST);
            newChar.MAtkBonus = Math.Min(SpAtkBonus.Min + SpAtkBonus.Length * (clampedLevel - LevelRange.Min) / LevelRange.Length, MonsterFormData.MAX_STAT_BOOST);
            newChar.MDefBonus = Math.Min(SpDefBonus.Min + SpDefBonus.Length * (clampedLevel - LevelRange.Min) / LevelRange.Length, MonsterFormData.MAX_STAT_BOOST);
            newChar.SpeedBonus = Math.Min(SpeedBonus.Min + SpeedBonus.Length * (clampedLevel - LevelRange.Min) / LevelRange.Length, MonsterFormData.MAX_STAT_BOOST);
            newChar.HP = newChar.MaxHP;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0}", this.GetType().GetFormattedTypeName());
        }
    }

    /// <summary>
    /// Spawn the mob with an item.
    /// </summary>
    [Serializable]
    public class MobSpawnItem : MobSpawnExtra
    {
        /// <summary>
        /// The possible items. Picks one from the spawn list.
        /// </summary>
        public SpawnList<InvItem> Items;

        /// <summary>
        /// Only give it the item on map generation.
        /// Respawns that occur after the map is generated do not get the item.
        /// </summary>
        public bool MapStartOnly;

        /// <summary>
        /// Chance of item spawn.
        /// </summary>
        public Multiplier Chance;

        /// <summary>
        /// Initializes a new instance of MobSpawnItem with default values.
        /// </summary>
        public MobSpawnItem()
        {
            Items = new SpawnList<InvItem>();
        }

        /// <summary>
        /// Initializes a new instance of MobSpawnItem with the specified items.
        /// </summary>
        /// <param name="startOnly">Whether to only apply the item at map generation time.</param>
        /// <param name="itemNum">Array of item IDs to add to the spawn list with equal weight.</param>
        public MobSpawnItem(bool startOnly, params string[] itemNum) : this()
        {
            Chance = new Multiplier(1, 1);
            MapStartOnly = startOnly;
            for(int ii = 0; ii < itemNum.Length; ii++)
                Items.Add(new InvItem(itemNum[ii]), 100);
        }

        /// <summary>
        /// Initializes a new instance of MobSpawnItem by copying another instance.
        /// </summary>
        /// <param name="other">The MobSpawnItem instance to copy.</param>
        public MobSpawnItem(MobSpawnItem other) : this()
        {
            MapStartOnly = other.MapStartOnly;
            Chance = other.Chance;
            for (int ii = 0; ii < other.Items.Count; ii++)
                Items.Add(new InvItem(other.Items.GetSpawn(ii)), other.Items.GetSpawnRate(ii));
        }

        /// <inheritdoc/>
        public override MobSpawnExtra Copy() { return new MobSpawnItem(this); }

        /// <summary>
        /// Applies an item to the spawned character with the configured probability.
        /// Will not apply if MapStartOnly is true and the map has already begun.
        /// </summary>
        /// <param name="map">The mob spawn map context containing spawn information.</param>
        /// <param name="newChar">The character to apply the feature to.</param>
        public override void ApplyFeature(IMobSpawnMap map, Character newChar)
        {
            if (MapStartOnly && map.Begun)
                return;

            if (Chance.Denominator > 0 && map.Rand.Next(Chance.Denominator) < Chance.Numerator)
                newChar.EquippedItem = Items.Pick(map.Rand);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (Items.Count != 1)
                return string.Format("{0}[{1}]", this.GetType().GetFormattedTypeName(), Items.Count.ToString());
            else
            {
                EntrySummary summary = DataManager.Instance.DataIndices[DataManager.DataType.Item].Get(Items.GetSpawn(0).ID);
                return string.Format("{0}: {1}", this.GetType().GetFormattedTypeName(), summary.Name.ToLocal());
            }
        }

        /// <summary>
        /// Called during deserialization to handle backward compatibility with older versions.
        /// </summary>
        /// <param name="context">The streaming context.</param>
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            //TODO: Remove in v1.1
            if (Serializer.OldVersion < new Version(0, 7, 20))
            {
                Chance = new Multiplier(1, 1);
            }
        }
    }


    /// <summary>
    /// Base class for spawning the mob with a box containing an exclusive item.
    /// </summary>
    [Serializable]
    public abstract class MobSpawnExclBase : MobSpawnExtra
    {
        /// <summary>
        /// The rarity level range of items to select from.
        /// </summary>
        public IntRange Rarity;

        /// <summary>
        /// Type of box item to create.
        /// </summary>
        [DataType(0, DataManager.DataType.Item, false)]
        public string Box;

        /// <summary>
        /// Only give it the item on map generation.
        /// Respawns that occur after the map is generated do not get the item.
        /// </summary>
        public bool MapStartOnly;

        /// <summary>
        /// Chance of item spawn.
        /// </summary>
        public Multiplier Chance;

        /// <summary>
        /// Initializes a new instance of MobSpawnExclBase with default values.
        /// </summary>
        public MobSpawnExclBase()
        {
            Box = "";
        }

        /// <summary>
        /// Initializes a new instance of MobSpawnExclBase with the specified parameters.
        /// </summary>
        /// <param name="box">The box item ID.</param>
        /// <param name="rarity">The rarity level range of items to select from.</param>
        /// <param name="startOnly">Whether to only apply at map generation time.</param>
        public MobSpawnExclBase(string box, IntRange rarity, bool startOnly) : this()
        {
            Chance = new Multiplier(1, 1);
            Box = box;
            MapStartOnly = startOnly;
            Rarity = rarity;
        }

        /// <summary>
        /// Initializes a new instance of MobSpawnExclBase by copying another instance.
        /// </summary>
        /// <param name="other">The MobSpawnExclBase instance to copy.</param>
        public MobSpawnExclBase(MobSpawnExclBase other) : this()
        {
            MapStartOnly = other.MapStartOnly;
            Rarity = other.Rarity;
            Box = other.Box;
            Chance = other.Chance;
        }

        /// <summary>
        /// Applies an exclusive box item to the spawned character with the configured probability.
        /// Selects an exclusive item from the rarity table of species determined by GetPossibleSpecies.
        /// Will not apply if MapStartOnly is true and the map has already begun.
        /// </summary>
        /// <param name="map">The mob spawn map context containing spawn information.</param>
        /// <param name="newChar">The character to apply the feature to.</param>
        public override void ApplyFeature(IMobSpawnMap map, Character newChar)
        {
            if (MapStartOnly && map.Begun)
                return;

            if (Chance.Denominator > 0 && map.Rand.Next(Chance.Denominator) < Chance.Numerator)
            {
                RarityData rarity = DataManager.Instance.UniversalData.Get<RarityData>();
                List<string> possibleItems = new List<string>();
                foreach (string baseSpecies in GetPossibleSpecies(map, newChar))
                {
                    for (int ii = Rarity.Min; ii < Rarity.Max; ii++)
                    {
                        Dictionary<int, List<string>> rarityTable;
                        if (rarity.RarityMap.TryGetValue(baseSpecies, out rarityTable))
                        {
                            if (rarityTable.ContainsKey(ii))
                            {
                                foreach (string item in rarityTable[ii])
                                {
                                    EntrySummary summary = DataManager.Instance.DataIndices[DataManager.DataType.Item].Get(item);
                                    if (summary.Released)
                                        possibleItems.Add(item);
                                }
                            }
                        }
                    }
                }

                if (possibleItems.Count > 0)
                {
                    InvItem equip = new InvItem(Box);
                    equip.HiddenValue = possibleItems[map.Rand.Next(possibleItems.Count)];
                    newChar.EquippedItem = equip;
                }
            }
        }

        /// <summary>
        /// Gets the collection of species to draw exclusive items from.
        /// </summary>
        /// <param name="map">The mob spawn map context.</param>
        /// <param name="newChar">The character being spawned.</param>
        /// <returns>An enumeration of species IDs to select exclusive items from.</returns>
        protected abstract IEnumerable<string> GetPossibleSpecies(IMobSpawnMap map, Character newChar);

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0}: {1}*", this.GetType().GetFormattedTypeName(), Rarity.ToString());
        }

        /// <summary>
        /// Called during deserialization to handle backward compatibility with older versions.
        /// </summary>
        /// <param name="context">The streaming context.</param>
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            //TODO: Remove in v1.1
            if (Serializer.OldVersion < new Version(0, 7, 20))
            {
                Chance = new Multiplier(1, 1);
            }
        }
    }


    /// <summary>
    /// Spawns the mob with a box containing an exclusive item for its evolutionary family.
    /// Searches both pre-evolutions and evolutions of the spawned monster.
    /// </summary>
    [Serializable]
    public class MobSpawnExclFamily : MobSpawnExclBase
    {
        /// <summary>
        /// Initializes a new instance of MobSpawnExclFamily with default values.
        /// </summary>
        public MobSpawnExclFamily()
        { }

        /// <summary>
        /// Initializes a new instance of MobSpawnExclFamily with the specified parameters.
        /// </summary>
        /// <param name="box">The box item ID.</param>
        /// <param name="rarity">The rarity level range of items to select from.</param>
        /// <param name="startOnly">Whether to only apply at map generation time.</param>
        public MobSpawnExclFamily(string box, IntRange rarity, bool startOnly) : base(box, rarity, startOnly)
        { }

        /// <summary>
        /// Initializes a new instance of MobSpawnExclFamily by copying another instance.
        /// </summary>
        /// <param name="other">The MobSpawnExclFamily instance to copy.</param>
        public MobSpawnExclFamily(MobSpawnExclFamily other) : base(other)
        { }

        /// <inheritdoc/>
        public override MobSpawnExtra Copy() { return new MobSpawnExclFamily(this); }

        /// <summary>
        /// Gets the evolutionary family species including all pre-evolutions and evolutions.
        /// </summary>
        /// <param name="map">The mob spawn map context.</param>
        /// <param name="newChar">The character being spawned.</param>
        /// <returns>An enumeration of all species in the evolutionary family.</returns>
        protected override IEnumerable<string> GetPossibleSpecies(IMobSpawnMap map, Character newChar)
        {
            //check prevos
            string prevo = newChar.BaseForm.Species;
            while (!String.IsNullOrEmpty(prevo))
            {
                yield return prevo;
                MonsterData data = DataManager.Instance.GetMonster(prevo);
                prevo = data.PromoteFrom;
            }

            string baseStage = newChar.BaseForm.Species;
            foreach (string evo in recurseEvos(baseStage))
                yield return evo;
        }

        /// <summary>
        /// Recursively enumerates all evolutions of the given species.
        /// </summary>
        /// <param name="baseStage">The species to get evolutions for.</param>
        /// <returns>An enumeration of all evolved species.</returns>
        private IEnumerable<string> recurseEvos(string baseStage)
        {
            MonsterData data = DataManager.Instance.GetMonster(baseStage);
            foreach (PromoteBranch branch in data.Promotions)
            {
                yield return branch.Result;
                foreach (string evo in recurseEvos(branch.Result))
                    yield return evo;
            }
        }
    }


    /// <summary>
    /// Spawns the mob with a box containing an exclusive item from any released monster.
    /// Allows exclusion of specific species.
    /// </summary>
    [Serializable]
    public class MobSpawnExclAny : MobSpawnExclBase
    {
        /// <summary>
        /// Species to exclude from the item pool.
        /// </summary>
        [DataType(1, DataManager.DataType.Monster, false)]
        public HashSet<string> ExceptFor { get; set; }

        /// <summary>
        /// Initializes a new instance of MobSpawnExclAny with default values.
        /// </summary>
        public MobSpawnExclAny()
        {
            ExceptFor = new HashSet<string>();
        }

        /// <summary>
        /// Initializes a new instance of MobSpawnExclAny with the specified parameters.
        /// </summary>
        /// <param name="box">The box item ID.</param>
        /// <param name="exceptFor">The set of species to exclude from the item pool.</param>
        /// <param name="rarity">The rarity level range of items to select from.</param>
        /// <param name="startOnly">Whether to only apply at map generation time.</param>
        public MobSpawnExclAny(string box, HashSet<string> exceptFor, IntRange rarity, bool startOnly) : base(box, rarity, startOnly)
        {
            ExceptFor = exceptFor;
        }

        /// <summary>
        /// Initializes a new instance of MobSpawnExclAny by copying another instance.
        /// </summary>
        /// <param name="other">The MobSpawnExclAny instance to copy.</param>
        public MobSpawnExclAny(MobSpawnExclAny other) : base(other)
        {
            ExceptFor = new HashSet<string>();
            foreach (string except in other.ExceptFor)
                ExceptFor.Add(except);
        }

        /// <inheritdoc/>
        public override MobSpawnExtra Copy() { return new MobSpawnExclAny(this); }

        /// <summary>
        /// Gets all released monster species, excluding those in the ExceptFor set.
        /// </summary>
        /// <param name="map">The mob spawn map context.</param>
        /// <param name="newChar">The character being spawned.</param>
        /// <returns>An enumeration of released species excluding those in ExceptFor.</returns>
        protected override IEnumerable<string> GetPossibleSpecies(IMobSpawnMap map, Character newChar)
        {
            MonsterFeatureData feature = DataManager.Instance.UniversalData.Get<MonsterFeatureData>();
            //iterate all species that have that element, except for
            foreach (string key in DataManager.Instance.DataIndices[DataManager.DataType.Monster].GetOrderedKeys(true))
            {
                if (ExceptFor.Contains(key))
                    continue;
                EntrySummary summary = DataManager.Instance.DataIndices[DataManager.DataType.Monster].Get(key);
                if (!summary.Released)
                    continue;

                Dictionary<int, FormFeatureSummary> species;
                if (!feature.FeatureData.TryGetValue(key, out species))
                    continue;
                FormFeatureSummary form;
                if (!species.TryGetValue(0, out form))
                    continue;

                yield return key;
            }
        }
    }


    /// <summary>
    /// Spawns the mob with a box containing an exclusive item from monsters sharing its element type.
    /// Matches against either primary or secondary element.
    /// </summary>
    [Serializable]
    public class MobSpawnExclElement : MobSpawnExclBase
    {
        /// <summary>
        /// Species to exclude from the item pool.
        /// </summary>
        [DataType(1, DataManager.DataType.Monster, false)]
        public HashSet<string> ExceptFor { get; set; }

        /// <summary>
        /// Initializes a new instance of MobSpawnExclElement with default values.
        /// </summary>
        public MobSpawnExclElement()
        {
            ExceptFor = new HashSet<string>();
        }

        /// <summary>
        /// Initializes a new instance of MobSpawnExclElement with the specified parameters.
        /// </summary>
        /// <param name="box">The box item ID.</param>
        /// <param name="exceptFor">The set of species to exclude from the item pool.</param>
        /// <param name="rarity">The rarity level range of items to select from.</param>
        /// <param name="startOnly">Whether to only apply at map generation time.</param>
        public MobSpawnExclElement(string box, HashSet<string> exceptFor, IntRange rarity, bool startOnly) : base(box, rarity, startOnly)
        {
            ExceptFor = exceptFor;
        }

        /// <summary>
        /// Initializes a new instance of MobSpawnExclElement by copying another instance.
        /// </summary>
        /// <param name="other">The MobSpawnExclElement instance to copy.</param>
        public MobSpawnExclElement(MobSpawnExclElement other) : base(other)
        {
            ExceptFor = new HashSet<string>();
            foreach (string except in other.ExceptFor)
                ExceptFor.Add(except);
        }

        /// <inheritdoc/>
        public override MobSpawnExtra Copy() { return new MobSpawnExclElement(this); }

        /// <summary>
        /// Gets all released monster species sharing the spawned character's element type.
        /// Matches against either primary or secondary element and excludes those in ExceptFor set.
        /// </summary>
        /// <param name="map">The mob spawn map context.</param>
        /// <param name="newChar">The character being spawned.</param>
        /// <returns>An enumeration of released species matching the character's element.</returns>
        protected override IEnumerable<string> GetPossibleSpecies(IMobSpawnMap map, Character newChar)
        {
            MonsterFeatureData feature = DataManager.Instance.UniversalData.Get<MonsterFeatureData>();
            //iterate all species that have that element, except for
            foreach (string key in DataManager.Instance.DataIndices[DataManager.DataType.Monster].GetOrderedKeys(true))
            {
                if (ExceptFor.Contains(key))
                    continue;
                EntrySummary summary = DataManager.Instance.DataIndices[DataManager.DataType.Monster].Get(key);
                if (!summary.Released)
                    continue;

                Dictionary<int, FormFeatureSummary> species;
                if (!feature.FeatureData.TryGetValue(key, out species))
                    continue;
                FormFeatureSummary form;
                if (!species.TryGetValue(0, out form))
                    continue;

                if (form.Element1 != DataManager.Instance.DefaultElement)
                {
                    if (newChar.HasElement(form.Element1))
                        yield return key;
                }
                if (form.Element2 != DataManager.Instance.DefaultElement)
                {
                    if (newChar.HasElement(form.Element2))
                        yield return key;
                }
            }
        }
    }


    /// <summary>
    /// Spawn the mob with its inventory filled with the specified items.
    /// Inventory items are not dropped when the mob is defeated.
    /// </summary>
    [Serializable]
    public class MobSpawnInv : MobSpawnExtra
    {
        /// <summary>
        /// Items to give. All of them will be placed in the mob's inventory.
        /// </summary>
        public List<InvItem> Items;

        /// <summary>
        /// Only give it the item on map generation.
        /// Respawns that occur after the map is generated do not get the item.
        /// </summary>
        public bool MapStartOnly;

        /// <summary>
        /// Initializes a new instance of MobSpawnInv with default values.
        /// </summary>
        public MobSpawnInv()
        {
            Items = new List<InvItem>();
        }

        /// <summary>
        /// Initializes a new instance of MobSpawnInv with the specified items.
        /// </summary>
        /// <param name="startOnly">Whether to only apply the items at map generation time.</param>
        /// <param name="itemNum">Array of item IDs to add to the inventory.</param>
        public MobSpawnInv(bool startOnly, params string[] itemNum) : this()
        {
            MapStartOnly = startOnly;
            for (int ii = 0; ii < itemNum.Length; ii++)
                Items.Add(new InvItem(itemNum[ii]));
        }

        /// <summary>
        /// Initializes a new instance of MobSpawnInv by copying another instance.
        /// </summary>
        /// <param name="other">The MobSpawnInv instance to copy.</param>
        public MobSpawnInv(MobSpawnInv other) : this()
        {
            MapStartOnly = other.MapStartOnly;
            for (int ii = 0; ii < other.Items.Count; ii++)
                Items.Add(new InvItem(other.Items[ii]));
        }

        /// <inheritdoc/>
        public override MobSpawnExtra Copy() { return new MobSpawnInv(this); }

        /// <summary>
        /// Adds the configured items to the spawned character's team inventory.
        /// Will not add items if MapStartOnly is true and the map has already begun.
        /// </summary>
        /// <param name="map">The mob spawn map context containing spawn information.</param>
        /// <param name="newChar">The character to apply the feature to.</param>
        public override void ApplyFeature(IMobSpawnMap map, Character newChar)
        {
            if (MapStartOnly && map.Begun)
                return;

            for (int ii = 0; ii < Items.Count; ii++)
                newChar.MemberTeam.AddToInv(Items[ii], true);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0}", this.GetType().GetFormattedTypeName());
        }
    }

    /// <summary>
    /// Spawn the mob with a level that scales based on the current floor.
    /// </summary>
    [Serializable]
    public class MobSpawnLevelScale : MobSpawnExtra
    {
        /// <summary>
        /// The floor to start scaling level at.
        /// </summary>
        [IntRange(0, true)]
        public int StartFromID;

        /// <summary>
        /// The level to start scaling at.
        /// </summary>
        public int MinLevel;

        /// <summary>
        /// The numerator for the fractional level to add per floor.
        /// </summary>
        public int AddNumerator;

        /// <summary>
        /// The denominator for the fractional level to add per floor.
        /// </summary>
        public int AddDenominator;

        /// <summary>
        /// Reroll the skills to be that of the resulting level.
        /// </summary>
        public bool RerollSkills;

        /// <summary>
        /// Initializes a new instance of MobSpawnLevelScale with default values.
        /// </summary>
        public MobSpawnLevelScale()
        {

        }

        /// <summary>
        /// Initializes a new instance of MobSpawnLevelScale with the specified parameters.
        /// </summary>
        /// <param name="minLevel">The starting level for scaling.</param>
        /// <param name="numerator">The numerator for per-floor level increase.</param>
        /// <param name="denominator">The denominator for per-floor level increase.</param>
        /// <param name="rerollSkills">Whether to reroll skills based on the final level.</param>
        public MobSpawnLevelScale(int minLevel, int numerator, int denominator, bool rerollSkills) : this()
        {
            MinLevel = minLevel;
            AddNumerator = numerator;
            AddDenominator = denominator;
            RerollSkills = rerollSkills;
        }

        /// <summary>
        /// Initializes a new instance of MobSpawnLevelScale by copying another instance.
        /// </summary>
        /// <param name="other">The MobSpawnLevelScale instance to copy.</param>
        public MobSpawnLevelScale(MobSpawnLevelScale other) : this()
        {
            MinLevel = other.MinLevel;
            StartFromID = other.StartFromID;
            AddNumerator = other.AddNumerator;
            AddDenominator = other.AddDenominator;
            RerollSkills = other.RerollSkills;
        }

        /// <inheritdoc/>
        public override MobSpawnExtra Copy() { return new MobSpawnLevelScale(this); }

        /// <summary>
        /// Applies level scaling to the spawned character based on the current floor ID.
        /// Calculates the level as: MinLevel + (map.ID - StartFromID) * AddNumerator / AddDenominator.
        /// Optionally rerolls the character's skills based on the calculated level.
        /// </summary>
        /// <param name="map">The mob spawn map context containing spawn information.</param>
        /// <param name="newChar">The character to apply the feature to.</param>
        public override void ApplyFeature(IMobSpawnMap map, Character newChar)
        {
            newChar.Level = MinLevel + (map.ID-StartFromID) * AddNumerator / AddDenominator;
            newChar.HP = newChar.MaxHP;

            if (RerollSkills)
            {
                BaseMonsterForm form = DataManager.Instance.GetMonster(newChar.BaseForm.Species).Forms[newChar.BaseForm.Form];

                List<string> final_skills = form.RollLatestSkills(newChar.Level, new List<string>());
                for (int ii = 0; ii < Character.MAX_SKILL_SLOTS; ii++)
                {
                    if (ii < final_skills.Count)
                        newChar.BaseSkills[ii] = new SlotSkill(final_skills[ii]);
                    else
                        newChar.BaseSkills[ii] = new SlotSkill();
                }
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0}", this.GetType().GetFormattedTypeName());
        }
    }

    /// <summary>
    /// Spawn the mob with a specific location and direction.
    /// </summary>
    [Serializable]
    public class MobSpawnLoc : MobSpawnExtra
    {
        /// <summary>
        /// The location where the character will spawn.
        /// </summary>
        public Loc Loc;

        /// <summary>
        /// The direction the character will face.
        /// </summary>
        public Dir8 Dir;

        /// <summary>
        /// Initializes a new instance of MobSpawnLoc with default values.
        /// </summary>
        public MobSpawnLoc() { }

        /// <summary>
        /// Initializes a new instance of MobSpawnLoc with the specified location.
        /// </summary>
        /// <param name="loc">The spawn location.</param>
        public MobSpawnLoc(Loc loc) { Loc = loc; }

        /// <summary>
        /// Initializes a new instance of MobSpawnLoc with the specified location and direction.
        /// </summary>
        /// <param name="loc">The spawn location.</param>
        /// <param name="dir">The direction to face.</param>
        public MobSpawnLoc(Loc loc, Dir8 dir) { Loc = loc; Dir = dir; }

        /// <summary>
        /// Initializes a new instance of MobSpawnLoc by copying another instance.
        /// </summary>
        /// <param name="other">The MobSpawnLoc instance to copy.</param>
        public MobSpawnLoc(MobSpawnLoc other)
        {
            Loc = other.Loc;
            Dir = other.Dir;
        }

        /// <inheritdoc/>
        public override MobSpawnExtra Copy() { return new MobSpawnLoc(this); }

        /// <summary>
        /// Sets the spawned character's location and direction.
        /// </summary>
        /// <param name="map">The mob spawn map context containing spawn information.</param>
        /// <param name="newChar">The character to apply the feature to.</param>
        public override void ApplyFeature(IMobSpawnMap map, Character newChar)
        {
            newChar.CharLoc = Loc;
            newChar.CharDir = Dir;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0}", this.GetType().GetFormattedTypeName());
        }
    }


    /// <summary>
    /// Spawn the mob with recruitment turned off.
    /// </summary>
    [Serializable]
    public class MobSpawnUnrecruitable : MobSpawnExtra
    {
        /// <inheritdoc/>
        public override MobSpawnExtra Copy() { return new MobSpawnUnrecruitable(); }

        /// <summary>
        /// Marks the spawned character as unrecruitable and sets its skin to default.
        /// </summary>
        /// <param name="map">The mob spawn map context containing spawn information.</param>
        /// <param name="newChar">The character to apply the feature to.</param>
        public override void ApplyFeature(IMobSpawnMap map, Character newChar)
        {
            newChar.Unrecruitable = true;
            newChar.BaseForm.Skin = DataManager.Instance.DefaultSkin;
            newChar.CurrentForm = newChar.BaseForm;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0}", this.GetType().GetFormattedTypeName());
        }
    }


    /// <summary>
    /// Spawns the mob with aggression towards enemy mobs. Only applies to neutral mobs.
    /// </summary>
    [Serializable]
    public class MobSpawnFoeConflict : MobSpawnExtra
    {
        /// <inheritdoc/>
        public override MobSpawnExtra Copy() { return new MobSpawnFoeConflict(); }

        /// <summary>
        /// Enables foe conflict for the spawned character's team if it is a monster team.
        /// </summary>
        /// <param name="map">The mob spawn map context containing spawn information.</param>
        /// <param name="newChar">The character to apply the feature to.</param>
        public override void ApplyFeature(IMobSpawnMap map, Character newChar)
        {
            if (newChar.MemberTeam is MonsterTeam)
                newChar.MemberTeam.FoeConflict = true;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0}", this.GetType().GetFormattedTypeName());
        }
    }


    /// <summary>
    /// Spawn the mob with an effect on interaction. Only applies to allies or neutral mobs.
    /// </summary>
    [Serializable]
    public class MobSpawnInteractable : MobSpawnExtra
    {
        /// <summary>
        /// The battle events to trigger when the mob is interacted with.
        /// </summary>
        public List<BattleEvent> CheckEvents;

        /// <summary>
        /// Initializes a new instance of MobSpawnInteractable with default values.
        /// </summary>
        public MobSpawnInteractable()
        {
            CheckEvents = new List<BattleEvent>();
        }

        /// <summary>
        /// Initializes a new instance of MobSpawnInteractable with the specified battle events.
        /// </summary>
        /// <param name="checkEvents">The battle events to add to the interaction list.</param>
        public MobSpawnInteractable(params BattleEvent[] checkEvents)
        {
            CheckEvents = new List<BattleEvent>();
            foreach (BattleEvent effect in checkEvents)
                CheckEvents.Add(effect);
        }

        /// <summary>
        /// Initializes a new instance of MobSpawnInteractable by copying another instance.
        /// </summary>
        /// <param name="other">The MobSpawnInteractable instance to copy.</param>
        public MobSpawnInteractable(MobSpawnInteractable other) : this()
        {
            foreach (BattleEvent effect in other.CheckEvents)
                CheckEvents.Add((BattleEvent)effect.Clone());
        }

        /// <inheritdoc/>
        public override MobSpawnExtra Copy() { return new MobSpawnInteractable(this); }

        /// <summary>
        /// Adds interaction events to the spawned character's action events.
        /// </summary>
        /// <param name="map">The mob spawn map context containing spawn information.</param>
        /// <param name="newChar">The character to apply the feature to.</param>
        public override void ApplyFeature(IMobSpawnMap map, Character newChar)
        {
            foreach (BattleEvent effect in CheckEvents)
                newChar.ActionEvents.Add((BattleEvent)effect.Clone());
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0}", this.GetType().GetFormattedTypeName());
        }
    }


    /// <summary>
    /// Spawn the mob with a lua data table.
    /// </summary>
    [Serializable]
    public class MobSpawnLuaTable : MobSpawnExtra
    {
        /// <summary>
        /// The lua table code as a string.
        /// </summary>
        [Multiline(0)]
        public string LuaTable;

        /// <summary>
        /// Initializes a new instance of MobSpawnLuaTable with an empty table.
        /// </summary>
        public MobSpawnLuaTable() { LuaTable = "{}"; }

        /// <summary>
        /// Initializes a new instance of MobSpawnLuaTable with the specified lua table.
        /// </summary>
        /// <param name="luaTable">The lua table code as a string.</param>
        public MobSpawnLuaTable(string luaTable) { LuaTable = luaTable; }

        /// <summary>
        /// Initializes a new instance of MobSpawnLuaTable by copying another instance.
        /// </summary>
        /// <param name="other">The MobSpawnLuaTable instance to copy.</param>
        protected MobSpawnLuaTable(MobSpawnLuaTable other)
        {
            LuaTable = other.LuaTable;
        }

        /// <inheritdoc/>
        public override MobSpawnExtra Copy() { return new MobSpawnLuaTable(this); }

        /// <summary>
        /// Evaluates the lua table code and assigns the result to the character's lua data table.
        /// </summary>
        /// <param name="map">The mob spawn map context containing spawn information.</param>
        /// <param name="newChar">The character to apply the feature to.</param>
        public override void ApplyFeature(IMobSpawnMap map, Character newChar)
        {
            newChar.LuaDataTable = LuaEngine.Instance.RunString("return " + LuaTable).First() as LuaTable;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0}", this.GetType().GetFormattedTypeName());
        }
    }


    /// <summary>
    /// Spawn the mob with a discriminator. This is used for personality calculations.
    /// </summary>
    [Serializable]
    public class MobSpawnDiscriminator : MobSpawnExtra
    {
        /// <summary>
        /// The discriminator value for personality calculations.
        /// </summary>
        public int Discriminator;

        /// <summary>
        /// Initializes a new instance of MobSpawnDiscriminator with default values.
        /// </summary>
        public MobSpawnDiscriminator() { }

        /// <summary>
        /// Initializes a new instance of MobSpawnDiscriminator with the specified discriminator value.
        /// </summary>
        /// <param name="discriminator">The discriminator value.</param>
        public MobSpawnDiscriminator(int discriminator) { Discriminator = discriminator; }

        /// <summary>
        /// Initializes a new instance of MobSpawnDiscriminator by copying another instance.
        /// </summary>
        /// <param name="other">The MobSpawnDiscriminator instance to copy.</param>
        protected MobSpawnDiscriminator(MobSpawnDiscriminator other)
        {
            Discriminator = other.Discriminator;
        }

        /// <inheritdoc/>
        public override MobSpawnExtra Copy() { return new MobSpawnDiscriminator(this); }

        /// <summary>
        /// Sets the discriminator value on the spawned character.
        /// </summary>
        /// <param name="map">The mob spawn map context containing spawn information.</param>
        /// <param name="newChar">The character to apply the feature to.</param>
        public override void ApplyFeature(IMobSpawnMap map, Character newChar)
        {
            newChar.Discriminator = Discriminator;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0}", this.GetType().GetFormattedTypeName());
        }
    }
	
	/// <summary>
    /// Allow mob to spawn with a chance to roll for Intrinsic3 (hidden ability).
    /// </summary>
    [Serializable]
    public class Intrinsic3Chance : MobSpawnExtra
    {
        /// <inheritdoc/>
        public override MobSpawnExtra Copy() { return new Intrinsic3Chance(); }

        /// <summary>
        /// Applies a random chance for the spawned character to learn its Intrinsic3 (hidden ability).
        /// The chance varies based on how many other intrinsics the character has:
        /// - If it has Intrinsic0 and Intrinsic1: 1-in-3 chance
        /// - If it has only Intrinsic0: 1-in-2 chance
        /// </summary>
        /// <param name="map">The mob spawn map context containing spawn information.</param>
        /// <param name="newChar">The character to apply the feature to.</param>
		public override void ApplyFeature(IMobSpawnMap map, Character newChar)
        {
            MonsterID form = newChar.BaseForm;
			MonsterData data = DataManager.Instance.GetMonster(form.Species);
			BaseMonsterForm baseForm = data.Forms[form.Form];

            // DataManager.Instance.DefaultIntrinsic resolves to True if the ability is not there. If false, then mob does have an Intrinsic 3.
            if (baseForm.Intrinsic3 != DataManager.Instance.DefaultIntrinsic)
			{
				var rand = new Random();
                int roll;

                // If not true, then mob has both Intrinsic 0 and Intrinsic 1 (in addition to Intrinsic 3).
                if (baseForm.Intrinsic2 != DataManager.Instance.DefaultIntrinsic)
					roll = rand.Next(3); // Hidden ability has 1-in-3 chance.
				else // Mob only has Intrinsic 0 (in addition to Intrinsic 3).
					roll = rand.Next(2); // Hidden ability has 1-in-2 chance.

                if (roll == 0)
                    newChar.LearnIntrinsic(baseForm.Intrinsic3, 0, false);
			}
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0}", this.GetType().GetFormattedTypeName());
        }
    }
}

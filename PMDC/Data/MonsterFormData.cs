using System;
using System.Collections.Generic;
using RogueElements;
using System.Drawing;
using System.Linq;
using RogueEssence;
using RogueEssence.Data;
using RogueEssence.Dungeon;
using RogueEssence.Dev;
using PMDC.Dungeon;
using Newtonsoft.Json;
using PMDC.Dev;
using System.Runtime.Serialization;

namespace PMDC.Data
{

    /// <summary>
    /// Stores form-specific data for a monster species.
    /// Extends the base monster form with additional stats, gender weights, learnable skills,
    /// and methods for stat calculations, rolling attributes, and skill validation.
    /// </summary>
    [Serializable]
    public class MonsterFormData : BaseMonsterForm
    {
        /// <summary>
        /// Maximum value for stat boost bonuses. Used in stat calculations to scale bonus stats.
        /// </summary>
        public const int MAX_STAT_BOOST = 256;


        /// <summary>
        /// The generation (game version) in which this form was first introduced.
        /// </summary>
        public int Generation;

        /// <summary>
        /// Weight value for spawning this monster as genderless.
        /// Higher values increase the probability of genderless spawns.
        /// </summary>
        public int GenderlessWeight;

        /// <summary>
        /// Weight value for spawning this monster as male.
        /// Higher values increase the probability of male spawns.
        /// </summary>
        public int MaleWeight;

        /// <summary>
        /// Weight value for spawning this monster as female.
        /// Higher values increase the probability of female spawns.
        /// </summary>
        public int FemaleWeight;

        /// <summary>
        /// Base HP stat before level scaling. Determines hit points at any level.
        /// </summary>
        public int BaseHP;

        /// <summary>
        /// Base Attack stat before level scaling. Affects physical move damage.
        /// </summary>
        public int BaseAtk;

        /// <summary>
        /// Base Defense stat before level scaling. Reduces physical damage taken.
        /// </summary>
        [SharedRow]
        public int BaseDef;

        /// <summary>
        /// Base Special Attack stat before level scaling. Affects special move damage.
        /// </summary>
        public int BaseMAtk;

        /// <summary>
        /// Base Special Defense stat before level scaling. Reduces special damage taken.
        /// </summary>
        [SharedRow]
        public int BaseMDef;

        /// <summary>
        /// Base Speed stat before level scaling. Affects turn order and evasion.
        /// </summary>
        public int BaseSpeed;

        /// <summary>
        /// Experience points awarded when this monster is defeated.
        /// </summary>
        public int ExpYield;

        /// <summary>
        /// Physical height of this monster form in meters.
        /// </summary>
        public double Height;

        /// <summary>
        /// Physical weight of this monster form in kilograms.
        /// </summary>
        [SharedRow]
        public double Weight;

        /// <summary>
        /// List of personality type indices this monster form can have.
        /// Used for determining behavior traits based on discriminator value.
        /// </summary>
        public List<byte> Personalities;

        /// <summary>
        /// Skills that can be learned via TM (Technical Machine) items.
        /// </summary>
        public List<LearnableSkill> TeachSkills;

        /// <summary>
        /// Skills inherited through breeding (egg moves).
        /// </summary>
        public List<LearnableSkill> SharedSkills;

        /// <summary>
        /// Skills learned through special tutors or events.
        /// </summary>
        public List<LearnableSkill> SecretSkills;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonsterFormData"/> class with empty skill and personality lists.
        /// </summary>
        public MonsterFormData()
        {
            Personalities = new List<byte>();

            TeachSkills = new List<LearnableSkill>();
            SharedSkills = new List<LearnableSkill>();
            SecretSkills = new List<LearnableSkill>();
        }

        /// <summary>
        /// Gets the base stat value for the specified stat type.
        /// </summary>
        /// <param name="stat">The stat category to retrieve.</param>
        /// <returns>The base stat value, or 0 if the stat type is not recognized.</returns>
        public int GetBaseStat(Stat stat)
        {
            switch (stat)
            {
                case Stat.HP:
                    return BaseHP;
                case Stat.Speed:
                    return BaseSpeed;
                case Stat.Attack:
                    return BaseAtk;
                case Stat.Defense:
                    return BaseDef;
                case Stat.MAtk:
                    return BaseMAtk;
                case Stat.MDef:
                    return BaseMDef;
                default:
                    return 0;
            }
        }

        /// <inheritdoc/>
        public override int GetStat(int level, Stat stat, int bonus)
        {
            int curStat = getMinStat(level, stat);
            int minStat = getMinStat(DataManager.Instance.Start.MaxLevel, stat);
            int maxStat = GetMaxStat(stat, DataManager.Instance.Start.MaxLevel);
            int statDiff = maxStat - minStat;

            return Math.Max(1, curStat + bonus * statDiff / MonsterFormData.MAX_STAT_BOOST);
        }

        /// <inheritdoc/>
        public override string RollSkin(IRandom rand)
        {
            SkinTableState table = DataManager.Instance.UniversalEvent.UniversalStates.GetWithDefault<SkinTableState>();
            if (table.AltColorOdds == 0)
                return DataManager.Instance.DefaultSkin;
            if (rand.Next(table.AltColorOdds) == 0)
                return table.AltColor;
            return DataManager.Instance.DefaultSkin;
        }

        /// <inheritdoc/>
        public override int GetPersonalityType(int discriminator)
        {
            return Personalities[discriminator / 256 % Personalities.Count];
        }

        /// <inheritdoc/>
        public override Gender RollGender(IRandom rand)
        {
            int totalWeight = FemaleWeight + MaleWeight + GenderlessWeight;
            int roll = rand.Next(0, totalWeight);
            if (roll < FemaleWeight)
                return Gender.Female;
            roll -= FemaleWeight;
            if (roll < MaleWeight)
                return Gender.Male;
            
            return Gender.Genderless;
        }

        /// <inheritdoc/>
        public override string RollIntrinsic(IRandom rand, int bounds)
        {
            List<string> abilities = new List<string>();
            abilities.Add(Intrinsic1);
            if (Intrinsic2 != DataManager.Instance.DefaultIntrinsic && bounds > 1)
                abilities.Add(Intrinsic2);
            if (Intrinsic3 != DataManager.Instance.DefaultIntrinsic && bounds > 2)
                abilities.Add(Intrinsic3);

            return abilities[rand.Next(abilities.Count)];
        }


        /// <inheritdoc/>
        public override List<Gender> GetPossibleGenders()
        {
            List<Gender> genders = new List<Gender>();

            if (MaleWeight > 0)
                genders.Add(Gender.Male);
            if (FemaleWeight > 0)
                genders.Add(Gender.Female);
            if (GenderlessWeight > 0 || genders.Count == 0)
                genders.Add(Gender.Genderless);
            return genders;
        }

        /// <inheritdoc/>
        public override List<string> GetPossibleSkins()
        {
            List<string> colors = new List<string>();

            SkinTableState table = DataManager.Instance.UniversalEvent.UniversalStates.GetWithDefault<SkinTableState>();
            colors.Add(DataManager.Instance.DefaultSkin);
            colors.Add(table.Challenge);

            return colors;
        }

        /// <inheritdoc/>
        public override List<int> GetPossibleIntrinsicSlots()
        {
            List<int> abilities = new List<int>();

            abilities.Add(0);
            //if intrinsic cannot be achieved, default to first intrinsic
            if (Intrinsic2 != DataManager.Instance.DefaultIntrinsic)
                abilities.Add(1);
            if (Intrinsic3 != DataManager.Instance.DefaultIntrinsic)
                abilities.Add(2);

            return abilities;
        }
        
        /// <summary>
        /// Gets all skills this monster form can potentially learn.
        /// Includes level-up skills, TM skills, egg moves, and tutor moves.
        /// </summary>
        /// <returns>Distinct list of all learnable skill IDs.</returns>
        // TODO: Consider moves from prior evolutions
        public List<string> GetPossibleSkills()
        {
            List<string> skills = new List<string>();
            skills.AddRange(LevelSkills.Select(x => x.Skill));
            skills.AddRange(TeachSkills.Select(x => x.Skill));
            skills.AddRange(SharedSkills.Select(x => x.Skill));
            skills.AddRange(SecretSkills.Select(x => x.Skill));
            return skills.Distinct().ToList();
        }


        /// <summary>
        /// Calculates the minimum stat value at a given level (no bonus applied).
        /// </summary>
        /// <param name="level">The level to calculate the stat for.</param>
        /// <param name="stat">The stat category to calculate.</param>
        /// <returns>The minimum stat value at the specified level.</returns>
        private int getMinStat(int level, Stat stat)
        {
            switch (stat)
            {
                case Stat.HP:
                    return hpStatCalc(BaseHP, level);
                case Stat.Speed:
                    return genericStatCalc(BaseSpeed, level);
                case Stat.Attack:
                    return genericStatCalc(BaseAtk, level);
                case Stat.Defense:
                    return genericStatCalc(BaseDef, level);
                case Stat.MAtk:
                    return genericStatCalc(BaseMAtk, level);
                case Stat.MDef:
                    return genericStatCalc(BaseMDef, level);
                default:
                    return 0;
            }
        }

        /// <inheritdoc/>
        public override int GetMaxStat(Stat stat, int level)
        {
            switch (stat)
            {
                case Stat.HP:
                    return hpStatMax(BaseHP, level);
                case Stat.Speed:
                    return genericStatMax(BaseSpeed, level);
                case Stat.Attack:
                    return genericStatMax(BaseAtk, level);
                case Stat.Defense:
                    return genericStatMax(BaseDef, level);
                case Stat.MAtk:
                    return genericStatMax(BaseMAtk, level);
                case Stat.MDef:
                    return genericStatMax(BaseMDef, level);
                default:
                    return 0;
            }
        }

        /// <inheritdoc/>
        public override int ReverseGetStat(Stat stat, int val, int level)
        {
            if (stat == Stat.HP)
                return (val - 10) * DataManager.Instance.Start.MaxLevel / level - 130;
            else
                return (val - 5) * DataManager.Instance.Start.MaxLevel / level - 30;
        }

        /// <inheritdoc/>
        public override int GetMaxStatBonus(Stat stat)
        {
            return MAX_STAT_BOOST;
        }

        /// <summary>
        /// Standard stat calculation formula for non-HP stats.
        /// </summary>
        /// <param name="baseStat">The base stat value.</param>
        /// <param name="level">The current level.</param>
        /// <returns>The calculated stat value.</returns>
        private int genericStatCalc(int baseStat, int level)
        {
            return (baseStat + 30) * level / DataManager.Instance.Start.MaxLevel + 5;
        }

        /// <summary>
        /// HP-specific stat calculation formula with higher base offset.
        /// Handles special case where base HP is 1 (Shedinja-style monsters).
        /// </summary>
        /// <param name="baseStat">The base HP stat value.</param>
        /// <param name="level">The current level.</param>
        /// <returns>The calculated HP value.</returns>
        private int hpStatCalc(int baseStat, int level)
        {
            if (baseStat > 1)
                return (baseStat + 130) * level / DataManager.Instance.Start.MaxLevel + 10;
            else
                return (level / 10 + 1);
        }

        /// <summary>
        /// Scales a base stat relative to the total base stat distribution.
        /// Used for calculating maximum stats with balanced stat totals.
        /// </summary>
        /// <param name="baseStat">The base stat value to scale.</param>
        /// <returns>The scaled stat value based on total distribution.</returns>
        private int scaleStatTotal(int baseStat)
        {
            if (baseStat > 1)
            {
                if (BaseHP > 1)
                    return 1536 * baseStat / (BaseHP + BaseAtk + BaseDef + BaseMAtk + BaseMDef + BaseSpeed);
                else
                    return 1280 * baseStat / (BaseAtk + BaseDef + BaseMAtk + BaseMDef + BaseSpeed);
            }
            return 1;
        }

        /// <summary>
        /// Calculates the maximum value for a non-HP stat at a given level.
        /// </summary>
        /// <param name="baseStat">The base stat value.</param>
        /// <param name="level">The current level.</param>
        /// <returns>The maximum stat value.</returns>
        private int genericStatMax(int baseStat, int level)
        {
            return genericStatCalc(scaleStatTotal(baseStat), level);
        }

        /// <summary>
        /// Calculates the maximum HP value at a given level.
        /// Handles special case for monsters with 1 base HP.
        /// </summary>
        /// <param name="baseStat">The base HP stat value.</param>
        /// <param name="level">The current level.</param>
        /// <returns>The maximum HP value.</returns>
        private int hpStatMax(int baseStat, int level)
        {
            if (baseStat > 1)
                return hpStatCalc(scaleStatTotal(baseStat), level);
            else
                return (level / 5 + 1);
        }

        /// <inheritdoc/>
        public override bool CanLearnSkill(string skill)
        {
            if (LevelSkills.FindIndex(a => a.Skill == skill) > -1)
                return true;
            if (TeachSkills.FindIndex(a => a.Skill == skill) > -1)
                return true;
            if (SharedSkills.FindIndex(a => a.Skill == skill) > -1)
                return true;
            if (SecretSkills.FindIndex(a => a.Skill == skill) > -1)
                return true;
            return false;
        }

    }



}
using System;
using System.Collections.Generic;
using RogueEssence.Data;
using RogueEssence.Menu;
using RogueElements;
using RogueEssence.Content;
using RogueEssence.LevelGen;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Dev;
using PMDC.Dev;
using PMDC.Data;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using NLua;
using RogueEssence.Script;
using System.Linq;

namespace PMDC.Dungeon
{
    // For battle events that alter or assign basepower

    /// <summary>
    /// Event that changes the move type depending on the character's held item.
    /// </summary>
    [Serializable]
    public class ItemPowerEvent : BattleEvent
    {

        /// <summary>
        /// The item ID mapped to a type.
        /// </summary>
        [JsonConverter(typeof(ItemElementDictConverter))]
        [DataType(1, DataManager.DataType.Item, false)]
        [DataType(2, DataManager.DataType.Element, false)]
        public Dictionary<string, string> ItemPair;

        /// <inheritdoc/>
        public ItemPowerEvent() { ItemPair = new Dictionary<string, string>(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemPowerEvent"/> class with the specified item-element pairs.
        /// </summary>
        /// <param name="weather">Dictionary mapping item IDs to element types.</param>
        public ItemPowerEvent(Dictionary<string, string> weather)
        {
            ItemPair = weather;
        }

        /// <inheritdoc/>
        protected ItemPowerEvent(ItemPowerEvent other)
            : this()
        {
            foreach (string item in other.ItemPair.Keys)
                ItemPair.Add(item, other.ItemPair[item]);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ItemPowerEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            string element;
            if (ItemPair.TryGetValue(context.User.EquippedItem.ID, out element))
            {
                context.Data.Element = element;
                ElementData elementData = DataManager.Instance.GetElement(element);
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_SKILL_TO_ELEMENT").ToLocal(), elementData.GetIconName()));
                yield break;
            }
        }
    }

    /// <summary>
    /// Event that modifies the base power of the move depending on the weight of the target.
    /// Heavier targets result in higher base power.
    /// </summary>
    [Serializable]
    public class WeightBasePowerEvent : BattleEvent
    {
        /// <inheritdoc/>
        public WeightBasePowerEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new WeightBasePowerEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            BasePowerState basePower = context.Data.SkillStates.GetWithDefault<BasePowerState>();
            if (basePower != null)
            {
                MonsterFormData formData = (MonsterFormData)DataManager.Instance.GetMonster(context.Target.CurrentForm.Species).Forms[context.Target.CurrentForm.Form];
                double weight = formData.Weight;

                //light/heavy flags here
                if (context.Target.CharStates.Contains<LightWeightState>())
                    weight /= 2;
                if (context.Target.CharStates.Contains<HeavyWeightState>())
                    weight *= 2;

                if (weight > 200)
                    basePower.Power = 160;
                else if (weight > 100)
                    basePower.Power = 120;
                else if (weight > 50)
                    basePower.Power = 100;
                else if (weight > 25)
                    basePower.Power = 80;
                else if (weight > 10)
                    basePower.Power = 60;
                else
                    basePower.Power = 40;
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that boosts the power of moves depending on the total stat changes.
    /// More positive stat changes result in higher base power.
    /// </summary>
    [Serializable]
    public class StatBasePowerEvent : BattleEvent
    {
        /// <summary>
        /// The base power added for each stat change stack.
        /// </summary>
        public int AddedPower;

        /// <summary>
        /// Whether to check the target's stats instead of the user's stats.
        /// </summary>
        public bool FromTarget;

        /// <summary>
        /// The set of status IDs representing stat changes to count.
        /// </summary>
        [JsonConverter(typeof(StatusSetConverter))]
        [DataType(1, DataManager.DataType.Status, false)]
        public HashSet<string> StatChangeIDs;

        /// <inheritdoc/>
        public StatBasePowerEvent() { StatChangeIDs = new HashSet<string>(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatBasePowerEvent"/> class with the specified parameters.
        /// </summary>
        /// <param name="addedPower">The power added per stat change stack.</param>
        /// <param name="fromTarget">Whether to check the target's stats.</param>
        /// <param name="statChangeIDs">The status IDs to count.</param>
        public StatBasePowerEvent(int addedPower, bool fromTarget, HashSet<string> statChangeIDs)
        {
            AddedPower = addedPower;
            FromTarget = fromTarget;
            StatChangeIDs = statChangeIDs;
        }

        /// <inheritdoc/>
        protected StatBasePowerEvent(StatBasePowerEvent other) : this()
        {
            AddedPower = other.AddedPower;
            FromTarget = other.FromTarget;
            foreach (string statID in other.StatChangeIDs)
                StatChangeIDs.Add(statID);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new StatBasePowerEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Character source = (FromTarget ? context.Target : context.User);
            BasePowerState basePower = context.Data.SkillStates.GetWithDefault<BasePowerState>();
            if (basePower != null)
            {
                int totalStacks = 0;

                foreach (string statID in StatChangeIDs)
                {
                    StatusEffect statChange = source.GetStatusEffect(statID);
                    if (statChange != null)
                        totalStacks += Math.Max(0, statChange.StatusStates.GetWithDefault<StackState>().Stack);
                }

                basePower.Power += AddedPower * totalStacks;
            }
            yield break;
        }
    }



    /// <summary>
    /// Event that boosts the power of moves depending on the number of times being hit before acting.
    /// More hits received result in higher base power.
    /// </summary>
    [Serializable]
    public class PrevHitBasePowerEvent : BattleEvent
    {
        /// <summary>
        /// The base power added for each hit received.
        /// </summary>
        public int AddedPower;

        /// <summary>
        /// The maximum stack limit for counting hits.
        /// </summary>
        public int MaxStack;

        /// <summary>
        /// Whether to check the target's hit count instead of the user's.
        /// </summary>
        public bool FromTarget;

        /// <summary>
        /// The status ID that tracks how many times the character has been hit.
        /// </summary>
        [DataType(0, DataManager.DataType.Status, false)]
        public string PrevHitID;

        /// <inheritdoc/>
        public PrevHitBasePowerEvent() { PrevHitID = ""; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrevHitBasePowerEvent"/> class with the specified parameters.
        /// </summary>
        /// <param name="addedPower">The power added per hit.</param>
        /// <param name="fromTarget">Whether to check the target's hit count.</param>
        /// <param name="prevHitID">The status ID tracking hits.</param>
        /// <param name="limit">The maximum stack limit.</param>
        public PrevHitBasePowerEvent(int addedPower, bool fromTarget, string prevHitID, int limit)
        {
            AddedPower = addedPower;
            FromTarget = fromTarget;
            PrevHitID = prevHitID;
            MaxStack = limit;
        }

        /// <inheritdoc/>
        protected PrevHitBasePowerEvent(PrevHitBasePowerEvent other) : this()
        {
            AddedPower = other.AddedPower;
            FromTarget = other.FromTarget;
            PrevHitID = other.PrevHitID;
            MaxStack = other.MaxStack;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new PrevHitBasePowerEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Character source = (FromTarget ? context.Target : context.User);

            StatusEffect recentHitStatus = source.GetStatusEffect(PrevHitID);
            if (recentHitStatus != null)
            {
                BasePowerState basePower = context.Data.SkillStates.GetWithDefault<BasePowerState>();
                if (basePower != null)
                {
                    int timesHit = Math.Min(recentHitStatus.StatusStates.GetWithDefault<StackState>().Stack, MaxStack);
                    basePower.Power += AddedPower * timesHit;
                }
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that makes a move deal more or less damage depending on the character's HP.
    /// Can be configured to deal more damage at low HP or high HP.
    /// </summary>
    [Serializable]
    public class HPBasePowerEvent : BattleEvent
    {

        /// <summary>
        /// The maximum base power of the move at full HP (or minimum HP if reversed).
        /// </summary>
        public int MaxPower;

        /// <summary>
        /// Whether the less HP the character has, the more damage dealt.
        /// </summary>
        public bool Reverse;

        /// <summary>
        /// Whether to calculate the power based on target HP instead of user HP.
        /// </summary>
        public bool FromTarget;

        /// <inheritdoc/>
        public HPBasePowerEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="HPBasePowerEvent"/> class with the specified parameters.
        /// </summary>
        /// <param name="maxPower">The maximum base power.</param>
        /// <param name="reverse">Whether lower HP means more damage.</param>
        /// <param name="affectTarget">Whether to use target's HP.</param>
        public HPBasePowerEvent(int maxPower, bool reverse, bool affectTarget)
        {
            MaxPower = maxPower;
            Reverse = reverse;
            FromTarget = affectTarget;
        }

        /// <inheritdoc/>
        protected HPBasePowerEvent(HPBasePowerEvent other)
        {
            MaxPower = other.MaxPower;
            Reverse = other.Reverse;
            FromTarget = other.FromTarget;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new HPBasePowerEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Character source = (FromTarget ? context.Target : context.User);
            BasePowerState basePower = context.Data.SkillStates.GetWithDefault<BasePowerState>();
            if (basePower != null)
                basePower.Power = MaxPower * (Reverse ? (source.MaxHP - source.HP) : source.HP) / source.MaxHP;
            yield break;
        }
    }


    /// <summary>
    /// Event that makes a move deal more or less damage depending on the PP amount.
    /// Can be configured to deal more damage at low PP or high PP.
    /// </summary>
    [Serializable]
    public class PPBasePowerEvent : BattleEvent
    {
        /// <summary>
        /// The maximum base power of the move at full PP (or minimum PP if reversed).
        /// </summary>
        public int MaxPower;

        /// <summary>
        /// Whether the less PP remaining, the more damage dealt.
        /// </summary>
        public bool Reverse;

        /// <summary>
        /// Whether to also consider the PP of other moves in the calculation.
        /// </summary>
        public bool Total;

        /// <inheritdoc/>
        public PPBasePowerEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PPBasePowerEvent"/> class with the specified parameters.
        /// </summary>
        /// <param name="maxPower">The maximum base power.</param>
        /// <param name="reverse">Whether lower PP means more damage.</param>
        /// <param name="total">Whether to include all moves' PP.</param>
        public PPBasePowerEvent(int maxPower, bool reverse, bool total)
        {
            MaxPower = maxPower;
            Reverse = reverse;
            Total = total;
        }

        /// <inheritdoc/>
        protected PPBasePowerEvent(PPBasePowerEvent other)
        {
            MaxPower = other.MaxPower;
            Reverse = other.Reverse;
            Total = other.Total;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new PPBasePowerEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            int pp = 0;
            int maxPP = 0;

            int slot = -1;
            if (context.ActionType == BattleActionType.Skill && context.UsageSlot > BattleContext.DEFAULT_ATTACK_SLOT && context.UsageSlot < CharData.MAX_SKILL_SLOTS)
            {
                Skill move = context.User.Skills[context.UsageSlot].Element;
                if (!String.IsNullOrEmpty(move.SkillNum))
                {
                    slot = context.UsageSlot;
                    SkillData data = DataManager.Instance.GetSkill(move.SkillNum);
                    int localMax = data.BaseCharges + context.User.ChargeBoost;
                    pp += Math.Min(move.Charges + 1, localMax);
                    maxPP += localMax;
                }
            }

            if (Total)
            {
                for (int ii = 0; ii < context.User.Skills.Count; ii++)
                {
                    Skill move = context.User.Skills[ii].Element;
                    if (ii != slot && !String.IsNullOrEmpty(move.SkillNum))
                    {
                        SkillData data = DataManager.Instance.GetSkill(move.SkillNum);
                        int localMax = data.BaseCharges + context.User.ChargeBoost;
                        pp += move.Charges;
                        maxPP += localMax;
                    }
                }
            }
            BasePowerState basePower = context.Data.SkillStates.GetWithDefault<BasePowerState>();
            if (basePower != null)
            {
                if (maxPP > 0)
                    basePower.Power = MaxPower * (Reverse ? (maxPP - pp) : pp) / maxPP;
                else
                    basePower.Power = MaxPower;
            }

            yield break;
        }
    }

    /// <summary>
    /// Event that modifies the base power of the move based on the number of allies around the character.
    /// More allies nearby can increase or decrease damage depending on configuration.
    /// </summary>
    [Serializable]
    public class AllyBasePowerEvent : BattleEvent
    {

        /// <summary>
        /// Whether the more allies nearby, the less damage dealt.
        /// </summary>
        public bool Reverse;

        /// <inheritdoc/>
        public AllyBasePowerEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AllyBasePowerEvent"/> class with the specified parameter.
        /// </summary>
        /// <param name="reverse">Whether more allies means less damage.</param>
        public AllyBasePowerEvent(bool reverse)
        {
            Reverse = reverse;
        }

        /// <inheritdoc/>
        protected AllyBasePowerEvent(AllyBasePowerEvent other)
        {
            Reverse = other.Reverse;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new AllyBasePowerEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            BasePowerState basePower = context.Data.SkillStates.GetWithDefault<BasePowerState>();

            if (basePower != null)
            {
                int totalAllies = 0;
                foreach (Character ally in context.User.MemberTeam.EnumerateChars())
                {
                    if (ZoneManager.Instance.CurrentMap.InRange(ally.CharLoc, context.User.CharLoc, 1))
                        totalAllies++;
                }
                if (Reverse)
                    basePower.Power = basePower.Power / totalAllies;
                else
                    basePower.Power = basePower.Power * totalAllies;
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that modifies the base power of the move based on the movement speed differences.
    /// Faster users deal more damage when speed difference is positive.
    /// </summary>
    [Serializable]
    public class SpeedPowerEvent : BattleEvent
    {

        /// <summary>
        /// Whether the less movement speed the user has relative to the target, the more damage dealt.
        /// </summary>
        public bool Reverse;

        /// <inheritdoc/>
        public SpeedPowerEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeedPowerEvent"/> class with the specified parameter.
        /// </summary>
        /// <param name="reverse">Whether slower user means more damage.</param>
        public SpeedPowerEvent(bool reverse)
        {
            Reverse = reverse;
        }

        /// <inheritdoc/>
        protected SpeedPowerEvent(SpeedPowerEvent other)
        {
            Reverse = other.Reverse;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new SpeedPowerEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            BasePowerState basePower = context.Data.SkillStates.GetWithDefault<BasePowerState>();

            if (basePower != null)
            {
                int speedDiff = context.User.MovementSpeed - context.Target.MovementSpeed;
                if (Reverse)
                    speedDiff *= -1;
                if (speedDiff > 0)
                    basePower.Power = (basePower.Power * (1 + speedDiff));
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that increases the base power of the move based on the weight ratio difference.
    /// Heavier users deal more damage to lighter targets.
    /// </summary>
    [Serializable]
    public class WeightCrushBasePowerEvent : BattleEvent
    {
        /// <inheritdoc/>
        public WeightCrushBasePowerEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new WeightCrushBasePowerEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            BasePowerState basePower = context.Data.SkillStates.GetWithDefault<BasePowerState>();
            if (basePower != null)
            {
                MonsterFormData userForm = (MonsterFormData)DataManager.Instance.GetMonster(context.User.CurrentForm.Species).Forms[context.User.CurrentForm.Form];
                double userWeight = userForm.Weight;
                //light/heavy flags here
                if (context.User.CharStates.Contains<LightWeightState>())
                    userWeight /= 2;
                if (context.User.CharStates.Contains<HeavyWeightState>())
                    userWeight *= 2;

                MonsterFormData targetForm = (MonsterFormData)DataManager.Instance.GetMonster(context.Target.CurrentForm.Species).Forms[context.Target.CurrentForm.Form];
                double targetWeight = targetForm.Weight;
                //light/heavy flags here
                if (context.Target.CharStates.Contains<LightWeightState>())
                    targetWeight /= 2;
                if (context.Target.CharStates.Contains<HeavyWeightState>())
                    targetWeight *= 2;

                basePower.Power = 0;
                int weightRatio = (int)(userWeight / targetWeight);
                if (weightRatio > 5)
                    basePower.Power = 160;
                else if (weightRatio > 4)
                    basePower.Power = 120;
                else if (weightRatio > 3)
                    basePower.Power = 80;
                else if (weightRatio > 2)
                    basePower.Power = 40;
                else
                    basePower.Power = 20;
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that modifies the base power of the move if the character is inflicted with the specified status.
    /// Doubles the base power when the status is present.
    /// </summary>
    [Serializable]
    public class StatusPowerEvent : BattleEvent
    {
        /// <summary>
        /// The status ID to check for.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string StatusID;

        /// <summary>
        /// Whether to check the status on the target instead of the user.
        /// </summary>
        public bool AffectTarget;

        /// <inheritdoc/>
        public StatusPowerEvent() { StatusID = ""; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusPowerEvent"/> class with the specified parameters.
        /// </summary>
        /// <param name="statusID">The status ID to check for.</param>
        /// <param name="affectTarget">Whether to check the target's status.</param>
        public StatusPowerEvent(string statusID, bool affectTarget)
        {
            StatusID = statusID;
            AffectTarget = affectTarget;
        }

        /// <inheritdoc/>
        protected StatusPowerEvent(StatusPowerEvent other)
        {
            StatusID = other.StatusID;
            AffectTarget = other.AffectTarget;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new StatusPowerEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Character target = (AffectTarget ? context.Target : context.User);

            BasePowerState basePower = context.Data.SkillStates.GetWithDefault<BasePowerState>();
            if (basePower != null)
            {
                if (target.GetStatusEffect(StatusID) != null)
                {
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_DMG_BOOST_STATUS").ToLocal()));
                    basePower.Power *= 2;
                }
            }
            yield break;
        }
    }


    /// <summary>
    /// Event that modifies the base power of the move if the target's HP is below 50%.
    /// Doubles the base power when the target is at low HP.
    /// </summary>
    [Serializable]
    public class BrineEvent : BattleEvent
    {
        /// <inheritdoc/>
        public BrineEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new BrineEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            BasePowerState basePower = context.Data.SkillStates.GetWithDefault<BasePowerState>();
            if (basePower != null)
            {
                if (context.Target.HP * 2 < context.Target.MaxHP)
                {
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_DMG_BOOST_LOW_HP").ToLocal()));
                    basePower.Power *= 2;
                }
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that modifies the base power of the move if the user is not holding an item.
    /// Doubles the base power when the user has no equipped item.
    /// </summary>
    [Serializable]
    public class AcrobaticEvent : BattleEvent
    {
        /// <inheritdoc/>
        public AcrobaticEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new AcrobaticEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            BasePowerState basePower = context.Data.SkillStates.GetWithDefault<BasePowerState>();
            if (basePower != null)
            {
                if (String.IsNullOrEmpty(context.User.EquippedItem.ID))
                {
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_DMG_BOOST_NO_ITEM").ToLocal()));
                    basePower.Power *= 2;
                }
            }
            yield break;
        }
    }



    /// <summary>
    /// Event that modifies the damage multiplier if the target in the status matches the enemy.
    /// Used for revenge-style attacks that deal more damage to enemies that recently attacked.
    /// </summary>
    [Serializable]
    public class RevengeEvent : BattleEvent
    {
        /// <summary>
        /// The status which contains the target.
        /// Should usually be "last targeted by".
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string TargetStatusID;

        /// <summary>
        /// Numerator of the damage modifier.
        /// </summary>
        public int Numerator;

        /// <summary>
        /// Denominator of the damage modifier.
        /// </summary>
        public int Denominator;

        /// <summary>
        /// Whether to check the status on the target instead of the user.
        /// </summary>
        public bool AffectTarget;

        /// <summary>
        /// Whether to display the message associated with this event.
        /// </summary>
        public bool Msg;

        /// <inheritdoc/>
        public RevengeEvent() { TargetStatusID = ""; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RevengeEvent"/> class with the specified parameters.
        /// </summary>
        /// <param name="targetStatusID">The status ID containing the attacker reference.</param>
        /// <param name="numerator">The damage multiplier numerator.</param>
        /// <param name="denominator">The damage multiplier denominator.</param>
        /// <param name="affectTarget">Whether to check the target's status.</param>
        /// <param name="msg">Whether to display the boost message.</param>
        public RevengeEvent(string targetStatusID, int numerator, int denominator, bool affectTarget, bool msg)
        {
            TargetStatusID = targetStatusID;
            Numerator = numerator;
            Denominator = denominator;
            AffectTarget = affectTarget;
            Msg = msg;
        }

        /// <inheritdoc/>
        protected RevengeEvent(RevengeEvent other)
        {
            TargetStatusID = other.TargetStatusID;
            Numerator = other.Numerator;
            Denominator = other.Denominator;
            AffectTarget = other.AffectTarget;
            Msg = other.Msg;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new RevengeEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Character target = (AffectTarget ? context.Target : context.User);
            Character origin = (AffectTarget ? context.User : context.Target);
            if (target.Dead)
                yield break;

            BasePowerState basePower = context.Data.SkillStates.GetWithDefault<BasePowerState>();
            if (basePower != null)
            {
                StatusEffect status = target.GetStatusEffect(TargetStatusID);
                if (status != null && status.TargetChar == origin && (status.StatusStates.GetWithDefault<HPState>().HP > 0))
                {
                    if (Msg)
                        DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_DMG_BOOST_REVENGE").ToLocal()));
                    basePower.Power = (basePower.Power * Numerator / Denominator);
                }
            }
            yield break;
        }
    }



    /// <summary>
    /// Event that modifies the damage multiplier if the character is inflicted with a major status condition.
    /// Applies a damage multiplier when any major status is present.
    /// </summary>
    [Serializable]
    public class MajorStatusPowerEvent : BattleEvent
    {
        /// <summary>
        /// Whether to check the status on the target instead of the user.
        /// </summary>
        public bool AffectTarget;

        /// <summary>
        /// The numerator of the damage modifier.
        /// </summary>
        public int Numerator;

        /// <summary>
        /// The denominator of the damage modifier.
        /// </summary>
        public int Denominator;

        /// <inheritdoc/>
        public MajorStatusPowerEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MajorStatusPowerEvent"/> class with the specified parameters.
        /// </summary>
        /// <param name="affectTarget">Whether to check the target's status.</param>
        /// <param name="numerator">The damage multiplier numerator.</param>
        /// <param name="denominator">The damage multiplier denominator.</param>
        public MajorStatusPowerEvent(bool affectTarget, int numerator, int denominator)
        {
            AffectTarget = affectTarget;
            Numerator = numerator;
            Denominator = denominator;
        }

        /// <inheritdoc/>
        protected MajorStatusPowerEvent(MajorStatusPowerEvent other)
        {
            AffectTarget = other.AffectTarget;
            Numerator = other.Numerator;
            Denominator = other.Denominator;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new MajorStatusPowerEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Character target = (AffectTarget ? context.Target : context.User);

            BasePowerState basePower = context.Data.SkillStates.GetWithDefault<BasePowerState>();
            if (basePower != null)
            {
                foreach (StatusEffect status in target.IterateStatusEffects())
                {
                    if (status.StatusStates.Contains<MajorStatusState>())
                    {
                        if (AffectTarget)
                            DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_DMG_BOOST_ANY_STATUS_OTHER").ToLocal()));
                        else
                            DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_DMG_BOOST_ANY_STATUS").ToLocal()));
                        basePower.Power *= Numerator;
                        basePower.Power /= Denominator;
                        break;
                    }
                }
            }
            yield break;
        }
    }

}

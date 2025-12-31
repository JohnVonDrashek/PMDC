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
    // Battle events that modify the hit rate of the attack, including forced misses

    /// <summary>
    /// Event that protects the user from all moves.
    /// </summary>
    [Serializable]
    public class ProtectEvent : BattleEvent
    {
        /// <summary>
        /// OBSOLETE - Use Effects instead.
        /// </summary>
        [NonEdited]
        public List<BattleAnimEvent> Anims;

        /// <summary>
        /// The list of battle events applied when protection is triggered.
        /// </summary>
        public List<BattleEvent> Effects;

        /// <inheritdoc/>
        public ProtectEvent()
        {
            Effects = new List<BattleEvent>();
        }

        /// <summary>
        /// Creates a new ProtectEvent with the specified effect events.
        /// </summary>
        public ProtectEvent(params BattleEvent[] anims)
        {
            Effects = new List<BattleEvent>();
            Effects.AddRange(anims);
        }

        /// <inheritdoc/>
        protected ProtectEvent(ProtectEvent other)
        {
            Effects = new List<BattleEvent>();
            foreach (BattleEvent anim in other.Effects)
                Effects.Add((BattleEvent)anim.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ProtectEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.User != context.Target)
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_PROTECT").ToLocal(), context.Target.GetDisplayName(false)));

                foreach (BattleEvent anim in Effects)
                    yield return CoroutineManager.Instance.StartCoroutine(anim.Apply(owner, ownerChar, context));

                context.AddContextStateMult<AccMult>(false, -1, 1);
            }
            yield break;
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            //TODO: remove on v1.1
            if (Serializer.OldVersion < new Version(0, 7, 15) && Anims != null)
            {
                Effects = new List<BattleEvent>();
                Effects.AddRange(Anims);
            }
        }
    }

    /// <summary>
    /// Event that causes the specified list of moves to always hit while all other moves will miss.
    /// </summary>
    [Serializable]
    public class SemiInvulEvent : BattleEvent
    {
        /// <summary>
        /// The list of moves that will always hit (bypass semi-invulnerability).
        /// </summary>
        [JsonConverter(typeof(SkillArrayConverter))]
        [DataType(1, DataManager.DataType.Skill, false)]
        public string[] ExceptionMoves;

        /// <inheritdoc/>
        public SemiInvulEvent()
        {
            ExceptionMoves = new string[0];
        }

        /// <summary>
        /// Creates a new SemiInvulEvent with the specified exception moves.
        /// </summary>
        public SemiInvulEvent(string[] exceptionMoves)
        {
            ExceptionMoves = exceptionMoves;
        }

        /// <inheritdoc/>
        protected SemiInvulEvent(SemiInvulEvent other)
        {
            ExceptionMoves = new string[other.ExceptionMoves.Length];
            other.ExceptionMoves.CopyTo(ExceptionMoves, 0);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new SemiInvulEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Skill)
            {
                for (int ii = 0; ii < ExceptionMoves.Length; ii++)
                {
                    if (context.Data.ID == ExceptionMoves[ii])
                    {
                        context.Data.HitRate = -1;
                        yield break;
                    }
                }
            }
            context.AddContextStateMult<AccMult>(false, 0, 1);
        }
    }


    /// <summary>
    /// Event that makes the user unable to target the enemy that applied the status.
    /// This event can only be used in statuses.
    /// </summary>
    [Serializable]
    public class CantAttackTargetEvent : BattleEvent
    {
        /// <summary>
        /// Whether to force the user to target only the enemy instead of avoiding them.
        /// </summary>
        public bool Invert;

        /// <summary>
        /// The message displayed in the dungeon log if the condition is met.
        /// </summary>
        [StringKey(0, true)]
        public StringKey Message;

        /// <inheritdoc/>
        public CantAttackTargetEvent() { }

        /// <summary>
        /// Creates a new CantAttackTargetEvent with the specified parameters.
        /// </summary>
        public CantAttackTargetEvent(bool invert, StringKey message)
        {
            Invert = invert;
            Message = message;
        }

        /// <inheritdoc/>
        protected CantAttackTargetEvent(CantAttackTargetEvent other)
        {
            Invert = other.Invert;
            Message = other.Message;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new CantAttackTargetEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Target != null && ((StatusEffect)owner).TargetChar != null && context.Target != context.User)
            {
                if ((((StatusEffect)owner).TargetChar == context.Target) != Invert)
                {
                    if (Message.IsValid())
                        DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.User.GetDisplayName(false), ((StatusEffect)owner).TargetChar.GetDisplayName(false)));
                    context.AddContextStateMult<AccMult>(false, -1, 1);
                }
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that causes the move to miss if the target has the specified status condition.
    /// </summary>
    [Serializable]
    public class EvadeInStatusEvent : BattleEvent
    {
        /// <summary>
        /// The status ID that triggers evasion.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string StatusID;

        /// <inheritdoc/>
        public EvadeInStatusEvent() { StatusID = ""; }

        /// <summary>
        /// Creates a new EvadeInStatusEvent for the specified status.
        /// </summary>
        public EvadeInStatusEvent(string statusID)
        {
            StatusID = statusID;
        }

        /// <inheritdoc/>
        protected EvadeInStatusEvent(EvadeInStatusEvent other)
        {
            StatusID = other.StatusID;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new EvadeInStatusEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Skill && context.Data.ID != DataManager.Instance.DefaultSkill)
            {
                if (context.Target.GetStatusEffect(StatusID) != null && DungeonScene.Instance.GetMatchup(context.User, context.Target) == Alignment.Foe)
                {
                    if (!ZoneManager.Instance.CurrentMap.InRange(context.StrikeStartTile, context.Target.CharLoc, 1) && context.Data.HitRate > -1)
                    {
                        DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_AVOID").ToLocal(), context.Target.GetDisplayName(false), owner.GetDisplayName()));
                        context.AddContextStateMult<AccMult>(false, -1, 1);
                    }
                }
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that causes the move to miss if the user uses their strongest base power move.
    /// </summary>
    [Serializable]
    public class EvadeStrongestEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new EvadeStrongestEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Skill && context.UsageSlot > BattleContext.DEFAULT_ATTACK_SLOT && context.UsageSlot < CharData.MAX_SKILL_SLOTS && DungeonScene.Instance.GetMatchup(context.User, context.Target) == Alignment.Foe)
            {
                int recordSlot = -1;
                int recordPower = -1;
                for (int ii = 0; ii < context.User.Skills.Count; ii++)
                {
                    if (!String.IsNullOrEmpty(context.User.Skills[ii].Element.SkillNum))
                    {
                        SkillData entry = DataManager.Instance.GetSkill(context.User.Skills[ii].Element.SkillNum);

                        int basePower = 0;
                        if (entry.Data.Category == BattleData.SkillCategory.Status)
                            basePower = -1;
                        else
                        {
                            BasePowerState state = entry.Data.SkillStates.GetWithDefault<BasePowerState>();
                            if (state != null)
                                basePower = state.Power;
                        }
                        if (basePower > recordPower)
                        {
                            recordSlot = ii;
                            recordPower = basePower;
                        }
                    }
                }

                if (context.UsageSlot == recordSlot && context.Data.HitRate > -1)
                {
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_AVOID").ToLocal(), ownerChar.GetDisplayName(false), owner.GetDisplayName()));
                    context.AddContextStateMult<AccMult>(false, -1, 1);
                }
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that causes the user's strongest super-effective move to miss.
    /// </summary>
    [Serializable]
    public class EvadeStrongestEffectiveEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new EvadeStrongestEffectiveEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Skill && context.UsageSlot > BattleContext.DEFAULT_ATTACK_SLOT && context.UsageSlot < CharData.MAX_SKILL_SLOTS && DungeonScene.Instance.GetMatchup(context.User, context.Target) == Alignment.Foe)
            {
                int recordSlot = -1;
                int recordPower = -1;
                for (int ii = 0; ii < context.User.Skills.Count; ii++)
                {
                    if (!String.IsNullOrEmpty(context.User.Skills[ii].Element.SkillNum))
                    {
                        SkillData entry = DataManager.Instance.GetSkill(context.User.Skills[ii].Element.SkillNum);

                        int typeMatchup = PreTypeEvent.GetDualEffectiveness(context.User, context.Target, entry.Data);

                        if (typeMatchup > PreTypeEvent.NRM_2)
                        {
                            int basePower = 0;
                            if (entry.Data.Category == BattleData.SkillCategory.Status)
                                basePower = -1;
                            else
                            {
                                BasePowerState state = entry.Data.SkillStates.GetWithDefault<BasePowerState>();
                                if (state != null)
                                    basePower = state.Power;
                            }
                            if (basePower > recordPower)
                            {
                                recordSlot = ii;
                                recordPower = basePower;
                            }
                        }
                    }
                }

                if (context.UsageSlot == recordSlot && context.Data.HitRate > -1)
                {
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_AVOID").ToLocal(), ownerChar.GetDisplayName(false), owner.GetDisplayName()));
                    context.AddContextStateMult<AccMult>(false, -1, 1);
                }
            }
            yield break;
        }
    }


    /// <summary>
    /// UNUSED.
    /// Event that causes the move to miss if the target is not at full HP.
    /// </summary>
    [Serializable]
    public class FullHPNeededEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new FullHPNeededEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Target.HP < context.Target.MaxHP)
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_FULL_HP_REQ").ToLocal(), context.Target.GetDisplayName(false)));
                context.AddContextStateMult<AccMult>(false, -1, 1);
            }
            yield break;
        }
    }


    /// <summary>
    /// Event that causes the user's move to miss if it contains one of the specified SkillStates.
    /// </summary>
    [Serializable]
    public class EvadeMoveStateEvent : BattleEvent
    {
        /// <summary>
        /// The list of SkillState types that trigger evasion.
        /// </summary>
        [StringTypeConstraint(1, typeof(SkillState))]
        public List<FlagType> States;

        /// <summary>
        /// The list of battle VFX events played when evasion is triggered.
        /// </summary>
        public List<BattleAnimEvent> Anims;

        /// <inheritdoc/>
        public EvadeMoveStateEvent()
        {
            States = new List<FlagType>();
            Anims = new List<BattleAnimEvent>();
        }

        /// <summary>
        /// Creates a new EvadeMoveStateEvent for the specified state type and animations.
        /// </summary>
        public EvadeMoveStateEvent(Type state, params BattleAnimEvent[] anims) : this()
        {
            States.Add(new FlagType(state));
            Anims.AddRange(anims);
        }

        /// <inheritdoc/>
        protected EvadeMoveStateEvent(EvadeMoveStateEvent other) : this()
        {
            States.AddRange(other.States);
            foreach (BattleAnimEvent anim in other.Anims)
                Anims.Add((BattleAnimEvent)anim.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new EvadeMoveStateEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            bool hasState = false;
            foreach (FlagType state in States)
            {
                if (context.Data.SkillStates.Contains(state.FullType))
                    hasState = true;
            }
            if (hasState)
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_PROTECT_WITH").ToLocal(), ownerChar.GetDisplayName(false), owner.GetDisplayName()));

                foreach (BattleAnimEvent anim in Anims)
                    yield return CoroutineManager.Instance.StartCoroutine(anim.Apply(owner, ownerChar, context));

                context.AddContextStateMult<AccMult>(false, -1, 1);
            }
            yield break;
        }

    }

    /// <summary>
    /// Event that causes the user's move to miss if the target is more than 1 tile away.
    /// </summary>
    [Serializable]
    public class EvadeDistanceEvent : BattleEvent
    {
        /// <summary>
        /// When true, evades attacks from within 1 tile instead of from a distance.
        /// </summary>
        public bool Inverted;

        /// <inheritdoc/>
        public EvadeDistanceEvent() { }

        /// <summary>
        /// Creates a new EvadeDistanceEvent with the specified inversion setting.
        /// </summary>
        public EvadeDistanceEvent(bool invert) { Inverted = invert; }

        /// <inheritdoc/>
        public EvadeDistanceEvent(EvadeDistanceEvent other)
        {
            Inverted = other.Inverted;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new EvadeDistanceEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Skill && context.Data.ID != DataManager.Instance.DefaultSkill)
            {
                if (ZoneManager.Instance.CurrentMap.InRange(context.StrikeStartTile, context.Target.CharLoc, 1) == Inverted && context.Data.HitRate > -1)
                {
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_AVOID").ToLocal(), context.Target.GetDisplayName(false), owner.GetDisplayName()));
                    context.AddContextStateMult<AccMult>(false, -1, 1);
                }
            }
            yield break;
        }
    }


    /// <summary>
    /// Event that modifies the accuracy rate if the target has the specified status condition.
    /// </summary>
    [Serializable]
    public class EvasiveWhenMissEvent : BattleEvent
    {
        /// <summary>
        /// The status condition that triggers increased evasion.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string StatusID;

        /// <inheritdoc/>
        public EvasiveWhenMissEvent() { StatusID = ""; }

        /// <summary>
        /// Creates a new EvasiveWhenMissEvent for the specified status.
        /// </summary>
        public EvasiveWhenMissEvent(string statusID)
        {
            StatusID = statusID;
        }

        /// <inheritdoc/>
        protected EvasiveWhenMissEvent(EvasiveWhenMissEvent other)
        {
            StatusID = other.StatusID;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new EvasiveWhenMissEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Target.GetStatusEffect(StatusID) != null && DungeonScene.Instance.GetMatchup(context.User, context.Target) == Alignment.Foe)
            {
                context.AddContextStateMult<AccMult>(false, 2, 3);
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that modifies the accuracy rate if the target is below one-third HP.
    /// </summary>
    [Serializable]
    public class EvasiveInPinchEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new EvasiveInPinchEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (DungeonScene.Instance.GetMatchup(context.User, context.Target) == Alignment.Foe)
            {
                if (context.Target.HP < context.Target.MaxHP / 3)
                {
                    context.AddContextStateMult<AccMult>(false, 1, 3);
                }
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that decreases the accuracy rate the further away the distance of the action.
    /// </summary>
    [Serializable]
    public class EvasiveInDistanceEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new EvasiveInDistanceEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Skill && context.Data.ID != DataManager.Instance.DefaultSkill)
            {
                if (DungeonScene.Instance.GetMatchup(context.User, context.Target) == Alignment.Foe)
                {
                    int diff = ZoneManager.Instance.CurrentMap.GetClosestDist8(context.StrikeStartTile, context.Target.CharLoc);
                    if (diff > 1)
                        context.AddContextStateMult<AccMult>(false, 4, 3 + diff);
                }
            }
            yield break;
        }
    }


    /// <summary>
    /// Event that decreases the accuracy rate at point blank range.
    /// </summary>
    [Serializable]
    public class EvasiveCloseUpEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new EvasiveCloseUpEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Skill && context.Data.ID != DataManager.Instance.DefaultSkill)
            {
                if (DungeonScene.Instance.GetMatchup(context.User, context.Target) == Alignment.Foe)
                {
                    if (ZoneManager.Instance.CurrentMap.InRange(context.StrikeStartTile, context.Target.CharLoc, 1))
                        context.AddContextStateMult<AccMult>(false, 1, 2);
                }
            }
            yield break;
        }
    }


    /// <summary>
    /// Event that causes the action to miss given the specified chance.
    /// </summary>
    [Serializable]
    public class CustomHitRateEvent : BattleEvent
    {
        /// <summary>
        /// The numerator of the hit chance fraction.
        /// </summary>
        public int Numerator;

        /// <summary>
        /// The denominator of the hit chance fraction.
        /// </summary>
        public int Denominator;

        /// <inheritdoc/>
        public CustomHitRateEvent() { }

        /// <summary>
        /// Creates a new CustomHitRateEvent with the specified hit chance fraction.
        /// </summary>
        public CustomHitRateEvent(int numerator, int denominator)
        {
            Numerator = numerator;
            Denominator = denominator;
        }

        /// <inheritdoc/>
        protected CustomHitRateEvent(CustomHitRateEvent other) : this()
        {
            Numerator = other.Numerator;
            Denominator = other.Denominator;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new CustomHitRateEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Data.HitRate > -1)
            {
                if (DataManager.Instance.Save.Rand.Next(0, Denominator) < Numerator)
                {
                    context.Data.HitRate = -1;
                }
                else
                {
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_MISS").ToLocal(), context.Target.GetDisplayName(false)));
                    context.AddContextStateMult<AccMult>(false, -1, 1);
                }
            }
            yield break;
        }
    }




    /// <summary>
    /// Event that causes the action to always hit.
    /// </summary>
    [Serializable]
    public class SureShotEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new SureShotEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            context.Data.HitRate = -1;
            yield break;
        }
    }


    /// <summary>
    /// Event that causes multi-strike moves to always hit.
    /// </summary>
    [Serializable]
    public class SkillLinkEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new SkillLinkEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Skill && context.Strikes > 1)
                context.Data.HitRate = -1;
            yield break;
        }
    }


    /// <summary>
    /// Event that causes the user to avoid moves of the specified skill category and alignment.
    /// </summary>
    [Serializable]
    public class EvadeCategoryEvent : BattleEvent
    {
        /// <summary>
        /// The alignments whose attacks can be evaded.
        /// </summary>
        public Alignment EvadeAlignment;

        /// <summary>
        /// The affected skill category.
        /// </summary>
        public BattleData.SkillCategory Category;

        /// <summary>
        /// The list of battle VFX events played when evasion is triggered.
        /// </summary>
        public List<BattleAnimEvent> Anims;

        /// <inheritdoc/>
        public EvadeCategoryEvent()
        {
            Anims = new List<BattleAnimEvent>();
        }

        /// <summary>
        /// Creates a new EvadeCategoryEvent for the specified alignment, category, and animations.
        /// </summary>
        public EvadeCategoryEvent(Alignment alignment, BattleData.SkillCategory category, params BattleAnimEvent[] anims)
        {
            EvadeAlignment = alignment;
            Category = category;

            Anims = new List<BattleAnimEvent>();
            Anims.AddRange(anims);
        }

        /// <inheritdoc/>
        protected EvadeCategoryEvent(EvadeCategoryEvent other)
        {
            EvadeAlignment = other.EvadeAlignment;
            Category = other.Category;
            Anims = new List<BattleAnimEvent>();
            foreach (BattleAnimEvent anim in other.Anims)
                Anims.Add((BattleAnimEvent)anim.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new EvadeCategoryEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (((DungeonScene.Instance.GetMatchup(context.User, context.Target) | EvadeAlignment) == EvadeAlignment) && context.Data.Category == Category)
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_PROTECT_WITH").ToLocal(), ownerChar.GetDisplayName(false), owner.GetDisplayName()));

                foreach (BattleAnimEvent anim in Anims)
                    yield return CoroutineManager.Instance.StartCoroutine(anim.Apply(owner, ownerChar, context));

                context.AddContextStateMult<AccMult>(false, -1, 1);
            }
            yield break;
        }
    }


    /// <summary>
    /// Event that causes the user to avoid damaging moves of friendly targets.
    /// </summary>
    [Serializable]
    public class TelepathyEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new TelepathyEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            BasePowerState basePower = context.Data.SkillStates.GetWithDefault<BasePowerState>();
            if (basePower != null && DungeonScene.Instance.GetMatchup(context.User, context.Target) == Alignment.Friend)
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_AVOID").ToLocal(), ownerChar.GetDisplayName(false), owner.GetDisplayName()));
                context.AddContextStateMult<AccMult>(false, -1, 1);
            }
            yield break;
        }
    }


    /// <summary>
    /// Event that modifies the accuracy rate with a multiplicative modifier.
    /// </summary>
    [Serializable]
    public class MultiplyAccuracyEvent : BattleEvent
    {
        /// <summary>
        /// The numerator of the accuracy multiplier.
        /// </summary>
        public int Numerator;

        /// <summary>
        /// The denominator of the accuracy multiplier.
        /// </summary>
        public int Denominator;

        /// <inheritdoc/>
        public MultiplyAccuracyEvent() { }

        /// <summary>
        /// Creates a new MultiplyAccuracyEvent with the specified multiplier fraction.
        /// </summary>
        public MultiplyAccuracyEvent(int numerator, int denominator)
        {
            Numerator = numerator;
            Denominator = denominator;
        }

        /// <inheritdoc/>
        protected MultiplyAccuracyEvent(MultiplyAccuracyEvent other)
        {
            Numerator = other.Numerator;
            Denominator = other.Denominator;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new MultiplyAccuracyEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            context.AddContextStateMult<AccMult>(false, Numerator, Denominator);
            yield break;
        }
    }


    /// <summary>
    /// Event that causes the move to miss if executed from a distance greater than the specified amount.
    /// </summary>
    [Serializable]
    public class DistantGuardEvent : BattleEvent
    {
        /// <summary>
        /// Attacks from distances greater than this will be blocked.
        /// </summary>
        public int Distance;

        /// <summary>
        /// The list of battle VFX events played when the guard is triggered.
        /// </summary>
        public List<BattleAnimEvent> Anims;

        /// <inheritdoc/>
        public DistantGuardEvent()
        {
            Anims = new List<BattleAnimEvent>();
        }

        /// <summary>
        /// Creates a new DistantGuardEvent with the specified distance and animations.
        /// </summary>
        public DistantGuardEvent(int distance, params BattleAnimEvent[] anims)
        {
            Distance = distance;
            Anims = new List<BattleAnimEvent>();
            Anims.AddRange(anims);
        }

        /// <inheritdoc/>
        protected DistantGuardEvent(DistantGuardEvent other)
        {
            Distance = other.Distance;
            Anims = new List<BattleAnimEvent>();
            foreach (BattleAnimEvent anim in other.Anims)
                Anims.Add((BattleAnimEvent)anim.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new DistantGuardEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Skill && context.Data.ID != DataManager.Instance.DefaultSkill)
            {
                if (!ZoneManager.Instance.CurrentMap.InRange(context.StrikeStartTile, context.Target.CharLoc, Distance))
                {
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_PROTECT_WITH").ToLocal(), context.Target.GetDisplayName(false), owner.GetDisplayName()));

                    foreach (BattleAnimEvent anim in Anims)
                        yield return CoroutineManager.Instance.StartCoroutine(anim.Apply(owner, ownerChar, context));

                    context.AddContextStateMult<AccMult>(false, -1, 1);
                }
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that causes the move to miss if the move's range is greater than 2 tiles.
    /// </summary>
    [Serializable]
    public class LongRangeGuardEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle VFX events played when the guard is triggered.
        /// </summary>
        public List<BattleAnimEvent> Anims;

        /// <inheritdoc/>
        public LongRangeGuardEvent()
        {
            Anims = new List<BattleAnimEvent>();
        }

        /// <summary>
        /// Creates a new LongRangeGuardEvent with the specified animations.
        /// </summary>
        public LongRangeGuardEvent(params BattleAnimEvent[] anims)
        {
            Anims = new List<BattleAnimEvent>();
            Anims.AddRange(anims);
        }

        /// <inheritdoc/>
        protected LongRangeGuardEvent(LongRangeGuardEvent other)
        {
            Anims = new List<BattleAnimEvent>();
            foreach (BattleAnimEvent anim in other.Anims)
                Anims.Add((BattleAnimEvent)anim.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new LongRangeGuardEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.User != context.Target && context.HitboxAction.GetEffectiveDistance() > 2)
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_PROTECT_WITH").ToLocal(), context.Target.GetDisplayName(false), owner.GetDisplayName()));

                foreach (BattleAnimEvent anim in Anims)
                    yield return CoroutineManager.Instance.StartCoroutine(anim.Apply(owner, ownerChar, context));

                context.AddContextStateMult<AccMult>(false, -1, 1);
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that causes the move to miss if the move is wide or an explosion.
    /// </summary>
    [Serializable]
    public class WideGuardEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle VFX events played when the guard is triggered.
        /// </summary>
        public List<BattleAnimEvent> Anims;

        /// <inheritdoc/>
        public WideGuardEvent()
        {
            Anims = new List<BattleAnimEvent>();
        }

        /// <summary>
        /// Creates a new WideGuardEvent with the specified animations.
        /// </summary>
        public WideGuardEvent(params BattleAnimEvent[] anims)
        {
            Anims = new List<BattleAnimEvent>();
            Anims.AddRange(anims);
        }

        /// <inheritdoc/>
        protected WideGuardEvent(WideGuardEvent other)
        {
            Anims = new List<BattleAnimEvent>();
            foreach (BattleAnimEvent anim in other.Anims)
                Anims.Add((BattleAnimEvent)anim.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new WideGuardEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.User != context.Target && (context.HitboxAction.IsWide() || context.Explosion.Range > 0))
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_PROTECT_WITH").ToLocal(), context.Target.GetDisplayName(false), owner.GetDisplayName()));

                foreach (BattleAnimEvent anim in Anims)
                    yield return CoroutineManager.Instance.StartCoroutine(anim.Apply(owner, ownerChar, context));

                context.AddContextStateMult<AccMult>(false, -1, 1);
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that only allows super-effective moves to hit.
    /// </summary>
    [Serializable]
    public class WonderGuardEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle VFX events played when the guard is triggered.
        /// </summary>
        public List<BattleAnimEvent> Anims;

        /// <inheritdoc/>
        public WonderGuardEvent()
        {
            Anims = new List<BattleAnimEvent>();
        }

        /// <summary>
        /// Creates a new WonderGuardEvent with the specified animations.
        /// </summary>
        public WonderGuardEvent(params BattleAnimEvent[] anims)
        {
            Anims = new List<BattleAnimEvent>();
            Anims.AddRange(anims);
        }

        /// <inheritdoc/>
        protected WonderGuardEvent(WonderGuardEvent other)
        {
            Anims = new List<BattleAnimEvent>();
            foreach (BattleAnimEvent anim in other.Anims)
                Anims.Add((BattleAnimEvent)anim.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new WonderGuardEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            //typeless attacks bypass
            if (context.Data.Element == DataManager.Instance.DefaultElement)
                yield break;

            int typeMatchup = PreTypeEvent.GetDualEffectiveness(context.User, context.Target, context.Data);
            if (typeMatchup <= PreTypeEvent.NRM_2 && (context.Data.Category == BattleData.SkillCategory.Physical || context.Data.Category == BattleData.SkillCategory.Magical))
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_PROTECT_WITH").ToLocal(), ownerChar.GetDisplayName(false), owner.GetDisplayName()));

                foreach (BattleAnimEvent anim in Anims)
                    yield return CoroutineManager.Instance.StartCoroutine(anim.Apply(owner, ownerChar, context));

                context.AddContextStateMult<AccMult>(false, -1, 1);
            }
            yield break;
        }
    }


    /// <summary>
    /// Event that causes the battle action to miss if the attacker is not due for a sure hit.
    /// </summary>
    [Serializable]
    public class EvadeIfPossibleEvent : BattleEvent
    {
        /// <inheritdoc/>
        public EvadeIfPossibleEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new EvadeIfPossibleEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (!context.User.MustHitNext)
                context.AddContextStateMult<AccMult>(false, 0, 1);
            yield break;
        }
    }


    /// <summary>
    /// Event that makes the move never miss and always land a critical hit if all moves have the same PP.
    /// </summary>
    [Serializable]
    public class BetterOddsEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new BetterOddsEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Skill && context.UsageSlot > BattleContext.DEFAULT_ATTACK_SLOT && context.UsageSlot < CharData.MAX_SKILL_SLOTS)
            {
                Skill baseMove = context.User.Skills[context.UsageSlot].Element;
                bool allEqual = true;
                for (int ii = 0; ii < context.User.Skills.Count; ii++)
                {
                    if (ii == context.UsageSlot)
                        continue;
                    Skill move = context.User.Skills[ii].Element;
                    if (String.IsNullOrEmpty(move.SkillNum))
                        continue;
                    if (move.Charges != baseMove.Charges + 1)
                    {
                        allEqual = false;
                        break;
                    }

                }
                if (allEqual)
                {
                    context.Data.HitRate = -1;
                    context.AddContextStateInt<CritLevel>(4);
                }
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that makes the move never miss and always land a critical hit if the move is on its last PP.
    /// </summary>
    [Serializable]
    public class FinalOddsEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new FinalOddsEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Skill && context.UsageSlot > BattleContext.DEFAULT_ATTACK_SLOT && context.UsageSlot < CharData.MAX_SKILL_SLOTS)
            {
                Skill move = context.User.Skills[context.UsageSlot].Element;
                if (!String.IsNullOrEmpty(move.SkillNum) && move.Charges == 0)
                {
                    context.Data.HitRate = -1;
                    context.AddContextStateInt<CritLevel>(4);
                }
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that sets the accuracy of the move.
    /// </summary>
    [Serializable]
    public class SetAccuracyEvent : BattleEvent
    {
        /// <summary>
        /// The new accuracy value.
        /// </summary>
        public int Accuracy;

        /// <inheritdoc/>
        public SetAccuracyEvent() { }

        /// <summary>
        /// Creates a new SetAccuracyEvent with the specified accuracy.
        /// </summary>
        public SetAccuracyEvent(int accuracy)
        {
            Accuracy = accuracy;
        }

        /// <inheritdoc/>
        protected SetAccuracyEvent(SetAccuracyEvent other)
        {
            Accuracy = other.Accuracy;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new SetAccuracyEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            context.Data.HitRate = Accuracy;
            yield break;
        }
    }



    /// <summary>
    /// Event that causes the battle action to miss if it is not used at max distance.
    /// </summary>
    [Serializable]
    public class TipOnlyEvent : BattleEvent
    {
        /// <inheritdoc/>
        public TipOnlyEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new TipOnlyEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            //TODO: this breaks in small wrapped maps
            int diff = ZoneManager.Instance.CurrentMap.GetClosestDist8(context.StrikeStartTile, context.Target.CharLoc);
            if (diff != context.HitboxAction.GetEffectiveDistance())
                context.AddContextStateMult<AccMult>(false, 0, 1);
            yield break;
        }
    }

    /// <summary>
    /// Event that causes the battle action to miss if the user is next to the target.
    /// </summary>
    [Serializable]
    public class DistanceOnlyEvent : BattleEvent
    {
        /// <inheritdoc/>
        public DistanceOnlyEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new DistanceOnlyEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (ZoneManager.Instance.CurrentMap.InRange(context.StrikeStartTile, context.Target.CharLoc, 1))
                context.AddContextStateMult<AccMult>(false, 0, 1);
            yield break;
        }
    }


    /// <summary>
    /// Event that causes the character to dodge items that contain the EdibleState item state.
    /// </summary>
    [Serializable]
    public class DodgeFoodEvent : BattleEvent
    {
        /// <summary>
        /// The message displayed in the dungeon log when food is dodged.
        /// </summary>
        public StringKey Message;

        /// <inheritdoc/>
        public DodgeFoodEvent() { }

        /// <summary>
        /// Creates a new DodgeFoodEvent with the specified message.
        /// </summary>
        public DodgeFoodEvent(StringKey message)
        {
            Message = message;
        }

        /// <inheritdoc/>
        protected DodgeFoodEvent(DodgeFoodEvent other)
        {
            Message = other.Message;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new DodgeFoodEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Item || context.ActionType == BattleActionType.Throw)
            {
                ItemData entry = DataManager.Instance.GetItem(context.Item.ID);
                if (entry.ItemStates.Contains<EdibleState>())
                {
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.Target.GetDisplayName(false)));
                    context.AddContextStateMult<AccMult>(false, -1, 1);
                }
            }
            yield break;
        }
    }

}


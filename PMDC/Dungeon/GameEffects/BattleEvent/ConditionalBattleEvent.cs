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
    //Battle events that trigger child battle events under some condition

    /// <summary>
    /// Event that applies child events if the target is not immune to the specified element type.
    /// </summary>
    [Serializable]
    public class CheckImmunityBattleEvent : BattleEvent
    {
        /// <summary>
        /// The element type to check immunity against.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string Element;

        /// <summary>
        /// When true, checks the target's immunity; otherwise checks the user's.
        /// </summary>
        public bool AffectTarget;

        /// <summary>
        /// The list of battle events applied if the character is not immune.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public CheckImmunityBattleEvent() { BaseEvents = new List<BattleEvent>(); Element = ""; }

        /// <inheritdoc/>
        public CheckImmunityBattleEvent(string element, bool affectTarget, params BattleEvent[] effects)
        {
            Element = element;
            AffectTarget = affectTarget;
            BaseEvents = new List<BattleEvent>();
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected CheckImmunityBattleEvent(CheckImmunityBattleEvent other)
        {
            Element = other.Element;
            AffectTarget = other.AffectTarget;
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new CheckImmunityBattleEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Character target = (AffectTarget ? context.Target : context.User);
            if (target.Dead)
                yield break;

            int typeMatchup = PreTypeEvent.GetDualEffectiveness(null, target, Element);
            if (typeMatchup > PreTypeEvent.N_E_2)
            {
                foreach (BattleEvent battleEffect in BaseEvents)
                    yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
            }
        }
    }


    /// <summary>
    /// Event that activates child events only if the battle context does NOT contain any of the specified ContextStates.
    /// </summary>
    [Serializable]
    public class ExceptionContextEvent : BattleEvent
    {
        /// <summary>
        /// The list of ContextState types that prevent child events from firing.
        /// </summary>
        [StringTypeConstraint(1, typeof(ContextState))]
        public List<FlagType> States;

        /// <summary>
        /// When true, checks global context states; otherwise checks local context states.
        /// </summary>
        public bool Global;

        /// <summary>
        /// Battle event that applies if none of the specified states are present.
        /// </summary>
        public BattleEvent BaseEvent;

        /// <inheritdoc/>
        public ExceptionContextEvent() { States = new List<FlagType>(); }

        /// <inheritdoc/>
        public ExceptionContextEvent(Type state, bool global, BattleEvent baseEffect) : this() { States.Add(new FlagType(state)); Global = global; BaseEvent = baseEffect; }

        /// <inheritdoc/>
        protected ExceptionContextEvent(ExceptionContextEvent other) : this()
        {
            States.AddRange(other.States);
            Global = other.Global;
            BaseEvent = (BattleEvent)other.BaseEvent.Clone();
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ExceptionContextEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            bool hasState = false;
            foreach (FlagType state in States)
            {
                if (Global ? context.GlobalContextStates.Contains(state.FullType) : context.ContextStates.Contains(state.FullType))
                    hasState = true;
            }
            if (!hasState)
                yield return CoroutineManager.Instance.StartCoroutine(BaseEvent.Apply(owner, ownerChar, context));
        }

    }


    /// <summary>
    /// Event that activates child events only if the user does not have the Infiltrator context state.
    /// </summary>
    [Serializable]
    public class ExceptInfiltratorEvent : BattleEvent
    {
        /// <summary>
        /// When true, logs a message when Infiltrator blocks the effect.
        /// </summary>
        public bool ExceptionMsg;

        /// <summary>
        /// The list of battle events applied if Infiltrator is not present.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public ExceptInfiltratorEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <inheritdoc/>
        public ExceptInfiltratorEvent(bool msg, params BattleEvent[] effects)
        {
            ExceptionMsg = msg;
            BaseEvents = new List<BattleEvent>();
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected ExceptInfiltratorEvent(ExceptInfiltratorEvent other)
        {
            ExceptionMsg = other.ExceptionMsg;
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ExceptInfiltratorEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Infiltrator state = context.ContextStates.GetWithDefault<Infiltrator>();
            if (state == null)
            {
                foreach (BattleEvent battleEffect in BaseEvents)
                    yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
            }
            else if (ExceptionMsg)
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(state.Msg.ToLocal(), context.User.GetDisplayName(false), owner.GetDisplayName()));
        }
    }

    /// <summary>
    /// Event that activates child events only if the character does NOT have any of the specified CharStates.
    /// </summary>
    [Serializable]
    public class ExceptionCharStateEvent : BattleEvent
    {
        /// <summary>
        /// The list of CharState types that prevent child events from firing.
        /// </summary>
        [StringTypeConstraint(1, typeof(CharState))]
        public List<FlagType> States;

        /// <summary>
        /// When true, checks the target's CharStates; otherwise checks the user's.
        /// </summary>
        public bool CheckTarget;

        /// <summary>
        /// Battle event that applies if none of the specified states are present.
        /// </summary>
        public BattleEvent BaseEvent;

        /// <inheritdoc/>
        public ExceptionCharStateEvent() { States = new List<FlagType>(); }

        /// <inheritdoc/>
        public ExceptionCharStateEvent(Type state, bool checkTarget, BattleEvent baseEffect) : this() { States.Add(new FlagType(state)); CheckTarget = checkTarget; BaseEvent = baseEffect; }

        /// <inheritdoc/>
        protected ExceptionCharStateEvent(ExceptionCharStateEvent other) : this()
        {
            States.AddRange(other.States);
            CheckTarget = other.CheckTarget;
            BaseEvent = (BattleEvent)other.BaseEvent.Clone();
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ExceptionCharStateEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Character target = (CheckTarget ? context.Target : context.User);

            bool hasState = false;
            foreach (FlagType state in States)
            {
                if (target.CharStates.Contains(state.FullType))
                    hasState = true;
            }
            if (!hasState)
                yield return CoroutineManager.Instance.StartCoroutine(BaseEvent.Apply(owner, ownerChar, context));
        }

    }

    /// <summary>
    /// Event that applies child events if the move contains one of the specified SkillStates.
    /// </summary>
    [Serializable]
    public class MoveStateNeededEvent : BattleEvent
    {
        /// <summary>
        /// The list of SkillState types that trigger the child events.
        /// </summary>
        [StringTypeConstraint(1, typeof(SkillState))]
        public List<FlagType> States;

        /// <summary>
        /// The list of battle events applied if any specified state is present.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public MoveStateNeededEvent() { States = new List<FlagType>(); BaseEvents = new List<BattleEvent>(); }

        /// <inheritdoc/>
        public MoveStateNeededEvent(Type state, params BattleEvent[] effects) : this()
        {
            States.Add(new FlagType(state));
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected MoveStateNeededEvent(MoveStateNeededEvent other) : this()
        {
            States.AddRange(other.States);
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new MoveStateNeededEvent(this); }

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
                foreach (BattleEvent battleEffect in BaseEvents)
                    yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
            }
            yield break;
        }
    }


    /// <summary>
    /// Item event that applies child events if the character belongs to the item's FamilyState evolution family.
    /// </summary>
    [Serializable]
    public class FamilyBattleEvent : BattleEvent
    {
        /// <summary>
        /// Battle event that applies if the character is a family member.
        /// </summary>
        public BattleEvent BaseEvent;

        /// <inheritdoc/>
        public FamilyBattleEvent()
        { }

        /// <inheritdoc/>
        public FamilyBattleEvent(BattleEvent baseEvent)
        {
            BaseEvent = baseEvent;
        }

        /// <inheritdoc/>
        protected FamilyBattleEvent(FamilyBattleEvent other)
        {
            BaseEvent = (BattleEvent)other.BaseEvent.Clone();
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new FamilyBattleEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            ItemData entry = DataManager.Instance.GetItem(owner.GetID());
            FamilyState family;
            if (!entry.ItemStates.TryGet<FamilyState>(out family))
                yield break;

            if (family.Members.Contains(ownerChar.BaseForm.Species))
                yield return CoroutineManager.Instance.StartCoroutine(BaseEvent.Apply(owner, ownerChar, context));
        }
    }


    /// <summary>
    /// Event that activates child events if the character's HP is below a threshold (HP less than or equal to MaxHP/Denominator).
    /// </summary>
    [Serializable]
    public class PinchNeededEvent : BattleEvent
    {
        /// <summary>
        /// HP threshold divisor (triggers when HP is at or below MaxHP/Denominator).
        /// </summary>
        public int Denominator;

        /// <summary>
        /// When true, checks the target's HP; otherwise checks the user's.
        /// </summary>
        public bool AffectTarget;

        /// <summary>
        /// The list of battle events applied if the HP condition is met.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public PinchNeededEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <inheritdoc/>
        public PinchNeededEvent(int denominator, params BattleEvent[] effects) : this()
        {
            Denominator = denominator;
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected PinchNeededEvent(PinchNeededEvent other) : this()
        {
            Denominator = other.Denominator;
            AffectTarget = other.AffectTarget;
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new PinchNeededEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Character target = (AffectTarget ? context.Target : context.User);

            if (target.HP <= target.MaxHP / Math.Max(1, Denominator))
            {
                foreach (BattleEvent battleEffect in BaseEvents)
                    yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
            }

            yield break;
        }
    }

    /// <summary>
    /// Event that applies child events only if the owning status does NOT contain any of the specified StatusStates.
    /// </summary>
    [Serializable]
    public class ExceptionStatusEvent : BattleEvent
    {
        /// <summary>
        /// The list of StatusState types that prevent child events from firing.
        /// </summary>
        [StringTypeConstraint(1, typeof(StatusState))]
        public List<FlagType> States;

        /// <summary>
        /// Battle event that applies if none of the specified states are present.
        /// </summary>
        public BattleEvent BaseEvent;

        /// <inheritdoc/>
        public ExceptionStatusEvent() { States = new List<FlagType>(); }

        /// <inheritdoc/>
        public ExceptionStatusEvent(Type state, BattleEvent baseEffect) : this() { States.Add(new FlagType(state)); BaseEvent = baseEffect; }

        /// <inheritdoc/>
        protected ExceptionStatusEvent(ExceptionStatusEvent other) : this()
        {
            States.AddRange(other.States);
            BaseEvent = (BattleEvent)other.BaseEvent.Clone();
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ExceptionStatusEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            bool hasState = false;
            foreach (FlagType state in States)
            {
                if (((StatusEffect)owner).StatusStates.Contains(state.FullType))
                    hasState = true;
            }
            if (!hasState)
                yield return CoroutineManager.Instance.StartCoroutine(BaseEvent.Apply(owner, ownerChar, context));
        }

    }


    /// <summary>
    /// Event that activates child events if the character has one of the specified status effects.
    /// </summary>
    [Serializable]
    public class HasStatusNeededEvent : BattleEvent
    {
        /// <summary>
        /// The list of status IDs that trigger the child events.
        /// </summary>
        [JsonConverter(typeof(StatusListConverter))]
        [DataType(1, DataManager.DataType.Status, false)]
        public List<string> Statuses;

        /// <summary>
        /// When true, checks the target's statuses; otherwise checks the user's.
        /// </summary>
        public bool AffectTarget;

        /// <summary>
        /// The list of battle events applied if any specified status is present.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public HasStatusNeededEvent() { Statuses = new List<string>(); BaseEvents = new List<BattleEvent>(); }

        /// <inheritdoc/>
        public HasStatusNeededEvent(bool affectTarget, string[] statuses, params BattleEvent[] effects) : this()
        {
            AffectTarget = affectTarget;
            foreach (string statusId in statuses)
                Statuses.Add(statusId);
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected HasStatusNeededEvent(HasStatusNeededEvent other) : this()
        {
            AffectTarget = other.AffectTarget;
            foreach (string statusId in other.Statuses)
                Statuses.Add(statusId);
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new HasStatusNeededEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Character target = (AffectTarget ? context.Target : context.User);

            bool hasStatus = false;
            foreach (StatusEffect status in target.IterateStatusEffects())
            {
                if (Statuses.Contains(status.ID))
                {
                    hasStatus = true;
                    break;
                }
            }

            if (hasStatus)
            {
                foreach (BattleEvent battleEffect in BaseEvents)
                    yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
            }

            yield break;
        }
    }

    /// <summary>
    /// Event that applies a random child event if damage was dealt and the AdditionalEffectState chance check passes.
    /// Should be placed in OnHits.
    /// </summary>
    [Serializable]
    public class AdditionalEvent : BattleEvent
    {
        /// <summary>
        /// The list of possible battle events (one is randomly selected if triggered).
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public AdditionalEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <inheritdoc/>
        public AdditionalEvent(params BattleEvent[] effects)
        {
            BaseEvents = new List<BattleEvent>();
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected AdditionalEvent(AdditionalEvent other) : this()
        {
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new AdditionalEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {

            if (context.GetContextStateInt<DamageDealt>(0) > 0)
            {
                if (DataManager.Instance.Save.Rand.Next(100) < context.Data.SkillStates.GetWithDefault<AdditionalEffectState>().EffectChance)
                    yield return CoroutineManager.Instance.StartCoroutine(BaseEvents[DataManager.Instance.Save.Rand.Next(BaseEvents.Count)].Apply(owner, ownerChar, context));
            }
        }
    }

    /// <summary>
    /// Event that applies a random child event if damage was dealt and the AdditionalEffectState chance check passes.
    /// Should be placed in AfterActions.
    /// </summary>
    [Serializable]
    public class AdditionalEndEvent : BattleEvent
    {
        /// <summary>
        /// The list of possible battle events (one is randomly selected if triggered).
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public AdditionalEndEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <inheritdoc/>
        public AdditionalEndEvent(params BattleEvent[] effects)
        {
            BaseEvents = new List<BattleEvent>();
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected AdditionalEndEvent(AdditionalEndEvent other)
            : this()
        {
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new AdditionalEndEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {

            if (context.GetContextStateInt<TotalDamageDealt>(true, 0) > 0)
            {
                if (DataManager.Instance.Save.Rand.Next(100) < context.Data.SkillStates.GetWithDefault<AdditionalEffectState>().EffectChance)
                    yield return CoroutineManager.Instance.StartCoroutine(BaseEvents[DataManager.Instance.Save.Rand.Next(BaseEvents.Count)].Apply(owner, ownerChar, context));
            }
        }
    }

    /// <summary>
    /// Event that applies child events only if the target is dead.
    /// </summary>
    [Serializable]
    public class TargetDeadNeededEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle events applied if the target is dead.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public TargetDeadNeededEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <inheritdoc/>
        public TargetDeadNeededEvent(params BattleEvent[] effects)
        {
            BaseEvents = new List<BattleEvent>();
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected TargetDeadNeededEvent(TargetDeadNeededEvent other)
            : this()
        {
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new TargetDeadNeededEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Target.Dead)
            {
                foreach (BattleEvent battleEffect in BaseEvents)
                    yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
            }
        }
    }

    /// <summary>
    /// Event that applies child events once per knockout made by the user during the action.
    /// </summary>
    [Serializable]
    public class KnockOutNeededEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle events applied for each knockout.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public KnockOutNeededEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <inheritdoc/>
        public KnockOutNeededEvent(params BattleEvent[] effects)
        {
            BaseEvents = new List<BattleEvent>();
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected KnockOutNeededEvent(KnockOutNeededEvent other)
            : this()
        {
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new KnockOutNeededEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            int knockOuts = context.GetContextStateInt<TotalKnockouts>(true, 0);
            for (int ii = 0; ii < knockOuts; ii++)
            {
                foreach (BattleEvent battleEffect in BaseEvents)
                    yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
            }
        }
    }

    /// <summary>
    /// Event that groups multiple battle events into one event, but only applies
    /// if the character uses an item with the EdibleState item state.
    /// </summary>
    [Serializable]
    public class FoodNeededEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle events that will be applied if the condition is met.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public FoodNeededEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <summary>
        /// Creates a new FoodNeededEvent with the specified effects.
        /// </summary>
        /// <param name="effects">The battle events to apply when food is used.</param>
        public FoodNeededEvent(params BattleEvent[] effects)
        {
            BaseEvents = new List<BattleEvent>();
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected FoodNeededEvent(FoodNeededEvent other)
            : this()
        {
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new FoodNeededEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Item || context.ActionType == BattleActionType.Throw)
            {
                ItemData itemData = DataManager.Instance.GetItem(context.Item.ID);
                if (itemData.ItemStates.Contains<EdibleState>())
                {
                    foreach (BattleEvent battleEffect in BaseEvents)
                        yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
                }
            }
        }
    }



    /// <summary>
    /// Event that groups multiple battle events into one event, but only applies if the specified map status is present.
    /// </summary>
    [Serializable]
    public class WeatherNeededEvent : BattleEvent
    {
        /// <summary>
        /// The map status ID that must be present for the events to trigger.
        /// </summary>
        [JsonConverter(typeof(MapStatusConverter))]
        [DataType(0, DataManager.DataType.MapStatus, false)]
        public string WeatherID;

        /// <summary>
        /// The list of battle events that will be applied if the condition is met.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public WeatherNeededEvent() { BaseEvents = new List<BattleEvent>(); WeatherID = ""; }

        /// <summary>
        /// Creates a new WeatherNeededEvent for the specified weather condition.
        /// </summary>
        /// <param name="id">The map status ID to check for.</param>
        /// <param name="effects">The battle events to apply when the weather is active.</param>
        public WeatherNeededEvent(string id, params BattleEvent[] effects)
            : this()
        {
            WeatherID = id;
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected WeatherNeededEvent(WeatherNeededEvent other) : this()
        {
            WeatherID = other.WeatherID;
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new WeatherNeededEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (ZoneManager.Instance.CurrentMap.Status.ContainsKey(WeatherID))
            {
                foreach (BattleEvent battleEffect in BaseEvents)
                    yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
            }
        }
    }

    /// <summary>
    /// Event that groups multiple battle events into one event, but only applies if a critical hit was landed.
    /// </summary>
    [Serializable]
    public class CritNeededEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle events that will be applied if the condition is met.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public CritNeededEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <summary>
        /// Creates a new CritNeededEvent with the specified effects.
        /// </summary>
        /// <param name="effects">The battle events to apply on critical hits.</param>
        public CritNeededEvent(params BattleEvent[] effects)
        {
            BaseEvents = new List<BattleEvent>();
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected CritNeededEvent(CritNeededEvent other)
            : this()
        {
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new CritNeededEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ContextStates.Contains<AttackCrit>())
            {
                foreach (BattleEvent battleEffect in BaseEvents)
                    yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
            }
        }
    }

    /// <summary>
    /// Event that groups multiple battle events into one event, but only applies if the character's type matches the specified type.
    /// </summary>
    [Serializable]
    public class CharElementNeededEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle events that will be applied if the condition is met.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <summary>
        /// The type to check for.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string NeededElement;

        /// <summary>
        /// Whether to run the type check on the user or target.
        /// </summary>
        public bool AffectTarget;

        /// <summary>
        /// If set, the events will only be applied if none of the character's types match the specified type.
        /// </summary>
        public bool Inverted;

        /// <inheritdoc/>
        public CharElementNeededEvent() { BaseEvents = new List<BattleEvent>(); NeededElement = ""; AffectTarget = true; Inverted = false; }

        /// <summary>
        /// Creates a new CharElementNeededEvent with the specified parameters.
        /// </summary>
        /// <param name="element">The element type to check for.</param>
        /// <param name="affectTarget">Whether to check the target's type instead of the user's.</param>
        /// <param name="inverted">Whether to invert the type check.</param>
        /// <param name="effects">The battle events to apply when the condition is met.</param>
        public CharElementNeededEvent(string element, bool affectTarget, bool inverted, params BattleEvent[] effects)
            : this()
        {
            NeededElement = element;
            AffectTarget = affectTarget;
            Inverted = inverted;
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected CharElementNeededEvent(CharElementNeededEvent other)
            : this()
        {
            NeededElement = other.NeededElement;
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new CharElementNeededEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Character character = AffectTarget ? context.Target : context.User;
            if (Inverted ^ character.HasElement(NeededElement)) //if inverted, must not correspond. If not inverted, must correspond
            {
                foreach (BattleEvent battleEffect in BaseEvents)
                    yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
            }
        }
    }

    /// <summary>
    /// Event that groups multiple battle events into one event, but only applies if the move type matches the specified type.
    /// </summary>
    [Serializable]
    public class ElementNeededEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle events that will be applied if the condition is met.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <summary>
        /// The type to check for.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string NeededElement;

        /// <inheritdoc/>
        public ElementNeededEvent() { BaseEvents = new List<BattleEvent>(); NeededElement = ""; }

        /// <summary>
        /// Creates a new ElementNeededEvent for the specified element type.
        /// </summary>
        /// <param name="element">The element type that the move must match.</param>
        /// <param name="effects">The battle events to apply when the element matches.</param>
        public ElementNeededEvent(string element, params BattleEvent[] effects)
            : this()
        {
            NeededElement = element;
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected ElementNeededEvent(ElementNeededEvent other)
            : this()
        {
            NeededElement = other.NeededElement;
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ElementNeededEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Data.Element == NeededElement)
            {
                foreach (BattleEvent battleEffect in BaseEvents)
                    yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
            }
        }
    }

    /// <summary>
    /// Event that groups multiple battle events into one event, but only applies if the action matches the skill category.
    /// </summary>
    [Serializable]
    public class CategoryNeededEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle events that will be applied if the condition is met.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <summary>
        /// The skill category to check for.
        /// </summary>
        public BattleData.SkillCategory NeededCategory;

        /// <inheritdoc/>
        public CategoryNeededEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <summary>
        /// Creates a new CategoryNeededEvent for the specified skill category.
        /// </summary>
        /// <param name="category">The skill category that must match.</param>
        /// <param name="effects">The battle events to apply when the category matches.</param>
        public CategoryNeededEvent(BattleData.SkillCategory category, params BattleEvent[] effects)
            : this()
        {
            NeededCategory = category;
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected CategoryNeededEvent(CategoryNeededEvent other)
            : this()
        {
            NeededCategory = other.NeededCategory;
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new CategoryNeededEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Data.Category == NeededCategory)
            {
                foreach (BattleEvent battleEffect in BaseEvents)
                    yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
            }
        }
    }

    /// <summary>
    /// Event that groups multiple battle events into one event, but only applies if an attacking move is used.
    /// </summary>
    [Serializable]
    public class AttackingMoveNeededEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle events that will be applied if the condition is met.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public AttackingMoveNeededEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <summary>
        /// Creates a new AttackingMoveNeededEvent with the specified effects.
        /// </summary>
        /// <param name="effects">The battle events to apply when an attacking move is used.</param>
        public AttackingMoveNeededEvent(params BattleEvent[] effects)
            : this()
        {
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected AttackingMoveNeededEvent(AttackingMoveNeededEvent other)
            : this()
        {
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new AttackingMoveNeededEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Data.Category == BattleData.SkillCategory.Physical || context.Data.Category == BattleData.SkillCategory.Magical)
            {
                foreach (BattleEvent battleEffect in BaseEvents)
                    yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
            }
        }
    }

    /// <summary>
    /// Event that groups multiple battle events into one event, but only applies on action.
    /// </summary>
    [Serializable]
    public class OnActionEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle events that will be applied if the condition is met.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public OnActionEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <summary>
        /// Creates a new OnActionEvent with the specified effects.
        /// </summary>
        /// <param name="effects">The battle events to apply on action.</param>
        public OnActionEvent(params BattleEvent[] effects)
        {
            BaseEvents = new List<BattleEvent>();
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected OnActionEvent(OnActionEvent other)
            : this()
        {
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new OnActionEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.UsageSlot == BattleContext.FORCED_SLOT)
                yield break;

            foreach (BattleEvent battleEffect in BaseEvents)
                yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
        }
    }

    /// <summary>
    /// Event that groups multiple battle events into one event, but only applies when the character attacks.
    /// </summary>
    [Serializable]
    public class OnAggressionEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle events that will be applied if the condition is met.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public OnAggressionEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <summary>
        /// Creates a new OnAggressionEvent with the specified effects.
        /// </summary>
        /// <param name="effects">The battle events to apply when the character attacks.</param>
        public OnAggressionEvent(params BattleEvent[] effects)
        {
            BaseEvents = new List<BattleEvent>();
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected OnAggressionEvent(OnAggressionEvent other)
            : this()
        {
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new OnAggressionEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.UsageSlot == BattleContext.FORCED_SLOT)
                yield break;
            if (context.ActionType != BattleActionType.Skill)
                yield break;
            foreach (BattleEvent battleEffect in BaseEvents)
                yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
        }
    }

    /// <summary>
    /// Event that groups multiple battle events into one event, but only applies if the character uses a move.
    /// </summary>
    [Serializable]
    public class OnMoveUseEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle events that will be applied if the condition is met.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public OnMoveUseEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <summary>
        /// Creates a new OnMoveUseEvent with the specified effects.
        /// </summary>
        /// <param name="effects">The battle events to apply when a move is used.</param>
        public OnMoveUseEvent(params BattleEvent[] effects)
        {
            BaseEvents = new List<BattleEvent>();
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected OnMoveUseEvent(OnMoveUseEvent other)
            : this()
        {
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new OnMoveUseEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.UsageSlot == BattleContext.FORCED_SLOT)
                yield break;

            if (context.ActionType == BattleActionType.Skill && context.Data.ID != DataManager.Instance.DefaultSkill)
            {
                foreach (BattleEvent battleEffect in BaseEvents)
                    yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
            }
        }
    }

    /// <summary>
    /// Event that groups multiple battle events into one event, but only applies
    /// when the target matches one of the specified alignments.
    /// </summary>
    [Serializable]
    public class TargetNeededEvent : BattleEvent
    {
        /// <summary>
        /// The alignments to check for.
        /// </summary>
        public Alignment Target;

        /// <summary>
        /// The list of battle events that will be applied if the condition is met.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public TargetNeededEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <summary>
        /// Creates a new TargetNeededEvent for the specified target alignment.
        /// </summary>
        /// <param name="target">The alignment that the target must match.</param>
        /// <param name="effects">The battle events to apply when the target matches.</param>
        public TargetNeededEvent(Alignment target, params BattleEvent[] effects)
        {
            Target = target;
            BaseEvents = new List<BattleEvent>();
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected TargetNeededEvent(TargetNeededEvent other)
            : this()
        {
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new TargetNeededEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if ((DungeonScene.Instance.GetMatchup(context.User, context.Target) & Target) != Alignment.None)
            {
                foreach (BattleEvent battleEffect in BaseEvents)
                    yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
            }
        }
    }

    /// <summary>
    /// Event that groups multiple battle events into one event, but only applies when the hitbox action is a SelfAction.
    /// </summary>
    [Serializable]
    public class OnSelfActionEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle events that will be applied if the condition is met.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public OnSelfActionEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <summary>
        /// Creates a new OnSelfActionEvent with the specified effects.
        /// </summary>
        /// <param name="effects">The battle events to apply when the hitbox is a SelfAction.</param>
        public OnSelfActionEvent(params BattleEvent[] effects)
        {
            BaseEvents = new List<BattleEvent>();
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected OnSelfActionEvent(OnSelfActionEvent other)
            : this()
        {
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new OnSelfActionEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.HitboxAction is SelfAction)
            {
                foreach (BattleEvent battleEffect in BaseEvents)
                    yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
            }
        }
    }



    /// <summary>
    /// Event that groups multiple battle events into one event,
    /// but only applies when the hitbox action is an item or throw action that has a berry.
    /// </summary>
    [Serializable]
    public class BerryNeededEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle events that will be applied if the condition is met.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public BerryNeededEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <summary>
        /// Creates a new BerryNeededEvent with the specified effects.
        /// </summary>
        /// <param name="effects">The battle events to apply when a berry is used.</param>
        public BerryNeededEvent(params BattleEvent[] effects)
        {
            BaseEvents = new List<BattleEvent>();
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected BerryNeededEvent(BerryNeededEvent other)
            : this()
        {
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new BerryNeededEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Item || context.ActionType == BattleActionType.Throw)
            {
                ItemData itemData = DataManager.Instance.GetItem(context.Item.ID);
                if (itemData.ItemStates.Contains<BerryState>())
                {
                    foreach (BattleEvent battleEffect in BaseEvents)
                        yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
                }
            }
        }
    }

    /// <summary>
    /// Event that groups multiple battle events into one event, but only applies if a StatusBattleEvent was used
    /// and its status matches one of the specified statuses.
    /// </summary>
    [Serializable]
    public class GiveStatusNeededEvent : BattleEvent
    {
        /// <summary>
        /// The list of statuses to check for.
        /// </summary>
        [JsonConverter(typeof(StatusArrayConverter))]
        [DataType(1, DataManager.DataType.Status, false)]
        public string[] Statuses;

        /// <summary>
        /// The list of battle events that will be applied if the condition is met.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public GiveStatusNeededEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <summary>
        /// Creates a new GiveStatusNeededEvent for the specified statuses.
        /// </summary>
        /// <param name="statuses">The status IDs to check for.</param>
        /// <param name="effects">The battle events to apply when the status matches.</param>
        public GiveStatusNeededEvent(string[] statuses, params BattleEvent[] effects)
        {
            Statuses = statuses;
            BaseEvents = new List<BattleEvent>();
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected GiveStatusNeededEvent(GiveStatusNeededEvent other)
            : this()
        {
            Statuses = new string[other.Statuses.Length];
            Array.Copy(other.Statuses, Statuses, Statuses.Length);
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new GiveStatusNeededEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            bool hasStatus = false;
            foreach (BattleEvent effect in context.Data.OnHits.EnumerateInOrder())
            {
                StatusBattleEvent statusEvent = effect as StatusBattleEvent;
                if (statusEvent != null)
                {
                    foreach (string status in Statuses)
                    {
                        if (statusEvent.StatusID == status)
                        {
                            hasStatus = true;
                            break;
                        }
                    }
                }
            }
            if (hasStatus)
            {
                foreach (BattleEvent battleEffect in BaseEvents)
                    yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
            }
        }
    }

    /// <summary>
    /// Event that groups multiple battle events into one event, but only applies if the hitbox action is a DashAction.
    /// </summary>
    [Serializable]
    public class OnDashActionEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle events that will be applied if the condition is met.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public OnDashActionEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <summary>
        /// Creates a new OnDashActionEvent with the specified effects.
        /// </summary>
        /// <param name="effects">The battle events to apply when the hitbox is a DashAction.</param>
        public OnDashActionEvent(params BattleEvent[] effects)
        {
            BaseEvents = new List<BattleEvent>();
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected OnDashActionEvent(OnDashActionEvent other)
            : this()
        {
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new OnDashActionEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.HitboxAction is DashAction)
            {
                foreach (BattleEvent battleEffect in BaseEvents)
                    yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
            }
        }
    }

    /// <summary>
    /// Event that groups multiple battle events into one event, but only applies if the hitbox action is a MeleeAction or DashAction.
    /// </summary>
    [Serializable]
    public class OnMeleeActionEvent : BattleEvent
    {
        /// <summary>
        /// Whether to check for any other hitbox action instead.
        /// </summary>
        public bool Invert;

        /// <summary>
        /// The list of battle events that will be applied if the condition is met.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public OnMeleeActionEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <summary>
        /// Creates a new OnMeleeActionEvent with the specified parameters.
        /// </summary>
        /// <param name="invert">Whether to invert the condition.</param>
        /// <param name="effects">The battle events to apply when the condition is met.</param>
        public OnMeleeActionEvent(bool invert, params BattleEvent[] effects)
        {
            Invert = invert;
            BaseEvents = new List<BattleEvent>();
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected OnMeleeActionEvent(OnMeleeActionEvent other)
            : this()
        {
            Invert = other.Invert;
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new OnMeleeActionEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if ((context.HitboxAction is AttackAction || context.HitboxAction is DashAction) != Invert)
            {
                foreach (BattleEvent battleEffect in BaseEvents)
                    yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
            }
        }
    }

    /// <summary>
    /// Event that groups multiple battle events into one event, but only applies if the move matches one of the specified moves.
    /// </summary>
    [Serializable]
    public class SpecificSkillNeededEvent : BattleEvent
    {
        /// <summary>
        /// The battle event that applies if the condition is met.
        /// </summary>
        public BattleEvent BaseEvent;

        /// <summary>
        /// The list of moves to check for.
        /// </summary>
        [JsonConverter(typeof(SkillListConverter))]
        [DataType(1, DataManager.DataType.Skill, false)]
        public List<string> AcceptedMoves;

        /// <inheritdoc/>
        public SpecificSkillNeededEvent() { AcceptedMoves = new List<string>(); }

        /// <summary>
        /// Creates a new SpecificSkillNeededEvent for the specified moves.
        /// </summary>
        /// <param name="effect">The battle event to apply when the move matches.</param>
        /// <param name="acceptableMoves">The move IDs that trigger the effect.</param>
        public SpecificSkillNeededEvent(BattleEvent effect, params string[] acceptableMoves)
            : this()
        {
            BaseEvent = effect;
            AcceptedMoves.AddRange(acceptableMoves);
        }

        /// <inheritdoc/>
        protected SpecificSkillNeededEvent(SpecificSkillNeededEvent other)
            : this()
        {
            BaseEvent = (BattleEvent)other.BaseEvent.Clone();
            AcceptedMoves.AddRange(other.AcceptedMoves);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new SpecificSkillNeededEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Skill && AcceptedMoves.Contains(context.Data.ID))
            {
                yield return CoroutineManager.Instance.StartCoroutine(BaseEvent.Apply(owner, ownerChar, context));
            }
        }
    }

    /// <summary>
    /// Event that groups multiple battle events into one event, but only applies if the action is a regular attack.
    /// </summary>
    [Serializable]
    public class RegularAttackNeededEvent : BattleEvent
    {
        /// <summary>
        /// The battle event that applies if the condition is met.
        /// </summary>
        public BattleEvent BaseEvent;

        /// <inheritdoc/>
        public RegularAttackNeededEvent() { }

        /// <summary>
        /// Creates a new RegularAttackNeededEvent with the specified effect.
        /// </summary>
        /// <param name="effect">The battle event to apply when a regular attack is used.</param>
        public RegularAttackNeededEvent(BattleEvent effect)
            : this()
        {
            BaseEvent = effect;
        }

        /// <inheritdoc/>
        protected RegularAttackNeededEvent(RegularAttackNeededEvent other)
            : this()
        {
            BaseEvent = (BattleEvent)other.BaseEvent.Clone();
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new RegularAttackNeededEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Skill && context.UsageSlot == BattleContext.DEFAULT_ATTACK_SLOT)
            {
                yield return CoroutineManager.Instance.StartCoroutine(BaseEvent.Apply(owner, ownerChar, context));
            }
        }
    }


    /// <summary>
    /// Event that groups multiple battle events into one event, but only applies if the used item has the WandState item state.
    /// </summary>
    [Serializable]
    public class WandAttackNeededEvent : BattleEvent
    {
        /// <summary>
        /// The list of item IDs that are excluded from triggering the effect.
        /// </summary>
        [JsonConverter(typeof(ItemListConverter))]
        [DataType(1, DataManager.DataType.Item, false)]
        public List<string> ExceptItems;

        /// <summary>
        /// The battle event that applies if the condition is met.
        /// </summary>
        public BattleEvent BaseEvent;

        /// <inheritdoc/>
        public WandAttackNeededEvent() { ExceptItems = new List<string>(); }

        /// <summary>
        /// Creates a new WandAttackNeededEvent with the specified parameters.
        /// </summary>
        /// <param name="exceptions">The item IDs that are excluded from triggering the effect.</param>
        /// <param name="effect">The battle event to apply when a wand is used.</param>
        public WandAttackNeededEvent(List<string> exceptions, BattleEvent effect)
            : this()
        {
            ExceptItems = exceptions;
            BaseEvent = effect;
        }

        /// <inheritdoc/>
        protected WandAttackNeededEvent(WandAttackNeededEvent other)
            : this()
        {
            BaseEvent = (BattleEvent)other.BaseEvent.Clone();
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new WandAttackNeededEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Item)
            {
                ItemData data = DataManager.Instance.GetItem(context.Item.ID);

                if (data.ItemStates.Contains<WandState>() && !ExceptItems.Contains(context.Item.ID))
                {
                    yield return CoroutineManager.Instance.StartCoroutine(BaseEvent.Apply(owner, ownerChar, context));
                }
            }
        }
    }

    /// <summary>
    /// Event that groups multiple battle events into one event, but only applies if an item was thrown.
    /// </summary>
    [Serializable]
    public class ThrownItemNeededEvent : BattleEvent
    {
        /// <summary>
        /// The battle event that applies if the condition is met.
        /// </summary>
        public BattleEvent BaseEvent;

        /// <inheritdoc/>
        public ThrownItemNeededEvent() { }

        /// <summary>
        /// Creates a new ThrownItemNeededEvent with the specified effect.
        /// </summary>
        /// <param name="effect">The battle event to apply when an item is thrown.</param>
        public ThrownItemNeededEvent(BattleEvent effect)
            : this()
        {
            BaseEvent = effect;
        }

        /// <inheritdoc/>
        protected ThrownItemNeededEvent(ThrownItemNeededEvent other)
            : this()
        {
            BaseEvent = (BattleEvent)other.BaseEvent.Clone();
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ThrownItemNeededEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Throw)
            {
                yield return CoroutineManager.Instance.StartCoroutine(BaseEvent.Apply(owner, ownerChar, context));
            }
        }
    }

    /// <summary>
    /// Event that groups multiple battle events into one event, but only applies if the user lands a hit on an enemy.
    /// </summary>
    [Serializable]
    public class OnHitEvent : BattleEvent
    {
        /// <summary>
        /// The battle events that will be applied if the condition is met.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <summary>
        /// Whether the hit must deal damage to trigger the effect.
        /// </summary>
        public bool RequireDamage;

        /// <summary>
        /// Whether the move must have contact to trigger the effect.
        /// </summary>
        public bool RequireContact;

        /// <summary>
        /// The percent chance (0-100) for the effect to trigger.
        /// </summary>
        public int Chance;

        /// <inheritdoc/>
        public OnHitEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <summary>
        /// Creates a new OnHitEvent with the specified parameters.
        /// </summary>
        /// <param name="requireDamage">Whether the hit must deal damage.</param>
        /// <param name="requireContact">Whether the move must have contact.</param>
        /// <param name="chance">The percent chance for the effect to trigger.</param>
        /// <param name="effects">The battle events to apply on hit.</param>
        public OnHitEvent(bool requireDamage, bool requireContact, int chance, params BattleEvent[] effects)
            : this()
        {
            RequireDamage = requireDamage;
            RequireContact = requireContact;
            Chance = chance;
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected OnHitEvent(OnHitEvent other)
            : this()
        {
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
            RequireDamage = other.RequireDamage;
            RequireContact = other.RequireContact;
            Chance = other.Chance;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new OnHitEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Trap)
                yield break;

            if ((!RequireDamage || context.GetContextStateInt<DamageDealt>(0) > 0)
                && (RequireDamage || DungeonScene.Instance.GetMatchup(context.User, context.Target) == Alignment.Foe)
                && (!RequireContact || context.Data.SkillStates.Contains<ContactState>()))
            {
                if (DataManager.Instance.Save.Rand.Next(100) <= Chance)
                    yield return CoroutineManager.Instance.StartCoroutine(BaseEvents[DataManager.Instance.Save.Rand.Next(BaseEvents.Count)].Apply(owner, ownerChar, context));
            }
        }
    }

    /// <summary>
    /// Event that groups multiple battle events into one event, but only applies if the character lands a hit on anyone.
    /// </summary>
    [Serializable]
    public class OnHitAnyEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle events that will be applied if the condition is met.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <summary>
        /// Whether the hit needs to deal damage.
        /// </summary>
        public bool RequireDamage;

        /// <summary>
        /// The chance for the events to apply (0-100).
        /// </summary>
        public int Chance;

        /// <inheritdoc/>
        public OnHitAnyEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <summary>
        /// Creates a new OnHitAnyEvent with the specified parameters.
        /// </summary>
        /// <param name="requireDamage">Whether the hit must deal damage.</param>
        /// <param name="chance">The percent chance for the effect to trigger.</param>
        /// <param name="effects">The battle events to apply on hit.</param>
        public OnHitAnyEvent(bool requireDamage, int chance, params BattleEvent[] effects)
            : this()
        {
            RequireDamage = requireDamage;
            Chance = chance;
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected OnHitAnyEvent(OnHitAnyEvent other)
            : this()
        {
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
            RequireDamage = other.RequireDamage;
            Chance = other.Chance;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new OnHitAnyEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.GetContextStateInt<AttackHitTotal>(true, 0) > 0
                && (!RequireDamage || context.GetContextStateInt<TotalDamageDealt>(true, 0) > 0))
            {
                if (DataManager.Instance.Save.Rand.Next(100) <= Chance)
                    yield return CoroutineManager.Instance.StartCoroutine(BaseEvents[DataManager.Instance.Save.Rand.Next(BaseEvents.Count)].Apply(owner, ownerChar, context));
            }
        }
    }

    /// <summary>
    /// Event that groups multiple battle events into one event, but only applies if the character is hit.
    /// </summary>
    [Serializable]
    public class HitCounterEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle events that will be applied if the condition is met.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <summary>
        /// The alignments that can be affected.
        /// </summary>
        public Alignment Targets;

        /// <summary>
        /// Whether the hit needs to deal damage.
        /// </summary>
        public bool RequireDamage;

        /// <summary>
        /// Whether the move needs to contain the ContactState skill state.
        /// </summary>
        public bool RequireContact;

        /// <summary>
        /// Whether the target must survive the hit.
        /// </summary>
        public bool RequireSurvive;

        /// <summary>
        /// The chance for the events to apply (0-100).
        /// </summary>
        public int Chance;

        /// <inheritdoc/>
        public HitCounterEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <summary>
        /// Creates a new HitCounterEvent with default damage and contact requirements.
        /// </summary>
        /// <param name="targets">The alignments that can trigger the counter.</param>
        /// <param name="chance">The percent chance for the effect to trigger.</param>
        /// <param name="effects">The battle events to apply on counter.</param>
        public HitCounterEvent(Alignment targets, int chance, params BattleEvent[] effects)
            : this(targets, true, true, false, chance, effects)
        { }

        /// <summary>
        /// Creates a new HitCounterEvent with the specified parameters.
        /// </summary>
        /// <param name="targets">The alignments that can trigger the counter.</param>
        /// <param name="requireDamage">Whether the hit must deal damage.</param>
        /// <param name="requireContact">Whether the move must have contact.</param>
        /// <param name="requireSurvive">Whether the target must survive the hit.</param>
        /// <param name="chance">The percent chance for the effect to trigger.</param>
        /// <param name="effects">The battle events to apply on counter.</param>
        public HitCounterEvent(Alignment targets, bool requireDamage, bool requireContact, bool requireSurvive, int chance, params BattleEvent[] effects)
            : this()
        {
            Targets = targets;
            RequireDamage = requireDamage;
            RequireContact = requireContact;
            RequireSurvive = requireSurvive;
            Chance = chance;
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected HitCounterEvent(HitCounterEvent other)
            : this()
        {
            Targets = other.Targets;
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
            RequireDamage = other.RequireDamage;
            RequireContact = other.RequireContact;
            RequireSurvive = other.RequireSurvive;
            Chance = other.Chance;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new HitCounterEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType != BattleActionType.Skill)
                yield break;

            if ((DungeonScene.Instance.GetMatchup(context.Target, context.User) & Targets) != Alignment.None
                && (!RequireDamage || context.GetContextStateInt<DamageDealt>(0) > 0)
                && (!RequireContact || context.Data.SkillStates.Contains<ContactState>())
                && (!RequireSurvive || !context.Target.Dead))
            {
                if (DataManager.Instance.Save.Rand.Next(100) <= Chance)
                    yield return CoroutineManager.Instance.StartCoroutine(BaseEvents[DataManager.Instance.Save.Rand.Next(BaseEvents.Count)].Apply(owner, ownerChar, context));
            }
        }
    }

}


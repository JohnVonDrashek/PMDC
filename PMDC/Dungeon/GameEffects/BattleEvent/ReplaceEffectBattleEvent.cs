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
    /// <summary>
    /// Battle events that replace the effect of the battle data with different or new data.
    /// These events modify hitbox actions, explosions, battle data, or entire skill effects based on various conditions.
    /// </summary>

    /// <summary>
    /// Event that uses a different battle action if the character is a certain type.
    /// Allows type-specific variants of the same move with different hitbox, explosion, and effects.
    /// </summary>
    [Serializable]
    public class ElementDifferentUseEvent : BattleEvent
    {
        /// <summary>
        /// The type required for this alternate battle action to activate.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string Element;

        /// <summary>
        /// Data on the hitbox of the attack. Controls range and targeting.
        /// </summary>
        public CombatAction HitboxAction;

        /// <summary>
        /// Optional data to specify a splash effect on the tiles hit.
        /// </summary>
        public ExplosionData Explosion;

        /// <summary>
        /// Events that occur with this skill.
        /// Before it's used, when it hits, after it's used, etc.
        /// </summary>
        public BattleData NewData;

        /// <inheritdoc/>
        public ElementDifferentUseEvent() { Element = ""; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementDifferentUseEvent"/> class with specified parameters.
        /// </summary>
        /// <param name="element">The element type that triggers the alternate action.</param>
        /// <param name="action">The hitbox action to use when triggered.</param>
        /// <param name="explosion">The explosion data to use when triggered.</param>
        /// <param name="moveData">The battle data to use when triggered.</param>
        public ElementDifferentUseEvent(string element, CombatAction action, ExplosionData explosion, BattleData moveData)
        {
            Element = element;
            HitboxAction = action;
            Explosion = explosion;
            NewData = moveData;
        }

        /// <inheritdoc/>
        protected ElementDifferentUseEvent(ElementDifferentUseEvent other)
            : this()
        {
            Element = other.Element;
            HitboxAction = other.HitboxAction;
            Explosion = other.Explosion;
            NewData = new BattleData(other.NewData);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ElementDifferentUseEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            //different effects for element
            if (context.User.HasElement(Element))
            {
                //change hitboxaction
                context.HitboxAction = HitboxAction.Clone();

                //change explosion
                context.Explosion = new ExplosionData(Explosion);

                //change move effects
                string id = context.Data.ID;
                DataManager.DataType dataType = context.Data.DataType;
                context.Data = new BattleData(NewData);
                context.Data.ID = id;
                context.Data.DataType = dataType;
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that uses different battle data if the target has a specific alignment (ally, enemy, self).
    /// Allows moves to have different effects on friends versus foes.
    /// </summary>
    [Serializable]
    public class AlignmentDifferentEvent : BattleEvent
    {
        /// <summary>
        /// The alignment(s) required for the alternate battle data to trigger.
        /// </summary>
        public Alignment Alignments;

        /// <summary>
        /// Events that occur with this skill when the alignment condition is met.
        /// Before it's used, when it hits, after it's used, etc.
        /// </summary>
        public BattleData NewData;

        /// <inheritdoc/>
        public AlignmentDifferentEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AlignmentDifferentEvent"/> class with specified parameters.
        /// </summary>
        /// <param name="alignments">The alignments that trigger the alternate data.</param>
        /// <param name="moveData">The battle data to use when alignment matches.</param>
        public AlignmentDifferentEvent(Alignment alignments, BattleData moveData)
        {
            Alignments = alignments;
            NewData = moveData;
        }

        /// <inheritdoc/>
        protected AlignmentDifferentEvent(AlignmentDifferentEvent other)
        {
            Alignments = other.Alignments;
            NewData = new BattleData(other.NewData);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new AlignmentDifferentEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            //different effects for allies
            if ((DungeonScene.Instance.GetMatchup(context.User, context.Target) & Alignments) != Alignment.None)
            {
                string id = context.Data.ID;
                DataManager.DataType dataType = context.Data.DataType;
                context.Data = new BattleData(NewData);
                context.Data.ID = id;
                context.Data.DataType = dataType;
            }
            yield break;
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            //TODO: remove on v1.1
            if (Serializer.OldVersion < new Version(0, 7, 21) && Alignments == Alignment.None)
            {
                Alignments = Alignment.Self | Alignment.Friend;
            }
        }
    }


    /// <summary>
    /// Event that checks whether a thrown item can be caught and changes the battle data accordingly.
    /// When caught, the item is equipped instead of dealing damage or triggering its normal effect.
    /// </summary>
    [Serializable]
    public class CatchableEvent : BattleEvent
    {
        /// <summary>
        /// Events that occur when the item is caught.
        /// Before it's used, when it hits, after it's used, etc.
        /// </summary>
        public BattleData NewData;

        /// <inheritdoc/>
        public CatchableEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CatchableEvent"/> class with specified catch data.
        /// </summary>
        /// <param name="moveData">The battle data to use when the item is caught.</param>
        public CatchableEvent(BattleData moveData)
        {
            NewData = moveData;
        }

        /// <inheritdoc/>
        protected CatchableEvent(CatchableEvent other)
        {
            NewData = new BattleData(other.NewData);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new CatchableEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            //can't catch pierce
            if (context.HitboxAction is LinearAction && !((LinearAction)context.HitboxAction).StopAtHit)
                yield break;

            //can't catch when holding
            if (!String.IsNullOrEmpty(context.Target.EquippedItem.ID))
                yield break;

            //can't catch when inv full
            if (context.Target.MemberTeam is ExplorerTeam && ((ExplorerTeam)context.Target.MemberTeam).GetInvCount() >= ((ExplorerTeam)context.Target.MemberTeam).GetMaxInvSlots(ZoneManager.Instance.CurrentZone))
                yield break;

            if (context.Target.MemberTeam is MonsterTeam)
            {
                //can't catch if it's a wild team, and it's a use-item
                ItemData entry = DataManager.Instance.GetItem(context.Item.ID);
                //can't catch if it's a wild team, and it's an edible or ammo
                if (entry.ItemStates.Contains<EdibleState>() || entry.ItemStates.Contains<AmmoState>())
                    yield break;
            }

            context.ContextStates.Set(new ItemCaught());

            string id = context.Data.ID;
            DataManager.DataType dataType = context.Data.DataType;
            context.Data = new BattleData(NewData);
            context.Data.ID = id;
            context.Data.DataType = dataType;
        }
    }

    /// <summary>
    /// Event that replaces the current hitbox action with a different one.
    /// </summary>
    [Serializable]
    public class ChangeActionEvent : BattleEvent
    {
        /// <summary>
        /// Data on the hitbox of the attack. Controls range and targeting.
        /// </summary>
        public CombatAction NewAction;

        /// <inheritdoc/>
        public ChangeActionEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeActionEvent"/> class with the specified action.
        /// </summary>
        /// <param name="newAction">The new hitbox action to use.</param>
        public ChangeActionEvent(CombatAction newAction)
        {
            NewAction = newAction;
        }

        /// <inheritdoc/>
        protected ChangeActionEvent(ChangeActionEvent other)
            : this()
        {
            NewAction = other.NewAction.Clone();
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ChangeActionEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            //change hitboxaction
            context.HitboxAction = NewAction.Clone();
            yield break;
        }
    }

    /// <summary>
    /// Event that replaces the current battle data with different data.
    /// </summary>
    [Serializable]
    public class ChangeDataEvent : BattleEvent
    {
        /// <summary>
        /// Events that occur with this skill.
        /// Before it's used, when it hits, after it's used, etc.
        /// </summary>
        public BattleData NewAction;

        /// <inheritdoc/>
        public ChangeDataEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeDataEvent"/> class with specified data.
        /// </summary>
        /// <param name="newAction">The new battle data to use.</param>
        public ChangeDataEvent(BattleData newAction)
        {
            NewAction = newAction;
        }

        /// <inheritdoc/>
        protected ChangeDataEvent(ChangeDataEvent other)
            : this()
        {
            NewAction = new BattleData(other.NewAction);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ChangeDataEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            //change data
            context.Data = new BattleData(NewAction);
            yield break;
        }
    }

    /// <summary>
    /// Event that replaces the current explosion data with different data.
    /// </summary>
    [Serializable]
    public class ChangeExplosionEvent : BattleEvent
    {
        /// <summary>
        /// Optional data to specify a splash effect on the tiles hit.
        /// </summary>
        public ExplosionData NewAction;

        /// <inheritdoc/>
        public ChangeExplosionEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeExplosionEvent"/> class with specified data.
        /// </summary>
        /// <param name="newAction">The new explosion data to use.</param>
        public ChangeExplosionEvent(ExplosionData newAction)
        {
            NewAction = newAction;
        }

        /// <inheritdoc/>
        protected ChangeExplosionEvent(ChangeExplosionEvent other)
            : this()
        {
            NewAction = new ExplosionData(other.NewAction);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ChangeExplosionEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            //change data
            context.Explosion = new ExplosionData(NewAction);
            yield break;
        }
    }



    /// <summary>
    /// Event that passes the effect of berries to nearby allies.
    /// When a berry item is used, it affects allies in a 1-tile radius instead of just the user.
    /// </summary>
    [Serializable]
    public class BerryAoEEvent : BattleEvent
    {

        /// <summary>
        /// The message displayed in the dungeon log when the effect activates.
        /// </summary>
        public StringKey Msg;

        /// <summary>
        /// The particle VFX that plays when the effect spreads.
        /// </summary>
        public FiniteEmitter Emitter;

        /// <summary>
        /// The sound effect that plays when the effect spreads.
        /// </summary>
        [Sound(0)]
        public string Sound;


        /// <inheritdoc/>
        public BerryAoEEvent() { Emitter = new EmptyFiniteEmitter(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="BerryAoEEvent"/> class with specified parameters.
        /// </summary>
        /// <param name="msg">The message to display.</param>
        /// <param name="emitter">The particle emitter for the visual effect.</param>
        /// <param name="sound">The sound effect to play.</param>
        public BerryAoEEvent(StringKey msg, FiniteEmitter emitter, string sound)
            : this()
        {
            Msg = msg;
            Emitter = emitter;
            Sound = sound;
        }

        /// <inheritdoc/>
        protected BerryAoEEvent(BerryAoEEvent other)
            : this()
        {
            Emitter = (FiniteEmitter)other.Emitter.Clone();
            Sound = other.Sound;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new BerryAoEEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Item)
            {
                ItemData itemData = DataManager.Instance.GetItem(context.Item.ID);
                if (itemData.ItemStates.Contains<BerryState>())
                {
                    AreaAction newAction = new AreaAction();
                    newAction.TargetAlignments = (Alignment.Self | Alignment.Friend);
                    newAction.Range = 1;
                    newAction.ActionFX.Emitter = Emitter;
                    newAction.Speed = 10;
                    newAction.ActionFX.Sound = Sound;
                    newAction.ActionFX.Delay = 30;
                    context.HitboxAction = newAction;
                    context.Explosion.TargetAlignments = (Alignment.Self | Alignment.Friend);

                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(Msg.ToLocal(), ownerChar.GetDisplayName(false), owner.GetDisplayName()));
                }
            }
            yield break;
        }
    }


    /// <summary>
    /// Event that uses different skill data depending on the stack number of a status.
    /// Each stack level can have different hitbox, explosion, and battle data.
    /// </summary>
    [Serializable]
    public class StatusStackDifferentEvent : BattleEvent
    {
        /// <summary>
        /// The status condition to track for stack count.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string StatusID;

        /// <summary>
        /// The message displayed in the dungeon log if the character doesn't have this status or the stack amount does not map to a skill data.
        /// </summary>
        public StringKey FailMsg;

        /// <summary>
        /// The stack amount mapped to a tuple of (CombatAction, ExplosionData, BattleData).
        /// </summary>
        public Dictionary<int, Tuple<CombatAction, ExplosionData, BattleData>> StackPair;

        /// <inheritdoc/>
        public StatusStackDifferentEvent() { StackPair = new Dictionary<int, Tuple<CombatAction, ExplosionData, BattleData>>(); StatusID = ""; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusStackDifferentEvent"/> class with specified parameters.
        /// </summary>
        /// <param name="statusID">The status ID to check.</param>
        /// <param name="failMsg">The message to display on failure.</param>
        /// <param name="stack">Dictionary mapping stack values to battle configurations.</param>
        public StatusStackDifferentEvent(string statusID, StringKey failMsg, Dictionary<int, Tuple<CombatAction, ExplosionData, BattleData>> stack)
        {
            StatusID = statusID;
            FailMsg = failMsg;
            StackPair = stack;
        }

        /// <inheritdoc/>
        protected StatusStackDifferentEvent(StatusStackDifferentEvent other)
            : this()
        {
            StatusID = other.StatusID;
            FailMsg = other.FailMsg;
            foreach (int stack in other.StackPair.Keys)
                StackPair.Add(stack, new Tuple<CombatAction, ExplosionData, BattleData>(other.StackPair[stack].Item1.Clone(), new ExplosionData(other.StackPair[stack].Item2), new BattleData(other.StackPair[stack].Item3)));
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new StatusStackDifferentEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            StatusEffect status = context.User.GetStatusEffect(StatusID);
            if (status == null)
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(FailMsg.ToLocal(), context.User.GetDisplayName(false)));
                yield break;
            }

            StackState stack = status.StatusStates.GetWithDefault<StackState>();
            if (StackPair.ContainsKey(stack.Stack))
            {
                //change hitboxaction
                context.HitboxAction = StackPair[stack.Stack].Item1.Clone();

                //change explosion
                context.Explosion = new ExplosionData(StackPair[stack.Stack].Item2);

                //change move effects
                string id = context.Data.ID;
                DataManager.DataType dataType = context.Data.DataType;
                context.Data = new BattleData(StackPair[stack.Stack].Item3);
                context.Data.ID = id;
                context.Data.DataType = dataType;
            }
            else
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(FailMsg.ToLocal(), context.User.GetDisplayName(false)));
        }
    }


    /// <summary>
    /// Event that uses different battle data depending on the current map status (weather).
    /// </summary>
    [Serializable]
    public class WeatherDifferentEvent : BattleEvent
    {
        /// <summary>
        /// The map status ID mapped to the battle data to use when that status is active.
        /// </summary>
        [JsonConverter(typeof(MapStatusBattleDataDictConverter))]
        public Dictionary<string, BattleData> WeatherPair;

        /// <inheritdoc/>
        public WeatherDifferentEvent() { WeatherPair = new Dictionary<string, BattleData>(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeatherDifferentEvent"/> class with weather-data pairs.
        /// </summary>
        /// <param name="weather">Dictionary mapping map status IDs to battle data.</param>
        public WeatherDifferentEvent(Dictionary<string, BattleData> weather)
        {
            WeatherPair = weather;
        }

        /// <inheritdoc/>
        protected WeatherDifferentEvent(WeatherDifferentEvent other)
            : this()
        {
            foreach (string weather in other.WeatherPair.Keys)
                WeatherPair.Add(weather, new BattleData(other.WeatherPair[weather]));
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new WeatherDifferentEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            foreach (string weather in WeatherPair.Keys)
            {
                if (ZoneManager.Instance.CurrentMap.Status.ContainsKey(weather))
                {
                    string id = context.Data.ID;
                    DataManager.DataType dataType = context.Data.DataType;
                    context.Data = new BattleData(WeatherPair[weather]);
                    context.Data.ID = id;
                    context.Data.DataType = dataType;
                    break;
                }
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that activates if the character is hit by a super-effective move.
    /// The move's damage is replaced with absorption effects.
    /// </summary>
    [Serializable]
    public class AbsorbWeaknessEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle events applied when a super-effective move is absorbed.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <summary>
        /// The particle VFX that plays when absorption occurs.
        /// </summary>
        public FiniteEmitter Emitter;

        /// <summary>
        /// The sound effect that plays when absorption occurs.
        /// </summary>
        [Sound(0)]
        public string Sound;

        /// <inheritdoc/>
        public AbsorbWeaknessEvent() { BaseEvents = new List<BattleEvent>(); Emitter = new EmptyFiniteEmitter(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbsorbWeaknessEvent"/> class with specified parameters.
        /// </summary>
        /// <param name="emitter">The particle emitter for the absorption effect.</param>
        /// <param name="sound">The sound effect to play.</param>
        /// <param name="effects">The battle events to apply on absorption.</param>
        public AbsorbWeaknessEvent(FiniteEmitter emitter, string sound, params BattleEvent[] effects)
            : this()
        {
            foreach (BattleEvent battleEffect in effects)
                BaseEvents.Add(battleEffect);
            Emitter = emitter;
            Sound = sound;
        }

        /// <inheritdoc/>
        protected AbsorbWeaknessEvent(AbsorbWeaknessEvent other)
            : this()
        {
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
            Emitter = (FiniteEmitter)other.Emitter.Clone();
            Sound = other.Sound;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new AbsorbWeaknessEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            int typeMatchup = PreTypeEvent.GetDualEffectiveness(context.User, context.Target, context.Data);
            typeMatchup -= PreTypeEvent.NRM_2;
            if (typeMatchup > 0 && context.User != context.Target)
            {
                string id = context.Data.ID;
                DataManager.DataType dataType = context.Data.DataType;
                BattleData newData = new BattleData();
                newData.Element = context.Data.Element;
                newData.Category = context.Data.Category;
                newData.HitRate = context.Data.HitRate;
                foreach (SkillState state in context.Data.SkillStates)
                    newData.SkillStates.Set(state.Clone<SkillState>());
                //add the absorption effects
                //newData.OnHits.Add(new BattleLogBattleEvent(new StringKey(new StringKey("MSG_ABSORB").ToLocal()), false, true));
                newData.OnHits.Add(0, new BattleAnimEvent((FiniteEmitter)Emitter.Clone(), Sound, true, 10));
                foreach (BattleEvent battleEffect in BaseEvents)
                    newData.OnHits.Add(0, (BattleEvent)battleEffect.Clone());

                foreach (BattleFX fx in context.Data.IntroFX)
                    newData.IntroFX.Add(new BattleFX(fx));
                newData.HitFX = new BattleFX(context.Data.HitFX);
                context.Data = newData;
                context.Data.ID = id;
                context.Data.DataType = dataType;
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that activates if the character is hit by a specific element type.
    /// The move's damage is replaced with absorption effects (e.g., healing, stat boosts).
    /// </summary>
    [Serializable]
    public class AbsorbElementEvent : BattleEvent
    {
        /// <summary>
        /// The type to absorb.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string AbsorbElement;


        /// <summary>
        /// Whether to only trigger once per attack (prevents multi-hit moves from triggering multiple absorptions).
        /// </summary>
        public bool SingleDraw;

        /// <summary>
        /// Whether to display a message when absorption occurs.
        /// </summary>
        public bool GiveMsg;

        /// <summary>
        /// Battle events that occur when hit by the absorbed element type.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <summary>
        /// The particle VFX that plays when absorption occurs.
        /// </summary>
        public FiniteEmitter Emitter;

        /// <summary>
        /// The sound effect that plays when absorption occurs.
        /// </summary>
        [Sound(0)]
        public string Sound;

        /// <inheritdoc/>
        public AbsorbElementEvent() { BaseEvents = new List<BattleEvent>(); Emitter = new EmptyFiniteEmitter(); AbsorbElement = ""; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbsorbElementEvent"/> class with specified element and effects.
        /// </summary>
        /// <param name="element">The element to absorb.</param>
        /// <param name="effects">The battle events to apply on absorption.</param>
        public AbsorbElementEvent(string element, params BattleEvent[] effects)
            : this(element, false, effects) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbsorbElementEvent"/> class with single draw option.
        /// </summary>
        /// <param name="element">The element to absorb.</param>
        /// <param name="singleDraw">Whether to only trigger once per attack.</param>
        /// <param name="effects">The battle events to apply on absorption.</param>
        public AbsorbElementEvent(string element, bool singleDraw, params BattleEvent[] effects)
            : this(element, singleDraw, false, new EmptyFiniteEmitter(), "", effects) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbsorbElementEvent"/> class with full parameters.
        /// </summary>
        /// <param name="element">The element to absorb.</param>
        /// <param name="singleDraw">Whether to only trigger once per attack.</param>
        /// <param name="giveMsg">Whether to display an absorption message.</param>
        /// <param name="emitter">The particle emitter for the absorption effect.</param>
        /// <param name="sound">The sound effect to play.</param>
        /// <param name="effects">The battle events to apply on absorption.</param>
        public AbsorbElementEvent(string element, bool singleDraw, bool giveMsg, FiniteEmitter emitter, string sound, params BattleEvent[] effects)
            : this()
        {
            AbsorbElement = element;
            SingleDraw = singleDraw;
            GiveMsg = giveMsg;
            foreach (BattleEvent battleEffect in effects)
                BaseEvents.Add(battleEffect);
            Emitter = emitter;
            Sound = sound;
        }

        /// <inheritdoc/>
        protected AbsorbElementEvent(AbsorbElementEvent other) : this()
        {
            AbsorbElement = other.AbsorbElement;
            SingleDraw = other.SingleDraw;
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
            Emitter = (FiniteEmitter)other.Emitter.Clone();
            Sound = other.Sound;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new AbsorbElementEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Data.Element == AbsorbElement && context.User != context.Target)
            {
                string id = context.Data.ID;
                DataManager.DataType dataType = context.Data.DataType;
                BattleData newData = new BattleData();
                newData.Element = context.Data.Element;
                newData.Category = context.Data.Category;
                newData.HitRate = context.Data.HitRate;
                foreach (SkillState state in context.Data.SkillStates)
                    newData.SkillStates.Set(state.Clone<SkillState>());
                //add the absorption effects
                if (!SingleDraw || !context.GlobalContextStates.Contains<SingleDrawAbsorb>())
                {
                    if (GiveMsg)
                    {
                        newData.OnHits.Add(0, new FormatLogLocalEvent(Text.FormatGrammar(new StringKey("MSG_ABSORB").ToLocal(), ownerChar.GetDisplayName(false), owner.GetDisplayName()), false));
                        newData.OnHits.Add(0, new BattleAnimEvent((FiniteEmitter)Emitter.Clone(), Sound, true, 10));
                    }
                    foreach (BattleEvent battleEffect in BaseEvents)
                        newData.OnHits.Add(0, (BattleEvent)battleEffect.Clone());
                }

                foreach (BattleFX fx in context.Data.IntroFX)
                    newData.IntroFX.Add(new BattleFX(fx));
                newData.HitFX = new BattleFX(context.Data.HitFX);
                context.Data = newData;
                context.Data.ID = id;
                context.Data.DataType = dataType;
            }
            yield break;
        }
    }


    /// <summary>
    /// Event that replaces the damage formula with a different battle event.
    /// Allows custom damage calculations or effects to replace the standard formula.
    /// </summary>
    [Serializable]
    public class SetDamageEvent : BattleEvent
    {
        /// <summary>
        /// The battle event to use instead of the normal damage formula.
        /// </summary>
        public BattleEvent BaseEvent;

        /// <summary>
        /// The list of battle VFXs to play when the replacement occurs.
        /// </summary>
        public List<BattleAnimEvent> Anims;

        /// <inheritdoc/>
        public SetDamageEvent() { Anims = new List<BattleAnimEvent>(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="SetDamageEvent"/> class with specified parameters.
        /// </summary>
        /// <param name="battleEffect">The battle event to replace the damage formula.</param>
        /// <param name="anims">Optional animation events to play.</param>
        public SetDamageEvent(BattleEvent battleEffect, params BattleAnimEvent[] anims)
            : this()
        {
            BaseEvent = battleEffect;
            Anims.AddRange(anims);
        }

        /// <inheritdoc/>
        protected SetDamageEvent(SetDamageEvent other) : this()
        {
            BaseEvent = other.BaseEvent;
            Anims = new List<BattleAnimEvent>();
            foreach (BattleAnimEvent anim in other.Anims)
                Anims.Add((BattleAnimEvent)anim.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new SetDamageEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.User != context.Target)
            {
                BattleData newData = new BattleData(context.Data);

                foreach (BattleAnimEvent anim in Anims)
                    yield return CoroutineManager.Instance.StartCoroutine(anim.Apply(owner, ownerChar, context));

                foreach (Priority priority in newData.OnHits.GetPriorities())
                {
                    int count = newData.OnHits.GetCountAtPriority(priority);
                    for (int jj = 0; jj < count; jj++)
                    {
                        BattleEvent effect = newData.OnHits.Get(priority, jj);
                        if (effect is DirectDamageEvent)
                            newData.OnHits.Set(priority, jj, (BattleEvent)BaseEvent.Clone());
                    }
                }

                context.Data = newData;
            }
            yield break;
        }
    }


    /// <summary>
    /// Event that maps the current map status to a battle event.
    /// If there is no match, it maps the current map type (terrain element) to a battle event.
    /// Used for moves like Nature Power that change based on terrain.
    /// </summary>
    [Serializable]
    public class NatureSpecialEvent : BattleEvent
    {
        /// <summary>
        /// The map status ID mapped to a battle event.
        /// Checked first before terrain element.
        /// </summary>
        [JsonConverter(typeof(MapStatusBattleEventDictConverter))]
        [DataType(0, DataManager.DataType.MapStatus, false)]
        public Dictionary<string, BattleEvent> TerrainPair;

        /// <summary>
        /// The element type mapped to a battle event.
        /// Used if no map status match is found.
        /// </summary>
        [JsonConverter(typeof(ElementBattleEventDictConverter))]
        [DataType(1, DataManager.DataType.Element, false)]
        public Dictionary<string, BattleEvent> NaturePair;

        /// <inheritdoc/>
        public NatureSpecialEvent()
        {
            TerrainPair = new Dictionary<string, BattleEvent>();
            NaturePair = new Dictionary<string, BattleEvent>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NatureSpecialEvent"/> class with terrain and element mappings.
        /// </summary>
        /// <param name="terrain">Dictionary mapping map statuses to battle events.</param>
        /// <param name="moves">Dictionary mapping elements to battle events.</param>
        public NatureSpecialEvent(Dictionary<string, BattleEvent> terrain, Dictionary<string, BattleEvent> moves)
        {
            TerrainPair = terrain;
            NaturePair = moves;
        }

        /// <inheritdoc/>
        protected NatureSpecialEvent(NatureSpecialEvent other)
            : this()
        {
            foreach (string terrain in other.TerrainPair.Keys)
                TerrainPair.Add(terrain, (BattleEvent)other.TerrainPair[terrain].Clone());
            foreach (string element in other.NaturePair.Keys)
                NaturePair.Add(element, (BattleEvent)other.NaturePair[element].Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new NatureSpecialEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            foreach (string terrain in TerrainPair.Keys)
            {
                if (ZoneManager.Instance.CurrentMap.Status.ContainsKey(terrain))
                {
                    yield return CoroutineManager.Instance.StartCoroutine(TerrainPair[terrain].Apply(owner, ownerChar, context));
                    yield break;
                }
            }

            BattleEvent effect;
            if (NaturePair.TryGetValue(ZoneManager.Instance.CurrentMap.Element, out effect))
                yield return CoroutineManager.Instance.StartCoroutine(effect.Apply(owner, ownerChar, context));
            else
                yield break;
        }
    }

}


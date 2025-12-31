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
    // Battle events that can cancel the action

    /// <summary>
    /// Event that cancels the action (intended to be used with -Needed events).
    /// Displays an optional message and sets the cancel state.
    /// </summary>
    [Serializable]
    public class CancelActionEvent : BattleEvent
    {
        /// <summary>
        /// The message displayed in the dungeon log when the action is cancelled.
        /// </summary>
        [StringKey(0, true)]
        public StringKey Message;

        /// <inheritdoc/>
        public CancelActionEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CancelActionEvent"/> class with the specified message.
        /// </summary>
        /// <param name="message">The message to display when cancelling.</param>
        public CancelActionEvent(StringKey message) : this()
        {
            Message = message;
        }

        /// <inheritdoc/>
        protected CancelActionEvent(CancelActionEvent other) : this()
        {
            Message = other.Message;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new CancelActionEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (Message.IsValid())
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.User.GetDisplayName(false)));
            context.CancelState.Cancel = true;
            yield break;
        }
    }

    /// <summary>
    /// Event that prevents the character from doing certain battle action types.
    /// Can block skills, items, throws, or other action types.
    /// </summary>
    [Serializable]
    public class PreventActionEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle action types that the character cannot perform.
        /// </summary>
        public HashSet<BattleActionType> Actions;

        /// <summary>
        /// The message displayed in the dungeon log when an action is prevented.
        /// </summary>
        [StringKey(0, true)]
        public StringKey Message;

        /// <inheritdoc/>
        public PreventActionEvent() { Actions = new HashSet<BattleActionType>(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreventActionEvent"/> class with the specified message and action types.
        /// </summary>
        /// <param name="message">The message to display when preventing.</param>
        /// <param name="actions">The action types to prevent.</param>
        public PreventActionEvent(StringKey message, params BattleActionType[] actions) : this()
        {
            Message = message;
            foreach (BattleActionType actionType in actions)
                Actions.Add(actionType);
        }

        /// <inheritdoc/>
        protected PreventActionEvent(PreventActionEvent other) : this()
        {
            Message = other.Message;
            foreach (BattleActionType actionType in other.Actions)
                Actions.Add(actionType);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new PreventActionEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.UsageSlot == BattleContext.FORCED_SLOT)
                yield break;

            if (!Actions.Contains(context.ActionType))
                yield break;

            if (Message.IsValid())
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.User.GetDisplayName(false)));
            context.CancelState.Cancel = true;
        }
    }

    /// <summary>
    /// Event that prevents the character from using items unless the item contains one of the specified item states.
    /// Allows exceptions for certain item types.
    /// </summary>
    [Serializable]
    public class PreventItemActionEvent : BattleEvent
    {

        /// <summary>
        /// The list of valid ItemState types that are allowed.
        /// </summary>
        [StringTypeConstraint(1, typeof(ItemState))]
        public HashSet<FlagType> ExceptTypes;

        /// <summary>
        /// The message displayed in the dungeon log if the condition is not met.
        /// </summary>
        [StringKey(0, true)]
        public StringKey Message;

        /// <inheritdoc/>
        public PreventItemActionEvent() { ExceptTypes = new HashSet<FlagType>(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreventItemActionEvent"/> class with the specified message and exception types.
        /// </summary>
        /// <param name="message">The message to display when preventing.</param>
        /// <param name="exceptTypes">The item state types that are allowed.</param>
        public PreventItemActionEvent(StringKey message, params FlagType[] exceptTypes) : this()
        {
            Message = message;
            foreach (FlagType useType in exceptTypes)
                ExceptTypes.Add(useType);
        }

        /// <inheritdoc/>
        protected PreventItemActionEvent(PreventItemActionEvent other)
        {
            Message = other.Message;
            foreach (FlagType useType in other.ExceptTypes)
                ExceptTypes.Add(useType);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new PreventItemActionEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Item)
            {
                if (context.ActionType == BattleActionType.Item)
                {
                    ItemData entry = DataManager.Instance.GetItem(context.Item.ID);
                    foreach (FlagType flag in ExceptTypes)
                    {
                        if (entry.ItemStates.Contains(flag.FullType))
                            yield break;
                    }
                }

                if (Message.IsValid())
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.User.GetDisplayName(false)));
                context.CancelState.Cancel = true;
            }
        }
    }

    /// <summary>
    /// Event that prevents the character from using items if they are paralyzed
    /// unless the item contains one of the specified item states.
    /// </summary>
    [Serializable]
    public class PreventItemParalysisEvent : BattleEvent
    {

        /// <summary>
        /// The list of valid ItemState types that are allowed even when paralyzed.
        /// </summary>
        [StringTypeConstraint(1, typeof(ItemState))]
        public HashSet<FlagType> ExceptTypes;

        /// <summary>
        /// The message displayed in the dungeon log if the condition is not met.
        /// </summary>
        [StringKey(0, true)]
        public StringKey Message;

        /// <inheritdoc/>
        public PreventItemParalysisEvent() { ExceptTypes = new HashSet<FlagType>(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreventItemParalysisEvent"/> class with the specified message and exception types.
        /// </summary>
        /// <param name="message">The message to display when preventing.</param>
        /// <param name="exceptTypes">The item state types that are allowed.</param>
        public PreventItemParalysisEvent(StringKey message, params FlagType[] exceptTypes) : this()
        {
            Message = message;
            foreach (FlagType useType in exceptTypes)
                ExceptTypes.Add(useType);
        }

        /// <inheritdoc/>
        protected PreventItemParalysisEvent(PreventItemParalysisEvent other)
        {
            Message = other.Message;
            foreach (FlagType useType in other.ExceptTypes)
                ExceptTypes.Add(useType);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new PreventItemParalysisEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Item || context.ActionType == BattleActionType.Throw)
            {
                if (context.ActionType == BattleActionType.Item)
                {
                    ItemData entry = DataManager.Instance.GetItem(context.Item.ID);
                    foreach (FlagType flag in ExceptTypes)
                    {
                        if (entry.ItemStates.Contains(flag.FullType))
                            yield break;
                    }
                }

                ParalyzeState para = ((StatusEffect)owner).StatusStates.GetWithDefault<ParalyzeState>();
                if (para.Recent)
                {
                    if (Message.IsValid())
                        DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.User.GetDisplayName(false)));
                    context.CancelState.Cancel = true;
                }
            }
        }
    }

    /// <summary>
    /// Event that prevents the character from using items if the item contains one of the specified item states.
    /// Blocks specific item types from being used.
    /// </summary>
    [Serializable]
    public class PreventItemUseEvent : BattleEvent
    {

        /// <summary>
        /// The list of ItemState types that will be blocked.
        /// </summary>
        [StringTypeConstraint(1, typeof(ItemState))]
        public HashSet<FlagType> UseTypes;

        /// <summary>
        /// The message displayed in the dungeon log if the condition is met.
        /// </summary>
        [StringKey(0, true)]
        public StringKey Message;

        /// <inheritdoc/>
        public PreventItemUseEvent() { UseTypes = new HashSet<FlagType>(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreventItemUseEvent"/> class with the specified message and blocked types.
        /// </summary>
        /// <param name="message">The message to display when preventing.</param>
        /// <param name="useTypes">The item state types to block.</param>
        public PreventItemUseEvent(StringKey message, params FlagType[] useTypes) : this()
        {
            Message = message;
            foreach (FlagType useType in useTypes)
                UseTypes.Add(useType);
        }

        /// <inheritdoc/>
        protected PreventItemUseEvent(PreventItemUseEvent other) : this()
        {
            Message = other.Message;
            foreach (FlagType useType in other.UseTypes)
                UseTypes.Add(useType);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new PreventItemUseEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Item)
            {
                ItemData entry = DataManager.Instance.GetItem(context.Item.ID);
                bool canceled = false;
                if (UseTypes.Count == 0)
                    canceled = true;
                foreach (FlagType flag in UseTypes)
                {
                    if (entry.ItemStates.Contains(flag.FullType))
                    {
                        canceled = true;
                        break;
                    }
                }

                if (canceled)
                {
                    if (Message.IsValid())
                        DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.User.GetDisplayName(false)));
                    context.CancelState.Cancel = true;
                }
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that prevents the character from using the item if the item's HiddenValue is set.
    /// Used to block items that are currently "active" or in use.
    /// </summary>
    [Serializable]
    public class CheckItemActiveEvent : BattleEvent
    {
        /// <inheritdoc/>
        public CheckItemActiveEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new CheckItemActiveEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (!String.IsNullOrEmpty(context.Item.HiddenValue))
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_ITEM_CANT_USE_NOW").ToLocal()));
                context.CancelState.Cancel = true;
            }
            yield break;
        }

    }

    /// <summary>
    /// Event that prevents the character from using certain specific items by ID.
    /// Blocks a specific list of items.
    /// </summary>
    [Serializable]
    public class PreventItemIndexEvent : BattleEvent
    {
        /// <summary>
        /// The list of item IDs the character cannot use.
        /// </summary>
        [JsonConverter(typeof(ItemListConverter))]
        [DataType(1, DataManager.DataType.Item, false)]
        public List<string> UseTypes;

        /// <summary>
        /// The message displayed in the dungeon log if the character cannot use the item.
        /// </summary>
        [StringKey(0, true)]
        public StringKey Message;

        /// <inheritdoc/>
        public PreventItemIndexEvent() { UseTypes = new List<string>(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreventItemIndexEvent"/> class with the specified message and item IDs.
        /// </summary>
        /// <param name="message">The message to display when preventing.</param>
        /// <param name="useTypes">The item IDs to block.</param>
        public PreventItemIndexEvent(StringKey message, params string[] useTypes)
        {
            Message = message;
            UseTypes = new List<string>();
            UseTypes.AddRange(useTypes);
        }

        /// <inheritdoc/>
        protected PreventItemIndexEvent(PreventItemIndexEvent other) : this()
        {
            Message = other.Message;
            foreach (string useType in other.UseTypes)
                UseTypes.Add(useType);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new PreventItemIndexEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Item)
            {
                if (UseTypes.Contains(context.Item.ID))
                {
                    if (Message.IsValid())
                        DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.User.GetDisplayName(false)));
                    context.CancelState.Cancel = true;
                }
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that prevents the character from taking action if they are below the specified HP threshold.
    /// Used for moves that require a minimum HP to use.
    /// </summary>
    [Serializable]
    public class HPActionCheckEvent : BattleEvent
    {
        /// <summary>
        /// The HP threshold given as 1/HPFraction of max HP.
        /// </summary>
        public int HPFraction;

        /// <inheritdoc/>
        public HPActionCheckEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="HPActionCheckEvent"/> class with the specified HP fraction.
        /// </summary>
        /// <param name="hpFraction">The fraction of max HP required (1/hpFraction).</param>
        public HPActionCheckEvent(int hpFraction)
        {
            HPFraction = hpFraction;
        }

        /// <inheritdoc/>
        protected HPActionCheckEvent(HPActionCheckEvent other)
        {
            HPFraction = other.HPFraction;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new HPActionCheckEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.User.HP <= context.User.MaxHP / HPFraction)
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_HP_NEEDED").ToLocal(), context.User.GetDisplayName(false)));
                context.CancelState.Cancel = true;
            }
            yield break;
        }
    }


    /// <summary>
    /// Event that prevents the character from taking action while asleep.
    /// This event can only be used in statuses.
    /// </summary>
    [Serializable]
    public class SleepEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new SleepEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.UsageSlot == BattleContext.FORCED_SLOT)
                yield break;

            if (context.ActionType == BattleActionType.Item)
            {
                ItemData entry = DataManager.Instance.GetItem(context.Item.ID);
                if (entry.ItemStates.Contains<CurerState>())
                    yield break;
            }

            if (((StatusEffect)owner).StatusStates.GetWithDefault<CountDownState>().Counter > 0)
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_ASLEEP").ToLocal(), context.User.GetDisplayName(false)));
            if (!context.ContextStates.Contains<SleepAttack>())
                context.CancelState.Cancel = true;
        }
    }

    /// <summary>
    /// Event that displays a message if the character does not have the BoundAttack context state.
    /// Used for bound/trapped status effects.
    /// </summary>
    [Serializable]
    public class BoundEvent : BattleEvent
    {
        /// <summary>
        /// The message displayed in the dungeon log when the character is bound.
        /// </summary>
        [StringKey(0, true)]
        public StringKey Message;

        /// <inheritdoc/>
        public BoundEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundEvent"/> class with the specified message.
        /// </summary>
        /// <param name="message">The message to display when bound.</param>
        public BoundEvent(StringKey message)
        {
            Message = message;
        }

        /// <inheritdoc/>
        protected BoundEvent(BoundEvent other)
        {
            Message = other.Message;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new BoundEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.UsageSlot == BattleContext.FORCED_SLOT)
                yield break;

            if (!context.ContextStates.Contains<BoundAttack>())
            {
                if (Message.IsValid())
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.User.GetDisplayName(false)));
                context.CancelState.Cancel = true;
            }
        }
    }


    /// <summary>
    /// Event that deals damage based on the value in the HPState status state and skips the character's turn.
    /// This event can only be used on statuses. Used for wrap/bind-style trapping moves.
    /// </summary>
    [Serializable]
    public class WrapTrapEvent : BattleEvent
    {
        /// <summary>
        /// The message displayed in the dungeon log when trapped.
        /// </summary>
        [StringKey(0, true)]
        public StringKey Message;

        /// <summary>
        /// The list of battle VFXs played when the character is trapped.
        /// </summary>
        public List<AnimEvent> Anims;

        /// <summary>
        /// The animation index played when the character is trapped.
        /// </summary>
        [FrameType(0, false)]
        public int CharAnim;

        /// <inheritdoc/>
        public WrapTrapEvent() { Anims = new List<AnimEvent>(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="WrapTrapEvent"/> class with the specified parameters.
        /// </summary>
        /// <param name="message">The message to display when trapped.</param>
        /// <param name="animType">The character animation index.</param>
        /// <param name="anims">The visual effects to play.</param>
        public WrapTrapEvent(StringKey message, int animType, params AnimEvent[] anims)
        {
            Message = message;
            CharAnim = animType;
            Anims = new List<AnimEvent>();
            Anims.AddRange(anims);
        }

        /// <inheritdoc/>
        protected WrapTrapEvent(WrapTrapEvent other)
        {
            Message = other.Message;
            Anims = new List<AnimEvent>();
            foreach (AnimEvent anim in other.Anims)
                Anims.Add((AnimEvent)anim.Clone());
            CharAnim = other.CharAnim;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new WrapTrapEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.UsageSlot == BattleContext.FORCED_SLOT)
                yield break;

            Character targetChar = ownerChar;
            StatusEffect status = (StatusEffect)owner;
            if (!targetChar.CharStates.Contains<MagicGuardState>())
            {
                if (Message.IsValid())
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.User.GetDisplayName(false)));

                CharAnimAction chargeAnim = new CharAnimAction(context.User.CharLoc, context.User.CharDir, CharAnim);
                yield return CoroutineManager.Instance.StartCoroutine(context.User.StartAnim(chargeAnim));

                foreach (AnimEvent anim in Anims)
                {
                    SingleCharContext singleContext = new SingleCharContext(targetChar);
                    yield return CoroutineManager.Instance.StartCoroutine(anim.Apply(owner, ownerChar, singleContext));
                }

                int trapdmg = status.StatusStates.GetWithDefault<HPState>().HP;
                yield return CoroutineManager.Instance.StartCoroutine(targetChar.InflictDamage(trapdmg));
            }
            context.CancelState.Cancel = true;
        }
    }

    /// <summary>
    /// Event used specifically for the freeze status.
    /// Allows fire-type moves to thaw the user and blocks other actions.
    /// </summary>
    [Serializable]
    public class FreezeEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new FreezeEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.UsageSlot == BattleContext.FORCED_SLOT)
                yield break;

            if (context.Data.Element == "fire")
            {
                yield return CoroutineManager.Instance.StartCoroutine(context.User.RemoveStatusEffect(((StatusEffect)owner).ID));
                yield break;
            }
            if (context.ActionType == BattleActionType.Item)
            {
                ItemData entry = DataManager.Instance.GetItem(context.Item.ID);
                if (entry.ItemStates.Contains<CurerState>())
                    yield break;
            }


            if (((StatusEffect)owner).StatusStates.GetWithDefault<CountDownState>().Counter > 0)
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_FROZEN").ToLocal(), context.User.GetDisplayName(false)));
            context.CancelState.Cancel = true;
        }
    }

    /// <summary>
    /// Event that thaws the character if the targeting move is a fire-type.
    /// Otherwise, the move will miss. This event can only be used on statuses.
    /// </summary>
    [Serializable]
    public class ThawEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new ThawEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Data.Element == "fire")
            {
                yield return CoroutineManager.Instance.StartCoroutine(context.Target.RemoveStatusEffect(((StatusEffect)owner).ID));
                yield break;
            }

            if (context.ContextStates.Contains<CureAttack>())
                yield break;

            if (context.Data.Category != BattleData.SkillCategory.None)
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_FROZEN").ToLocal(), context.Target.GetDisplayName(false)));
                context.AddContextStateMult<AccMult>(false, -1, 1);
            }
        }
    }

    /// <summary>
    /// Event that prevents the character from taking action if the ParalyzeState is recent.
    /// This event can only be used on statuses.
    /// </summary>
    [Serializable]
    public class ParalysisEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle VFXs played if the paralysis prevents action.
        /// </summary>
        public List<BattleAnimEvent> Anims;

        /// <inheritdoc/>
        public ParalysisEvent()
        {
            Anims = new List<BattleAnimEvent>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParalysisEvent"/> class with the specified animations.
        /// </summary>
        /// <param name="anims">The visual effects to play when paralyzed.</param>
        public ParalysisEvent(params BattleAnimEvent[] anims)
        {
            Anims = new List<BattleAnimEvent>();
            Anims.AddRange(anims);
        }

        /// <inheritdoc/>
        protected ParalysisEvent(ParalysisEvent other)
        {
            Anims = new List<BattleAnimEvent>();
            foreach (BattleAnimEvent anim in other.Anims)
                Anims.Add((BattleAnimEvent)anim.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ParalysisEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.UsageSlot == BattleContext.FORCED_SLOT)
                yield break;

            if (context.ActionType == BattleActionType.Item)
            {
                ItemData entry = DataManager.Instance.GetItem(context.Item.ID);
                if (entry.ItemStates.Contains<CurerState>())
                    yield break;
            }

            ParalyzeState para = ((StatusEffect)owner).StatusStates.GetWithDefault<ParalyzeState>();
            if (para.Recent)
            {
                foreach (BattleAnimEvent anim in Anims)
                    yield return CoroutineManager.Instance.StartCoroutine(anim.Apply(owner, ownerChar, context));

                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_PARALYZED").ToLocal(), context.User.GetDisplayName(false)));
                yield return new WaitForFrames(GameManager.Instance.ModifyBattleSpeed(30));
                context.CancelState.Cancel = true;
            }

        }
    }


    /// <summary>
    /// Event that prevents the character from using the move if the specified status is not present.
    /// Used for moves that require a specific status to be active.
    /// </summary>
    [Serializable]
    public class StatusNeededEvent : BattleEvent
    {
        /// <summary>
        /// The status ID that must be present to allow the action.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string StatusID;

        /// <summary>
        /// The message displayed in the dungeon log if the status is missing.
        /// </summary>
        public StringKey Message;

        /// <inheritdoc/>
        public StatusNeededEvent() { StatusID = ""; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusNeededEvent"/> class with the specified status and message.
        /// </summary>
        /// <param name="statusID">The required status ID.</param>
        /// <param name="msg">The message to display when the status is missing.</param>
        public StatusNeededEvent(string statusID, StringKey msg)
        {
            StatusID = statusID;
            Message = msg;
        }

        /// <inheritdoc/>
        protected StatusNeededEvent(StatusNeededEvent other)
        {
            StatusID = other.StatusID;
            Message = other.Message;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new StatusNeededEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.User.GetStatusEffect(StatusID) == null)
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.User.GetDisplayName(false)));
                context.CancelState.Cancel = true;
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that prevents the character from using the move if the specified map status is not present.
    /// Used for moves that require specific weather or terrain.
    /// </summary>
    [Serializable]
    public class WeatherRequiredEvent : BattleEvent
    {
        /// <summary>
        /// The map status IDs that allow the action (any one must be present).
        /// </summary>
        [JsonConverter(typeof(SkillListConverter))]
        [DataType(1, DataManager.DataType.MapStatus, false)]
        public List<string> AcceptedWeather;

        /// <summary>
        /// The message displayed in the dungeon log if the condition is not met.
        /// </summary>
        public StringKey Message;

        /// <inheritdoc/>
        public WeatherRequiredEvent() { AcceptedWeather = new List<string>(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeatherRequiredEvent"/> class with the specified message and weather IDs.
        /// </summary>
        /// <param name="msg">The message to display when weather is missing.</param>
        /// <param name="statusIDs">The accepted weather/map status IDs.</param>
        public WeatherRequiredEvent(StringKey msg, params string[] statusIDs)
        {
            AcceptedWeather = new List<string>();
            AcceptedWeather.AddRange(statusIDs);
            Message = msg;
        }

        /// <inheritdoc/>
        protected WeatherRequiredEvent(WeatherRequiredEvent other)
        {
            AcceptedWeather = new List<string>();
            AcceptedWeather.AddRange(other.AcceptedWeather);
            Message = other.Message;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new WeatherRequiredEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            foreach (string weatherId in AcceptedWeather)
            {
                if (ZoneManager.Instance.CurrentMap.Status.ContainsKey(weatherId))
                    yield break;
            }

            DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.User.GetDisplayName(false)));
            context.CancelState.Cancel = true;
        }
    }



    /// <summary>
    /// Event that spawns an enemy from a fake item if someone attempted to use it.
    /// This should only be used in a MapEffectStep.
    /// </summary>
    [Serializable]
    public class FakeItemBattleEvent : BattleEvent
    {
        /// <summary>
        /// The fake item mapped to an enemy spawn.
        /// </summary>
        [JsonConverter(typeof(ItemFakeTableConverter))]
        public Dictionary<ItemFake, MobSpawn> SpawnTable;

        /// <inheritdoc/>
        public FakeItemBattleEvent()
        {
            SpawnTable = new Dictionary<ItemFake, MobSpawn>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeItemBattleEvent"/> class with the specified spawn table.
        /// </summary>
        /// <param name="spawnTable">Dictionary mapping fake items to mob spawns.</param>
        public FakeItemBattleEvent(Dictionary<ItemFake, MobSpawn> spawnTable)
        {
            this.SpawnTable = spawnTable;
        }

        /// <inheritdoc/>
        public FakeItemBattleEvent(FakeItemBattleEvent other)
        {
            this.SpawnTable = new Dictionary<ItemFake, MobSpawn>();
            foreach (ItemFake fake in other.SpawnTable.Keys)
                this.SpawnTable.Add(fake, other.SpawnTable[fake].Copy());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new FakeItemBattleEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            ItemFake fake = new ItemFake(context.Item.ID, context.Item.HiddenValue);
            MobSpawn spawn;
            if (SpawnTable.TryGetValue(fake, out spawn))
            {
                if (context.UsageSlot == BattleContext.FLOOR_ITEM_SLOT)
                {
                    int mapSlot = ZoneManager.Instance.CurrentMap.GetItem(context.User.CharLoc);
                    ZoneManager.Instance.CurrentMap.Items.RemoveAt(mapSlot);
                }
                else if (context.UsageSlot == BattleContext.EQUIP_ITEM_SLOT)
                    context.User.SilentDequipItem();
                else
                    context.User.MemberTeam.RemoveFromInv(context.UsageSlot);

                yield return CoroutineManager.Instance.StartCoroutine(FakeItemEvent.SpawnFake(context.User, context.Item, spawn));

                //cancel the operation
                context.CancelState.Cancel = true;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Data;
using RogueEssence.Content;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Dev;
using Newtonsoft.Json;
using Avalonia.X11;
using DynamicData;

namespace PMDC.Dungeon
{
    /// <summary>
    /// Status given event wrapper that only applies when the event is triggered by its own status.
    /// Used for effects that should only trigger during their own application.
    /// </summary>
    [Serializable]
    public class ThisStatusGivenEvent : StatusGivenEvent
    {
        /// <summary>
        /// The event to apply when triggered by the owning status.
        /// </summary>
        public StatusGivenEvent BaseEvent;

        public ThisStatusGivenEvent() { }
        public ThisStatusGivenEvent(StatusGivenEvent baseEffect)
        {
            BaseEvent = baseEffect;
        }
        protected ThisStatusGivenEvent(ThisStatusGivenEvent other)
        {
            BaseEvent = (StatusGivenEvent)other.BaseEvent.Clone();
        }
        public override GameEvent Clone() { return new ThisStatusGivenEvent(this); }

        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (context.Status != owner)
                yield break;
            
            yield return CoroutineManager.Instance.StartCoroutine(BaseEvent.Apply(owner, ownerChar, context));

        }
    }


    /// <summary>
    /// Status given event wrapper that only applies when the owner belongs to a specific family.
    /// Used for family-exclusive status effects.
    /// </summary>
    [Serializable]
    public class FamilyStatusEvent : StatusGivenEvent
    {
        /// <summary>
        /// The event to apply when the family condition is met.
        /// </summary>
        public StatusGivenEvent BaseEvent;

        public FamilyStatusEvent() { }
        public FamilyStatusEvent(StatusGivenEvent baseEffect)
        {
            BaseEvent = baseEffect;
        }
        protected FamilyStatusEvent(FamilyStatusEvent other)
        {
            BaseEvent = (StatusGivenEvent)other.BaseEvent.Clone();
        }
        public override GameEvent Clone() { return new FamilyStatusEvent(this); }

        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
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
    /// Status given event that wraps a SingleCharEvent to execute in a status context.
    /// Allows single character events to be used during status application.
    /// </summary>
    [Serializable]
    public class StatusCharEvent : StatusGivenEvent
    {
        /// <summary>
        /// The single character event to execute.
        /// </summary>
        public SingleCharEvent BaseEvent;

        /// <summary>
        /// If true, affects the target of the status. If false, affects the user who applied the status.
        /// </summary>
        public bool AffectTarget;

        public StatusCharEvent() { }
        public StatusCharEvent(SingleCharEvent baseEffect, bool affectTarget)
        {
            BaseEvent = baseEffect;
            AffectTarget = affectTarget;
        }
        protected StatusCharEvent(StatusCharEvent other)
        {
            BaseEvent = (SingleCharEvent)other.BaseEvent.Clone();
            AffectTarget = other.AffectTarget;
        }
        public override GameEvent Clone() { return new StatusCharEvent(this); }

        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            SingleCharContext singleContext = new SingleCharContext(AffectTarget ? context.Target : context.User);
            yield return CoroutineManager.Instance.StartCoroutine(BaseEvent.Apply(owner, ownerChar, singleContext));
        }
    }



    /// <summary>
    /// Status given event that synchronizes countdown state when reapplying a status.
    /// Decrements the existing countdown when a status is reapplied.
    /// </summary>
    [Serializable]
    public class StatusCountdownCheck : StatusGivenEvent
    {
        public StatusCountdownCheck() { }
        public override GameEvent Clone() { return new StatusCountdownCheck(); }

        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (owner == context.Status)
            {
                CountDownState countdown = context.Status.StatusStates.GetWithDefault<CountDownState>();

                StatusEffect existingStatus = context.Target.GetStatusEffect(context.Status.ID);
                if (existingStatus != null)
                {
                    int counter = existingStatus.StatusStates.GetWithDefault<CountDownState>().Counter;
                    countdown.Counter = counter - 1;
                }
            }
            yield break;
        }
    }


    /// <summary>
    /// Abstract base class for status stack checks that enforce minimum and maximum stack limits.
    /// Prevents stat stages from going above or below their limits.
    /// </summary>
    [Serializable]
    public abstract class StatusStackCheck : StatusGivenEvent
    {
        /// <summary>
        /// The minimum allowed stack value.
        /// </summary>
        public int Minimum;

        /// <summary>
        /// The maximum allowed stack value.
        /// </summary>
        public int Maximum;

        protected StatusStackCheck() { }
        protected StatusStackCheck(int min, int max)
        {
            Minimum = min;
            Maximum = max;
        }
        protected StatusStackCheck(StatusStackCheck other)
        {
            Minimum = other.Minimum;
            Maximum = other.Maximum;
        }

        protected abstract string GetLimitMsg(Character target, bool upperLimit);

        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (owner == context.Status)
            {
                int counter = 0;
                StatusEffect existingStatus = context.Target.GetStatusEffect(context.Status.ID);
                if (existingStatus != null)
                    counter = existingStatus.StatusStates.GetWithDefault<StackState>().Stack;

                StackState boost = context.Status.StatusStates.GetWithDefault<StackState>();
                int stackDiff = boost.Stack;
                if (counter + boost.Stack > Maximum)
                {
                    stackDiff = Maximum - counter;
                    if (stackDiff == 0)
                    {
                        DungeonScene.Instance.LogMsg(GetLimitMsg(context.Target, true));
                        context.CancelState.Cancel = true;
                    }
                }
                else if (counter + boost.Stack < Minimum)
                {
                    stackDiff = Minimum - counter;
                    if (stackDiff == 0)
                    {
                        DungeonScene.Instance.LogMsg(GetLimitMsg(context.Target, false));
                        context.CancelState.Cancel = true;
                    }
                }
                boost.Stack = counter + stackDiff;
                if (stackDiff != 0)
                    context.StackDiff = stackDiff;
            }
            yield break;
        }
    }
    /// <summary>
    /// Status stack check that uses custom string messages for limit notifications.
    /// </summary>
    [Serializable]
    public class StringStackCheck : StatusStackCheck
    {
        /// <summary>
        /// Message displayed when the upper limit is reached.
        /// </summary>
        public StringKey HiLimitMsg;

        /// <summary>
        /// Message displayed when the lower limit is reached.
        /// </summary>
        public StringKey LoLimitMsg;

        public StringStackCheck() { }
        public StringStackCheck(int min, int max, StringKey hiMsg, StringKey loMsg)
            : base(min, max)
        {
            HiLimitMsg = hiMsg;
            LoLimitMsg = loMsg;
        }
        protected StringStackCheck(StringStackCheck other)
            : base(other)
        {
            HiLimitMsg = other.HiLimitMsg;
            LoLimitMsg = other.LoLimitMsg;
        }
        public override GameEvent Clone() { return new StringStackCheck(this); }

        protected override string GetLimitMsg(Character target, bool upperLimit)
        {
            if (upperLimit)
                return Text.FormatGrammar(HiLimitMsg.ToLocal(), target.GetDisplayName(false));
            else
                return Text.FormatGrammar(LoLimitMsg.ToLocal(), target.GetDisplayName(false));
        }
    }
    /// <summary>
    /// Status stack check for stat stage changes that uses standard stat boost/drop messages.
    /// </summary>
    [Serializable]
    public class StatStackCheck : StatusStackCheck
    {
        /// <summary>
        /// The stat being modified by this stack.
        /// </summary>
        public Stat Stack;

        public StatStackCheck() { }
        public StatStackCheck(int min, int max, Stat stack)
            : base(min, max)
        {
            Stack = stack;
        }
        protected StatStackCheck(StatStackCheck other)
            : base(other)
        {
            Stack = other.Stack;
        }
        public override GameEvent Clone() { return new StatStackCheck(this); }

        protected override string GetLimitMsg(Character target, bool upperLimit)
        {
            if (upperLimit)
                return Text.FormatGrammar(new StringKey("MSG_BUFF_NO_MORE").ToLocal(), target.GetDisplayName(false), Stack.ToLocal());
            else
                return Text.FormatGrammar(new StringKey("MSG_BUFF_NO_LESS").ToLocal(), target.GetDisplayName(false), Stack.ToLocal());
        }
    }

    /// <summary>
    /// Status given event that multiplies the stack value of stat change statuses.
    /// Used for abilities that amplify or reverse stat changes.
    /// </summary>
    [Serializable]
    public class StatusStackMod : StatusGivenEvent
    {
        /// <summary>
        /// The multiplier to apply to the stack value.
        /// </summary>
        public int Mod;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public StatusStackMod() { }

        /// <summary>
        /// Initializes a new instance with the specified modifier.
        /// </summary>
        /// <param name="mod">The stack multiplier.</param>
        public StatusStackMod(int mod)
        {
            Mod = mod;
        }

        /// <summary>
        /// Copy constructor for cloning an existing StatusStackMod.
        /// </summary>
        protected StatusStackMod(StatusStackMod other)
        {
            Mod = other.Mod;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new StatusStackMod(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (owner != context.Status)//can't check on self
            {
                if (context.Status.StatusStates.GetWithDefault<StatChangeState>() != null)
                {
                    StackState stackState = context.Status.StatusStates.GetWithDefault<StackState>();
                    if (stackState != null)
                        stackState.Stack *= Mod;
                }
            }
            yield break;
        }
    }
    /// <summary>
    /// Status given event that adds to the stack value when the user has specific character states.
    /// Used for abilities that boost stat changes under certain conditions.
    /// </summary>
    [Serializable]
    public class StatusStackBoostMod : StatusGivenEvent
    {
        /// <summary>
        /// List of character states that enable the stack boost.
        /// </summary>
        [StringTypeConstraint(1, typeof(CharState))]
        public List<FlagType> States;

        /// <summary>
        /// The amount to add to the stack value.
        /// </summary>
        public int Stack;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public StatusStackBoostMod() { States = new List<FlagType>(); }

        /// <summary>
        /// Initializes a new instance with the specified state and stack amount.
        /// </summary>
        /// <param name="state">The character state type required.</param>
        /// <param name="stack">The stack amount to add.</param>
        public StatusStackBoostMod(Type state, int stack) : this() { States.Add(new FlagType(state)); Stack = stack; }

        /// <summary>
        /// Copy constructor for cloning an existing StatusStackBoostMod.
        /// </summary>
        public StatusStackBoostMod(StatusStackBoostMod other) : this() { States.AddRange(other.States); Stack = other.Stack; }

        /// <inheritdoc/>
        public override GameEvent Clone() => new StatusStackBoostMod(this);

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (owner == context.Status)//done BY the pending status
            {
                //check if the attacker has the right charstate
                bool hasState = false;
                foreach (FlagType state in States)
                {
                    if (context.User.CharStates.Contains(state.FullType))
                        hasState = true;
                }
                if (context.User != null && hasState)
                {
                    StackState stack = context.Status.StatusStates.GetWithDefault<StackState>();
                    if (stack != null)
                        stack.Stack += Stack;
                }
            }
            yield break;
        }

    }

    /// <summary>
    /// Status given event that adds to the count value when the user has specific character states.
    /// Used for abilities that boost status counts under certain conditions.
    /// </summary>
    [Serializable]
    public class StatusCountBoostMod : StatusGivenEvent
    {
        /// <summary>
        /// List of character states that enable the count boost.
        /// </summary>
        [StringTypeConstraint(1, typeof(CharState))]
        public List<FlagType> States;

        /// <summary>
        /// The amount to add to the count value.
        /// </summary>
        public int Stack;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public StatusCountBoostMod() { States = new List<FlagType>(); }

        /// <summary>
        /// Initializes a new instance with the specified state and stack amount.
        /// </summary>
        /// <param name="state">The character state type required.</param>
        /// <param name="stack">The count amount to add.</param>
        public StatusCountBoostMod(Type state, int stack) : this() { States.Add(new FlagType(state)); Stack = stack; }

        /// <summary>
        /// Copy constructor for cloning an existing StatusCountBoostMod.
        /// </summary>
        public StatusCountBoostMod(StatusCountBoostMod other) : this() { States.AddRange(other.States); Stack = other.Stack; }

        /// <inheritdoc/>
        public override GameEvent Clone() => new StatusCountBoostMod(this);

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (owner == context.Status)//done BY the pending status
            {
                //check if the attacker has the right charstate
                bool hasState = false;
                foreach (FlagType state in States)
                {
                    if (context.User.CharStates.Contains(state.FullType))
                        hasState = true;
                }
                if (context.User != null && hasState)
                {
                    CountState stack = context.Status.StatusStates.GetWithDefault<CountState>();
                    if (stack != null)
                        stack.Count += Stack;
                }
            }
            yield break;
        }

    }

    /// <summary>
    /// Status given event that doubles the HP value when the user has specific character states.
    /// Used for abilities that boost HP-based status effects.
    /// </summary>
    [Serializable]
    public class StatusHPBoostMod : StatusGivenEvent
    {
        /// <summary>
        /// List of character states that enable the HP boost.
        /// </summary>
        [StringTypeConstraint(1, typeof(CharState))]
        public List<FlagType> States;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public StatusHPBoostMod() { States = new List<FlagType>(); }

        /// <summary>
        /// Initializes a new instance with the specified state.
        /// </summary>
        /// <param name="state">The character state type required.</param>
        public StatusHPBoostMod(Type state) : this() { States.Add(new FlagType(state)); }

        /// <summary>
        /// Copy constructor for cloning an existing StatusHPBoostMod.
        /// </summary>
        protected StatusHPBoostMod(StatusHPBoostMod other) : this() { States.AddRange(other.States); }

        /// <inheritdoc/>
        public override GameEvent Clone() => new StatusHPBoostMod(this);

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (owner == context.Status)//done BY the pending status
            {
                //check if the attacker has the right charstate
                bool hasState = false;
                foreach (FlagType state in States)
                {
                    if (context.User.CharStates.Contains(state.FullType))
                        hasState = true;
                }
                if (context.User != null && hasState)
                {
                    HPState stack = context.Status.StatusStates.GetWithDefault<HPState>();
                    if (stack != null)
                        stack.HP *= 2;
                }
            }
            yield break;
        }

    }
    /// <summary>
    /// Status given event that modifies countdown duration when the user has specific character states.
    /// Used for abilities that extend or reduce status durations.
    /// </summary>
    [Serializable]
    public class CountDownBoostMod : StatusGivenEvent
    {
        /// <summary>
        /// List of character states that enable the countdown modification.
        /// </summary>
        [StringTypeConstraint(1, typeof(CharState))]
        public List<FlagType> States;

        /// <summary>
        /// The numerator of the duration multiplier ratio.
        /// </summary>
        public int Numerator;

        /// <summary>
        /// The denominator of the duration multiplier ratio.
        /// </summary>
        public int Denominator;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public CountDownBoostMod() { States = new List<FlagType>(); }

        /// <summary>
        /// Initializes a new instance with the specified parameters.
        /// </summary>
        /// <param name="state">The character state type required.</param>
        /// <param name="num">The numerator of the multiplier ratio.</param>
        /// <param name="den">The denominator of the multiplier ratio.</param>
        public CountDownBoostMod(Type state, int num, int den) : this()
        {
            States.Add(new FlagType(state));
            Numerator = num;
            Denominator = den;
        }

        /// <summary>
        /// Copy constructor for cloning an existing CountDownBoostMod.
        /// </summary>
        protected CountDownBoostMod(CountDownBoostMod other) : this()
        {
            States.AddRange(other.States);
            Numerator = other.Numerator;
            Denominator = other.Denominator;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() => new CountDownBoostMod(this);

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (owner == context.Status && context.User != null)// done BY pending status
            {
                //check if the attacker has the right charstate
                bool hasState = false;
                foreach (FlagType state in States)
                {
                    if (context.User.CharStates.Contains(state.FullType))
                        hasState = true;
                }
                if (hasState)
                {
                    //multiply turns, rounded up
                    CountDownState countDown = context.Status.StatusStates.GetWithDefault<CountDownState>();
                    if (countDown != null)
                    {
                        countDown.Counter *= Numerator;
                        countDown.Counter--;
                        countDown.Counter /= Denominator;
                        countDown.Counter++;
                    }
                }
            }
            yield break;
        }

    }

    /// <summary>
    /// Status given event that reduces bad status durations on self.
    /// Used for abilities that cause the character to recover from bad statuses faster.
    /// </summary>
    [Serializable]
    public class SelfCurerEvent : StatusGivenEvent
    {
        /// <summary>
        /// The numerator of the duration reduction ratio.
        /// </summary>
        public int Numerator;

        /// <summary>
        /// The denominator of the duration reduction ratio.
        /// </summary>
        public int Denominator;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public SelfCurerEvent() { }

        /// <summary>
        /// Initializes a new instance with the specified ratio.
        /// </summary>
        /// <param name="num">The numerator of the reduction ratio.</param>
        /// <param name="den">The denominator of the reduction ratio.</param>
        public SelfCurerEvent(int num, int den) : this()
        {
            Numerator = num;
            Denominator = den;
        }

        /// <summary>
        /// Copy constructor for cloning an existing SelfCurerEvent.
        /// </summary>
        protected SelfCurerEvent(SelfCurerEvent other) : this()
        {
            Numerator = other.Numerator;
            Denominator = other.Denominator;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() => new SelfCurerEvent(this);

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            //multiply turns, rounded down
            CountDownState countDown = context.Status.StatusStates.GetWithDefault<CountDownState>();
            if (countDown != null && context.Status.StatusStates.Contains<BadStatusState>())
            {
                int minCounter = Math.Min(2, countDown.Counter);
                countDown.Counter *= Numerator;
                countDown.Counter /= Denominator;
                if (countDown.Counter < minCounter)
                    countDown.Counter = minCounter;
            }
            yield break;
        }

    }

    /// <summary>
    /// Status given event that prevents duplicate statuses from being applied.
    /// Cancels the status application if the same status already exists.
    /// </summary>
    [Serializable]
    public class SameStatusCheck : StatusGivenEvent
    {
        /// <summary>
        /// The message to display when the status is blocked.
        /// </summary>
        [StringKey(0, true)]
        public StringKey Message;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public SameStatusCheck() { }

        /// <summary>
        /// Initializes a new instance with the specified message.
        /// </summary>
        /// <param name="message">The message to display when blocked.</param>
        public SameStatusCheck(StringKey message)
        {
            Message = message;
        }

        /// <summary>
        /// Copy constructor for cloning an existing SameStatusCheck.
        /// </summary>
        protected SameStatusCheck(SameStatusCheck other)
        {
            Message = other.Message;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() => new SameStatusCheck(this);

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (owner != context.Status)//can't check on self
            {
                if (context.Status.ID == ((StatusEffect)owner).ID)
                {
                    if (context.msg && Message.IsValid())
                        DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.Target.GetDisplayName(false), owner.GetDisplayName()));
                    context.CancelState.Cancel = true;
                }
            }
            yield break;
        }
    }
    /// <summary>
    /// Status given event that prevents duplicate targeted statuses based on target reference.
    /// Cancels application if the same status targeting the same character already exists.
    /// </summary>
    [Serializable]
    public class SameTargetedStatusCheck : StatusGivenEvent
    {
        /// <summary>
        /// The message to display when the status is blocked.
        /// </summary>
        [StringKey(0, true)]
        public StringKey Message;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public SameTargetedStatusCheck() { }

        /// <summary>
        /// Initializes a new instance with the specified message.
        /// </summary>
        /// <param name="message">The message to display when blocked.</param>
        public SameTargetedStatusCheck(StringKey message)
        {
            Message = message;
        }

        /// <summary>
        /// Copy constructor for cloning an existing SameTargetedStatusCheck.
        /// </summary>
        protected SameTargetedStatusCheck(SameTargetedStatusCheck other)
        {
            Message = other.Message;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() => new SameTargetedStatusCheck(this);

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (owner != context.Status)//can't check on self
            {
                if (context.Status.ID == ((StatusEffect)owner).ID)
                {
                    if (context.msg && Message.IsValid() && ((StatusEffect)owner).TargetChar != null)
                        DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.Target.GetDisplayName(false), ((StatusEffect)owner).TargetChar.GetDisplayName(false)));
                    context.CancelState.Cancel = true;
                }
            }
            yield break;
        }
    }
    /// <summary>
    /// Status given event that prevents major statuses when one already exists.
    /// Cancels application if the target already has any MajorStatusState.
    /// </summary>
    [Serializable]
    public class OKStatusCheck : StatusGivenEvent
    {
        /// <summary>
        /// The message to display when the status is blocked.
        /// </summary>
        [StringKey(0, true)]
        public StringKey Message;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public OKStatusCheck() { }

        /// <summary>
        /// Initializes a new instance with the specified message.
        /// </summary>
        /// <param name="message">The message to display when blocked.</param>
        public OKStatusCheck(StringKey message)
        {
            Message = message;
        }

        /// <summary>
        /// Copy constructor for cloning an existing OKStatusCheck.
        /// </summary>
        protected OKStatusCheck(OKStatusCheck other)
        {
            Message = other.Message;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() => new OKStatusCheck(this);

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (context.Status == owner)//this check is done BY the pending status only
            {
                foreach (StatusEffect status in context.Target.IterateStatusEffects())
                {
                    if (status.StatusStates.Contains<MajorStatusState>())
                    {
                        if (context.msg && Message.IsValid())
                            DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.Target.GetDisplayName(false)));
                        context.CancelState.Cancel = true;
                        yield break;
                    }
                }
            }
        }
    }
    /// <summary>
    /// Status given event that prevents slot-based statuses when the slot has no move.
    /// Cancels application if the target skill slot is empty.
    /// </summary>
    [Serializable]
    public class EmptySlotStatusCheck : StatusGivenEvent
    {
        /// <summary>
        /// The message to display when the status is blocked.
        /// </summary>
        [StringKey(0, true)]
        public StringKey Message;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public EmptySlotStatusCheck() { }

        /// <summary>
        /// Initializes a new instance with the specified message.
        /// </summary>
        /// <param name="message">The message to display when blocked.</param>
        public EmptySlotStatusCheck(StringKey message)
        {
            Message = message;
        }

        /// <summary>
        /// Copy constructor for cloning an existing EmptySlotStatusCheck.
        /// </summary>
        protected EmptySlotStatusCheck(EmptySlotStatusCheck other)
        {
            Message = other.Message;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() => new EmptySlotStatusCheck(this);

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (context.Status == owner)//this check is done BY the pending status only
            {
                int slot = context.Status.StatusStates.GetWithDefault<SlotState>().Slot;
                if (String.IsNullOrEmpty(context.Target.Skills[slot].Element.SkillNum))
                {
                    if (context.msg && Message.IsValid())
                        DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.Target.GetDisplayName(false)));
                    context.CancelState.Cancel = true;
                    yield break;
                }
            }
        }
    }
    /// <summary>
    /// Status given event that prevents status application based on gender compatibility.
    /// Cancels if target and user have incompatible genders for the effect.
    /// </summary>
    [Serializable]
    public class GenderStatusCheck : StatusGivenEvent
    {
        /// <summary>
        /// The message to display when the status is blocked.
        /// </summary>
        [StringKey(0, true)]
        public StringKey Message;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public GenderStatusCheck() { }

        /// <summary>
        /// Initializes a new instance with the specified message.
        /// </summary>
        /// <param name="message">The message to display when blocked.</param>
        public GenderStatusCheck(StringKey message)
        {
            Message = message;
        }

        /// <summary>
        /// Copy constructor for cloning an existing GenderStatusCheck.
        /// </summary>
        protected GenderStatusCheck(GenderStatusCheck other)
        {
            Message = other.Message;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() => new GenderStatusCheck(this);

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (context.Status == owner)//this check is done BY the pending status only
            {
                if ((context.Target.CurrentForm.Gender == Gender.Genderless) != (context.Target.CurrentForm.Gender == context.User.CurrentForm.Gender))
                {
                    if (context.msg && Message.IsValid())
                        DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.Target.GetDisplayName(false), context.User.GetDisplayName(false)));
                    context.CancelState.Cancel = true;
                }
            }
            yield break;
        }
    }

    /// <summary>
    /// Status given event that prevents status application based on elemental type.
    /// Cancels if the target has the specified elemental type.
    /// </summary>
    [Serializable]
    public class TypeCheck : StatusGivenEvent
    {
        /// <summary>
        /// The element type ID that blocks this status.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string Element;

        /// <summary>
        /// The message to display when the status is blocked.
        /// </summary>
        [StringKey(0, true)]
        public StringKey Message;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public TypeCheck() { Element = ""; }

        /// <summary>
        /// Initializes a new instance with the specified parameters.
        /// </summary>
        /// <param name="element">The element type that blocks this status.</param>
        /// <param name="message">The message to display when blocked.</param>
        public TypeCheck(string element, StringKey message)
        {
            Element = element;
            Message = message;
        }

        /// <summary>
        /// Copy constructor for cloning an existing TypeCheck.
        /// </summary>
        protected TypeCheck(TypeCheck other)
        {
            Element = other.Element;
            Message = other.Message;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() => new TypeCheck(this);

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (context.Status == owner)//this check is done BY the pending status only
            {
                if (context.Target.HasElement(Element))
                {
                    if (context.msg && Message.IsValid())
                        DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.Target.GetDisplayName(false)));
                    context.CancelState.Cancel = true;
                }
            }
            yield break;
        }
    }
    /// <summary>
    /// Status given event that prevents a specific status from being applied.
    /// Used for immunities against specific status conditions.
    /// </summary>
    [Serializable]
    public class PreventStatusCheck : StatusGivenEvent
    {
        /// <summary>
        /// The status ID to block.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string StatusID;

        /// <summary>
        /// Message to display when blocking the status.
        /// </summary>
        [StringKey(0, true)]
        public StringKey Message;

        /// <summary>
        /// Animations to play when blocking the status.
        /// </summary>
        public List<StatusAnimEvent> Anims;

        public PreventStatusCheck()
        {
            Anims = new List<StatusAnimEvent>();
            StatusID = "";
        }
        public PreventStatusCheck(string statusID, StringKey message)
        {
            StatusID = statusID;
            Message = message;
            Anims = new List<StatusAnimEvent>();
        }
        public PreventStatusCheck(string statusID, StringKey message, params StatusAnimEvent[] anims)
        {
            StatusID = statusID;
            Message = message;

            Anims = new List<StatusAnimEvent>();
            Anims.AddRange(anims);
        }
        protected PreventStatusCheck(PreventStatusCheck other)
        {
            StatusID = other.StatusID;
            Message = other.Message;

            Anims = new List<StatusAnimEvent>();
            foreach (StatusAnimEvent anim in other.Anims)
                Anims.Add((StatusAnimEvent)anim.Clone());
        }
        public override GameEvent Clone() { return new PreventStatusCheck(this); }

        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (owner != context.Status)//can't check on self
            {
                if (StatusID == context.Status.ID)
                {
                    if (context.msg)
                    {
                        if (Message.IsValid())
                            DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.Target.GetDisplayName(false), owner.GetDisplayName()));

                        foreach (StatusAnimEvent anim in Anims)
                            yield return CoroutineManager.Instance.StartCoroutine(anim.Apply(owner, ownerChar, context));

                    }
                    context.CancelState.Cancel = true;
                }
            }
            yield break;
        }
    }

    /// <summary>
    /// Status given event that prevents status effects with specific state types from being applied.
    /// Used for immunities based on status categories (like all bad statuses).
    /// </summary>
    [Serializable]
    public class StateStatusCheck : StatusGivenEvent
    {
        /// <summary>
        /// List of StatusState types that trigger the prevention.
        /// </summary>
        [StringTypeConstraint(1, typeof(StatusState))]
        public List<FlagType> States;

        /// <summary>
        /// Message to display when blocking the status.
        /// </summary>
        [StringKey(0, true)]
        public StringKey Message;

        /// <summary>
        /// Animations to play when blocking the status.
        /// </summary>
        public List<StatusAnimEvent> Anims;

        public StateStatusCheck()
        {
            States = new List<FlagType>();
            Anims = new List<StatusAnimEvent>();
        }
        public StateStatusCheck(Type state, StringKey message) : this()
        {
            States.Add(new FlagType(state));
            Message = message;
        }
        public StateStatusCheck(Type state, StringKey message, params StatusAnimEvent[] anims) : this()
        {
            States.Add(new FlagType(state));
            Message = message;
            Anims.AddRange(anims);
        }
        protected StateStatusCheck(StateStatusCheck other)
        {
            States.AddRange(other.States);
            Message = other.Message;
            Anims = new List<StatusAnimEvent>();
            foreach (StatusAnimEvent anim in other.Anims)
                Anims.Add((StatusAnimEvent)anim.Clone());
        }
        public override GameEvent Clone() { return new StateStatusCheck(this); }

        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (owner != context.Status)//can't check on self
            {
                bool hasState = false;
                foreach (FlagType state in States)
                {
                    if (context.Status.StatusStates.Contains(state.FullType))
                        hasState = true;
                }
                if (hasState)
                {
                    if (context.msg && Message.IsValid())
                    {
                        DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.Target.GetDisplayName(false), owner.GetDisplayName()));

                        foreach (StatusAnimEvent anim in Anims)
                            yield return CoroutineManager.Instance.StartCoroutine(anim.Apply(owner, ownerChar, context));

                    }
                    context.CancelState.Cancel = true;
                }
            }
            yield break;
        }

    }


    /// <summary>
    /// Abstract base class for events that intercept stat change statuses.
    /// Used for abilities that prevent or reflect stat changes.
    /// </summary>
    [Serializable]
    public abstract class StatChangeCheckBase : StatusGivenEvent
    {
        /// <summary>
        /// The message to display when blocking stat changes.
        /// </summary>
        [StringKey(0, true)]
        public StringKey Message;

        /// <summary>
        /// List of stats this check applies to. Empty means all stats.
        /// </summary>
        public List<Stat> Stats;

        /// <summary>
        /// Whether to trigger on stat drops (negative changes).
        /// </summary>
        public bool Drop;

        /// <summary>
        /// Whether to trigger on stat boosts (positive changes).
        /// </summary>
        public bool Boost;

        /// <summary>
        /// Whether to trigger when the user targets themselves.
        /// </summary>
        public bool IncludeSelf;

        /// <summary>
        /// Animations to play when blocking.
        /// </summary>
        public List<StatusAnimEvent> Anims;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public StatChangeCheckBase()
        {
            Stats = new List<Stat>();
            Anims = new List<StatusAnimEvent>();
        }

        /// <summary>
        /// Initializes a new instance with the specified parameters.
        /// </summary>
        /// <param name="stats">The stats to check.</param>
        /// <param name="message">The message to display when blocking.</param>
        /// <param name="drop">Whether to trigger on drops.</param>
        /// <param name="boost">Whether to trigger on boosts.</param>
        /// <param name="includeSelf">Whether to trigger on self-targeting.</param>
        public StatChangeCheckBase(List<Stat> stats, StringKey message, bool drop, bool boost, bool includeSelf)
        {
            Stats = stats;
            Message = message;
            Drop = drop;
            Boost = boost;
            IncludeSelf = includeSelf;
            Anims = new List<StatusAnimEvent>();
        }

        /// <summary>
        /// Initializes a new instance with the specified parameters and animations.
        /// </summary>
        /// <param name="stats">The stats to check.</param>
        /// <param name="message">The message to display when blocking.</param>
        /// <param name="drop">Whether to trigger on drops.</param>
        /// <param name="boost">Whether to trigger on boosts.</param>
        /// <param name="includeSelf">Whether to trigger on self-targeting.</param>
        /// <param name="anims">The animations to play.</param>
        public StatChangeCheckBase(List<Stat> stats, StringKey message, bool drop, bool boost, bool includeSelf, params StatusAnimEvent[] anims)
        {
            Stats = stats;
            Message = message;
            Drop = drop;
            Boost = boost;
            IncludeSelf = includeSelf;
            Anims = new List<StatusAnimEvent>();
            Anims.AddRange(anims);
        }

        /// <summary>
        /// Copy constructor for cloning an existing StatChangeCheckBase.
        /// </summary>
        protected StatChangeCheckBase(StatChangeCheckBase other)
        {
            Message = other.Message;
            Stats = new List<Stat>();
            Stats.AddRange(other.Stats);
            Drop = other.Drop;
            Boost = other.Boost;
            IncludeSelf = other.IncludeSelf;
            Anims = new List<StatusAnimEvent>();
            foreach (StatusAnimEvent anim in other.Anims)
                Anims.Add((StatusAnimEvent)anim.Clone());
        }

        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (owner != context.Status)//can't check on self
            {
                StatChangeState statChange = context.Status.StatusStates.GetWithDefault<StatChangeState>();
                if (statChange != null && (context.User != context.Target || IncludeSelf))
                {
                    bool block = false;
                    int delta = context.Status.StatusStates.GetWithDefault<StackState>().Stack;
                    if (delta < 0 && Drop || delta > 0 && Boost)
                    {
                        if (Stats.Count == 0)
                            block = true;
                        else
                        {
                            foreach (Stat statType in Stats)
                            {
                                if (statType == statChange.ChangeStat)
                                    block = true;
                            }
                        }
                    }
                    if (block)
                        yield return CoroutineManager.Instance.StartCoroutine(BlockEffect(owner, ownerChar, context));
                }
            }
            yield break;
        }

        /// <summary>
        /// Executes the blocking effect when conditions are met.
        /// </summary>
        /// <param name="owner">The ability or effect that owns this event.</param>
        /// <param name="ownerChar">The character who has the ability.</param>
        /// <param name="context">The status check context.</param>
        /// <returns>A coroutine for the block effect.</returns>
        protected abstract IEnumerator<YieldInstruction> BlockEffect(GameEventOwner owner, Character ownerChar, StatusCheckContext context);
    }

    /// <summary>
    /// Stat change check that cancels the stat change completely.
    /// Used for abilities like Clear Body that prevent stat drops.
    /// </summary>
    [Serializable]
    public class StatChangeCheck : StatChangeCheckBase
    {
        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public StatChangeCheck()
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified parameters.
        /// </summary>
        public StatChangeCheck(List<Stat> stats, StringKey message, bool drop, bool boost, bool includeSelf)
            : base(stats, message, drop, boost, includeSelf)
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified parameters and animations.
        /// </summary>
        public StatChangeCheck(List<Stat> stats, StringKey message, bool drop, bool boost, bool includeSelf, params StatusAnimEvent[] anims)
            : base(stats, message, drop, boost, includeSelf, anims)
        {
        }

        /// <summary>
        /// Copy constructor for cloning an existing StatChangeCheck.
        /// </summary>
        protected StatChangeCheck(StatChangeCheck other)
            : base(other)
        {
        }

        /// <inheritdoc/>
        public override GameEvent Clone() => new StatChangeCheck(this);

        /// <inheritdoc/>
        protected override IEnumerator<YieldInstruction> BlockEffect(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (context.msg && Message.IsValid())
            {
                StatChangeState statChange = context.Status.StatusStates.GetWithDefault<StatChangeState>();
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.Target.GetDisplayName(false), owner.GetDisplayName(), statChange.ChangeStat.ToLocal()));

                foreach (StatusAnimEvent anim in Anims)
                    yield return CoroutineManager.Instance.StartCoroutine(anim.Apply(owner, ownerChar, context));

            }
            context.CancelState.Cancel = true;
        }
    }


    /// <summary>
    /// Stat change check that reflects the stat change back to the user.
    /// Used for abilities like Magic Bounce that reflect stat drops.
    /// </summary>
    [Serializable]
    public class StatChangeReflect : StatChangeCheckBase
    {
        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public StatChangeReflect()
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified parameters.
        /// </summary>
        public StatChangeReflect(List<Stat> stats, StringKey message, bool drop, bool boost, bool includeSelf)
            : base(stats, message, drop, boost, includeSelf)
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified parameters and animations.
        /// </summary>
        public StatChangeReflect(List<Stat> stats, StringKey message, bool drop, bool boost, bool includeSelf, params StatusAnimEvent[] anims)
            : base(stats, message, drop, boost, includeSelf, anims)
        {
        }

        /// <summary>
        /// Copy constructor for cloning an existing StatChangeReflect.
        /// </summary>
        protected StatChangeReflect(StatChangeReflect other)
            : base(other)
        {
        }

        /// <inheritdoc/>
        public override GameEvent Clone() => new StatChangeReflect(this);

        /// <inheritdoc/>
        protected override IEnumerator<YieldInstruction> BlockEffect(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (context.User != null)
            {
                if (context.msg && Message.IsValid())
                {
                    StatChangeState statChange = context.Status.StatusStates.GetWithDefault<StatChangeState>();
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.Target.GetDisplayName(false), owner.GetDisplayName(), statChange.ChangeStat.ToLocal()));

                    foreach (StatusAnimEvent anim in Anims)
                        yield return CoroutineManager.Instance.StartCoroutine(anim.Apply(owner, ownerChar, context));

                }
                yield return CoroutineManager.Instance.StartCoroutine(context.User.AddStatusEffect(null, context.Status, null, false, true));
                context.CancelState.Cancel = true;
            }
        }
    }


    /// <summary>
    /// Status given event that prevents all status effects from being applied.
    /// Used for abilities that provide complete status immunity.
    /// </summary>
    [Serializable]
    public class PreventAnyStatusCheck : StatusGivenEvent
    {
        /// <summary>
        /// The message to display when blocking status effects.
        /// </summary>
        public StringKey Message;

        /// <summary>
        /// Animations to play when blocking.
        /// </summary>
        public List<StatusAnimEvent> Anims;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public PreventAnyStatusCheck()
        {
            Anims = new List<StatusAnimEvent>();
        }

        /// <summary>
        /// Initializes a new instance with the specified message and animations.
        /// </summary>
        /// <param name="message">The message to display when blocking.</param>
        /// <param name="anims">The animations to play.</param>
        public PreventAnyStatusCheck(StringKey message, params StatusAnimEvent[] anims)
        {
            Message = message;
            Anims = new List<StatusAnimEvent>();
            Anims.AddRange(anims);
        }

        /// <summary>
        /// Copy constructor for cloning an existing PreventAnyStatusCheck.
        /// </summary>
        protected PreventAnyStatusCheck(PreventAnyStatusCheck other)
        {
            Message = other.Message;
            Anims = new List<StatusAnimEvent>();
            foreach (StatusAnimEvent anim in other.Anims)
                Anims.Add((StatusAnimEvent)anim.Clone());
        }
        public override GameEvent Clone() { return new PreventAnyStatusCheck(this); }

        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (owner != context.Status)//can't check on self
            {
                string index = ((StatusEffect)owner).StatusStates.GetWithDefault<IDState>().ID;
                if (index == context.Status.ID)
                {
                    if (context.msg)
                    {
                        DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.Target.GetDisplayName(false), DataManager.Instance.GetStatus(index).GetColoredName()));

                        foreach (StatusAnimEvent anim in Anims)
                            yield return CoroutineManager.Instance.StartCoroutine(anim.Apply(owner, ownerChar, context));

                    }
                    context.CancelState.Cancel = true;
                }
            }
            yield break;
        }
    }




    /// <summary>
    /// Status given event that adds a context state to the status check context.
    /// Used to modify the behavior of subsequent status checks within a context.
    /// </summary>
    [Serializable]
    public class AddStatusContextStateEvent : StatusGivenEvent
    {
        /// <summary>
        /// The context state to add to the check context.
        /// </summary>
        public ContextState AddedState;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public AddStatusContextStateEvent() { }

        /// <summary>
        /// Initializes a new instance with the specified context state.
        /// </summary>
        /// <param name="state">The context state to add.</param>
        public AddStatusContextStateEvent(ContextState state) { AddedState = state; }

        /// <summary>
        /// Copy constructor for cloning an existing AddStatusContextStateEvent.
        /// </summary>
        protected AddStatusContextStateEvent(AddStatusContextStateEvent other)
        {
            AddedState = other.AddedState.Clone<ContextState>();
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new AddStatusContextStateEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            context.ContextStates.Set(AddedState.Clone<ContextState>());
            yield break;
        }
    }

    /// <summary>
    /// Status given event that executes only when specific context states are NOT present.
    /// Used for exceptions to status effect rules based on context.
    /// </summary>
    [Serializable]
    public class ExceptionStatusContextEvent : StatusGivenEvent
    {
        /// <summary>
        /// List of context states that prevent the base event from executing.
        /// </summary>
        [StringTypeConstraint(1, typeof(ContextState))]
        public List<FlagType> States;

        /// <summary>
        /// The event to execute when no exception states are present.
        /// </summary>
        public StatusGivenEvent BaseEvent;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public ExceptionStatusContextEvent() { States = new List<FlagType>(); }

        /// <summary>
        /// Initializes a new instance with the specified context state and base event.
        /// </summary>
        /// <param name="state">The context state that prevents execution.</param>
        /// <param name="baseEffect">The event to execute when the state is absent.</param>
        public ExceptionStatusContextEvent(Type state, StatusGivenEvent baseEffect) : this() { States.Add(new FlagType(state)); BaseEvent = baseEffect; }

        /// <summary>
        /// Copy constructor for cloning an existing ExceptionStatusContextEvent.
        /// </summary>
        protected ExceptionStatusContextEvent(ExceptionStatusContextEvent other) : this()
        {
            States.AddRange(other.States);
            BaseEvent = (StatusGivenEvent)other.BaseEvent.Clone();
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ExceptionStatusContextEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            bool hasState = false;
            foreach (FlagType state in States)
            {
                if (context.ContextStates.Contains(state.FullType))
                    hasState = true;
            }
            if (!hasState)
                yield return CoroutineManager.Instance.StartCoroutine(BaseEvent.Apply(owner, ownerChar, context));
        }

    }


    /// <summary>
    /// Status given event that executes only when the Infiltrator ability state is NOT present.
    /// Used for effects that are blocked by the Infiltrator ability.
    /// </summary>
    [Serializable]
    public class ExceptInfiltratorStatusEvent : StatusGivenEvent
    {
        /// <summary>
        /// The event to execute when Infiltrator is not present.
        /// </summary>
        public StatusGivenEvent BaseEvent;

        /// <summary>
        /// Whether to show the Infiltrator exception message when blocked.
        /// </summary>
        public bool ExceptionMsg;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public ExceptInfiltratorStatusEvent() { }

        /// <summary>
        /// Initializes a new instance with the specified base event and exception message flag.
        /// </summary>
        /// <param name="exceptionMsg">Whether to display the exception message.</param>
        /// <param name="baseEffect">The event to execute when Infiltrator is absent.</param>
        public ExceptInfiltratorStatusEvent(bool exceptionMsg, StatusGivenEvent baseEffect) { BaseEvent = baseEffect; ExceptionMsg = exceptionMsg; }

        /// <summary>
        /// Copy constructor for cloning an existing ExceptInfiltratorStatusEvent.
        /// </summary>
        protected ExceptInfiltratorStatusEvent(ExceptInfiltratorStatusEvent other)
        {
            BaseEvent = (StatusGivenEvent)other.BaseEvent.Clone();
            ExceptionMsg = other.ExceptionMsg;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ExceptInfiltratorStatusEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            Infiltrator state = context.ContextStates.GetWithDefault<Infiltrator>();
            if (state == null)
                yield return CoroutineManager.Instance.StartCoroutine(BaseEvent.Apply(owner, ownerChar, context));
            else if (ExceptionMsg && state.Msg.IsValid())
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(state.Msg.ToLocal(), context.User.GetDisplayName(false), owner.GetDisplayName()));
        }
    }



    /// <summary>
    /// Status given event that replaces a major status effect on the target when applied.
    /// Removes any existing major status when a new one is being added.
    /// </summary>
    [Serializable]
    public class ReplaceMajorStatusEvent : StatusGivenEvent
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public ReplaceMajorStatusEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ReplaceMajorStatusEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (context.Status == owner)
                yield break;

            if (context.Status.StatusStates.GetWithDefault<MajorStatusState>() != null)
                yield return CoroutineManager.Instance.StartCoroutine(context.Target.RemoveStatusEffect(((StatusEffect)owner).ID, false));
        }
    }

    /// <summary>
    /// Status given event that logs a message to the battle log when the status is applied.
    /// </summary>
    [Serializable]
    public class StatusBattleLogEvent : StatusGivenEvent
    {
        /// <summary>
        /// The message to display in the battle log.
        /// </summary>
        public StringKey Message;

        /// <summary>
        /// Whether to add a frame delay after displaying the message.
        /// </summary>
        public bool Delay;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public StatusBattleLogEvent() { }

        /// <summary>
        /// Initializes a new instance with the specified message.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public StatusBattleLogEvent(StringKey message) : this(message, false) { }

        /// <summary>
        /// Initializes a new instance with the specified message and delay flag.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="delay">Whether to add a delay after the message.</param>
        public StatusBattleLogEvent(StringKey message, bool delay)
        {
            Message = message;
            Delay = delay;
        }

        /// <summary>
        /// Copy constructor for cloning an existing StatusBattleLogEvent.
        /// </summary>
        protected StatusBattleLogEvent(StatusBattleLogEvent other)
        {
            Message = other.Message;
            Delay = other.Delay;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new StatusBattleLogEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (context.Status != owner)
                yield break;

            if (context.msg)
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.Target.GetDisplayName(false), owner.GetDisplayName()));
                if (Delay)
                    yield return new WaitForFrames(GameManager.Instance.ModifyBattleSpeed(10));
            }
        }
    }


    /// <summary>
    /// Status given event that causes a character to faint when the countdown expires.
    /// Used for effects like Perish Song that defeat the character after a duration.
    /// </summary>
    [Serializable]
    public class PerishStatusEvent : StatusGivenEvent
    {
        /// <summary>
        /// The message to display about the countdown.
        /// </summary>
        public StringKey Message;

        /// <summary>
        /// Whether to add a frame delay after displaying the message.
        /// </summary>
        public bool Delay;

        /// <summary>
        /// Animations to play when the character faints.
        /// </summary>
        public List<StatusAnimEvent> Anims;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public PerishStatusEvent()
        {
            Anims = new List<StatusAnimEvent>();
        }

        /// <summary>
        /// Initializes a new instance with the specified message, delay, and animations.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="delay">Whether to add a delay after the message.</param>
        /// <param name="anims">Animations to play when the character faints.</param>
        public PerishStatusEvent(StringKey message, bool delay, params StatusAnimEvent[] anims)
        {
            Message = message;
            Delay = delay;
            Anims = new List<StatusAnimEvent>();
            Anims.AddRange(anims);
        }

        /// <summary>
        /// Copy constructor for cloning an existing PerishStatusEvent.
        /// </summary>
        protected PerishStatusEvent(PerishStatusEvent other)
        {
            Message = other.Message;
            Delay = other.Delay;
            Anims = new List<StatusAnimEvent>();
            foreach (StatusAnimEvent anim in other.Anims)
                Anims.Add((StatusAnimEvent)anim.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new PerishStatusEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (context.Status != owner)
                yield break;
            if (context.User.Dead)
                yield break;
            CountDownState counter = ((StatusEffect)owner).StatusStates.GetWithDefault<CountDownState>();

            DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.Target.GetDisplayName(false), counter.Counter));
            if (Delay)
                yield return new WaitForFrames(GameManager.Instance.ModifyBattleSpeed(10));

            if (counter.Counter <= 0)
            {
                yield return CoroutineManager.Instance.StartCoroutine(context.Target.RemoveStatusEffect(((StatusEffect)owner).ID, false));

                foreach (StatusAnimEvent anim in Anims)
                    yield return CoroutineManager.Instance.StartCoroutine(anim.Apply(owner, ownerChar, context));
                GameManager.Instance.BattleSE("DUN_Hit_Super_Effective");
                yield return CoroutineManager.Instance.StartCoroutine(context.Target.InflictDamage(-1));
            }
        }
    }

    /// <summary>
    /// Status given event that executes only when a specific weather condition is active.
    /// Used for weather-dependent status effects.
    /// </summary>
    [Serializable]
    public class WeatherNeededStatusEvent : StatusGivenEvent
    {
        /// <summary>
        /// The required weather status ID for the effects to execute.
        /// </summary>
        [JsonConverter(typeof(MapStatusConverter))]
        [DataType(0, DataManager.DataType.MapStatus, false)]
        public string WeatherID;

        /// <summary>
        /// The events to execute when the required weather is present.
        /// </summary>
        public List<StatusGivenEvent> BaseEvents;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public WeatherNeededStatusEvent() { BaseEvents = new List<StatusGivenEvent>(); WeatherID = ""; }

        /// <summary>
        /// Initializes a new instance with the specified weather ID and effects.
        /// </summary>
        /// <param name="id">The weather status ID required.</param>
        /// <param name="effects">The events to execute when the weather is active.</param>
        public WeatherNeededStatusEvent(string id, params StatusGivenEvent[] effects)
            : this()
        {
            WeatherID = id;
            foreach (StatusGivenEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <summary>
        /// Copy constructor for cloning an existing WeatherNeededStatusEvent.
        /// </summary>
        protected WeatherNeededStatusEvent(WeatherNeededStatusEvent other) : this()
        {
            WeatherID = other.WeatherID;
            foreach (StatusGivenEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((StatusGivenEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new WeatherNeededStatusEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (ZoneManager.Instance.CurrentMap.Status.ContainsKey(WeatherID))
            {
                foreach (StatusGivenEvent battleEffect in BaseEvents)
                    yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
            }
        }
    }

    /// <summary>
    /// Status given event that executes only when the character is in a specific form.
    /// Used for form-specific status effects.
    /// </summary>
    [Serializable]
    public class FormeNeededStatusEvent : StatusGivenEvent
    {
        /// <summary>
        /// The set of form indices that allow the event to execute.
        /// </summary>
        public HashSet<int> Forms;

        /// <summary>
        /// The events to execute when the character is in a valid form.
        /// </summary>
        public List<StatusGivenEvent> BaseEvents;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public FormeNeededStatusEvent() { Forms = new HashSet<int>(); BaseEvents = new List<StatusGivenEvent>(); }

        /// <summary>
        /// Initializes a new instance with the specified event and forms.
        /// </summary>
        /// <param name="effects">The event to execute when in a valid form.</param>
        /// <param name="forms">The form indices that allow execution.</param>
        public FormeNeededStatusEvent(StatusGivenEvent effects, params int[] forms)
            : this()
        {
            BaseEvents.Add(effects);
            foreach (int form in forms)
                Forms.Add(form);
        }

        /// <summary>
        /// Copy constructor for cloning an existing FormeNeededStatusEvent.
        /// </summary>
        protected FormeNeededStatusEvent(FormeNeededStatusEvent other) : this()
        {
            foreach (int form in other.Forms)
                Forms.Add(form);
            foreach (StatusGivenEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((StatusGivenEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new FormeNeededStatusEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (Forms.Contains(ownerChar.CurrentForm.Form))
            {
                foreach (StatusGivenEvent battleEffect in BaseEvents)
                    yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
            }
        }
    }

    /// <summary>
    /// Status given event that plays a visual animation and sound effect when the status is applied.
    /// </summary>
    [Serializable]
    public class StatusAnimEvent : StatusGivenEvent
    {
        /// <summary>
        /// The particle emitter to display during the animation.
        /// </summary>
        public FiniteEmitter Emitter;

        /// <summary>
        /// The sound effect to play during the animation.
        /// </summary>
        [Sound(0)]
        public string Sound;

        /// <summary>
        /// The frame delay before playing the animation.
        /// </summary>
        public int Delay;

        /// <summary>
        /// Whether this animation should only play if triggered by the owning status itself.
        /// </summary>
        public bool NeedSelf;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public StatusAnimEvent()
        {
            Emitter = new EmptyFiniteEmitter();
        }

        /// <summary>
        /// Initializes a new instance with the specified emitter, sound, and delay.
        /// </summary>
        /// <param name="emitter">The particle emitter.</param>
        /// <param name="sound">The sound effect to play.</param>
        /// <param name="delay">The frame delay.</param>
        public StatusAnimEvent(FiniteEmitter emitter, string sound, int delay) : this(emitter, sound, delay, false) { }

        /// <summary>
        /// Initializes a new instance with all parameters.
        /// </summary>
        /// <param name="emitter">The particle emitter.</param>
        /// <param name="sound">The sound effect to play.</param>
        /// <param name="delay">The frame delay.</param>
        /// <param name="needSelf">Whether animation is only for self-triggered events.</param>
        public StatusAnimEvent(FiniteEmitter emitter, string sound, int delay, bool needSelf)
        {
            Emitter = emitter;
            Sound = sound;
            Delay = delay;
            NeedSelf = needSelf;
        }

        /// <summary>
        /// Copy constructor for cloning an existing StatusAnimEvent.
        /// </summary>
        protected StatusAnimEvent(StatusAnimEvent other)
        {
            Emitter = (FiniteEmitter)other.Emitter.Clone();
            Sound = other.Sound;
            Delay = other.Delay;
            NeedSelf = other.NeedSelf;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new StatusAnimEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (NeedSelf && context.Status != owner || !context.msg)
                yield break;

            GameManager.Instance.BattleSE(Sound);
            if (!context.Target.Unidentifiable)
            {
                FiniteEmitter endEmitter = (FiniteEmitter)Emitter.Clone();
                endEmitter.SetupEmit(context.Target.MapLoc, context.Target.MapLoc, context.Target.CharDir);
                DungeonScene.Instance.CreateAnim(endEmitter, DrawLayer.NoDraw);
            }
            yield return new WaitForFrames(GameManager.Instance.ModifyBattleSpeed(Delay));
        }
    }

    /// <summary>
    /// Status given event that plays a character animation when the status is applied.
    /// Used for visual character actions during status application.
    /// </summary>
    [Serializable]
    public class StatusCharAnimEvent : StatusGivenEvent
    {
        /// <summary>
        /// The character animation data to play.
        /// </summary>
        public CharAnimData CharAnim;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public StatusCharAnimEvent()
        {
            CharAnim = new CharAnimFrameType();
        }

        /// <summary>
        /// Initializes a new instance with the specified character animation.
        /// </summary>
        /// <param name="charAnim">The character animation to play.</param>
        public StatusCharAnimEvent(CharAnimData charAnim)
        {
            CharAnim = charAnim;
        }

        /// <summary>
        /// Copy constructor for cloning an existing StatusCharAnimEvent.
        /// </summary>
        protected StatusCharAnimEvent(StatusCharAnimEvent other)
        {
            CharAnim = other.CharAnim;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new StatusCharAnimEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (context.Status != owner)
                yield break;

            StaticCharAnimation anim = CharAnim.GetCharAnim();
            anim.CharLoc = context.Target.CharLoc;
            anim.CharDir = context.Target.CharDir;

            yield return CoroutineManager.Instance.StartCoroutine(context.Target.StartAnim(anim));
        }
    }

    /// <summary>
    /// Status given event that plays an emote animation when the status is applied.
    /// Used for emotional reactions during status application.
    /// </summary>
    [Serializable]
    public class StatusEmoteEvent : StatusGivenEvent
    {
        /// <summary>
        /// The emote to display above the character.
        /// </summary>
        public EmoteFX Emote;

        /// <summary>
        /// Whether this emote should only play if triggered by the owning status itself.
        /// </summary>
        public bool NeedSelf;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public StatusEmoteEvent()
        { }

        /// <summary>
        /// Initializes a new instance with the specified emote.
        /// </summary>
        /// <param name="emote">The emote to display.</param>
        public StatusEmoteEvent(EmoteFX emote) : this(emote, false) { }

        /// <summary>
        /// Initializes a new instance with the specified emote and self-trigger flag.
        /// </summary>
        /// <param name="emote">The emote to display.</param>
        /// <param name="needSelf">Whether emote is only for self-triggered events.</param>
        public StatusEmoteEvent(EmoteFX emote, bool needSelf)
        {
            Emote = emote;
            NeedSelf = needSelf;
        }

        /// <summary>
        /// Copy constructor for cloning an existing StatusEmoteEvent.
        /// </summary>
        protected StatusEmoteEvent(StatusEmoteEvent other)
        {
            Emote = new EmoteFX(other.Emote);
            NeedSelf = other.NeedSelf;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new StatusEmoteEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (NeedSelf && context.Status != owner || !context.msg)
                yield break;

            yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.ProcessEmoteFX(context.Target, Emote));
        }
    }

    /// <summary>
    /// Status given event that removes a specific status from the target character.
    /// Used for sync effects like Destiny Knot that transfer status between partners.
    /// </summary>
    [Serializable]
    public class RemoveTargetStatusEvent : StatusGivenEvent
    {
        /// <summary>
        /// The status ID to remove from the target.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string StatusID;

        /// <summary>
        /// Whether to show a message when removing the status.
        /// </summary>
        public bool ShowMessage;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public RemoveTargetStatusEvent() { StatusID = ""; }

        /// <summary>
        /// Initializes a new instance with the specified status ID and message flag.
        /// </summary>
        /// <param name="statusID">The status to remove.</param>
        /// <param name="showMessage">Whether to display a removal message.</param>
        public RemoveTargetStatusEvent(string statusID, bool showMessage)
        {
            StatusID = statusID;
            ShowMessage = showMessage;
        }

        /// <summary>
        /// Copy constructor for cloning an existing RemoveTargetStatusEvent.
        /// </summary>
        protected RemoveTargetStatusEvent(RemoveTargetStatusEvent other)
        {
            StatusID = other.StatusID;
            ShowMessage = other.ShowMessage;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new RemoveTargetStatusEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (context.Status != owner)
                yield break;

            if (context.Status.TargetChar != null)
                yield return CoroutineManager.Instance.StartCoroutine(context.Status.TargetChar.RemoveStatusEffect(StatusID, false));
        }
    }

    /// <summary>
    /// Status given event that logs a targeted message mentioning both the target and another character.
    /// </summary>
    [Serializable]
    public class TargetedBattleLogEvent : StatusGivenEvent
    {
        /// <summary>
        /// The message to display in the battle log.
        /// </summary>
        public StringKey Message;

        /// <summary>
        /// Whether to add a frame delay after displaying the message.
        /// </summary>
        public bool Delay;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public TargetedBattleLogEvent() { }

        /// <summary>
        /// Initializes a new instance with the specified message.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public TargetedBattleLogEvent(StringKey message) : this(message, false) { }

        /// <summary>
        /// Initializes a new instance with the specified message and delay flag.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="delay">Whether to add a delay after the message.</param>
        public TargetedBattleLogEvent(StringKey message, bool delay)
        {
            Message = message;
            Delay = delay;
        }

        /// <summary>
        /// Copy constructor for cloning an existing TargetedBattleLogEvent.
        /// </summary>
        protected TargetedBattleLogEvent(TargetedBattleLogEvent other)
        {
            Message = other.Message;
            Delay = other.Delay;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new TargetedBattleLogEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (owner != context.Status)
                yield break;
            if (context.msg && ((StatusEffect)owner).TargetChar != null)
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.Target.GetDisplayName(false), ((StatusEffect)owner).TargetChar.GetDisplayName(false)));
                if (Delay)
                    yield return new WaitForFrames(GameManager.Instance.ModifyBattleSpeed(10));
            }
        }
    }

    /// <summary>
    /// Status given event that logs a message including the status category.
    /// </summary>
    [Serializable]
    public class StatusLogCategoryEvent : StatusGivenEvent
    {
        /// <summary>
        /// The message to display in the battle log.
        /// </summary>
        public StringKey Message;

        /// <summary>
        /// Whether to add a frame delay after displaying the message.
        /// </summary>
        public bool Delay;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public StatusLogCategoryEvent() { }

        /// <summary>
        /// Initializes a new instance with the specified message.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public StatusLogCategoryEvent(StringKey message) : this(message, false) { }

        /// <summary>
        /// Initializes a new instance with the specified message and delay flag.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="delay">Whether to add a delay after the message.</param>
        public StatusLogCategoryEvent(StringKey message, bool delay)
        {
            Message = message;
            Delay = delay;
        }

        /// <summary>
        /// Copy constructor for cloning an existing StatusLogCategoryEvent.
        /// </summary>
        protected StatusLogCategoryEvent(StatusLogCategoryEvent other)
        {
            Message = other.Message;
            Delay = other.Delay;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new StatusLogCategoryEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (context.Status != owner)
                yield break;
            if (context.msg)
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.Target.GetDisplayName(false), ((StatusEffect)owner).StatusStates.GetWithDefault<CategoryState>().Category.ToLocal()));
                if (Delay)
                    yield return new WaitForFrames(GameManager.Instance.ModifyBattleSpeed(10));
            }

        }
    }

    /// <summary>
    /// Status given event that logs a message including the status element type.
    /// </summary>
    [Serializable]
    public class StatusLogElementEvent : StatusGivenEvent
    {
        /// <summary>
        /// The message to display in the battle log.
        /// </summary>
        public StringKey Message;

        /// <summary>
        /// Whether to add a frame delay after displaying the message.
        /// </summary>
        public bool Delay;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public StatusLogElementEvent() { }

        /// <summary>
        /// Initializes a new instance with the specified message.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public StatusLogElementEvent(StringKey message) : this(message, false) { }

        /// <summary>
        /// Initializes a new instance with the specified message and delay flag.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="delay">Whether to add a delay after the message.</param>
        public StatusLogElementEvent(StringKey message, bool delay)
        {
            Message = message;
            Delay = delay;
        }

        /// <summary>
        /// Copy constructor for cloning an existing StatusLogElementEvent.
        /// </summary>
        protected StatusLogElementEvent(StatusLogElementEvent other)
        {
            Message = other.Message;
            Delay = other.Delay;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new StatusLogElementEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (context.Status != owner)
                yield break;

            if (context.msg)
            {
                string elementIndex = ((StatusEffect)owner).StatusStates.GetWithDefault<ElementState>().Element;
                ElementData elementData = DataManager.Instance.GetElement(elementIndex);
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.Target.GetDisplayName(false), elementData.GetIconName()));
                if (Delay)
                    yield return new WaitForFrames(GameManager.Instance.ModifyBattleSpeed(10));
            }

        }
    }

    /// <summary>
    /// Status given event that logs a message including the related status name.
    /// </summary>
    [Serializable]
    public class StatusLogStatusEvent : StatusGivenEvent
    {
        /// <summary>
        /// The message to display in the battle log.
        /// </summary>
        public StringKey Message;

        /// <summary>
        /// Whether to add a frame delay after displaying the message.
        /// </summary>
        public bool Delay;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public StatusLogStatusEvent() { }

        /// <summary>
        /// Initializes a new instance with the specified message.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public StatusLogStatusEvent(StringKey message) : this(message, false) { }

        /// <summary>
        /// Initializes a new instance with the specified message and delay flag.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="delay">Whether to add a delay after the message.</param>
        public StatusLogStatusEvent(StringKey message, bool delay)
        {
            Message = message;
            Delay = delay;
        }

        /// <summary>
        /// Copy constructor for cloning an existing StatusLogStatusEvent.
        /// </summary>
        protected StatusLogStatusEvent(StatusLogStatusEvent other)
        {
            Message = other.Message;
            Delay = other.Delay;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new StatusLogStatusEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (context.Status != owner)
                yield break;

            if (context.msg)
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.Target.GetDisplayName(false), DataManager.Instance.GetStatus(((StatusEffect)owner).StatusStates.GetWithDefault<IDState>().ID).GetColoredName()));
                if (Delay)
                    yield return new WaitForFrames(GameManager.Instance.ModifyBattleSpeed(10));
            }

        }
    }

    /// <summary>
    /// Status given event that logs a message including the status move slot number.
    /// </summary>
    [Serializable]
    public class StatusLogMoveSlotEvent : StatusGivenEvent
    {
        /// <summary>
        /// The message to display in the battle log.
        /// </summary>
        public StringKey Message;

        /// <summary>
        /// Whether to add a frame delay after displaying the message.
        /// </summary>
        public bool Delay;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public StatusLogMoveSlotEvent() { }

        /// <summary>
        /// Initializes a new instance with the specified message.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public StatusLogMoveSlotEvent(StringKey message) : this(message, false) { }

        /// <summary>
        /// Initializes a new instance with the specified message and delay flag.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="delay">Whether to add a delay after the message.</param>
        public StatusLogMoveSlotEvent(StringKey message, bool delay)
        {
            Message = message;
            Delay = delay;
        }

        /// <summary>
        /// Copy constructor for cloning an existing StatusLogMoveSlotEvent.
        /// </summary>
        protected StatusLogMoveSlotEvent(StatusLogMoveSlotEvent other)
        {
            Message = other.Message;
            Delay = other.Delay;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new StatusLogMoveSlotEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (context.Status != owner)
                yield break;

            if (context.msg)
            {
                int slot = ((StatusEffect)owner).StatusStates.GetWithDefault<SlotState>().Slot;
                SkillData entry = DataManager.Instance.GetSkill(context.Target.Skills[slot].Element.SkillNum);
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.Target.GetDisplayName(false), entry.GetIconName()));
                if (Delay)
                    yield return new WaitForFrames(GameManager.Instance.ModifyBattleSpeed(10));
            }

        }
    }

    /// <summary>
    /// Status given event that logs a message including the status stack value.
    /// </summary>
    [Serializable]
    public class StatusLogStackEvent : StatusGivenEvent
    {
        /// <summary>
        /// The message to display in the battle log.
        /// </summary>
        public StringKey Message;

        /// <summary>
        /// Whether to add a frame delay after displaying the message.
        /// </summary>
        public bool Delay;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public StatusLogStackEvent() { }

        /// <summary>
        /// Initializes a new instance with the specified message.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public StatusLogStackEvent(StringKey message) : this(message, false) { }

        /// <summary>
        /// Initializes a new instance with the specified message and delay flag.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="delay">Whether to add a delay after the message.</param>
        public StatusLogStackEvent(StringKey message, bool delay)
        {
            Message = message;
            Delay = delay;
        }

        /// <summary>
        /// Copy constructor for cloning an existing StatusLogStackEvent.
        /// </summary>
        protected StatusLogStackEvent(StatusLogStackEvent other)
        {
            Message = other.Message;
            Delay = other.Delay;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new StatusLogStackEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (context.Status != owner)
                yield break;

            if (context.msg)
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.Target.GetDisplayName(false), ((StatusEffect)owner).StatusStates.GetWithDefault<StackState>().Stack));
                if (Delay)
                    yield return new WaitForFrames(GameManager.Instance.ModifyBattleSpeed(10));
            }

        }
    }

    /// <summary>
    /// Status given event that reports the target character's current movement speed.
    /// </summary>
    [Serializable]
    public class ReportSpeedEvent : StatusGivenEvent
    {
        /// <summary>
        /// Whether to add a frame delay after displaying the message.
        /// </summary>
        public bool Delay;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public ReportSpeedEvent() { }

        /// <summary>
        /// Initializes a new instance with the specified delay flag.
        /// </summary>
        /// <param name="delay">Whether to add a delay after the message.</param>
        public ReportSpeedEvent(bool delay)
        {
            Delay = delay;
        }

        /// <summary>
        /// Copy constructor for cloning an existing ReportSpeedEvent.
        /// </summary>
        protected ReportSpeedEvent(ReportSpeedEvent other)
        {
            Delay = other.Delay;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ReportSpeedEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (context.Status != owner)
                yield break;

            if (context.msg)
            {
                string speedString = new StringKey("MSG_SPEED_NORMAL").ToLocal();
                switch (context.Target.MovementSpeed)
                {
                    case -3:
                        speedString = new StringKey("MSG_SPEED_FOURTH").ToLocal();
                        break;
                    case -2:
                        speedString = new StringKey("MSG_SPEED_THIRD").ToLocal();
                        break;
                    case -1:
                        speedString = new StringKey("MSG_SPEED_HALF").ToLocal();
                        break;
                    case 1:
                        speedString = new StringKey("MSG_SPEED_DOUBLE").ToLocal();
                        break;
                    case 2:
                        speedString = new StringKey("MSG_SPEED_TRIPLE").ToLocal();
                        break;
                    case 3:
                        speedString = new StringKey("MSG_SPEED_QUADRUPLE").ToLocal();
                        break;
                }
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(speedString, context.Target.GetDisplayName(false)));
                if (Delay)
                    yield return new WaitForFrames(GameManager.Instance.ModifyBattleSpeed(20));
            }
        }
    }
    /// <summary>
    /// Status given event that reports how much a specific stat has changed.
    /// </summary>
    [Serializable]
    public class ReportStatEvent : StatusGivenEvent
    {
        /// <summary>
        /// The maximum stat boost value before it's capped.
        /// </summary>
        public const int MAX_BUFF = 6;

        /// <summary>
        /// The minimum stat boost value before it's capped.
        /// </summary>
        public const int MIN_BUFF = -6;

        /// <summary>
        /// The stat being reported.
        /// </summary>
        public Stat Stat;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public ReportStatEvent() { }

        /// <summary>
        /// Initializes a new instance with the specified stat.
        /// </summary>
        /// <param name="stat">The stat to report.</param>
        public ReportStatEvent(Stat stat)
        {
            Stat = stat;
        }

        /// <summary>
        /// Copy constructor for cloning an existing ReportStatEvent.
        /// </summary>
        protected ReportStatEvent(ReportStatEvent other)
        {
            Stat = other.Stat;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ReportStatEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (context.Status != owner)
                yield break;

            if (context.msg)
            {
                string changeString = "";
                int counter = ((StatusEffect)owner).StatusStates.GetWithDefault<StackState>().Stack;
                if (counter == MIN_BUFF)
                    changeString = new StringKey("MSG_BUFF_MIN").ToLocal();
                else if (counter == MAX_BUFF)
                    changeString = new StringKey("MSG_BUFF_MAX").ToLocal();
                else
                {
                    int boost = context.StackDiff;
                    if (boost == 0)
                        changeString = new StringKey("MSG_BUFF_UNCHANGED").ToLocal();
                    else if (boost == 1)
                        changeString = new StringKey("MSG_BUFF_PLUS_1").ToLocal();
                    else if (boost == -1)
                        changeString = new StringKey("MSG_BUFF_MINUS_1").ToLocal();
                    else if (boost == 2)
                        changeString = new StringKey("MSG_BUFF_PLUS_2").ToLocal();
                    else if (boost == -2)
                        changeString = new StringKey("MSG_BUFF_MINUS_2").ToLocal();
                    else if (boost > 2)
                        changeString = new StringKey("MSG_BUFF_PLUS_3").ToLocal();
                    else if (boost < -2)
                        changeString = new StringKey("MSG_BUFF_MINUS_3").ToLocal();
                }
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(changeString, context.Target.GetDisplayName(false), Stat.ToLocal()));
                yield return new WaitForFrames(GameManager.Instance.ModifyBattleSpeed(20));
            }
        }
    }

    /// <summary>
    /// Status given event that reports when a stat boost/drop is removed.
    /// </summary>
    [Serializable]
    public class ReportStatRemoveEvent : StatusGivenEvent
    {
        /// <summary>
        /// Whether to add a frame delay after displaying the message.
        /// </summary>
        public bool Delay;

        /// <summary>
        /// The stat that was removed.
        /// </summary>
        public Stat Stat;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public ReportStatRemoveEvent() { }

        /// <summary>
        /// Initializes a new instance with the specified stat and delay flag.
        /// </summary>
        /// <param name="stat">The stat being removed.</param>
        /// <param name="delay">Whether to add a delay after the message.</param>
        public ReportStatRemoveEvent(Stat stat, bool delay)
        {
            Stat = stat;
            Delay = delay;
        }

        /// <summary>
        /// Copy constructor for cloning an existing ReportStatRemoveEvent.
        /// </summary>
        protected ReportStatRemoveEvent(ReportStatRemoveEvent other)
        {
            Stat = other.Stat;
            Delay = other.Delay;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ReportStatRemoveEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (context.Status != owner)
                yield break;

            if (context.msg)
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_BUFF_REVERT").ToLocal(), context.Target.GetDisplayName(false), Stat.ToLocal()));
                if (Delay)
                    yield return new WaitForFrames(GameManager.Instance.ModifyBattleSpeed(10));
            }
        }
    }

    /// <summary>
    /// Status given event that displays a visual effect for stat changes.
    /// Shows animations and plays sounds when stats are boosted or dropped.
    /// </summary>
    [Serializable]
    public class ShowStatChangeEvent : StatusGivenEvent
    {
        /// <summary>
        /// The sound to play when stats are boosted.
        /// </summary>
        [Sound(0)]
        public string StatUpSound;

        /// <summary>
        /// The sound to play when stats are dropped.
        /// </summary>
        [Sound(0)]
        public string StatDownSound;

        /// <summary>
        /// The animation asset for the stat change circle.
        /// </summary>
        public string StatCircle;

        /// <summary>
        /// The animation asset for the stat change lines/particles.
        /// </summary>
        public string StatLines;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public ShowStatChangeEvent() { }

        /// <summary>
        /// Initializes a new instance with the specified animations and sounds.
        /// </summary>
        /// <param name="statUp">The sound for stat boosts.</param>
        /// <param name="statDown">The sound for stat drops.</param>
        /// <param name="statCircle">The circle animation asset.</param>
        /// <param name="statLines">The lines animation asset.</param>
        public ShowStatChangeEvent(string statUp, string statDown, string statCircle, string statLines)
        {
            StatUpSound = statUp;
            StatDownSound = statDown;
            StatCircle = statCircle;
            StatLines = statLines;
        }

        /// <summary>
        /// Copy constructor for cloning an existing ShowStatChangeEvent.
        /// </summary>
        protected ShowStatChangeEvent(ShowStatChangeEvent other)
        {
            StatUpSound = other.StatUpSound;
            StatDownSound = other.StatDownSound;
            StatCircle = other.StatCircle;
            StatLines = other.StatLines;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ShowStatChangeEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (context.Status != owner)
                yield break;

            if (context.msg)
            {
                int boost = context.StackDiff;
                if (boost != 0)
                {
                    SqueezedAreaEmitter emitter;

                    if (boost > 0)
                    {
                        GameManager.Instance.BattleSE(StatUpSound);

                        if (!context.Target.Unidentifiable)
                        {
                            StaticAnim anim = new StaticAnim(new AnimData(StatCircle, 3));
                            anim.SetupEmitted(context.Target.MapLoc, -6, context.Target.CharDir);
                            DungeonScene.Instance.CreateAnim(anim, DrawLayer.Bottom);
                        }

                        emitter = new SqueezedAreaEmitter(new AnimData(StatLines, 2, Dir8.Up));
                    }
                    else
                    {
                        GameManager.Instance.BattleSE(StatDownSound);
                        emitter = new SqueezedAreaEmitter(new AnimData(StatLines, 2, Dir8.Down));
                    }

                    if (!context.Target.Unidentifiable)
                    {
                        emitter.Bursts = 3;
                        emitter.ParticlesPerBurst = 2;
                        emitter.BurstTime = 6;
                        emitter.Range = GraphicsManager.TileSize;
                        emitter.StartHeight = 0;
                        emitter.HeightSpeed = 6;
                        emitter.SetupEmit(context.Target.MapLoc, context.Target.MapLoc, context.Target.CharDir);

                        DungeonScene.Instance.CreateAnim(emitter, DrawLayer.NoDraw);
                    }
                }
            }
            yield break;
        }
    }
    /// <summary>
    /// Status given event that removes the status when its stack reaches zero.
    /// </summary>
    [Serializable]
    public class RemoveStackZeroEvent : StatusGivenEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new RemoveStackZeroEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (context.Status != owner)
                yield break;

            if (((StatusEffect)owner).StatusStates.GetWithDefault<StackState>().Stack == 0)
                yield return CoroutineManager.Instance.StartCoroutine(context.Target.RemoveStatusEffect(owner.GetID(), false));
        }
    }

    /// <summary>
    /// Status given event that applies the same status to the user when applied by another.
    /// Used for synchronized statuses like Destiny Knot that transfer between partners.
    /// </summary>
    [Serializable]
    public class StatusSyncEvent : StatusGivenEvent
    {
        /// <summary>
        /// The status ID to check for and sync.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string StatusID;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public StatusSyncEvent() { StatusID = ""; }

        /// <summary>
        /// Initializes a new instance with the specified status ID.
        /// </summary>
        /// <param name="statusID">The status to sync.</param>
        public StatusSyncEvent(string statusID)
        {
            StatusID = statusID;
        }

        /// <summary>
        /// Copy constructor for cloning an existing StatusSyncEvent.
        /// </summary>
        protected StatusSyncEvent(StatusSyncEvent other)
        {
            StatusID = other.StatusID;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new StatusSyncEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (owner != context.Status)//can't check on self
            {
                if (context.Status.ID == StatusID && context.User != null && context.User != context.Target)
                {
                    StatusEffect newStatus = context.Status.Clone();
                    if (context.Status.TargetChar != null)
                    {
                        if (context.Status.TargetChar == context.User)
                            newStatus.TargetChar = context.Target;
                        else if (context.Status.TargetChar == context.Target)
                            newStatus.TargetChar = context.User;
                    }
                    yield return CoroutineManager.Instance.StartCoroutine(context.User.AddStatusEffect(newStatus));
                }
            }
        }
    }

    /// <summary>
    /// Status given event that spreads a status to nearby enemies with a specific state.
    /// </summary>
    [Serializable]
    public class StateStatusShareEvent : StatusGivenEvent
    {
        /// <summary>
        /// List of StatusState types that trigger the spread.
        /// </summary>
        [StringTypeConstraint(1, typeof(StatusState))]
        public List<FlagType> States;

        /// <summary>
        /// The range in tiles to spread the status within.
        /// </summary>
        public int Range;

        /// <summary>
        /// The message to display when spreading.
        /// </summary>
        public StringKey Message;

        /// <summary>
        /// Animations to play when spreading to each target.
        /// </summary>
        public List<StatusAnimEvent> Anims;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public StateStatusShareEvent()
        {
            States = new List<FlagType>();
            Anims = new List<StatusAnimEvent>();
        }

        /// <summary>
        /// Initializes a new instance with the specified parameters.
        /// </summary>
        /// <param name="state">The status state type that triggers the spread.</param>
        /// <param name="range">The spread range in tiles.</param>
        /// <param name="msg">The message to display.</param>
        /// <param name="anims">Animations to play.</param>
        public StateStatusShareEvent(Type state, int range, StringKey msg, params StatusAnimEvent[] anims) : this()
        {
            States.Add(new FlagType(state));
            Range = range;
            Message = msg;

            Anims.AddRange(anims);
        }

        /// <summary>
        /// Copy constructor for cloning an existing StateStatusShareEvent.
        /// </summary>
        protected StateStatusShareEvent(StateStatusShareEvent other) : this()
        {
            States.AddRange(other.States);
            Range = other.Range;
            Message = other.Message;

            foreach (StatusAnimEvent anim in other.Anims)
                Anims.Add((StatusAnimEvent)anim.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new StateStatusShareEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (owner != context.Status)//can't check on self
            {
                bool hasState = false;
                foreach (FlagType state in States)
                {
                    if (context.Status.StatusStates.Contains(state.FullType))
                        hasState = true;
                }
                if (hasState)
                {
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), ownerChar.GetDisplayName(false), owner.GetDisplayName()));

                    foreach (StatusAnimEvent anim in Anims)
                        yield return CoroutineManager.Instance.StartCoroutine(anim.Apply(owner, ownerChar, context));
                    
                    foreach (Character character in ZoneManager.Instance.CurrentMap.GetCharsInFillRect(context.Target.CharLoc, Rect.FromPointRadius(context.Target.CharLoc, Range)))
                    {
                        if (!character.Dead && DungeonScene.Instance.GetMatchup(context.Target, character) == Alignment.Foe)
                        {
                            StatusEffect newStatus = context.Status.Clone();
                            if (context.Status.TargetChar != null)
                            {
                                if (context.Status.TargetChar == character)
                                    newStatus.TargetChar = context.Target;
                                else if (context.Status.TargetChar == context.Target)
                                    newStatus.TargetChar = character;
                            }
                            yield return CoroutineManager.Instance.StartCoroutine(character.AddStatusEffect(newStatus));
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Status given event that syncs a status to the user with a specific state.
    /// </summary>
    [Serializable]
    public class StateStatusSyncEvent : StatusGivenEvent
    {
        /// <summary>
        /// List of StatusState types that trigger the sync.
        /// </summary>
        [StringTypeConstraint(1, typeof(StatusState))]
        public List<FlagType> States;

        /// <summary>
        /// The message to display when syncing.
        /// </summary>
        public StringKey Message;

        /// <summary>
        /// Animations to play when syncing.
        /// </summary>
        public List<StatusAnimEvent> Anims;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public StateStatusSyncEvent()
        {
            States = new List<FlagType>();
            Anims = new List<StatusAnimEvent>();
        }

        /// <summary>
        /// Initializes a new instance with the specified parameters.
        /// </summary>
        /// <param name="state">The status state type that triggers the sync.</param>
        /// <param name="msg">The message to display.</param>
        /// <param name="anims">Animations to play.</param>
        public StateStatusSyncEvent(Type state, StringKey msg, params StatusAnimEvent[] anims) : this()
        {
            States.Add(new FlagType(state));
            Message = msg;

            Anims.AddRange(anims);
        }

        /// <summary>
        /// Copy constructor for cloning an existing StateStatusSyncEvent.
        /// </summary>
        protected StateStatusSyncEvent(StateStatusSyncEvent other) : this()
        {
            States.AddRange(other.States);
            Message = other.Message;

            foreach (StatusAnimEvent anim in other.Anims)
                Anims.Add((StatusAnimEvent)anim.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new StateStatusSyncEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (owner != context.Status)//can't check on self
            {
                bool hasState = false;
                foreach (FlagType state in States)
                {
                    if (context.Status.StatusStates.Contains(state.FullType))
                        hasState = true;
                }
                if (context.User != null && context.User != context.Target && hasState)
                {
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), ownerChar.GetDisplayName(false), owner.GetDisplayName()));

                    foreach (StatusAnimEvent anim in Anims)
                        yield return CoroutineManager.Instance.StartCoroutine(anim.Apply(owner, ownerChar, context));

                    StatusEffect newStatus = context.Status.Clone();
                    if (context.Status.TargetChar != null)
                    {
                        if (context.Status.TargetChar == context.User)
                            newStatus.TargetChar = context.Target;
                        else if (context.Status.TargetChar == context.Target)
                            newStatus.TargetChar = context.User;
                    }
                    yield return CoroutineManager.Instance.StartCoroutine(context.User.AddStatusEffect(newStatus));
                }
            }
        }
    }

    /// <summary>
    /// Status given event that syncs stat drops to the user.
    /// </summary>
    [Serializable]
    public class StatDropSyncEvent : StatusGivenEvent
    {
        /// <summary>
        /// The message to display when syncing the stat drop.
        /// </summary>
        public StringKey Message;

        /// <summary>
        /// Animations to play when syncing.
        /// </summary>
        public List<StatusAnimEvent> Anims;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public StatDropSyncEvent()
        {
            Anims = new List<StatusAnimEvent>();
        }

        /// <summary>
        /// Initializes a new instance with the specified message and animations.
        /// </summary>
        /// <param name="msg">The message to display.</param>
        /// <param name="anims">Animations to play.</param>
        public StatDropSyncEvent(StringKey msg, params StatusAnimEvent[] anims) : this()
        {
            Message = msg;

            Anims.AddRange(anims);
        }

        /// <summary>
        /// Copy constructor for cloning an existing StatDropSyncEvent.
        /// </summary>
        protected StatDropSyncEvent(StatDropSyncEvent other) : this()
        {
            Message = other.Message;

            foreach (StatusAnimEvent anim in other.Anims)
                Anims.Add((StatusAnimEvent)anim.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new StatDropSyncEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (owner != context.Status)//can't check on self
            {

                if (context.User != null && context.User != context.Target && context.Status.StatusStates.Contains<StatChangeState>() && context.StackDiff < 0)
                {
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), ownerChar.GetDisplayName(false), owner.GetDisplayName()));

                    foreach (StatusAnimEvent anim in Anims)
                        yield return CoroutineManager.Instance.StartCoroutine(anim.Apply(owner, ownerChar, context));

                    StatusEffect newStatus = context.Status.Clone();
                    StackState stack = newStatus.StatusStates.GetWithDefault<StackState>();
                    stack.Stack = context.StackDiff;
                    yield return CoroutineManager.Instance.StartCoroutine(context.User.AddStatusEffect(newStatus));
                }
            }
        }
    }

    /// <summary>
    /// Status given event that triggers a response when a specific status is applied.
    /// Used for abilities that react to status application.
    /// </summary>
    [Serializable]
    public class StatusResponseEvent : StatusGivenEvent
    {
        /// <summary>
        /// The status ID that triggers the response.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string StatusID;

        /// <summary>
        /// The event to execute in response.
        /// </summary>
        public SingleCharEvent BaseEvent;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public StatusResponseEvent() { StatusID = ""; }

        /// <summary>
        /// Initializes a new instance with the specified status ID and response event.
        /// </summary>
        /// <param name="statusID">The status that triggers the response.</param>
        /// <param name="baseEffect">The response event to execute.</param>
        public StatusResponseEvent(string statusID, SingleCharEvent baseEffect)
        {
            StatusID = statusID;
            BaseEvent = baseEffect;
        }

        /// <summary>
        /// Copy constructor for cloning an existing StatusResponseEvent.
        /// </summary>
        protected StatusResponseEvent(StatusResponseEvent other)
        {
            StatusID = other.StatusID;
            BaseEvent = other.BaseEvent;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new StatusResponseEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (owner != context.Status)//can't check on self
            {
                if (context.Status.ID == StatusID)
                {
                    SingleCharContext singleContext = new SingleCharContext(context.Target);
                    yield return CoroutineManager.Instance.StartCoroutine(BaseEvent.Apply(owner, ownerChar, singleContext));
                }
            }
        }
    }

    /// <summary>
    /// Status given event that triggers a response when a stat drop occurs.
    /// </summary>
    [Serializable]
    public class StatDropResponseEvent : StatusGivenEvent
    {
        /// <summary>
        /// The event to execute in response to a stat drop.
        /// </summary>
        public SingleCharEvent BaseEvent;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public StatDropResponseEvent() { }

        /// <summary>
        /// Initializes a new instance with the specified response event.
        /// </summary>
        /// <param name="baseEffect">The response event to execute.</param>
        public StatDropResponseEvent(SingleCharEvent baseEffect)
        {
            BaseEvent = baseEffect;
        }

        /// <summary>
        /// Copy constructor for cloning an existing StatDropResponseEvent.
        /// </summary>
        protected StatDropResponseEvent(StatDropResponseEvent other)
        {
            BaseEvent = other.BaseEvent;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new StatDropResponseEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (context.User == context.Target) // can't check self-inflicted effect
                yield break;
            if (owner == context.Status)//can't check its own status addition
                yield break;

            if (context.Status.StatusStates.Contains<StatChangeState>())//if it is a stat change
            {
                if (context.StackDiff < 0)
                {
                    SingleCharContext singleContext = new SingleCharContext(context.Target);
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_STAT_DROP_TRIGGER").ToLocal(), ownerChar.GetDisplayName(false), owner.GetDisplayName()));
                    yield return CoroutineManager.Instance.StartCoroutine(BaseEvent.Apply(owner, ownerChar, singleContext));
                }
            }
        }
    }

    /// <summary>
    /// Abstract base class for status events that share effects from equipped items.
    /// </summary>
    [Serializable]
    public abstract class ShareEquipStatusEvent : StatusGivenEvent
    {
        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, StatusCheckContext context)
        {
            if (!String.IsNullOrEmpty(ownerChar.EquippedItem.ID))
            {
                ItemData entry = (ItemData)ownerChar.EquippedItem.GetData();
                if (CheckEquipPassValidityEvent.CanItemEffectBePassed(entry))
                {
                    foreach (var effect in GetEvents(entry))
                        yield return CoroutineManager.Instance.StartCoroutine(effect.Value.Apply(owner, ownerChar, context));
                }
            }
        }

        /// <summary>
        /// Gets the status events from the item data.
        /// </summary>
        /// <param name="entry">The item data to get events from.</param>
        /// <returns>The priority list of status given events.</returns>
        protected abstract PriorityList<StatusGivenEvent> GetEvents(ItemData entry);
    }

    /// <summary>
    /// Status given event that shares the item's BeforeStatusAdds effects.
    /// </summary>
    [Serializable]
    public class ShareBeforeStatusAddsEvent : ShareEquipStatusEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new ShareBeforeStatusAddsEvent(); }

        /// <inheritdoc/>
        protected override PriorityList<StatusGivenEvent> GetEvents(ItemData entry) => entry.BeforeStatusAdds;
    }

    /// <summary>
    /// Status given event that shares the item's BeforeStatusAddings effects.
    /// </summary>
    [Serializable]
    public class ShareBeforeStatusAddingsEvent : ShareEquipStatusEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new ShareBeforeStatusAddingsEvent(); }

        /// <inheritdoc/>
        protected override PriorityList<StatusGivenEvent> GetEvents(ItemData entry) => entry.BeforeStatusAddings;
    }

    /// <summary>
    /// Status given event that shares the item's OnStatusAdds effects.
    /// </summary>
    [Serializable]
    public class ShareOnStatusAddsEvent : ShareEquipStatusEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new ShareOnStatusAddsEvent(); }

        /// <inheritdoc/>
        protected override PriorityList<StatusGivenEvent> GetEvents(ItemData entry) => entry.OnStatusAdds;
    }

    /// <summary>
    /// Status given event that shares the item's OnStatusRemoves effects.
    /// </summary>
    [Serializable]
    public class ShareOnStatusRemovesEvent : ShareEquipStatusEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new ShareOnStatusRemovesEvent(); }

        /// <inheritdoc/>
        protected override PriorityList<StatusGivenEvent> GetEvents(ItemData entry) => entry.OnStatusRemoves;
    }
}

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
    /// Battle events that use or modify status state values.
    /// </summary>


    /// <summary>
    /// Event that modifies the user's specified stat boost by adding the value from the StackState status state.
    /// This event can only be used on statuses that have a StackState.
    /// </summary>
    [Serializable]
    public class UserStatBoostEvent : BattleEvent
    {
        /// <summary>
        /// The stat to modify with the stack value.
        /// </summary>
        public Stat Stat;

        /// <inheritdoc/>
        public UserStatBoostEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserStatBoostEvent"/> class for the specified stat.
        /// </summary>
        /// <param name="stat">The stat to boost.</param>
        public UserStatBoostEvent(Stat stat)
        {
            Stat = stat;
        }

        /// <inheritdoc/>
        protected UserStatBoostEvent(UserStatBoostEvent other)
        {
            Stat = other.Stat;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new UserStatBoostEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            int boost = ((StatusEffect)owner).StatusStates.GetWithDefault<StackState>().Stack;
            switch (Stat)
            {
                case Stat.Attack:
                    context.AddContextStateInt<UserAtkBoost>(boost);
                    break;
                case Stat.Defense:
                    context.AddContextStateInt<UserDefBoost>(boost);
                    break;
                case Stat.MAtk:
                    context.AddContextStateInt<UserSpAtkBoost>(boost);
                    break;
                case Stat.MDef:
                    context.AddContextStateInt<UserSpDefBoost>(boost);
                    break;
                case Stat.HitRate:
                    context.AddContextStateInt<UserAccuracyBoost>(boost);
                    break;
                case Stat.Range:
                    context.RangeMod += boost;
                    break;
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that modifies the target's stat boost by adding the value from the StackState status state.
    /// This event can only be used on statuses that have a StackState.
    /// </summary>
    [Serializable]
    public class TargetStatBoostEvent : BattleEvent
    {
        /// <summary>
        /// The stat to modify with the stack value.
        /// </summary>
        public Stat Stat;

        /// <inheritdoc/>
        public TargetStatBoostEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetStatBoostEvent"/> class for the specified stat.
        /// </summary>
        /// <param name="stat">The stat to boost.</param>
        public TargetStatBoostEvent(Stat stat)
        {
            Stat = stat;
        }

        /// <inheritdoc/>
        protected TargetStatBoostEvent(TargetStatBoostEvent other)
        {
            Stat = other.Stat;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new TargetStatBoostEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            int boost = ((StatusEffect)owner).StatusStates.GetWithDefault<StackState>().Stack;
            switch (Stat)
            {
                case Stat.Attack:
                    context.AddContextStateInt<TargetAtkBoost>(boost);
                    break;
                case Stat.Defense:
                    context.AddContextStateInt<TargetDefBoost>(boost);
                    break;
                case Stat.MAtk:
                    context.AddContextStateInt<TargetSpAtkBoost>(boost);
                    break;
                case Stat.MDef:
                    context.AddContextStateInt<TargetSpDefBoost>(boost);
                    break;
                case Stat.DodgeRate:
                    context.AddContextStateInt<TargetEvasionBoost>(boost);
                    break;
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that modifies the target's specified stat boost by a fixed amount.
    /// Unlike TargetStatBoostEvent, this uses a constant value rather than reading from a status state.
    /// </summary>
    [Serializable]
    public class TargetStatAddEvent : BattleEvent
    {

        /// <summary>
        /// The stat to modify.
        /// </summary>
        public Stat Stat;

        /// <summary>
        /// The fixed value to add to the stat boost.
        /// </summary>
        public int Mod;

        /// <inheritdoc/>
        public TargetStatAddEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetStatAddEvent"/> class with the specified stat and modifier.
        /// </summary>
        /// <param name="stat">The stat to modify.</param>
        /// <param name="mod">The value to add to the stat boost.</param>
        public TargetStatAddEvent(Stat stat, int mod)
        {
            Stat = stat;
            Mod = mod;
        }

        /// <inheritdoc/>
        protected TargetStatAddEvent(TargetStatAddEvent other)
        {
            Stat = other.Stat;
            Mod = other.Mod;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new TargetStatAddEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            switch (Stat)
            {
                case Stat.Attack:
                    context.AddContextStateInt<TargetAtkBoost>(Mod);
                    break;
                case Stat.Defense:
                    context.AddContextStateInt<TargetDefBoost>(Mod);
                    break;
                case Stat.MAtk:
                    context.AddContextStateInt<TargetSpAtkBoost>(Mod);
                    break;
                case Stat.MDef:
                    context.AddContextStateInt<TargetSpDefBoost>(Mod);
                    break;
                case Stat.DodgeRate:
                    context.AddContextStateInt<TargetEvasionBoost>(Mod);
                    break;
            }
            yield break;
        }
    }



    /// <summary>
    /// Event that sets the AttackedThisTurnState status state to true, indicating that the character attacked this turn.
    /// This event can only be used on statuses that have an AttackedThisTurnState.
    /// </summary>
    [Serializable]
    public class AttackedThisTurnEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new AttackedThisTurnEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            AttackedThisTurnState recent = ((StatusEffect)owner).StatusStates.GetWithDefault<AttackedThisTurnState>();
            recent.Attacked = true;
            yield break;
        }
    }




    /// <summary>
    /// Event that reverses the character's stat changes by negating StackState values.
    /// Only affects statuses that contain one of the specified status states.
    /// </summary>
    [Serializable]
    public class ReverseStateStatusBattleEvent : BattleEvent
    {
        /// <summary>
        /// If the status contains one of the specified status states, its stack amount will be negated.
        /// </summary>
        [StringTypeConstraint(1, typeof(StatusState))]
        public List<FlagType> States;

        /// <summary>
        /// Whether to affect the target (true) or user (false).
        /// </summary>
        public bool AffectTarget;

        /// <summary>
        /// The message displayed in the dungeon log when stat changes are reversed.
        /// </summary>
        [StringKey(0, true)]
        public StringKey Msg;

        /// <inheritdoc/>
        public ReverseStateStatusBattleEvent() { States = new List<FlagType>(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReverseStateStatusBattleEvent"/> class with specified parameters.
        /// </summary>
        /// <param name="state">The status state type to check for.</param>
        /// <param name="affectTarget">Whether to affect the target or user.</param>
        /// <param name="msg">The message to display.</param>
        public ReverseStateStatusBattleEvent(Type state, bool affectTarget, StringKey msg) : this()
        {
            States.Add(new FlagType(state));
            AffectTarget = affectTarget;
            Msg = msg;
        }

        /// <inheritdoc/>
        protected ReverseStateStatusBattleEvent(ReverseStateStatusBattleEvent other) : this()
        {
            States.AddRange(other.States);
            AffectTarget = other.AffectTarget;
            Msg = other.Msg;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ReverseStateStatusBattleEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Character target = (AffectTarget ? context.Target : context.User);
            if (target.Dead)
                yield break;

            bool affected = false;
            foreach (StatusEffect status in target.IterateStatusEffects())
            {
                bool hasState = false;
                foreach (FlagType state in States)
                {
                    if (status.StatusStates.Contains(state.FullType))
                        hasState = true;
                }
                if (hasState)
                {
                    StackState stack = status.StatusStates.GetWithDefault<StackState>();
                    stack.Stack = stack.Stack * -1;
                    affected = true;
                }
            }
            if (affected && Msg.IsValid())
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(Msg.ToLocal(), target.GetDisplayName(false)));
        }

    }

    /// <summary>
    /// Event that decreases the counter in the status's CountDownState when the character performs an action.
    /// The status is removed when the countdown reaches 0.
    /// This event can only be used on statuses that have a CountDownState.
    /// </summary>
    [Serializable]
    public class CountDownOnActionEvent : BattleEvent
    {
        /// <summary>
        /// Whether to display the removal message when the status is removed.
        /// </summary>
        public bool ShowMessage;

        /// <inheritdoc/>
        public CountDownOnActionEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CountDownOnActionEvent"/> class with the specified message visibility.
        /// </summary>
        /// <param name="showMessage">Whether to show a message when the status is removed.</param>
        public CountDownOnActionEvent(bool showMessage)
        {
            ShowMessage = showMessage;
        }

        /// <inheritdoc/>
        protected CountDownOnActionEvent(CountDownOnActionEvent other)
        {
            ShowMessage = other.ShowMessage;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new CountDownOnActionEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.UsageSlot == BattleContext.FORCED_SLOT)
                yield break;

            ((StatusEffect)owner).StatusStates.GetWithDefault<CountDownState>().Counter--;
            if (((StatusEffect)owner).StatusStates.GetWithDefault<CountDownState>().Counter <= 0)
                yield return CoroutineManager.Instance.StartCoroutine(context.User.RemoveStatusEffect(((StatusEffect)owner).ID, ShowMessage));
        }
    }

    /// <summary>
    /// Event that removes the RecentState from the status, allowing countdown mechanics to proceed.
    /// This event can only be used on statuses that have a RecentState.
    /// </summary>
    [Serializable]
    public class RemoveRecentEvent : BattleEvent
    {
        /// <inheritdoc/>
        public RemoveRecentEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new RemoveRecentEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            ((StatusEffect)owner).StatusStates.Remove<RecentState>();//allow the counter to count down
            yield break;
        }
    }

    /// <summary>
    /// Event that sets the CountDownState counter to 0 if the character receives damage, causing immediate wake-up.
    /// Non-damaging hits will still reduce the counter by 1.
    /// </summary>
    [Serializable]
    public class ForceWakeEvent : BattleEvent
    {
        /// <inheritdoc/>
        public ForceWakeEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ForceWakeEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            int damage = context.GetContextStateInt<DamageDealt>(0);
            bool hit = context.ContextStates.Contains<AttackHit>();
            bool recent = ((StatusEffect)owner).StatusStates.Contains<RecentState>();
            if (!recent && context.Target != context.User)//don't immediately count down after status is inflicted
            {
                if (damage > 0)
                {
                    //yield return CoroutineManager.Instance.StartCoroutine(context.Target.RemoveStatusEffect(((StatusEffect)owner).ID, true));
                    ((StatusEffect)owner).StatusStates.GetWithDefault<CountDownState>().Counter = 0;
                }
                else if (hit)
                    ((StatusEffect)owner).StatusStates.GetWithDefault<CountDownState>().Counter = Math.Max(((StatusEffect)owner).StatusStates.GetWithDefault<CountDownState>().Counter - 1, 0);
            }
            yield break;
        }
    }
}


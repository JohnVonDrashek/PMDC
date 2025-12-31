using System;
using System.Collections.Generic;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Data;
using Newtonsoft.Json;
using RogueEssence.Dev;
using RogueElements;

namespace PMDC.Dungeon
{
    /// <summary>
    /// Context state storing the attacker's attacking stat value.
    /// Used during damage calculation to track the offensive stat being used.
    /// </summary>
    [Serializable]
    public class UserAtkStat : ContextIntState
    {
        /// <inheritdoc/>
        public UserAtkStat() { }

        /// <summary>
        /// Initializes a new instance with the specified attack stat value.
        /// </summary>
        /// <param name="count">The attack stat value.</param>
        public UserAtkStat(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing UserAtkStat.
        /// </summary>
        protected UserAtkStat(UserAtkStat other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new UserAtkStat(this); }
    }

    /// <summary>
    /// Context state storing the target's defensive stat value.
    /// Used during damage calculation to track the defensive stat being used.
    /// </summary>
    [Serializable]
    public class TargetDefStat : ContextIntState
    {
        /// <inheritdoc/>
        public TargetDefStat() { }

        /// <summary>
        /// Initializes a new instance with the specified defense stat value.
        /// </summary>
        /// <param name="count">The defense stat value.</param>
        public TargetDefStat(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing TargetDefStat.
        /// </summary>
        protected TargetDefStat(TargetDefStat other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new TargetDefStat(this); }
    }

    /// <summary>
    /// Context state storing the attacker's hit rate stat value.
    /// Used during accuracy calculation to determine hit chance.
    /// </summary>
    [Serializable]
    public class UserHitStat : ContextIntState
    {
        /// <inheritdoc/>
        public UserHitStat() { }

        /// <summary>
        /// Initializes a new instance with the specified hit rate value.
        /// </summary>
        /// <param name="count">The hit rate stat value.</param>
        public UserHitStat(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing UserHitStat.
        /// </summary>
        protected UserHitStat(UserHitStat other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new UserHitStat(this); }
    }

    /// <summary>
    /// Context state storing the target's evasion stat value.
    /// Used during accuracy calculation to determine dodge chance.
    /// </summary>
    [Serializable]
    public class TargetEvadeStat : ContextIntState
    {
        /// <inheritdoc/>
        public TargetEvadeStat() { }

        /// <summary>
        /// Initializes a new instance with the specified evasion value.
        /// </summary>
        /// <param name="count">The evasion stat value.</param>
        public TargetEvadeStat(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing TargetEvadeStat.
        /// </summary>
        protected TargetEvadeStat(TargetEvadeStat other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new TargetEvadeStat(this); }
    }

    /// <summary>
    /// Context state storing the attacker's level.
    /// Used in damage calculations that scale with level.
    /// </summary>
    [Serializable]
    public class UserLevel : ContextIntState
    {
        /// <inheritdoc/>
        public UserLevel() { }

        /// <summary>
        /// Initializes a new instance with the specified level.
        /// </summary>
        /// <param name="count">The user's level.</param>
        public UserLevel(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing UserLevel.
        /// </summary>
        protected UserLevel(UserLevel other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new UserLevel(this); }
    }

    /// <summary>
    /// Context state storing the target's level.
    /// Used in damage calculations and effects that depend on level differences.
    /// </summary>
    [Serializable]
    public class TargetLevel : ContextIntState
    {
        /// <inheritdoc/>
        public TargetLevel() { }

        /// <summary>
        /// Initializes a new instance with the specified level.
        /// </summary>
        /// <param name="count">The target's level.</param>
        public TargetLevel(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing TargetLevel.
        /// </summary>
        protected TargetLevel(TargetLevel other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new TargetLevel(this); }
    }

    /// <summary>
    /// Context state storing damage multiplier for scaling attack effects.
    /// Multipliers work as follows (before reaching damage calc):
    /// Num greater than 0: Process damage normally with msg.
    /// Num equals 0: Process 0 damage with msg.
    /// Num less than 0: Process 0 damage without msg.
    /// Denominator is always greater than 0.
    /// </summary>
    [Serializable]
    public class DmgMult : ContextMultState
    {
        /// <inheritdoc/>
        public DmgMult() { }

        /// <summary>
        /// Copy constructor for cloning an existing DmgMult.
        /// </summary>
        protected DmgMult(DmgMult other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new DmgMult(this); }
    }

    /// <summary>
    /// Context state storing HP damage multiplier for effects that deal percentage-based damage.
    /// Modifies the final HP damage after base calculations.
    /// </summary>
    [Serializable]
    public class HPDmgMult : ContextMultState
    {
        /// <inheritdoc/>
        public HPDmgMult() { }

        /// <summary>
        /// Copy constructor for cloning an existing HPDmgMult.
        /// </summary>
        protected HPDmgMult(HPDmgMult other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new HPDmgMult(this); }
    }

    /// <summary>
    /// Context state storing accuracy multiplier for scaling hit chance effects.
    /// Multipliers work as follows:
    /// Num greater than 0: Process accuracy calcs normally with msg.
    /// Num equals 0: Process automatic miss with msg, unless the attack never misses. Ignores miss compensation.
    /// Num less than 0: Process automatic miss without msg, even if the attack never misses.
    /// Denominator is always greater than 0.
    /// </summary>
    [Serializable]
    public class AccMult : ContextMultState
    {
        /// <inheritdoc/>
        public AccMult() { }

        /// <summary>
        /// Copy constructor for cloning an existing AccMult.
        /// </summary>
        protected AccMult(AccMult other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new AccMult(this); }
    }

    /// <summary>
    /// Context state storing hunger consumption multiplier.
    /// Used to modify how much belly is consumed by actions.
    /// </summary>
    [Serializable]
    public class HungerMult : ContextMultState
    {
        /// <inheritdoc/>
        public HungerMult() { }

        /// <summary>
        /// Copy constructor for cloning an existing HungerMult.
        /// </summary>
        protected HungerMult(HungerMult other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new HungerMult(this); }
    }

    /// <summary>
    /// Context state indicating a tainted drain effect that modifies healing from drain moves.
    /// Used when drain attacks should heal less or even damage the user.
    /// </summary>
    [Serializable]
    public class TaintedDrain : ContextState
    {
        /// <summary>
        /// The multiplier applied to drain healing (negative values cause damage).
        /// </summary>
        public int Mult;

        /// <inheritdoc/>
        public TaintedDrain() { }

        /// <summary>
        /// Initializes a new instance with the specified drain multiplier.
        /// </summary>
        /// <param name="mult">The drain healing multiplier.</param>
        public TaintedDrain(int mult) { Mult = mult; }

        /// <summary>
        /// Copy constructor for cloning an existing TaintedDrain.
        /// </summary>
        public TaintedDrain(TaintedDrain other) { Mult = other.Mult; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new TaintedDrain(this); }
    }

    /// <summary>
    /// Marker context state indicating the move is in its charging phase.
    /// Used by two-turn moves like Solar Beam during the charge turn.
    /// </summary>
    [Serializable]
    public class MoveCharge : ContextState
    {
        /// <inheritdoc/>
        public MoveCharge() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new MoveCharge(); }
    }

    /// <summary>
    /// Marker context state indicating a Bide-style move is storing damage.
    /// Used by moves that accumulate damage to release later.
    /// </summary>
    [Serializable]
    public class MoveBide : ContextState
    {
        /// <inheritdoc/>
        public MoveBide() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new MoveBide(); }
    }

    /// <summary>
    /// Marker context state indicating a follow-up attack after the main action.
    /// Used by abilities like Parental Bond for additional hits.
    /// </summary>
    [Serializable]
    public class FollowUp : ContextState
    {
        /// <inheritdoc/>
        public FollowUp() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new FollowUp(); }
    }

    /// <summary>
    /// Marker context state indicating an attack while the user is asleep.
    /// Used by Sleep Talk and Snore to enable attacks during sleep.
    /// </summary>
    [Serializable]
    public class SleepAttack : ContextState
    {
        /// <inheritdoc/>
        public SleepAttack() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new SleepAttack(); }
    }

    /// <summary>
    /// Marker context state indicating the attack cured a status condition.
    /// Used to track when attacks like Wake-Up Slap cure the target's status.
    /// </summary>
    [Serializable]
    public class CureAttack : ContextState
    {
        /// <inheritdoc/>
        public CureAttack() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new CureAttack(); }
    }

    /// <summary>
    /// Marker context state indicating an attack from a bound/trapped state.
    /// Used by moves like Wrap and Bind during their multi-turn damage.
    /// </summary>
    [Serializable]
    public class BoundAttack : ContextState
    {
        /// <inheritdoc/>
        public BoundAttack() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new BoundAttack(); }
    }

    /// <summary>
    /// Marker context state indicating the attack successfully hit the target.
    /// Set when an attack lands for use by effects that trigger on hit.
    /// </summary>
    [Serializable]
    public class AttackHit : ContextState
    {
        /// <inheritdoc/>
        public AttackHit() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new AttackHit(); }
    }

    /// <summary>
    /// Context state tracking the total number of hits from a multi-hit attack.
    /// Used by moves like Fury Attack to track hit count.
    /// </summary>
    [Serializable]
    public class AttackHitTotal : ContextIntState
    {
        /// <inheritdoc/>
        public AttackHitTotal() { }

        /// <summary>
        /// Initializes a new instance with the specified hit count.
        /// </summary>
        /// <param name="count">The total number of hits.</param>
        public AttackHitTotal(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing AttackHitTotal.
        /// </summary>
        protected AttackHitTotal(AttackHitTotal other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new AttackHitTotal(this); }
    }

    /// <summary>
    /// Marker context state indicating the target was knocked out by this attack.
    /// Used to trigger knockout-related effects and experience gain.
    /// </summary>
    [Serializable]
    public class Knockout : ContextState
    {
        /// <inheritdoc/>
        public Knockout() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new Knockout(); }
    }

    /// <summary>
    /// Marker context state indicating the move's damage category was swapped.
    /// Used by effects like Psyshock that use physical defense against special attack.
    /// </summary>
    [Serializable]
    public class CrossCategory : ContextState
    {
        /// <inheritdoc/>
        public CrossCategory() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new CrossCategory(); }
    }

    /// <summary>
    /// Marker context state indicating the target endured the attack with 1 HP.
    /// Used by Endure and Focus Sash effects to track survival.
    /// </summary>
    [Serializable]
    public class AttackEndure : ContextState
    {
        /// <inheritdoc/>
        public AttackEndure() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new AttackEndure(); }
    }

    /// <summary>
    /// Marker context state indicating the attack landed a critical hit.
    /// Used to apply critical hit damage bonuses and trigger related effects.
    /// </summary>
    [Serializable]
    public class AttackCrit : ContextState
    {
        /// <inheritdoc/>
        public AttackCrit() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new AttackCrit(); }
    }

    /// <summary>
    /// Marker context state indicating a thrown item was caught by the target.
    /// Used by catching abilities to intercept thrown items.
    /// </summary>
    [Serializable]
    public class ItemCaught : ContextState
    {
        /// <inheritdoc/>
        public ItemCaught() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new ItemCaught(); }
    }

    /// <summary>
    /// Marker context state indicating an item was destroyed during the action.
    /// Used to track item destruction for preventing duplicate effects.
    /// </summary>
    [Serializable]
    public class ItemDestroyed : ContextState
    {
        /// <inheritdoc/>
        public ItemDestroyed() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new ItemDestroyed(); }
    }

    /// <summary>
    /// Marker context state indicating the attack was redirected to a different target.
    /// Used by Lightning Rod and Storm Drain to track redirection.
    /// </summary>
    [Serializable]
    public class Redirected : ContextState
    {
        /// <inheritdoc/>
        public Redirected() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new Redirected(); }
    }

    /// <summary>
    /// Marker context state indicating extra PP consumption from Pressure ability.
    /// Used to track when moves cost additional PP due to Pressure.
    /// </summary>
    [Serializable]
    public class PressurePlus : ContextState
    {
        /// <inheritdoc/>
        public PressurePlus() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new PressurePlus(); }
    }

    /// <summary>
    /// Marker context state indicating the Corrosion ability is active.
    /// Allows Poison-type moves to affect Steel and Poison types.
    /// </summary>
    [Serializable]
    public class Corrosion : ContextState
    {
        /// <inheritdoc/>
        public Corrosion() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new Corrosion(); }
    }

    /// <summary>
    /// Context state indicating the Infiltrator ability is bypassing protective effects.
    /// Allows moves to ignore screens, Substitute, and Safeguard.
    /// </summary>
    [Serializable]
    public class Infiltrator : ContextState
    {
        /// <summary>
        /// Message to display when Infiltrator bypasses a protection.
        /// </summary>
        [StringKey(0, true)]
        public StringKey Msg;

        /// <inheritdoc/>
        public Infiltrator() { }

        /// <summary>
        /// Initializes a new instance with a bypass message.
        /// </summary>
        /// <param name="msg">The message to display when Infiltrator bypasses a protection.</param>
        public Infiltrator(StringKey msg) { Msg = msg; }

        /// <summary>
        /// Copy constructor for cloning an existing Infiltrator.
        /// </summary>
        protected Infiltrator(Infiltrator other) { Msg = other.Msg; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new Infiltrator(this); }
    }

    /// <summary>
    /// Marker context state indicating Ball Fetch ability should retrieve a failed capture item.
    /// Used when a thrown ball misses to retrieve it.
    /// </summary>
    [Serializable]
    public class BallFetch : ContextState
    {
        /// <inheritdoc/>
        public BallFetch() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new BallFetch(); }
    }

    /// <summary>
    /// Context state indicating a failed recruitment attempt and where the recruit ball landed.
    /// Used to track unsuccessful capture locations.
    /// </summary>
    [Serializable]
    public class RecruitFail : ContextState
    {
        /// <summary>
        /// The location where the failed recruit ball landed, if any.
        /// </summary>
        public Loc? ResultLoc;

        /// <inheritdoc/>
        public RecruitFail() { }

        /// <summary>
        /// Initializes a new instance with the result location.
        /// </summary>
        /// <param name="resultLoc">Where the failed ball landed.</param>
        public RecruitFail(Loc? resultLoc) { ResultLoc = resultLoc; }

        /// <summary>
        /// Copy constructor for cloning an existing RecruitFail.
        /// </summary>
        public RecruitFail(RecruitFail other) { ResultLoc = other.ResultLoc; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new RecruitFail(this); }
    }

    /// <summary>
    /// Marker context state indicating a single draw absorption effect.
    /// Used by abilities that absorb specific move types once per battle.
    /// </summary>
    [Serializable]
    public class SingleDrawAbsorb : ContextState
    {
        /// <inheritdoc/>
        public SingleDrawAbsorb() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new SingleDrawAbsorb(); }
    }

    /// <summary>
    /// Marker context state indicating Friend Guard has already activated.
    /// Prevents duplicate damage reduction from multiple Friend Guard allies.
    /// </summary>
    [Serializable]
    public class FriendGuardProcEvent : ContextState
    {
        /// <inheritdoc/>
        public FriendGuardProcEvent() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new FriendGuardProcEvent(); }
    }

    /// <summary>
    /// Context state storing the distance of the last hit in a ranged attack.
    /// Used by abilities that modify damage based on distance traveled.
    /// </summary>
    [Serializable]
    public class LastHitDist : ContextIntState
    {
        /// <inheritdoc/>
        public LastHitDist() { }

        /// <summary>
        /// Initializes a new instance with the specified distance.
        /// </summary>
        /// <param name="count">The distance in tiles.</param>
        public LastHitDist(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing LastHitDist.
        /// </summary>
        protected LastHitDist(LastHitDist other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new LastHitDist(this); }
    }

    /// <summary>
    /// Context state storing the current critical hit stage level.
    /// Higher levels increase the chance of landing a critical hit.
    /// </summary>
    [Serializable]
    public class CritLevel : ContextIntState
    {
        /// <inheritdoc/>
        public CritLevel() { }

        /// <summary>
        /// Initializes a new instance with the specified crit level.
        /// </summary>
        /// <param name="count">The critical hit stage level.</param>
        public CritLevel(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing CritLevel.
        /// </summary>
        protected CritLevel(CritLevel other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new CritLevel(this); }
    }

    /// <summary>
    /// Context state storing the amount of damage dealt by a single hit.
    /// Used by drain moves and damage-reactive abilities.
    /// </summary>
    [Serializable]
    public class DamageDealt : ContextIntState
    {
        /// <inheritdoc/>
        public DamageDealt() { }

        /// <summary>
        /// Initializes a new instance with the specified damage amount.
        /// </summary>
        /// <param name="count">The damage dealt.</param>
        public DamageDealt(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing DamageDealt.
        /// </summary>
        protected DamageDealt(DamageDealt other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new DamageDealt(this); }
    }

    /// <summary>
    /// Context state storing the cumulative damage dealt across all hits.
    /// Used to track total damage for multi-hit moves and end-of-turn effects.
    /// </summary>
    [Serializable]
    public class TotalDamageDealt : ContextIntState
    {
        /// <inheritdoc/>
        public TotalDamageDealt() { }

        /// <summary>
        /// Initializes a new instance with the specified total damage.
        /// </summary>
        /// <param name="count">The total damage dealt.</param>
        public TotalDamageDealt(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing TotalDamageDealt.
        /// </summary>
        protected TotalDamageDealt(TotalDamageDealt other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new TotalDamageDealt(this); }
    }

    /// <summary>
    /// Context state storing the amount of HP healed on the target from damage dealt.
    /// Used by absorption abilities and drain attack mechanics.
    /// </summary>
    [Serializable]
    public class DamageHealedTarget : ContextIntState
    {
        /// <inheritdoc/>
        public DamageHealedTarget() { }

        /// <summary>
        /// Initializes a new instance with the specified healed amount.
        /// </summary>
        /// <param name="count">The HP healed.</param>
        public DamageHealedTarget(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing DamageHealedTarget.
        /// </summary>
        protected DamageHealedTarget(DamageHealedTarget other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new DamageHealedTarget(this); }
    }

    /// <summary>
    /// Context state storing a recruitment rate bonus.
    /// Used to modify the chance of successfully recruiting wild monsters.
    /// </summary>
    [Serializable]
    public class RecruitBoost : ContextIntState
    {
        /// <inheritdoc/>
        public RecruitBoost() { }

        /// <summary>
        /// Initializes a new instance with the specified recruitment bonus.
        /// </summary>
        /// <param name="count">The recruitment rate bonus percentage.</param>
        public RecruitBoost(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing RecruitBoost.
        /// </summary>
        protected RecruitBoost(RecruitBoost other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new RecruitBoost(this); }
    }

    /// <summary>
    /// Context state tracking the number of knockouts from this action.
    /// Used by abilities that trigger or scale based on KO count.
    /// </summary>
    [Serializable]
    public class TotalKnockouts : ContextIntState
    {
        /// <inheritdoc/>
        public TotalKnockouts() { }

        /// <summary>
        /// Initializes a new instance with the specified knockout count.
        /// </summary>
        /// <param name="count">The number of knockouts.</param>
        public TotalKnockouts(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing TotalKnockouts.
        /// </summary>
        protected TotalKnockouts(TotalKnockouts other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new TotalKnockouts(this); }
    }

    /// <summary>
    /// Context state storing the amount of HP lost by a single hit.
    /// Used by counter attacks and damage reflection abilities.
    /// </summary>
    [Serializable]
    public class HPLost : ContextIntState
    {
        /// <inheritdoc/>
        public HPLost() { }

        /// <summary>
        /// Initializes a new instance with the specified HP loss.
        /// </summary>
        /// <param name="count">The HP lost.</param>
        public HPLost(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing HPLost.
        /// </summary>
        protected HPLost(HPLost other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new HPLost(this); }
    }

    /// <summary>
    /// Context state storing the cumulative HP lost across all hits.
    /// Used for damage tracking across multi-hit attacks.
    /// </summary>
    [Serializable]
    public class TotalHPLost : ContextIntState
    {
        /// <inheritdoc/>
        public TotalHPLost() { }

        /// <summary>
        /// Initializes a new instance with the specified total HP loss.
        /// </summary>
        /// <param name="count">The total HP lost.</param>
        public TotalHPLost(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing TotalHPLost.
        /// </summary>
        protected TotalHPLost(TotalHPLost other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new TotalHPLost(this); }
    }





    /// <summary>
    /// Context state storing the user's attack stat stage boost.
    /// Used in damage calculations accounting for stat modifiers.
    /// </summary>
    [Serializable]
    public class UserAtkBoost : ContextIntState
    {
        /// <inheritdoc/>
        public UserAtkBoost() { }

        /// <summary>
        /// Initializes a new instance with the specified boost level.
        /// </summary>
        /// <param name="count">The attack stat stage modifier.</param>
        public UserAtkBoost(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing UserAtkBoost.
        /// </summary>
        protected UserAtkBoost(UserAtkBoost other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new UserAtkBoost(this); }
    }

    /// <summary>
    /// Context state storing the user's defense stat stage boost.
    /// Used in damage calculations accounting for stat modifiers.
    /// </summary>
    [Serializable]
    public class UserDefBoost : ContextIntState
    {
        /// <inheritdoc/>
        public UserDefBoost() { }

        /// <summary>
        /// Initializes a new instance with the specified boost level.
        /// </summary>
        /// <param name="count">The defense stat stage modifier.</param>
        public UserDefBoost(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing UserDefBoost.
        /// </summary>
        protected UserDefBoost(UserDefBoost other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new UserDefBoost(this); }
    }

    /// <summary>
    /// Context state storing the user's special attack stat stage boost.
    /// Used in damage calculations accounting for stat modifiers.
    /// </summary>
    [Serializable]
    public class UserSpAtkBoost : ContextIntState
    {
        /// <inheritdoc/>
        public UserSpAtkBoost() { }

        /// <summary>
        /// Initializes a new instance with the specified boost level.
        /// </summary>
        /// <param name="count">The special attack stat stage modifier.</param>
        public UserSpAtkBoost(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing UserSpAtkBoost.
        /// </summary>
        protected UserSpAtkBoost(UserSpAtkBoost other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new UserSpAtkBoost(this); }
    }

    /// <summary>
    /// Context state storing the user's special defense stat stage boost.
    /// Used in damage calculations accounting for stat modifiers.
    /// </summary>
    [Serializable]
    public class UserSpDefBoost : ContextIntState
    {
        /// <inheritdoc/>
        public UserSpDefBoost() { }

        /// <summary>
        /// Initializes a new instance with the specified boost level.
        /// </summary>
        /// <param name="count">The special defense stat stage modifier.</param>
        public UserSpDefBoost(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing UserSpDefBoost.
        /// </summary>
        protected UserSpDefBoost(UserSpDefBoost other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new UserSpDefBoost(this); }
    }

    /// <summary>
    /// Context state storing the user's accuracy stat stage boost.
    /// Used in hit chance calculations accounting for stat modifiers.
    /// </summary>
    [Serializable]
    public class UserAccuracyBoost : ContextIntState
    {
        /// <inheritdoc/>
        public UserAccuracyBoost() { }

        /// <summary>
        /// Initializes a new instance with the specified boost level.
        /// </summary>
        /// <param name="count">The accuracy stat stage modifier.</param>
        public UserAccuracyBoost(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing UserAccuracyBoost.
        /// </summary>
        protected UserAccuracyBoost(UserAccuracyBoost other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new UserAccuracyBoost(this); }
    }

    /// <summary>
    /// Context state storing the target's attack stat stage boost.
    /// Used in damage calculations where target's attack matters.
    /// </summary>
    [Serializable]
    public class TargetAtkBoost : ContextIntState
    {
        /// <inheritdoc/>
        public TargetAtkBoost() { }

        /// <summary>
        /// Initializes a new instance with the specified boost level.
        /// </summary>
        /// <param name="count">The attack stat stage modifier.</param>
        public TargetAtkBoost(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing TargetAtkBoost.
        /// </summary>
        protected TargetAtkBoost(TargetAtkBoost other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new TargetAtkBoost(this); }
    }

    /// <summary>
    /// Context state storing the target's defense stat stage boost.
    /// Used in damage calculations accounting for target's defenses.
    /// </summary>
    [Serializable]
    public class TargetDefBoost : ContextIntState
    {
        /// <inheritdoc/>
        public TargetDefBoost() { }

        /// <summary>
        /// Initializes a new instance with the specified boost level.
        /// </summary>
        /// <param name="count">The defense stat stage modifier.</param>
        public TargetDefBoost(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing TargetDefBoost.
        /// </summary>
        protected TargetDefBoost(TargetDefBoost other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new TargetDefBoost(this); }
    }

    /// <summary>
    /// Context state storing the target's special attack stat stage boost.
    /// Used in damage calculations where target's special attack matters.
    /// </summary>
    [Serializable]
    public class TargetSpAtkBoost : ContextIntState
    {
        /// <inheritdoc/>
        public TargetSpAtkBoost() { }

        /// <summary>
        /// Initializes a new instance with the specified boost level.
        /// </summary>
        /// <param name="count">The special attack stat stage modifier.</param>
        public TargetSpAtkBoost(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing TargetSpAtkBoost.
        /// </summary>
        protected TargetSpAtkBoost(TargetSpAtkBoost other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new TargetSpAtkBoost(this); }
    }

    /// <summary>
    /// Context state storing the target's special defense stat stage boost.
    /// Used in damage calculations accounting for target's special defenses.
    /// </summary>
    [Serializable]
    public class TargetSpDefBoost : ContextIntState
    {
        /// <inheritdoc/>
        public TargetSpDefBoost() { }

        /// <summary>
        /// Initializes a new instance with the specified boost level.
        /// </summary>
        /// <param name="count">The special defense stat stage modifier.</param>
        public TargetSpDefBoost(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing TargetSpDefBoost.
        /// </summary>
        protected TargetSpDefBoost(TargetSpDefBoost other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new TargetSpDefBoost(this); }
    }

    /// <summary>
    /// Context state storing the target's evasion stat stage boost.
    /// Used in hit chance calculations accounting for target's evasion.
    /// </summary>
    [Serializable]
    public class TargetEvasionBoost : ContextIntState
    {
        /// <inheritdoc/>
        public TargetEvasionBoost() { }

        /// <summary>
        /// Initializes a new instance with the specified boost level.
        /// </summary>
        /// <param name="count">The evasion stat stage modifier.</param>
        public TargetEvasionBoost(int count) : base(count) { }

        /// <summary>
        /// Copy constructor for cloning an existing TargetEvasionBoost.
        /// </summary>
        protected TargetEvasionBoost(TargetEvasionBoost other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new TargetEvasionBoost(this); }
    }

    /// <summary>
    /// Context state for tracking form changes during battle.
    /// Used by transformation moves and abilities that change forms.
    /// </summary>
    [Serializable]
    public class SwitchFormContext : ContextState
    {
        /// <summary>
        /// The target form index to switch to.
        /// </summary>
        public int Form;

        /// <inheritdoc/>
        public SwitchFormContext() { }

        /// <summary>
        /// Initializes a new instance with the specified target form index.
        /// </summary>
        /// <param name="form">The target form index to switch to.</param>
        public SwitchFormContext(int form) { Form = form; }

        /// <summary>
        /// Copy constructor for cloning an existing SwitchFormContext.
        /// </summary>
        protected SwitchFormContext(SwitchFormContext other)
        {
            Form = other.Form;
        }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new SwitchFormContext(this); }
    }

    /// <summary>
    /// Context state for tracking move learning operations.
    /// Stores which move to learn and which slot to replace.
    /// </summary>
    [Serializable]
    public class MoveLearnContext : ContextState
    {
        /// <summary>
        /// The skill ID of the move to learn.
        /// </summary>
        public string MoveLearn;

        /// <summary>
        /// The slot index to place the learned move in (or replace).
        /// </summary>
        public int ReplaceSlot;

        /// <inheritdoc/>
        public MoveLearnContext() { }

        /// <summary>
        /// Initializes a new instance with the move to learn and the replacement slot.
        /// </summary>
        /// <param name="moveLearn">The skill ID of the move to learn.</param>
        /// <param name="replaceSlot">The slot index to place the learned move in.</param>
        public MoveLearnContext(string moveLearn, int replaceSlot)
        {
            MoveLearn = moveLearn;
            ReplaceSlot = replaceSlot;
        }

        /// <summary>
        /// Copy constructor for cloning an existing MoveLearnContext.
        /// </summary>
        protected MoveLearnContext(MoveLearnContext other)
        {
            MoveLearn = other.MoveLearn;
            ReplaceSlot = other.ReplaceSlot;
        }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new MoveLearnContext(this); }
    }

    /// <summary>
    /// Context state for tracking ability learning operations.
    /// Stores which ability to learn and which slot to replace.
    /// </summary>
    [Serializable]
    public class AbilityLearnContext : ContextState
    {
        /// <summary>
        /// The intrinsic ID of the ability to learn.
        /// </summary>
        public string AbilityLearn;

        /// <summary>
        /// The slot index to place the learned ability in (or replace).
        /// </summary>
        public int ReplaceSlot;

        /// <inheritdoc/>
        public AbilityLearnContext() { }

        /// <summary>
        /// Initializes a new instance with the ability to learn and the replacement slot.
        /// </summary>
        /// <param name="abilityLearn">The intrinsic ID of the ability to learn.</param>
        /// <param name="replaceSlot">The slot index to place the learned ability in.</param>
        public AbilityLearnContext(string abilityLearn, int replaceSlot)
        {
            AbilityLearn = abilityLearn;
            ReplaceSlot = replaceSlot;
        }

        /// <summary>
        /// Copy constructor for cloning an existing AbilityLearnContext.
        /// </summary>
        protected AbilityLearnContext(AbilityLearnContext other)
        {
            AbilityLearn = other.AbilityLearn;
            ReplaceSlot = other.ReplaceSlot;
        }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new AbilityLearnContext(this); }
    }

    /// <summary>
    /// Context state for tracking move deletion operations.
    /// Stores which move slot to delete.
    /// </summary>
    [Serializable]
    public class MoveDeleteContext : ContextState
    {
        /// <summary>
        /// The slot index of the move to delete.
        /// </summary>
        public int MoveDelete;

        /// <inheritdoc/>
        public MoveDeleteContext() { }

        /// <summary>
        /// Initializes a new instance with the specified move slot to delete.
        /// </summary>
        /// <param name="slot">The slot index of the move to delete.</param>
        public MoveDeleteContext(int slot) { MoveDelete = slot; }

        /// <summary>
        /// Copy constructor for cloning an existing MoveDeleteContext.
        /// </summary>
        protected MoveDeleteContext(MoveDeleteContext other)
        {
            MoveDelete = other.MoveDelete;
        }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new MoveDeleteContext(this); }
    }

    /// <summary>
    /// Context state for tracking ability deletion operations.
    /// Stores which ability slot to delete.
    /// </summary>
    [Serializable]
    public class AbilityDeleteContext : ContextState
    {
        /// <summary>
        /// The slot index of the ability to delete.
        /// </summary>
        public int AbilityDelete;

        /// <inheritdoc/>
        public AbilityDeleteContext() { }

        /// <summary>
        /// Initializes a new instance with the specified ability slot to delete.
        /// </summary>
        /// <param name="slot">The slot index of the ability to delete.</param>
        public AbilityDeleteContext(int slot) { AbilityDelete = slot; }

        /// <summary>
        /// Copy constructor for cloning an existing AbilityDeleteContext.
        /// </summary>
        protected AbilityDeleteContext(AbilityDeleteContext other)
        {
            AbilityDelete = other.AbilityDelete;
        }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new AbilityDeleteContext(this); }
    }

    /// <summary>
    /// Context state for tracking team member withdrawal from assembly.
    /// Stores which assembly slot to withdraw from.
    /// </summary>
    [Serializable]
    public class WithdrawAssemblyContext : ContextState
    {
        /// <summary>
        /// The assembly slot index to withdraw the team member from.
        /// </summary>
        public int WithdrawSlot;

        /// <inheritdoc/>
        public WithdrawAssemblyContext() { }

        /// <summary>
        /// Initializes a new instance with the specified assembly slot to withdraw from.
        /// </summary>
        /// <param name="slot">The assembly slot index to withdraw from.</param>
        public WithdrawAssemblyContext(int slot) { WithdrawSlot = slot; }

        /// <summary>
        /// Copy constructor for cloning an existing WithdrawAssemblyContext.
        /// </summary>
        protected WithdrawAssemblyContext(WithdrawAssemblyContext other)
        {
            WithdrawSlot = other.WithdrawSlot;
        }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new WithdrawAssemblyContext(this); }
    }

    /// <summary>
    /// Context state for tracking item withdrawal from storage.
    /// Stores which storage slot to withdraw from.
    /// </summary>
    [Serializable]
    public class WithdrawStorageContext : ContextState
    {
        /// <summary>
        /// The storage slot to withdraw the item from.
        /// </summary>
        public WithdrawSlot WithdrawSlot;

        /// <inheritdoc/>
        public WithdrawStorageContext() { }

        /// <summary>
        /// Initializes a new instance with the specified storage slot to withdraw from.
        /// </summary>
        /// <param name="slot">The storage slot to withdraw from.</param>
        public WithdrawStorageContext(WithdrawSlot slot) { WithdrawSlot = slot; }

        /// <summary>
        /// Copy constructor for cloning an existing WithdrawStorageContext.
        /// </summary>
        protected WithdrawStorageContext(WithdrawStorageContext other)
        {
            WithdrawSlot = other.WithdrawSlot;
        }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new WithdrawStorageContext(this); }
    }

    /// <summary>
    /// Context state for tracking item deposit to storage.
    /// Stores which inventory slot to deposit from.
    /// </summary>
    [Serializable]
    public class DepositStorageContext : ContextState
    {
        /// <summary>
        /// The inventory slot to deposit the item from.
        /// </summary>
        public InvSlot DepositSlot;

        /// <inheritdoc/>
        public DepositStorageContext() { }

        /// <summary>
        /// Initializes a new instance with the specified inventory slot to deposit from.
        /// </summary>
        /// <param name="slot">The inventory slot to deposit from.</param>
        public DepositStorageContext(InvSlot slot) { DepositSlot = slot; }

        /// <summary>
        /// Copy constructor for cloning an existing DepositStorageContext.
        /// </summary>
        protected DepositStorageContext(DepositStorageContext other)
        {
            DepositSlot = other.DepositSlot;
        }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new DepositStorageContext(this); }
    }

    /// <summary>
    /// Context state for Judgment and Multi-Attack type determination.
    /// Stores the list of possible element types based on held plate/memory.
    /// </summary>
    [Serializable]
    public class JudgmentContext : ContextState
    {
        /// <summary>
        /// List of element type IDs that can be used by the move.
        /// </summary>
        [JsonConverter(typeof(ElementListConverter))]
        public List<string> Elements;

        /// <summary>
        /// Initializes a new instance with an empty element list.
        /// </summary>
        public JudgmentContext() { Elements = new List<string>(); }

        /// <summary>
        /// Initializes a new instance with the specified element list.
        /// </summary>
        /// <param name="elements">The list of possible element type IDs that can be used by the move.</param>
        public JudgmentContext(List<string> elements) { Elements = elements; }

        /// <summary>
        /// Copy constructor for cloning an existing JudgmentContext.
        /// </summary>
        protected JudgmentContext(JudgmentContext other) : this()
        {
            Elements.AddRange(other.Elements);
        }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new JudgmentContext(this); }
    }

    /// <summary>
    /// Marker context state indicating silk-type item boost is active.
    /// Used by type-boosting held items (Silk Scarf, plates, etc.).
    /// </summary>
    [Serializable]
    public class SilkState : ContextState
    {
        /// <inheritdoc/>
        public SilkState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new SilkState(); }
    }

    /// <summary>
    /// Marker context state indicating dust-type item effect is active.
    /// Used by dust items that modify type effectiveness.
    /// </summary>
    [Serializable]
    public class DustState : ContextState
    {
        /// <inheritdoc/>
        public DustState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new DustState(); }
    }
}

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
    /// Abstract base class for battle events that handle direct damage calculations and application.
    /// Provides common functionality for inflicting damage with type effectiveness feedback.
    /// </summary>
    [Serializable]
    public abstract class DirectDamageEvent : BattleEvent
    {
        /// <summary>
        /// Inflicts damage to the target with visual and audio feedback based on type effectiveness.
        /// </summary>
        /// <param name="context">The battle context containing user, target, and action data.</param>
        /// <param name="dmg">The amount of damage to inflict. Use -1 for OHKO.</param>
        /// <returns>A coroutine that handles the damage animation and application.</returns>
        protected IEnumerator<YieldInstruction> InflictDamage(BattleContext context, int dmg)
        {
            bool fastSpeed = (DiagManager.Instance.CurSettings.BattleFlow > Settings.BattleSpeed.Fast);
            bool hasEffect = (context.Data.HitFX.Delay == 0 && context.Data.HitFX.Sound != "");//determines if a sound plays at the same frame the move hits

            if (hasEffect && fastSpeed)
            {

            }
            else
            {
                if (hasEffect)
                    yield return new WaitForFrames(GameManager.Instance.ModifyBattleSpeed(10, context.Target.CharLoc));
                int typeMatchup = PreTypeEvent.GetDualEffectiveness(context.User, context.Target, context.Data);

                SingleEmitter endEmitter = null;
                if (typeMatchup == PreTypeEvent.NRM_2 || fastSpeed)
                {
                    GameManager.Instance.BattleSE("DUN_Hit_Neutral");
                    endEmitter = new SingleEmitter(new AnimData("Hit_Neutral", 3));
                }
                else if (typeMatchup >= PreTypeEvent.S_E_2)
                {
                    GameManager.Instance.BattleSE("DUN_Hit_Super_Effective");
                    endEmitter = new SingleEmitter(new AnimData("Hit_Super_Effective", 3));
                }
                else
                {
                    GameManager.Instance.BattleSE("DUN_Hit_NVE");
                    endEmitter = new SingleEmitter(new AnimData("Hit_Neutral", 3));
                }

                if (!context.Target.Unidentifiable)
                {
                    endEmitter.SetupEmit(context.Target.MapLoc, context.User.MapLoc, context.Target.CharDir);
                    DungeonScene.Instance.CreateAnim(endEmitter, DrawLayer.NoDraw);
                }
            }

            bool endure = context.ContextStates.Contains<AttackEndure>();
            yield return CoroutineManager.Instance.StartCoroutine(context.Target.InflictDamage(dmg, true, endure));

            if (context.Target.HP == 0)
            {
                context.ContextStates.Set(new Knockout());
                context.AddContextStateInt<TotalKnockouts>(true, 1);
            }
        }

        /// <summary>
        /// Records damage dealt and HP lost in the battle context for use by subsequent events.
        /// </summary>
        /// <param name="context">The battle context to update.</param>
        /// <param name="dmg">The calculated damage amount.</param>
        /// <param name="hpLost">The actual HP lost by the target.</param>
        protected void ReportDamage(BattleContext context, int dmg, int hpLost)
        {
            context.ContextStates.Set(new DamageDealt(dmg));
            context.AddContextStateInt<TotalDamageDealt>(true, dmg);
            context.ContextStates.Set(new HPLost(hpLost));
            context.AddContextStateInt<TotalHPLost>(true, hpLost);
        }
    }

    /// <summary>
    /// Event that inflicts a one-hit knockout (OHKO) on the target.
    /// Respects type immunity and damage multiplier modifiers.
    /// </summary>
    [Serializable]
    public class OHKODamageEvent : DirectDamageEvent
    {
        /// <inheritdoc/>
        public OHKODamageEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new OHKODamageEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            int prevHP = context.Target.HP;

            int dmg = -1;

            if (!context.GetContextStateMult<DmgMult>().IsNeutralized())
            {
                int typeMatchup = PreTypeEvent.GetDualEffectiveness(context.User, context.Target, context.Data);
                if (typeMatchup <= PreTypeEvent.N_E_2)
                {
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(PreTypeEvent.EffectivenessToPhrase(typeMatchup), context.Target.GetDisplayName(false)));
                    context.AddContextStateMult<DmgMult>(false, -1, 4);
                }
            }

            int dmgMod = context.GetContextStateMult<DmgMult>().Multiply(0);
            if (dmgMod >= 0)
            {
                if (context.GetContextStateMult<DmgMult>().IsNeutralized())
                    dmg = 0;

                yield return CoroutineManager.Instance.StartCoroutine(InflictDamage(context, dmg));
            }

            int hpLost = prevHP - context.Target.HP;
            ReportDamage(context, hpLost, hpLost);
        }
    }

    /// <summary>
    /// Abstract base class for damage events that require custom damage calculation logic.
    /// Subclasses implement the CalculateDamage method to define their specific damage formula.
    /// </summary>
    [Serializable]
    public abstract class CalculatedDamageEvent : DirectDamageEvent
    {
        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            int damage = CalculateDamage(owner, context);

            int prevHP = context.Target.HP;
            if (damage >= 0)
                yield return CoroutineManager.Instance.StartCoroutine(InflictDamage(context, damage));

            int hpLost = prevHP - context.Target.HP;
            ReportDamage(context, Math.Max(0, damage), hpLost);
        }

        /// <summary>
        /// Calculates the damage to be dealt based on the specific event implementation.
        /// </summary>
        /// <param name="owner">The owner of this event.</param>
        /// <param name="context">The battle context.</param>
        /// <returns>The calculated damage value. Negative values indicate the attack should not deal damage.</returns>
        public abstract int CalculateDamage(GameEventOwner owner, BattleContext context);
    }

    /// <summary>
    /// Event that calculates damage using the standard damage formula.
    /// Takes into account type effectiveness, critical hits, stat boosts, and STAB (Same Type Attack Bonus).
    /// </summary>
    [Serializable]
    public class DamageFormulaEvent : CalculatedDamageEvent
    {
        /// <inheritdoc/>
        public DamageFormulaEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new DamageFormulaEvent(); }

        /// <inheritdoc/>
        public override int CalculateDamage(GameEventOwner owner, BattleContext context)
        {
            return CalculateDamageFormula(owner, context);
        }

        /// <summary>
        /// Static method that performs the full damage formula calculation including
        /// attack/defense stats, type effectiveness, STAB, and critical hits.
        /// </summary>
        /// <param name="owner">The owner of this event.</param>
        /// <param name="context">The battle context.</param>
        /// <returns>The calculated damage value.</returns>
        public static int CalculateDamageFormula(GameEventOwner owner, BattleContext context)
        {
            //PreExecuteAction: attacker attack/spAtk and level are assigned
            //in OnAction:
            //  -AttackBoost, SpAtkBoost, DefBoost, SpDefBoost, AccuracyMod are added

            //PreMoveHit: target defense/SpDef is assigned
            //in BeforeHit:
            //  -TargetAttackBoost, TargetSpAtkBoost, TargetDefenseBoost, TargetSpDefBoost, EvasionMod are added

            if (!context.GetContextStateMult<DmgMult>().IsNeutralized())
            {
                string effectivenessMsg = null;

                //modify attack based on battle tag
                int atkBoost = 0;
                int defBoost = 0;
                if (context.Data.Category == BattleData.SkillCategory.Physical || context.Data.Category == BattleData.SkillCategory.Magical)
                {
                    BattleData.SkillCategory attackCategory = context.Data.Category;
                    if (context.ContextStates.Contains<CrossCategory>())
                    {
                        if (attackCategory == BattleData.SkillCategory.Physical)
                            attackCategory = BattleData.SkillCategory.Magical;
                        else if (attackCategory == BattleData.SkillCategory.Magical)
                            attackCategory = BattleData.SkillCategory.Physical;
                    }

                    //adjust attack
                    if (attackCategory == BattleData.SkillCategory.Physical)
                        atkBoost = context.GetContextStateInt<UserAtkBoost>(0);
                    else if (attackCategory == BattleData.SkillCategory.Magical)
                        atkBoost = context.GetContextStateInt<UserSpAtkBoost>(0);

                    //adjust defense
                    if (context.Data.Category == BattleData.SkillCategory.Physical)
                        defBoost = context.GetContextStateInt<TargetDefBoost>(0);
                    else if (context.Data.Category == BattleData.SkillCategory.Magical)
                        defBoost = context.GetContextStateInt<TargetSpDefBoost>(0);
                }

                int critLevel = context.GetContextStateInt<CritLevel>(0);
                CritRateLevelTableState critTable = DataManager.Instance.UniversalEvent.UniversalStates.GetWithDefault<CritRateLevelTableState>();
                if (DataManager.Instance.Save.Rand.Next(0, 12) < critTable.GetCritChance(critLevel))
                {
                    //see if it criticals
                    if (context.User.CharStates.Contains<SnipeState>())
                        context.AddContextStateMult<DmgMult>(false, 5, 2);
                    else
                        context.AddContextStateMult<DmgMult>(false, 3, 2);

                    atkBoost = Math.Max(0, atkBoost);
                    defBoost = Math.Min(0, defBoost);

                    effectivenessMsg = Text.FormatGrammar(new StringKey("MSG_CRITICAL_HIT").ToLocal());
                    context.ContextStates.Set(new AttackCrit());
                }

                AtkDefLevelTableState dmgModTable = DataManager.Instance.UniversalEvent.UniversalStates.GetWithDefault<AtkDefLevelTableState>();
                int attackStat = dmgModTable.AtkLevelMult(context.GetContextStateInt<UserAtkStat>(1), atkBoost);
                int defenseStat = Math.Max(1, dmgModTable.DefLevelMult(context.GetContextStateInt<TargetDefStat>(1), defBoost));

                //STAB
                if (context.User.HasElement(context.Data.Element))
                    context.AddContextStateMult<DmgMult>(false, 4, 3);

                int typeMatchup = PreTypeEvent.GetDualEffectiveness(context.User, context.Target, context.Data);
                if (typeMatchup != PreTypeEvent.NRM_2)
                {
                    if (effectivenessMsg != null)
                        effectivenessMsg += (" " + Text.FormatGrammar(PreTypeEvent.EffectivenessToPhrase(typeMatchup), context.Target.GetDisplayName(false)));
                    else
                        effectivenessMsg = Text.FormatGrammar(PreTypeEvent.EffectivenessToPhrase(typeMatchup), context.Target.GetDisplayName(false));

                    int effectiveness = PreTypeEvent.GetEffectivenessMult(typeMatchup);
                    if (effectiveness == 0)
                        effectiveness = -1;

                    context.AddContextStateMult<DmgMult>(false, effectiveness, PreTypeEvent.GetEffectivenessMult(PreTypeEvent.NRM_2));
                }

                if (effectivenessMsg != null)
                    DungeonScene.Instance.LogMsg(effectivenessMsg);

                if (context.GetContextStateMult<DmgMult>().IsNeutralized())
                    return context.GetContextStateMult<DmgMult>().Multiply(0);

                int power = context.Data.SkillStates.GetWithDefault<BasePowerState>().Power;
                int damage = context.GetContextStateMult<DmgMult>().Multiply((context.GetContextStateInt<UserLevel>(0) / 3 + 6) * attackStat * power) / defenseStat / 50 * DataManager.Instance.Save.Rand.Next(90, 101) / 100;

                if (!(context.ActionType == BattleActionType.Skill && context.Data.ID == DataManager.Instance.DefaultSkill))
                    damage = Math.Max(1, damage);

                return damage;
            }
            else
                return context.GetContextStateMult<DmgMult>().Multiply(0);
        }
    }


    /// <summary>
    /// Abstract base class for damage events that deal fixed damage amounts.
    /// Bypasses the normal damage formula but still respects type immunity.
    /// </summary>
    [Serializable]
    public abstract class FixedDamageEvent : CalculatedDamageEvent
    {
        /// <inheritdoc/>
        public override int CalculateDamage(GameEventOwner owner, BattleContext context)
        {
            int damage = CalculateFixedDamage(owner, context);

            int typeMatchup = PreTypeEvent.GetDualEffectiveness(context.User, context.Target, context.Data);
            if (typeMatchup <= PreTypeEvent.N_E_2)
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(PreTypeEvent.EffectivenessToPhrase(typeMatchup), context.Target.GetDisplayName(false)));
                damage = -1;
            }

            return damage;
        }

        /// <summary>
        /// Calculates the fixed damage amount for this event.
        /// </summary>
        /// <param name="owner">The owner of this event.</param>
        /// <param name="context">The battle context.</param>
        /// <returns>The fixed damage value.</returns>
        protected abstract int CalculateFixedDamage(GameEventOwner owner, BattleContext context);
    }

    /// <summary>
    /// Event that deals fixed damage equal to the skill's base power value.
    /// </summary>
    [Serializable]
    public class BasePowerDamageEvent : FixedDamageEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new BasePowerDamageEvent(); }

        /// <inheritdoc/>
        protected override int CalculateFixedDamage(GameEventOwner owner, BattleContext context)
        {
            BasePowerState state = context.Data.SkillStates.GetWithDefault<BasePowerState>();
            if (state != null)
                return state.Power;
            return 0;
        }
    }

    /// <summary>
    /// Event that deals a specific fixed damage amount to the target.
    /// </summary>
    [Serializable]
    public class SpecificDamageEvent : FixedDamageEvent
    {
        /// <summary>
        /// The fixed damage amount to deal.
        /// </summary>
        public int Damage;

        /// <inheritdoc/>
        public SpecificDamageEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpecificDamageEvent"/> class with the specified damage amount.
        /// </summary>
        /// <param name="dmg">The fixed damage to deal.</param>
        public SpecificDamageEvent(int dmg) { Damage = dmg; }

        /// <inheritdoc/>
        public SpecificDamageEvent(SpecificDamageEvent other)
        {
            Damage = other.Damage;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new SpecificDamageEvent(this); }

        /// <inheritdoc/>
        protected override int CalculateFixedDamage(GameEventOwner owner, BattleContext context)
        {
            return Damage;
        }
    }

    /// <summary>
    /// Event that calculates damage based on a character's level.
    /// The formula is: level * Numerator / Denominator.
    /// </summary>
    [Serializable]
    public class LevelDamageEvent : FixedDamageEvent
    {
        /// <summary>
        /// Whether to calculate with the target's level (true) or user's level (false).
        /// </summary>
        public bool AffectTarget;

        /// <summary>
        /// The numerator of the level modifier fraction.
        /// </summary>
        public int Numerator;

        /// <summary>
        /// The denominator of the level modifier fraction.
        /// </summary>
        public int Denominator;

        /// <inheritdoc/>
        public LevelDamageEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LevelDamageEvent"/> class with the specified parameters.
        /// </summary>
        /// <param name="affectTarget">Whether to use the target's level.</param>
        /// <param name="numerator">The numerator for level calculation.</param>
        /// <param name="denominator">The denominator for level calculation.</param>
        public LevelDamageEvent(bool affectTarget, int numerator, int denominator)
        {
            AffectTarget = affectTarget;
            Numerator = numerator;
            Denominator = denominator;
        }

        /// <inheritdoc/>
        protected LevelDamageEvent(LevelDamageEvent other)
        {
            AffectTarget = other.AffectTarget;
            Numerator = other.Numerator;
            Denominator = other.Denominator;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new LevelDamageEvent(this); }

        /// <inheritdoc/>
        protected override int CalculateFixedDamage(GameEventOwner owner, BattleContext context)
        {
            int level = (AffectTarget ? context.Target.Level : context.GetContextStateInt<UserLevel>(0));
            return level * Numerator / Denominator;
        }
    }

    /// <summary>
    /// Event that deals fixed damage based on the target's distance from the attack origin and the user's level.
    /// Damage follows a wave pattern (0, 1, 2, 1, 0, 1, 2, 1...) based on distance modulo 4.
    /// </summary>
    [Serializable]
    public class PsywaveDamageEvent : FixedDamageEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new PsywaveDamageEvent(); }

        /// <inheritdoc/>
        protected override int CalculateFixedDamage(GameEventOwner owner, BattleContext context)
        {
            // 1 2 1 0 1 2 1 0
            // sine wave function
            //TODO: this breaks in small wrapped maps
            int locDiff = ZoneManager.Instance.CurrentMap.GetClosestDist8(context.StrikeStartTile, context.Target.CharLoc);
            int diff = locDiff % 4;
            int power = (diff > 2) ? 1 : diff;
            return Math.Max(1, context.GetContextStateInt<UserLevel>(0) * power / 2);
        }
    }

    /// <summary>
    /// Event that deals fixed damage based on the user's current HP or missing HP.
    /// </summary>
    [Serializable]
    public class UserHPDamageEvent : FixedDamageEvent
    {
        /// <summary>
        /// When true, deals damage based on the HP the user is missing (MaxHP - HP) instead of current HP.
        /// </summary>
        public bool Reverse;

        /// <inheritdoc/>
        public UserHPDamageEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserHPDamageEvent"/> class.
        /// </summary>
        /// <param name="reverse">If true, use missing HP; if false, use current HP.</param>
        public UserHPDamageEvent(bool reverse)
        {
            Reverse = reverse;
        }

        /// <inheritdoc/>
        protected UserHPDamageEvent(UserHPDamageEvent other)
        {
            Reverse = other.Reverse;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new UserHPDamageEvent(this); }

        /// <inheritdoc/>
        protected override int CalculateFixedDamage(GameEventOwner owner, BattleContext context)
        {
            return Reverse ? (context.User.MaxHP - context.User.HP) : context.User.HP;
        }
    }

    /// <summary>
    /// Event that reduces the target's HP to match the user's HP.
    /// Deals damage equal to the difference if the target has more HP than the user.
    /// </summary>
    [Serializable]
    public class EndeavorEvent : FixedDamageEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new EndeavorEvent(); }

        /// <inheritdoc/>
        protected override int CalculateFixedDamage(GameEventOwner owner, BattleContext context)
        {
            return Math.Max(0, context.Target.HP - context.User.HP);
        }
    }

    /// <summary>
    /// Event that reduces the target's HP by half their current HP.
    /// </summary>
    [Serializable]
    public class CutHPDamageEvent : FixedDamageEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new CutHPDamageEvent(); }

        /// <inheritdoc/>
        protected override int CalculateFixedDamage(GameEventOwner owner, BattleContext context)
        {
            return Math.Max(1, context.GetContextStateMult<HPDmgMult>().Multiply(context.Target.HP / 2));
        }
    }

    /// <summary>
    /// Event that deals damage equal to a fraction of the target's maximum HP.
    /// </summary>
    [Serializable]
    public class MaxHPDamageEvent : FixedDamageEvent
    {
        /// <summary>
        /// The divisor for the target's max HP to calculate damage.
        /// For example, a value of 4 deals 25% of max HP as damage.
        /// </summary>
        public int HPFraction;

        /// <inheritdoc/>
        public MaxHPDamageEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaxHPDamageEvent"/> class with the specified HP fraction divisor.
        /// </summary>
        /// <param name="hpFraction">The divisor for max HP calculation.</param>
        public MaxHPDamageEvent(int hpFraction)
        {
            HPFraction = hpFraction;
        }

        /// <inheritdoc/>
        protected MaxHPDamageEvent(MaxHPDamageEvent other)
        {
            HPFraction = other.HPFraction;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new MaxHPDamageEvent(this); }

        /// <inheritdoc/>
        protected override int CalculateFixedDamage(GameEventOwner owner, BattleContext context)
        {
            return Math.Max(1, context.GetContextStateMult<HPDmgMult>().Multiply(context.Target.MaxHP / HPFraction));
        }
    }

}


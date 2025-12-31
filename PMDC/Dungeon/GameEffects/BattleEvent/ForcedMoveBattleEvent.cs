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
    /// Battle events that force the user to perform a specific move.
    /// </summary>

    /// <summary>
    /// Event that forces the character to use the specified move.
    /// Usually used for moves that charge up.
    /// </summary>
    [Serializable]
    public class ForceMoveEvent : BattleEvent
    {
        /// <summary>
        /// The move ID to force the character to use.
        /// </summary>
        [JsonConverter(typeof(SkillConverter))]
        [DataType(0, DataManager.DataType.Skill, false)]
        public string MoveIndex;

        /// <inheritdoc/>
        public ForceMoveEvent() { MoveIndex = ""; }

        /// <summary>
        /// Creates a new ForceMoveEvent for the specified move.
        /// </summary>
        /// <param name="moveIndex">The move ID to force the character to use.</param>
        public ForceMoveEvent(string moveIndex)
        {
            MoveIndex = moveIndex;
        }

        /// <inheritdoc/>
        protected ForceMoveEvent(ForceMoveEvent other)
        {
            MoveIndex = other.MoveIndex;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ForceMoveEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.UsageSlot == BattleContext.FORCED_SLOT)
                yield break;

            context.UsageSlot = BattleContext.FAKE_ATTACK_SLOT;

            SkillData entry = DataManager.Instance.GetSkill(MoveIndex);
            context.Data = new BattleData(entry.Data);
            context.Data.ID = MoveIndex;
            context.Data.DataType = DataManager.DataType.Skill;
            context.Explosion = new ExplosionData(entry.Explosion);
            context.HitboxAction = entry.HitboxAction.Clone();
            context.Item = new InvItem();
            context.Strikes = entry.Strikes;

            context.SetActionMsg(Text.FormatGrammar(new StringKey("MSG_SKILL_USE").ToLocal(), context.User.GetDisplayName(false), entry.GetIconName()));
        }
    }


    /// <summary>
    /// Event that changes the hitbox action, explosion data, and battle data
    /// if the MoveCharge context state is not present.
    /// </summary>
    [Serializable]
    public class ChargeCustomEvent : BattleEvent
    {
        /// <summary>
        /// The alternate hitbox action.
        /// </summary>
        public CombatAction HitboxAction;

        /// <summary>
        /// The alternate explosion data.
        /// </summary>
        public ExplosionData Explosion;

        /// <summary>
        /// The alternate battle data.
        /// </summary>
        public BattleData NewData;

        /// <inheritdoc/>
        public ChargeCustomEvent() { }

        /// <summary>
        /// Creates a new ChargeCustomEvent with alternate action, explosion, and battle data.
        /// </summary>
        /// <param name="action">The alternate hitbox action.</param>
        /// <param name="explosion">The alternate explosion data.</param>
        /// <param name="moveData">The alternate battle data.</param>
        public ChargeCustomEvent(CombatAction action, ExplosionData explosion, BattleData moveData)
        {
            HitboxAction = action;
            Explosion = explosion;
            NewData = moveData;
        }

        /// <inheritdoc/>
        protected ChargeCustomEvent(ChargeCustomEvent other)
        {
            HitboxAction = other.HitboxAction;
            Explosion = other.Explosion;
            NewData = new BattleData(other.NewData);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ChargeCustomEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (!context.ContextStates.Contains<MoveCharge>())
            {
                context.Data = new BattleData(NewData);
                context.Data.ID = context.Data.ID;
                context.Data.DataType = context.Data.DataType;

                context.Explosion = new ExplosionData(Explosion);

                context.HitboxAction = HitboxAction.Clone();

                context.Item = new InvItem();
                context.Strikes = 1;

                context.SetActionMsg("");
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that sets the specified charge status and alternate hitbox action
    /// if the MoveCharge context state is not present.
    /// Usually used for moves that charge up.
    /// </summary>
    [Serializable]
    public class ChargeOrReleaseEvent : BattleEvent
    {
        /// <summary>
        /// The status representing the move charging up.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string ChargeStatus;

        /// <summary>
        /// Alternate data on the hitbox of the attack. Controls range and targeting.
        /// </summary>
        public CombatAction HitboxAction;

        /// <inheritdoc/>
        public ChargeOrReleaseEvent() { ChargeStatus = ""; }

        /// <summary>
        /// Creates a new ChargeOrReleaseEvent with the specified charge status and hitbox action.
        /// </summary>
        /// <param name="chargeStatus">The status representing the move charging up.</param>
        /// <param name="action">The alternate hitbox action.</param>
        public ChargeOrReleaseEvent(string chargeStatus, CombatAction action)
        {
            ChargeStatus = chargeStatus;
            HitboxAction = action;
        }

        /// <inheritdoc/>
        protected ChargeOrReleaseEvent(ChargeOrReleaseEvent other)
        {
            ChargeStatus = other.ChargeStatus;
            HitboxAction = other.HitboxAction.Clone();
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ChargeOrReleaseEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (!context.ContextStates.Contains<MoveCharge>())
            {
                BattleData altMoveData = new BattleData();
                altMoveData.Element = DataManager.Instance.DefaultElement;
                altMoveData.Category = BattleData.SkillCategory.None;
                altMoveData.HitRate = -1;
                altMoveData.OnHits.Add(0, new StatusBattleEvent(ChargeStatus, true, false));
                altMoveData.ID = context.Data.ID;
                altMoveData.DataType = context.Data.DataType;
                context.Data = new BattleData(altMoveData);

                ExplosionData altExplosion = new ExplosionData();
                altExplosion.TargetAlignments |= Alignment.Self;
                context.Explosion = new ExplosionData(altExplosion);

                context.HitboxAction = HitboxAction.Clone();

                context.Item = new InvItem();
                context.Strikes = 1;

                context.SetActionMsg("");
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that sets the specified charge status if the MoveCharge context state is not present.
    /// Used specifically for the move Bide.
    /// </summary>
    [Serializable]
    public class BideOrReleaseEvent : BattleEvent
    {
        /// <summary>
        /// The status representing the move charging up.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string ChargeStatus;

        /// <summary>
        /// The particle VFX emitter.
        /// </summary>
        public FiniteEmitter IntroEmitter;

        /// <summary>
        /// The sound effect of the VFX.
        /// </summary>
        [Sound(0)]
        public string IntroSound;

        /// <inheritdoc/>
        public BideOrReleaseEvent() { ChargeStatus = ""; }

        /// <summary>
        /// Creates a new BideOrReleaseEvent with the specified parameters.
        /// </summary>
        /// <param name="chargeStatus">The status representing the move charging up.</param>
        /// <param name="introEmitter">The particle VFX emitter.</param>
        /// <param name="introSound">The sound effect to play.</param>
        public BideOrReleaseEvent(string chargeStatus, FiniteEmitter introEmitter, string introSound)
        {
            ChargeStatus = chargeStatus;
            IntroEmitter = introEmitter;
            IntroSound = introSound;
        }

        /// <inheritdoc/>
        protected BideOrReleaseEvent(BideOrReleaseEvent other)
        {
            ChargeStatus = other.ChargeStatus;
            IntroEmitter = (FiniteEmitter)other.IntroEmitter.Clone();
            IntroSound = other.IntroSound;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new BideOrReleaseEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (!context.ContextStates.Contains<MoveBide>())
            {
                BattleData altMoveData = new BattleData();
                altMoveData.Element = DataManager.Instance.DefaultElement;
                altMoveData.Category = BattleData.SkillCategory.None;
                altMoveData.HitRate = -1;
                altMoveData.OnHits.Add(0, new StatusBattleEvent(ChargeStatus, true, false));
                altMoveData.ID = context.Data.ID;
                altMoveData.DataType = context.Data.DataType;
                context.Data = new BattleData(altMoveData);

                ExplosionData altExplosion = new ExplosionData();
                altExplosion.TargetAlignments |= Alignment.Self;
                context.Explosion = new ExplosionData(altExplosion);

                SelfAction altAction = new SelfAction();
                altAction.CharAnimData = new CharAnimFrameType(GraphicsManager.ChargeAction);
                altAction.TargetAlignments |= Alignment.Self;
                BattleFX newFX = new BattleFX();
                newFX.Emitter = (FiniteEmitter)IntroEmitter.Clone();
                newFX.Sound = IntroSound;
                altAction.PreActions.Add(newFX);
                context.HitboxAction = altAction;

                context.Item = new InvItem();
                context.Strikes = 1;

                //still declare the move
            }
            else
                context.SetActionMsg(Text.FormatGrammar(new StringKey("MSG_BIDE_ATTACK").ToLocal(), context.User.GetDisplayName(false)));
            yield break;
        }
    }

    /// <summary>
    /// Event that sets the specified charge status if the FollowUp context state is not present.
    /// Used specifically for the moves Retaliate and Fire/Water/Grass Pledge.
    /// </summary>
    [Serializable]
    public class WatchOrStrikeEvent : BattleEvent
    {
        /// <summary>
        /// The status representing the move charging up.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string ChargeStatus;

        /// <summary>
        /// The particle VFX emitter.
        /// </summary>
        public FiniteEmitter IntroEmitter;

        /// <summary>
        /// The sound effect of the VFX.
        /// </summary>
        [Sound(0)]
        public string IntroSound;

        /// <inheritdoc/>
        public WatchOrStrikeEvent() { ChargeStatus = ""; }

        /// <summary>
        /// Creates a new WatchOrStrikeEvent with the specified parameters.
        /// </summary>
        /// <param name="chargeStatus">The status representing the move charging up.</param>
        /// <param name="introEmitter">The particle VFX emitter.</param>
        /// <param name="introSound">The sound effect to play.</param>
        public WatchOrStrikeEvent(string chargeStatus, FiniteEmitter introEmitter, string introSound)
        {
            ChargeStatus = chargeStatus;
            IntroEmitter = introEmitter;
            IntroSound = introSound;
        }

        /// <inheritdoc/>
        protected WatchOrStrikeEvent(WatchOrStrikeEvent other)
        {
            ChargeStatus = other.ChargeStatus;
            IntroEmitter = (FiniteEmitter)other.IntroEmitter.Clone();
            IntroSound = other.IntroSound;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new WatchOrStrikeEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (!context.ContextStates.Contains<FollowUp>())
            {
                BattleData altMoveData = new BattleData();
                altMoveData.Element = DataManager.Instance.DefaultElement;
                altMoveData.Category = BattleData.SkillCategory.None;
                altMoveData.HitRate = -1;
                altMoveData.OnHits.Add(0, new StatusBattleEvent(ChargeStatus, true, false));
                altMoveData.ID = context.Data.ID;
                altMoveData.DataType = context.Data.DataType;
                context.Data = new BattleData(altMoveData);

                ExplosionData altExplosion = new ExplosionData();
                altExplosion.TargetAlignments |= Alignment.Self;
                context.Explosion = new ExplosionData(altExplosion);

                SelfAction altAction = new SelfAction();
                altAction.CharAnimData = new CharAnimFrameType(GraphicsManager.ChargeAction);
                altAction.TargetAlignments |= Alignment.Self;
                BattleFX newFX = new BattleFX();
                newFX.Emitter = (FiniteEmitter)IntroEmitter.Clone();
                newFX.Sound = IntroSound;
                altAction.PreActions.Add(newFX);
                context.HitboxAction = altAction;

                context.Item = new InvItem();
                context.Strikes = 1;

                context.SetActionMsg("");
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that increases the HP in the HPState status state by the damage received.
    /// </summary>
    [Serializable]
    public class BideEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new BideEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            HPState state = ((StatusEffect)owner).StatusStates.GetWithDefault<HPState>();
            if (state != null)
                state.HP += context.GetContextStateInt<DamageDealt>(0);
            yield break;
        }
    }

    /// <summary>
    /// Event that unleashes double the damage in HPState status state when the CountDownState status state reaches 0.
    /// Used by the Biding status.
    /// </summary>
    [Serializable]
    public class UnleashEvent : BattleEvent
    {
        /// <inheritdoc/>
        public UnleashEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new UnleashEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.UsageSlot == BattleContext.FORCED_SLOT)
                yield break;

            ((StatusEffect)owner).StatusStates.GetWithDefault<CountDownState>().Counter--;
            if (((StatusEffect)owner).StatusStates.GetWithDefault<CountDownState>().Counter <= 0)
            {
                yield return CoroutineManager.Instance.StartCoroutine(context.User.RemoveStatusEffect(((StatusEffect)owner).ID));

                HPState state = ((StatusEffect)owner).StatusStates.GetWithDefault<HPState>();
                BasePowerState basePower = context.Data.SkillStates.GetWithDefault<BasePowerState>();
                if (basePower != null && state != null)
                    basePower.Power += state.HP * 2;
            }
            else
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_STORE_ENERGY").ToLocal(), context.User.GetDisplayName(false)));
                context.CancelState.Cancel = true;
            }
        }
    }
}

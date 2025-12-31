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
    /// Battle events that handle recoil damage to the user.
    /// </summary>

    /// <summary>
    /// Event that recoil damage to the user based on how much damage was dealt
    /// </summary>
    [Serializable]
    public class DamageRecoilEvent : RecoilEvent
    {
        /// <summary>
        /// The value dividing the total damage dealt representing the recoil damage
        /// </summary>
        public int Fraction;

        /// <summary>
        /// Initializes a new instance of the <see cref="DamageRecoilEvent"/> class.
        /// </summary>
        public DamageRecoilEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DamageRecoilEvent"/> class with specified fraction.
        /// </summary>
        public DamageRecoilEvent(int damageFraction) { Fraction = damageFraction; }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        protected DamageRecoilEvent(DamageRecoilEvent other)
        {
            Fraction = other.Fraction;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new DamageRecoilEvent(this); }

        /// <inheritdoc/>
        protected override int GetRecoilDamage(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            int damageDone = context.GetContextStateInt<TotalDamageDealt>(true, 0);
            return Math.Max(1, damageDone / Fraction);
        }
    }

    /// <summary>
    /// Event that deals recoil damage to the user if the move landed
    /// </summary>
    [Serializable]
    public class HPRecoilEvent : RecoilEvent
    {

        /// <summary>
        /// The value dividing the user's HP representing the recoil damage
        /// </summary>
        public int Fraction;

        /// <summary>
        /// Whether to use the user's max HP or current HP
        /// </summary>
        public bool MaxHP;

        /// <summary>
        /// Initializes a new instance of the <see cref="HPRecoilEvent"/> class.
        /// </summary>
        public HPRecoilEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="HPRecoilEvent"/> class with specified parameters.
        /// </summary>
        public HPRecoilEvent(int fraction, bool maxHP) { Fraction = fraction; MaxHP = maxHP; }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        protected HPRecoilEvent(HPRecoilEvent other)
        {
            Fraction = other.Fraction;
            MaxHP = other.MaxHP;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new HPRecoilEvent(this); }

        /// <inheritdoc/>
        protected override int GetRecoilDamage(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (MaxHP)
                return Math.Max(1, context.User.MaxHP / Fraction);
            else
                return Math.Max(1, context.User.HP / Fraction);
        }
    }


    /// <summary>
    /// Abstract base class for recoil damage events.
    /// Deals damage to the user when damage was dealt to targets.
    /// </summary>
    [Serializable]
    public abstract class RecoilEvent : BattleEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RecoilEvent"/> class.
        /// </summary>
        public RecoilEvent() { }

        /// <summary>
        /// Calculates the recoil damage to inflict on the user.
        /// </summary>
        protected abstract int GetRecoilDamage(GameEventOwner owner, Character ownerChar, BattleContext context);

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            int damageDone = context.GetContextStateInt<TotalDamageDealt>(true, 0);
            if (damageDone > 0)
            {
                if (!context.User.CharStates.Contains<NoRecoilState>() && !context.User.CharStates.Contains<MagicGuardState>())
                {
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_HIT_RECOIL").ToLocal(), context.User.GetDisplayName(false)));

                    GameManager.Instance.BattleSE("DUN_Hit_Neutral");
                    if (!context.User.Unidentifiable)
                    {
                        SingleEmitter endEmitter = new SingleEmitter(new AnimData("Hit_Neutral", 3));
                        endEmitter.SetupEmit(context.User.MapLoc, context.User.MapLoc, context.User.CharDir);
                        DungeonScene.Instance.CreateAnim(endEmitter, DrawLayer.NoDraw);
                    }

                    int recoil = GetRecoilDamage(owner, ownerChar, context);
                    yield return CoroutineManager.Instance.StartCoroutine(context.User.InflictDamage(recoil));
                }
            }
        }
    }

    /// <summary>
    /// Event that deals recoil damage to the user if the move missed
    /// </summary>
    [Serializable]
    public class CrashLandEvent : BattleEvent
    {

        /// <summary>
        /// The value dividing the user's max HP representing the recoil damage
        /// </summary>
        public int HPFraction;

        /// <summary>
        /// Initializes a new instance of the <see cref="CrashLandEvent"/> class.
        /// </summary>
        public CrashLandEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CrashLandEvent"/> class with specified fraction.
        /// </summary>
        public CrashLandEvent(int damageFraction) { HPFraction = damageFraction; }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        protected CrashLandEvent(CrashLandEvent other)
        {
            HPFraction = other.HPFraction;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new CrashLandEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.GetContextStateInt<AttackHitTotal>(true, 0) == 0)
            {
                if (!context.User.CharStates.Contains<NoRecoilState>() && !context.User.CharStates.Contains<MagicGuardState>())
                {
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_HIT_CRASH").ToLocal(), context.User.GetDisplayName(false)));
                    yield return CoroutineManager.Instance.StartCoroutine(context.User.InflictDamage(Math.Max(1, context.User.MaxHP / HPFraction)));
                }
            }
        }
    }
}


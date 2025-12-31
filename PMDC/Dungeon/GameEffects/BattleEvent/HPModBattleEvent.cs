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
    //Battle events that change HP without doing direct damage or healing



    /// <summary>
    /// Event that deals damage based on the specified fraction of the character's max HP.
    /// </summary>
    [Serializable]
    public class ChipDamageEvent : BattleEvent
    {
        /// <summary>
        /// The divisor for the character's max HP to calculate damage.
        /// </summary>
        public int HPFraction;

        /// <summary>
        /// The message displayed in the dungeon log.
        /// </summary>
        [StringKey(0, true)]
        public StringKey Msg;

        /// <summary>
        /// Whether to play the VFX associated with this event.
        /// </summary>
        public bool VFX;

        /// <summary>
        /// Whether to skip the damage animation.
        /// </summary>
        public bool SkipAction;

        /// <inheritdoc/>
        public ChipDamageEvent() { }

        /// <summary>
        /// Creates a new ChipDamageEvent with the specified HP fraction divisor.
        /// </summary>
        public ChipDamageEvent(int hpFraction) { HPFraction = hpFraction; }

        /// <summary>
        /// Creates a new ChipDamageEvent with the specified HP fraction and message.
        /// </summary>
        public ChipDamageEvent(int hpFraction, StringKey msg) { HPFraction = hpFraction; Msg = msg; }

        /// <summary>
        /// Creates a new ChipDamageEvent with all parameters.
        /// </summary>
        public ChipDamageEvent(int hpFraction, StringKey msg, bool vfx, bool skipAction) { HPFraction = hpFraction; Msg = msg; VFX = vfx; SkipAction = skipAction; }

        /// <inheritdoc/>
        protected ChipDamageEvent(ChipDamageEvent other)
        {
            HPFraction = other.HPFraction;
            Msg = other.Msg;
            VFX = other.VFX;
            SkipAction = other.SkipAction;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ChipDamageEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.User.Dead)
                yield break;
            if (!context.User.CharStates.Contains<MagicGuardState>())
            {
                if (Msg.IsValid())
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(Msg.ToLocal(), context.User.GetDisplayName(false), owner.GetDisplayName(), ownerChar.GetDisplayName(false)));
                if (VFX)
                {
                    GameManager.Instance.BattleSE("DUN_Hit_Neutral");
                    if (!context.User.Unidentifiable)
                    {
                        SingleEmitter endEmitter = new SingleEmitter(new AnimData("Hit_Neutral", 3));
                        endEmitter.SetupEmit(context.User.MapLoc, context.User.MapLoc, context.User.CharDir);
                        DungeonScene.Instance.CreateAnim(endEmitter, DrawLayer.NoDraw);
                    }
                }
                int recoil = Math.Max(1, context.User.MaxHP / HPFraction);
                yield return CoroutineManager.Instance.StartCoroutine(context.User.InflictDamage(recoil, !SkipAction));
            }
        }
    }

    /// <summary>
    /// Event that deals damage based on the specified fraction of the character's max HP.
    /// This event should only be used on trap tiles.
    /// </summary>
    [Serializable]
    public class IndirectDamageEvent : BattleEvent
    {
        /// <summary>
        /// The divisor for the character's max HP to calculate damage.
        /// </summary>
        public int HPFraction;

        /// <inheritdoc/>
        public IndirectDamageEvent() { }

        /// <summary>
        /// Creates a new IndirectDamageEvent with the specified HP fraction divisor.
        /// </summary>
        public IndirectDamageEvent(int hpFraction) { HPFraction = hpFraction; }

        /// <inheritdoc/>
        protected IndirectDamageEvent(IndirectDamageEvent other)
        {
            HPFraction = other.HPFraction;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new IndirectDamageEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (!context.Target.CharStates.Contains<MagicGuardState>())
            {
                GameManager.Instance.BattleSE("DUN_Hit_Neutral");
                if (!context.Target.Unidentifiable)
                {
                    SingleEmitter endEmitter = new SingleEmitter(new AnimData("Hit_Neutral", 3));
                    endEmitter.SetupEmit(context.Target.MapLoc, context.Target.MapLoc, context.Target.CharDir);
                    DungeonScene.Instance.CreateAnim(endEmitter, DrawLayer.NoDraw);
                }

                int dmg = Math.Max(1, context.Target.MaxHP / HPFraction);
                yield return CoroutineManager.Instance.StartCoroutine(context.Target.InflictDamage(dmg));
            }
        }
    }

    /// <summary>
    /// Event that deals damage based on the specified fraction of the character's max HP and the type effectiveness.
    /// This event should only be used on trap tiles.
    /// </summary>
    [Serializable]
    public class IndirectElementDamageEvent : BattleEvent
    {
        /// <summary>
        /// The element type for determining type effectiveness.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string Element;

        /// <summary>
        /// The divisor for the character's max HP to calculate base damage.
        /// </summary>
        public int HPFraction;

        /// <inheritdoc/>
        public IndirectElementDamageEvent() { Element = ""; }

        /// <summary>
        /// Creates a new IndirectElementDamageEvent with the specified element and HP fraction.
        /// </summary>
        public IndirectElementDamageEvent(string element, int hpFraction)
        {
            Element = element;
            HPFraction = hpFraction;
        }

        /// <inheritdoc/>
        protected IndirectElementDamageEvent(IndirectElementDamageEvent other)
        {
            Element = other.Element;
            HPFraction = other.HPFraction;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new IndirectElementDamageEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (!context.Target.CharStates.Contains<MagicGuardState>())
            {
                int typeMatchup = PreTypeEvent.GetDualEffectiveness(null, context.Target, Element);
                int effectiveness = PreTypeEvent.GetEffectivenessMult(typeMatchup);
                if (effectiveness > 0)
                {
                    GameManager.Instance.BattleSE("DUN_Hit_Neutral");
                    if (!context.Target.Unidentifiable)
                    {
                        SingleEmitter endEmitter = new SingleEmitter(new AnimData("Hit_Neutral", 3));
                        endEmitter.SetupEmit(context.Target.MapLoc, context.Target.MapLoc, context.Target.CharDir);
                        DungeonScene.Instance.CreateAnim(endEmitter, DrawLayer.NoDraw);
                    }

                    int dmg = Math.Max(1, context.Target.MaxHP / HPFraction * effectiveness / 4);
                    yield return CoroutineManager.Instance.StartCoroutine(context.Target.InflictDamage(dmg));
                }
            }
        }
    }


    /// <summary>
    /// Event that inflicts damage to the character based on the HP in the HPState status state.
    /// </summary>
    [Serializable]
    public class CurseEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle VFX events played when the curse triggers.
        /// </summary>
        public List<BattleAnimEvent> Anims;

        /// <inheritdoc/>
        public CurseEvent() { Anims = new List<BattleAnimEvent>(); }

        /// <summary>
        /// Creates a new CurseEvent with the specified animation events.
        /// </summary>
        public CurseEvent(params BattleAnimEvent[] anims)
        {
            Anims = new List<BattleAnimEvent>();
            Anims.AddRange(anims);
        }

        /// <inheritdoc/>
        protected CurseEvent(CurseEvent other)
        {
            Anims = new List<BattleAnimEvent>();
            foreach (BattleAnimEvent anim in other.Anims)
                Anims.Add((BattleAnimEvent)anim.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new CurseEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.User.Dead)
                yield break;
            if ((context.Data.Category == BattleData.SkillCategory.Physical || context.Data.Category == BattleData.SkillCategory.Magical)
                && DungeonScene.Instance.GetMatchup(context.User, context.Target) == Alignment.Foe
                && !context.User.CharStates.Contains<MagicGuardState>())
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_CURSE").ToLocal(), context.User.GetDisplayName(false)));

                foreach (BattleAnimEvent anim in Anims)
                    yield return CoroutineManager.Instance.StartCoroutine(anim.Apply(owner, ownerChar, context));

                yield return CoroutineManager.Instance.StartCoroutine(context.User.InflictDamage(((StatusEffect)owner).StatusStates.GetWithDefault<HPState>().HP));
            }
        }
    }


    /// <summary>
    /// Event that inflicts damage based on the character max HP.
    /// This event can only be used on statuses.
    /// </summary>
    [Serializable]
    public class PoisonEvent : BattleEvent
    {
        /// <summary>
        /// Whether the character is badly poisoned (damage increases over time).
        /// </summary>
        public bool Toxic;

        /// <summary>
        /// The divisor for the character's max HP to calculate damage.
        /// </summary>
        public int HPFraction;

        /// <summary>
        /// The divisor for HP restoration when the character has Poison Heal.
        /// </summary>
        public int RestoreHPFraction;

        /// <inheritdoc/>
        public PoisonEvent() { }

        /// <summary>
        /// Creates a new PoisonEvent with the specified parameters.
        /// </summary>
        public PoisonEvent(bool toxic, int hpFraction, int restoreHpFraction)
        {
            Toxic = toxic;
            HPFraction = hpFraction;
            RestoreHPFraction = restoreHpFraction;
        }

        /// <inheritdoc/>
        protected PoisonEvent(PoisonEvent other)
        {
            Toxic = other.Toxic;
            HPFraction = other.HPFraction;
            RestoreHPFraction = other.RestoreHPFraction;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new PoisonEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.User.Dead)
                yield break;
            if (context.UsageSlot == BattleContext.FORCED_SLOT)
                yield break;

            if (!context.User.CharStates.Contains<MagicGuardState>())
            {
                CountState countState = ((StatusEffect)owner).StatusStates.Get<CountState>();
                if (Toxic && countState.Count < HPFraction)
                    countState.Count++;
                if (context.User.CharStates.Contains<PoisonHealState>())
                {
                    if (context.User.HP < context.User.MaxHP)
                    {
                        DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_POISON_HEAL").ToLocal(), context.User.GetDisplayName(false)));
                        yield return CoroutineManager.Instance.StartCoroutine(context.User.RestoreHP(Math.Max(1, context.User.MaxHP / RestoreHPFraction), false));
                    }
                }
                else
                {
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_POISONED").ToLocal(), context.User.GetDisplayName(false)));
                    yield return CoroutineManager.Instance.StartCoroutine(context.User.InflictDamage(Math.Max(1, (context.User.MaxHP * countState.Count) / HPFraction)));
                }
            }
        }
    }

    /// <summary>
    /// Event that sets the character's HP to 1.
    /// </summary>
    [Serializable]
    public class HPTo1Event : BattleEvent
    {
        /// <summary>
        /// Whether to affect the target or user.
        /// </summary>
        public bool AffectTarget;

        /// <inheritdoc/>
        public HPTo1Event() { }

        /// <summary>
        /// Creates a new HPTo1Event with the specified target setting.
        /// </summary>
        public HPTo1Event(bool affectTarget) { AffectTarget = affectTarget; }

        /// <inheritdoc/>
        protected HPTo1Event(HPTo1Event other)
        {
            AffectTarget = other.AffectTarget;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new HPTo1Event(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Character target = (AffectTarget ? context.Target : context.User);
            if (target.Dead)
                yield break;

            target.HP = 1;
            DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_HP_TO_ONE").ToLocal(), target.GetDisplayName(false)));
        }
    }




    /// <summary>
    /// Event that adds the user's and target's HP, then splits the combined HP evenly.
    /// </summary>
    [Serializable]
    public class PainSplitEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new PainSplitEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            int hp = (context.User.HP + context.Target.HP) / 2;

            context.User.HP = Math.Min(hp, context.User.MaxHP);
            context.Target.HP = Math.Min(hp, context.Target.MaxHP);
            DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_HP_SPLIT").ToLocal(), context.User.GetDisplayName(false), context.Target.GetDisplayName(false)));
            yield break;
        }
    }

}


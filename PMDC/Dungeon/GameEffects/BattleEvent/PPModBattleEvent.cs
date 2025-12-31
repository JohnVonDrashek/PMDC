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
    /// Event that subtracts PP from all the user's moves if the target was dealt damage by a move.
    /// </summary>
    [Serializable]
    public class GrudgeEvent : BattleEvent
    {
        /// <inheritdoc/>
        public GrudgeEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new GrudgeEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (DungeonScene.Instance.GetMatchup(context.User, context.Target) == Alignment.Foe && context.GetContextStateInt<DamageDealt>(0) > 0 && context.ActionType == BattleActionType.Skill
                && context.UsageSlot > BattleContext.DEFAULT_ATTACK_SLOT && context.UsageSlot < CharData.MAX_SKILL_SLOTS)
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_GRUDGE").ToLocal()));
                yield return CoroutineManager.Instance.StartCoroutine(context.User.DeductCharges(-1, 3));
            }
        }
    }

    /// <summary>
    /// Event that increases the user's move PP usage by the specified amount.
    /// </summary>
    [Serializable]
    public class PressureEvent : BattleEvent
    {
        /// <summary>
        /// The increased PP usage amount.
        /// </summary>
        public int Amount;

        /// <summary>
        /// Initializes a new instance of the <see cref="PressureEvent"/> class.
        /// </summary>
        public PressureEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PressureEvent"/> class with specified amount.
        /// </summary>
        /// <param name="amount">The additional PP cost.</param>
        public PressureEvent(int amount)
        {
            Amount = amount;
        }

        /// <inheritdoc/>
        protected PressureEvent(PressureEvent other)
        {
            Amount = other.Amount;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new PressureEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (DungeonScene.Instance.GetMatchup(context.User, context.Target) == Alignment.Foe && context.ActionType == BattleActionType.Skill
                && context.UsageSlot > BattleContext.DEFAULT_ATTACK_SLOT && context.UsageSlot < CharData.MAX_SKILL_SLOTS)
            {
                if (context.User.Skills[context.UsageSlot].Element.Charges > 0)
                {
                    int deduction = Amount;
                    if (context.ContextStates.Contains<PressurePlus>())
                    {
                        deduction += 1;
                        context.ContextStates.Remove<PressurePlus>();
                    }

                    if (deduction > 0)
                    {
                        yield return CoroutineManager.Instance.StartCoroutine(context.User.DeductCharges(context.UsageSlot, deduction, true, false, true));
                        if (context.User.Skills[context.UsageSlot].Element.Charges == 0)
                            context.SkillUsedUp.Skill = context.User.Skills[context.UsageSlot].Element.SkillNum;
                    }
                }
            }
        }
    }




    /// <summary>
    /// Event that sets the PP of all the character's moves to 1.
    /// </summary>
    [Serializable]
    public class PPTo1Event : BattleEvent
    {
        /// <summary>
        /// Whether to affect the target or user.
        /// </summary>
        public bool AffectTarget;

        /// <summary>
        /// Initializes a new instance of the <see cref="PPTo1Event"/> class.
        /// </summary>
        public PPTo1Event() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PPTo1Event"/> class with specified target.
        /// </summary>
        /// <param name="affectTarget">Whether to affect the target or user.</param>
        public PPTo1Event(bool affectTarget) { AffectTarget = affectTarget; }

        /// <inheritdoc/>
        protected PPTo1Event(PPTo1Event other)
        {
            AffectTarget = other.AffectTarget;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new PPTo1Event(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Character target = (AffectTarget ? context.Target : context.User);
            if (target.Dead)
                yield break;

            for (int ii = 0; ii < target.Skills.Count; ii++)
            {
                if (!String.IsNullOrEmpty(target.Skills[ii].Element.SkillNum))
                    target.SetSkillCharges(ii, 1);
            }

            DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_PP_TO_ONE").ToLocal(), target.GetDisplayName(false)));
        }
    }





    /// <summary>
    /// Event that subtracts PP from the target if the user is hit by a move.
    /// </summary>
    [Serializable]
    public class SpiteEvent : BattleEvent
    {
        /// <summary>
        /// The status that contains the last used move slot.
        /// This should usually be "last_used_move_slot".
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string LastSlotStatusID;

        /// <summary>
        /// The amount of PP to subtract.
        /// </summary>
        public int PP;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpiteEvent"/> class.
        /// </summary>
        public SpiteEvent() { LastSlotStatusID = ""; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpiteEvent"/> class with specified parameters.
        /// </summary>
        /// <param name="statusID">The status ID that tracks the last used move slot.</param>
        /// <param name="pp">The amount of PP to subtract.</param>
        public SpiteEvent(string statusID, int pp) { LastSlotStatusID = statusID; PP = pp; }

        /// <inheritdoc/>
        protected SpiteEvent(SpiteEvent other)
        {
            LastSlotStatusID = other.LastSlotStatusID;
            PP = other.PP;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new SpiteEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            StatusEffect status = context.Target.GetStatusEffect(LastSlotStatusID);
            if (status != null)
            {
                int slot = status.StatusStates.GetWithDefault<SlotState>().Slot;
                if (slot > -1 && slot < CharData.MAX_SKILL_SLOTS)
                    yield return CoroutineManager.Instance.StartCoroutine(context.Target.DeductCharges(slot, PP));
                else
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_NO_EFFECT").ToLocal(), context.Target.GetDisplayName(false)));
            }
            else
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_NO_EFFECT").ToLocal(), context.Target.GetDisplayName(false)));
        }
    }

    /// <summary>
    /// Event that restores PP on all move slots.
    /// </summary>
    [Serializable]
    public class RestorePPEvent : BattleEvent
    {
        /// <summary>
        /// The amount of PP to restore.
        /// </summary>
        public int PP;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestorePPEvent"/> class.
        /// </summary>
        public RestorePPEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestorePPEvent"/> class with specified PP amount.
        /// </summary>
        /// <param name="pp">The amount of PP to restore.</param>
        public RestorePPEvent(int pp) { PP = pp; }

        /// <inheritdoc/>
        protected RestorePPEvent(RestorePPEvent other)
        {
            PP = other.PP;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new RestorePPEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            yield return CoroutineManager.Instance.StartCoroutine(context.Target.RestoreCharges(PP));
        }
    }

}


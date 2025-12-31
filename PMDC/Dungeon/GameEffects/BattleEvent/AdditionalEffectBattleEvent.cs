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
    // Battle events that relate to additional chance effects on attacking moves, and crits


    /// <summary>
    /// Event that doubles the additional effect chance rate in the AdditionalEffectState skill state.
    /// </summary>
    [Serializable]
    public class BoostAdditionalEvent : BattleEvent
    {
        /// <inheritdoc/>
        public BoostAdditionalEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new BoostAdditionalEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            AdditionalEffectState state = ((BattleData)context.Data).SkillStates.GetWithDefault<AdditionalEffectState>();
            if (state != null)
                state.EffectChance *= 2;
            yield break;
        }
    }

    /// <summary>
    /// Event that sets the additional effect chance rate to 0, blocking secondary effects.
    /// </summary>
    [Serializable]
    public class BlockAdditionalEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new BlockAdditionalEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            AdditionalEffectState state = ((BattleData)context.Data).SkillStates.GetWithDefault<AdditionalEffectState>();
            if (state != null)
                state.EffectChance = 0;
            yield break;
        }
    }



    /// <summary>
    /// Event that sets the additional effect chance to 0 and boosts the damage multiplier by 4/3.
    /// Used for the Sheer Force ability.
    /// </summary>
    [Serializable]
    public class SheerForceEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new BlockAdditionalEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            AdditionalEffectState state = ((BattleData)context.Data).SkillStates.GetWithDefault<AdditionalEffectState>();
            if (state != null)
            {
                state.EffectChance = 0;
                context.AddContextStateMult<DmgMult>(false, 4, 3);
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that modifies the critical hit rate based on type effectiveness.
    /// </summary>
    [Serializable]
    public class CritEffectiveEvent : BattleEvent
    {
        /// <summary>
        /// When true, boosts crit rate for not-very-effective moves; otherwise boosts for super-effective moves.
        /// </summary>
        public bool Reverse;

        /// <summary>
        /// The amount to add to the critical hit level.
        /// </summary>
        public int AddCrit;

        /// <inheritdoc/>
        public CritEffectiveEvent() { }

        /// <inheritdoc/>
        public CritEffectiveEvent(bool reverse, int addCrit)
        {
            Reverse = reverse;
            AddCrit = addCrit;
        }

        /// <inheritdoc/>
        protected CritEffectiveEvent(CritEffectiveEvent other)
        {
            Reverse = other.Reverse;
            AddCrit = other.AddCrit;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new CritEffectiveEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            int typeMatchup = PreTypeEvent.GetDualEffectiveness(context.User, context.Target, context.Data);
            typeMatchup -= PreTypeEvent.NRM_2;
            if (Reverse)
                typeMatchup *= -1;
            if (typeMatchup > 0)
                context.AddContextStateInt<CritLevel>(AddCrit);

            yield break;
        }
    }


    /// <summary>
    /// Event that increases the critical hit rate by a specified amount.
    /// </summary>
    [Serializable]
    public class BoostCriticalEvent : BattleEvent
    {
        /// <summary>
        /// The amount to add to the critical hit level (1=25%, 2=50%, 3=75%, 4+=100%).
        /// </summary>
        public int AddCrit;

        /// <inheritdoc/>
        public BoostCriticalEvent() { }

        /// <inheritdoc/>
        public BoostCriticalEvent(int addCrit)
        {
            AddCrit = addCrit;
        }

        /// <inheritdoc/>
        protected BoostCriticalEvent(BoostCriticalEvent other)
        {
            AddCrit = other.AddCrit;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new BoostCriticalEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            context.AddContextStateInt<CritLevel>(AddCrit);
            yield break;
        }
    }

    /// <summary>
    /// Event that sets the critical hit level to 0, preventing critical hits.
    /// </summary>
    [Serializable]
    public class BlockCriticalEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new BlockCriticalEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            CritLevel critLevel = context.ContextStates.GetWithDefault<CritLevel>();
            if (critLevel != null)
                critLevel.Count = 0;
            yield break;
        }
    }
}


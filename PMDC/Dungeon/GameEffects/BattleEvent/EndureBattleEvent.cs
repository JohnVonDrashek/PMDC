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
    /// Event that sets the AttackEndure context state if the character is at full HP.
    /// Allows the character to survive an otherwise fatal attack.
    /// </summary>
    [Serializable]
    public class FullEndureEvent : BattleEvent
    {
        /// <inheritdoc/>
        public FullEndureEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new FullEndureEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Target.HP == context.Target.MaxHP)
                context.ContextStates.Set(new AttackEndure());
            yield break;
        }
    }

    /// <summary>
    /// Event that sets the AttackEndure context state if the character is hit by the specified skill category.
    /// </summary>
    [Serializable]
    public class EndureCategoryEvent : BattleEvent
    {
        /// <summary>
        /// The affected skill category.
        /// </summary>
        public BattleData.SkillCategory Category;

        /// <inheritdoc/>
        public EndureCategoryEvent() { }

        /// <summary>
        /// Creates a new EndureCategoryEvent for the specified skill category.
        /// </summary>
        /// <param name="category">The skill category that triggers endurance.</param>
        public EndureCategoryEvent(BattleData.SkillCategory category)
        {
            Category = category;
        }

        /// <inheritdoc/>
        protected EndureCategoryEvent(EndureCategoryEvent other)
        {
            Category = other.Category;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new EndureCategoryEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Data.Category == Category || Category == BattleData.SkillCategory.None)
                context.ContextStates.Set(new AttackEndure());
            yield break;
        }
    }

    /// <summary>
    /// Event that sets the AttackEndure context state if the character is hit by the specified move type.
    /// </summary>
    [Serializable]
    public class EndureElementEvent : BattleEvent
    {
        /// <summary>
        /// The affected move type.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string Element;

        /// <inheritdoc/>
        public EndureElementEvent() { Element = ""; }

        /// <summary>
        /// Creates a new EndureElementEvent for the specified element type.
        /// </summary>
        /// <param name="element">The element type that triggers endurance.</param>
        public EndureElementEvent(string element)
        {
            Element = element;
        }

        /// <inheritdoc/>
        protected EndureElementEvent(EndureElementEvent other)
        {
            Element = other.Element;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new EndureElementEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Data.Element == Element)
                context.ContextStates.Set(new AttackEndure());
            yield break;
        }
    }
}


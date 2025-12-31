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
    //Battle events that change attributes of the BattleData being used, other than effects covered in ReplaceEffectBattleEvent.cs or BasePowerBattleEvent.cs



    /// <summary>
    /// Event that prepares the Judgment move's type and strike count based on held plates in the inventory.
    /// </summary>
    [Serializable]
    public class PrepareJudgmentEvent : BattleEvent
    {
        /// <summary>
        /// Mapping of plate item IDs to their corresponding element types.
        /// </summary>
        [JsonConverter(typeof(ItemElementDictConverter))]
        [DataType(1, DataManager.DataType.Item, false)]
        [DataType(2, DataManager.DataType.Element, false)]
        public Dictionary<string, string> TypePair;

        /// <inheritdoc/>
        public PrepareJudgmentEvent() { TypePair = new Dictionary<string, string>(); }

        /// <inheritdoc/>
        public PrepareJudgmentEvent(Dictionary<string, string> typePair)
        {
            TypePair = typePair;
        }

        /// <inheritdoc/>
        protected PrepareJudgmentEvent(PrepareJudgmentEvent other)
            : this()
        {
            foreach (string plate in other.TypePair.Keys)
                TypePair.Add(plate, other.TypePair[plate]);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new PrepareJudgmentEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            //check to make sure the strike number is 0
            if (context.StrikesMade == 0)
            {
                JudgmentContext judgment = new JudgmentContext();
                string heldElement;
                if (!TypePair.TryGetValue(context.User.EquippedItem.ID, out heldElement))
                    heldElement = "normal";
                judgment.Elements.Add(heldElement);

                if (context.User.MemberTeam is ExplorerTeam)
                {
                    //create a list of types to match the plates held, in a context state
                    ExplorerTeam team = (ExplorerTeam)context.User.MemberTeam;
                    for (int ii = 0; ii < team.GetInvCount(); ii++)
                    {
                        string element;
                        if (TypePair.TryGetValue(team.GetInv(ii).ID, out element))
                        {
                            //check to see if it's not on the list already
                            if (!judgment.Elements.Contains(element))
                                judgment.Elements.Add(element);
                        }
                    }
                }
                context.GlobalContextStates.Set(judgment);
                //change the strike number to match the plates in bag
                context.Strikes = judgment.Elements.Count;
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that changes the Judgment move's type based on the current strike number.
    /// </summary>
    [Serializable]
    public class PassJudgmentEvent : BattleEvent
    {
        /// <inheritdoc/>
        public PassJudgmentEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new PassJudgmentEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            //change the type to that of the context state
            JudgmentContext judgment = context.GlobalContextStates.GetWithDefault<JudgmentContext>();
            if (judgment != null && judgment.Elements.Count > context.StrikesMade)
                context.Data.Element = judgment.Elements[context.StrikesMade];

            ElementData element = DataManager.Instance.GetElement(context.Data.Element);
            DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_SKILL_TO_ELEMENT").ToLocal(), element.GetIconName()));
            yield break;
        }
    }

    /// <summary>
    /// Event that changes a move's element type from one to another.
    /// </summary>
    [Serializable]
    public class ChangeMoveElementEvent : BattleEvent
    {
        /// <summary>
        /// The element type to change from (or default element for any type).
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string ElementFrom;

        /// <summary>
        /// The element type to change to.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string ElementTo;

        /// <inheritdoc/>
        public ChangeMoveElementEvent() { ElementFrom = ""; ElementTo = ""; }

        /// <inheritdoc/>
        public ChangeMoveElementEvent(string elementFrom, string elementTo)
        {
            ElementFrom = elementFrom;
            ElementTo = elementTo;
        }

        /// <inheritdoc/>
        protected ChangeMoveElementEvent(ChangeMoveElementEvent other)
        {
            ElementFrom = other.ElementFrom;
            ElementTo = other.ElementTo;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ChangeMoveElementEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (ElementFrom == DataManager.Instance.DefaultElement || context.Data.Element == ElementFrom)
                context.Data.Element = ElementTo;
            yield break;
        }
    }


    /// <summary>
    /// Event that sets the move type based on the element stored in the owning status's ElementState.
    /// </summary>
    [Serializable]
    public class ChangeMoveElementStateEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new ChangeMoveElementStateEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            context.Data.Element = ((StatusEffect)owner).StatusStates.GetWithDefault<ElementState>().Element;
            yield break;
        }
    }



    /// <summary>
    /// Event that removes specified SkillStates from the battle data.
    /// </summary>
    [Serializable]
    public class RemoveMoveStateEvent : BattleEvent
    {
        /// <summary>
        /// The list of SkillState types to remove from the move.
        /// </summary>
        [StringTypeConstraint(1, typeof(SkillState))]
        public List<FlagType> States;

        /// <inheritdoc/>
        public RemoveMoveStateEvent() { States = new List<FlagType>(); }

        /// <inheritdoc/>
        public RemoveMoveStateEvent(Type state) : this()
        {
            States.Add(new FlagType(state));
        }

        /// <inheritdoc/>
        protected RemoveMoveStateEvent(RemoveMoveStateEvent other) : this()
        {
            States.AddRange(other.States);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new RemoveMoveStateEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            foreach (FlagType state in States)
                context.Data.SkillStates.Remove(state.FullType);
            yield break;
        }
    }


    /// <summary>
    /// Event that changes the move type based on a deterministic hash of the map seed and character ID.
    /// Used for the Hidden Power move.
    /// </summary>
    [Serializable]
    public class HiddenPowerEvent : BattleEvent
    {
        /// <inheritdoc/>
        public HiddenPowerEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new HiddenPowerEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            List<string> possibleElements = new List<string>();
            foreach (string key in DataManager.Instance.DataIndices[DataManager.DataType.Element].GetOrderedKeys(true))
            {
                if (key != DataManager.Instance.DefaultElement)
                    possibleElements.Add(key);
            }
            ulong elementID = (ZoneManager.Instance.CurrentMap.Rand.FirstSeed ^ (ulong)context.User.Discriminator) % (ulong)(possibleElements.Count);
            context.Data.Element = possibleElements[(int)elementID];
            ElementData element = DataManager.Instance.GetElement(context.Data.Element);
            DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_SKILL_TO_ELEMENT").ToLocal(), element.GetIconName()));
            yield break;
        }
    }

    /// <summary>
    /// Event that changes the move element to match the user's primary type.
    /// </summary>
    [Serializable]
    public class MatchAttackToTypeEvent : BattleEvent
    {
        /// <inheritdoc/>
        public MatchAttackToTypeEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new MatchAttackToTypeEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            context.Data.Element = context.User.Element1;
            yield break;
        }
    }


    /// <summary>
    /// Event that sets the move category to physical or special based on the user's higher base attack stat.
    /// </summary>
    [Serializable]
    public class BestCategoryEvent : BattleEvent
    {
        /// <inheritdoc/>
        public BestCategoryEvent() { }

        /// <inheritdoc/>
        protected BestCategoryEvent(BestCategoryEvent other)
        {

        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new BestCategoryEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.User.Atk < context.User.MAtk)
                context.Data.Category = BattleData.SkillCategory.Magical;
            else
                context.Data.Category = BattleData.SkillCategory.Physical;
            yield break;
        }
    }

    /// <summary>
    /// Event that swaps the move category between physical and special.
    /// </summary>
    [Serializable]
    public class FlipCategoryEvent : BattleEvent
    {
        /// <summary>
        /// When true, toggles the CrossCategory context state for midway stat recalculation.
        /// </summary>
        public bool MidwayCross;

        /// <inheritdoc/>
        public FlipCategoryEvent() { }

        /// <inheritdoc/>
        public FlipCategoryEvent(bool midway)
        {
            MidwayCross = midway;
        }

        /// <inheritdoc/>
        protected FlipCategoryEvent(FlipCategoryEvent other)
        {
            MidwayCross = other.MidwayCross;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new FlipCategoryEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.StrikesMade == 0)
            {
                if (context.Data.Category == BattleData.SkillCategory.Physical)
                    context.Data.Category = BattleData.SkillCategory.Magical;
                else if (context.Data.Category == BattleData.SkillCategory.Magical)
                    context.Data.Category = BattleData.SkillCategory.Physical;

                if (MidwayCross)
                {
                    if (context.ContextStates.Contains<CrossCategory>())
                        context.ContextStates.Remove<CrossCategory>();
                    else
                        context.ContextStates.Set(new CrossCategory());
                }
            }
            yield break;
        }
    }
}


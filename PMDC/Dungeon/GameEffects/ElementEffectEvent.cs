using System;
using RogueEssence.Data;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Dev;
using System.Collections.Generic;
using RogueElements;
using Newtonsoft.Json;

namespace PMDC.Dungeon
{
    /// <summary>
    /// Event that calculates the base type effectiveness for elemental matchups.
    /// This is the primary handler for type advantage/disadvantage calculations using the element table.
    /// </summary>
    [Serializable]
    public class PreTypeEvent : ElementEffectEvent
    {
        /// <summary>
        /// Constant for no effect (immune) type matchup.
        /// </summary>
        public const int N_E = 0;

        /// <summary>
        /// Constant for not very effective type matchup.
        /// </summary>
        public const int NVE = 3;

        /// <summary>
        /// Constant for normal effectiveness type matchup.
        /// </summary>
        public const int NRM = 4;

        /// <summary>
        /// Constant for super effective type matchup.
        /// </summary>
        public const int S_E = 5;

        /// <summary>
        /// Constant for combined no effect (immune) against dual types.
        /// </summary>
        public const int N_E_2 = 5;

        /// <summary>
        /// Constant for combined not very effective against dual types.
        /// </summary>
        public const int NVE_2 = 7;

        /// <summary>
        /// Constant for combined normal effectiveness against dual types.
        /// </summary>
        public const int NRM_2 = 8;

        /// <summary>
        /// Constant for combined super effective against dual types.
        /// </summary>
        public const int S_E_2 = 9;

        /// <summary>
        /// Converts an effectiveness value to a localized display phrase.
        /// </summary>
        /// <param name="effectiveness">The combined effectiveness value.</param>
        /// <returns>A localized string describing the effectiveness, or null for normal effectiveness.</returns>
        public static string EffectivenessToPhrase(int effectiveness)
        {
            if (effectiveness <= N_E_2)
                return new StringKey("MSG_MATCHUP_NE").ToLocal();
            if (effectiveness < NVE_2)
                return new StringKey("MSG_MATCHUP_NVE_2").ToLocal();
            else if (effectiveness == NVE_2)
                return new StringKey("MSG_MATCHUP_NVE").ToLocal();
            else if (effectiveness == S_E_2)
                return new StringKey("MSG_MATCHUP_SE").ToLocal();
            else if (effectiveness > S_E_2)
                return new StringKey("MSG_MATCHUP_SE_2").ToLocal();
            else
                return null;
        }

        /// <summary>
        /// Calculates the type matchup value between an attacking and defending type.
        /// </summary>
        /// <param name="attackerType">The attacking element type ID.</param>
        /// <param name="targetType">The defending element type ID.</param>
        /// <returns>The effectiveness value from the type matchup table.</returns>
        public static int CalculateTypeMatchup(string attackerType, string targetType)
        {
            ElementTableState table = DataManager.Instance.UniversalEvent.UniversalStates.GetWithDefault<ElementTableState>();
            return table.GetMatchup(attackerType, targetType);
        }

        /// <summary>
        /// Gets the damage multiplier for a given effectiveness value.
        /// </summary>
        /// <param name="effectiveness">The effectiveness value.</param>
        /// <returns>The damage multiplier corresponding to the effectiveness.</returns>
        public static int GetEffectivenessMult(int effectiveness)
        {
            ElementTableState table = DataManager.Instance.UniversalEvent.UniversalStates.GetWithDefault<ElementTableState>();
            return table.Effectiveness[effectiveness];
        }

        /// <summary>
        /// Calculates combined effectiveness against a dual-type target.
        /// </summary>
        /// <param name="attacker">The attacking character.</param>
        /// <param name="target">The target character.</param>
        /// <param name="targetElement">The attacking element type.</param>
        /// <returns>The combined effectiveness value against both types.</returns>
        public static int GetDualEffectiveness(Character attacker, Character target, string targetElement)
        {
            return (DungeonScene.GetEffectiveness(attacker, target, targetElement, target.Element1) + DungeonScene.GetEffectiveness(attacker, target, targetElement, target.Element2));
        }

        /// <summary>
        /// Calculates combined effectiveness of a skill against a dual-type target.
        /// </summary>
        /// <param name="attacker">The attacking character.</param>
        /// <param name="target">The target character.</param>
        /// <param name="skill">The battle data containing the move's element.</param>
        /// <returns>The combined effectiveness value against both types.</returns>
        public static int GetDualEffectiveness(Character attacker, Character target, BattleData skill)
        {
            return (DungeonScene.GetEffectiveness(attacker, target, skill, target.Element1) + DungeonScene.GetEffectiveness(attacker, target, skill, target.Element2));
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new PreTypeEvent(); }

        /// <inheritdoc/>
        public override void Apply(GameEventOwner owner, Character ownerChar, string moveType, string targetType, ref int effectiveness)
        {
            effectiveness = CalculateTypeMatchup(moveType, targetType);
        }
    }

    /// <summary>
    /// Event that applies element effects only to members of a specific monster family.
    /// Used by family-specific items that modify type matchups for certain species.
    /// </summary>
    [Serializable]
    public class FamilyMatchupEvent : ElementEffectEvent
    {
        /// <summary>
        /// The base element effect event to apply when family check passes.
        /// </summary>
        public ElementEffectEvent BaseEvent;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public FamilyMatchupEvent() { }

        /// <summary>
        /// Initializes a new instance with the specified base event.
        /// </summary>
        /// <param name="baseEvent">The element effect event to apply for family members.</param>
        public FamilyMatchupEvent(ElementEffectEvent baseEvent) { BaseEvent = baseEvent; }

        /// <summary>
        /// Copy constructor for cloning an existing FamilyMatchupEvent.
        /// </summary>
        /// <param name="other">The FamilyMatchupEvent to clone.</param>
        protected FamilyMatchupEvent(FamilyMatchupEvent other)
        {
            BaseEvent = (FamilyMatchupEvent)other.BaseEvent.Clone();
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new FamilyMatchupEvent(this); }

        /// <inheritdoc/>
        public override void Apply(GameEventOwner owner, Character ownerChar, string moveType, string targetType, ref int effectiveness)
        {
            ItemData entry = DataManager.Instance.GetItem(owner.GetID());
            FamilyState family;
            if (!entry.ItemStates.TryGet<FamilyState>(out family))
                return;
            if (family.Members.Contains(ownerChar.BaseForm.Species))
                BaseEvent.Apply(owner, ownerChar, moveType, targetType, ref effectiveness);
        }
    }

    /// <summary>
    /// Event that removes all type matchup effects for a specific defending type.
    /// Makes all attacks deal neutral damage to the specified type.
    /// </summary>
    [Serializable]
    public class RemoveTypeMatchupEvent : ElementEffectEvent
    {
        /// <summary>
        /// The element type to neutralize matchups for.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string Element;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public RemoveTypeMatchupEvent() { Element = ""; }

        /// <summary>
        /// Initializes a new instance with the specified element.
        /// </summary>
        /// <param name="element">The element type to neutralize.</param>
        public RemoveTypeMatchupEvent(string element) { Element = element; }

        /// <summary>
        /// Copy constructor for cloning an existing RemoveTypeMatchupEvent.
        /// </summary>
        /// <param name="other">The RemoveTypeMatchupEvent to clone.</param>
        protected RemoveTypeMatchupEvent(RemoveTypeMatchupEvent other) { Element = other.Element; }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new RemoveTypeMatchupEvent(this); }

        /// <inheritdoc/>
        public override void Apply(GameEventOwner owner, Character ownerChar, string moveType, string targetType, ref int effectiveness)
        {
            if (targetType == Element)
                effectiveness = PreTypeEvent.NRM;
        }
    }

    /// <summary>
    /// Event that removes type weaknesses for a specific defending type.
    /// Reduces super effective matchups to neutral for the specified type.
    /// </summary>
    [Serializable]
    public class RemoveTypeWeaknessEvent : ElementEffectEvent
    {
        /// <summary>
        /// The element type to remove weaknesses from.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string Element;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public RemoveTypeWeaknessEvent() { Element = ""; }

        /// <summary>
        /// Initializes a new instance with the specified element.
        /// </summary>
        /// <param name="element">The element type to remove weaknesses from.</param>
        public RemoveTypeWeaknessEvent(string element) { Element = element; }

        /// <summary>
        /// Copy constructor for cloning an existing RemoveTypeWeaknessEvent.
        /// </summary>
        /// <param name="other">The RemoveTypeWeaknessEvent to clone.</param>
        protected RemoveTypeWeaknessEvent(RemoveTypeWeaknessEvent other) { Element = other.Element; }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new RemoveTypeWeaknessEvent(this); }

        /// <inheritdoc/>
        public override void Apply(GameEventOwner owner, Character ownerChar, string moveType, string targetType, ref int effectiveness)
        {
            if (targetType == Element && effectiveness > PreTypeEvent.NRM)
                effectiveness = PreTypeEvent.NRM;
        }
    }

    /// <summary>
    /// Event that removes all type immunities, making immune matchups deal neutral damage.
    /// Used by abilities like Miracle Eye that bypass type immunities.
    /// </summary>
    [Serializable]
    public class NoImmunityEvent : ElementEffectEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone()
        {
            return new NoImmunityEvent();
        }

        /// <inheritdoc/>
        public override void Apply(GameEventOwner owner, Character ownerChar, string moveType, string targetType, ref int effectiveness)
        {
            if (effectiveness == PreTypeEvent.N_E)
                effectiveness = PreTypeEvent.NRM;
        }
    }

    /// <summary>
    /// Event that reduces a specific type immunity to not very effective.
    /// Used for partial immunity bypass effects.
    /// </summary>
    [Serializable]
    public class LessImmunityEvent : ElementEffectEvent
    {
        /// <summary>
        /// The attacking element that triggers this effect.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string AttackElement;

        /// <summary>
        /// The target element that would normally be immune.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string TargetElement;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public LessImmunityEvent() { AttackElement = ""; TargetElement = ""; }

        /// <summary>
        /// Initializes a new instance with the specified elements.
        /// </summary>
        /// <param name="attackElement">The attacking element type.</param>
        /// <param name="targetElement">The target element type.</param>
        public LessImmunityEvent(string attackElement, string targetElement) { AttackElement = attackElement; TargetElement = targetElement; }

        /// <summary>
        /// Copy constructor for cloning an existing LessImmunityEvent.
        /// </summary>
        /// <param name="other">The LessImmunityEvent to clone.</param>
        protected LessImmunityEvent(LessImmunityEvent other) { AttackElement = other.AttackElement; TargetElement = other.TargetElement; }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new LessImmunityEvent(this); }

        /// <inheritdoc/>
        public override void Apply(GameEventOwner owner, Character ownerChar, string moveType, string targetType, ref int effectiveness)
        {
            if (effectiveness == PreTypeEvent.N_E && moveType == AttackElement && targetType == TargetElement)
                effectiveness = PreTypeEvent.NVE;
        }
    }

    /// <summary>
    /// Event that removes all type resistances, making resisted matchups deal neutral damage.
    /// Used by abilities that bypass defensive type advantages.
    /// </summary>
    [Serializable]
    public class NoResistanceEvent : ElementEffectEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone()
        {
            return new NoResistanceEvent();
        }

        /// <inheritdoc/>
        public override void Apply(GameEventOwner owner, Character ownerChar, string moveType, string targetType, ref int effectiveness)
        {
            if (effectiveness == PreTypeEvent.NVE)
                effectiveness = PreTypeEvent.NRM;
        }
    }

    /// <summary>
    /// Event that grants complete immunity to a specific element type.
    /// Used by abilities like Levitate that provide type immunity.
    /// </summary>
    [Serializable]
    public class TypeImmuneEvent : ElementEffectEvent
    {
        /// <summary>
        /// The element type to become immune to.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string Element;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public TypeImmuneEvent() { Element = ""; }

        /// <summary>
        /// Initializes a new instance with the specified element.
        /// </summary>
        /// <param name="element">The element type to become immune to.</param>
        public TypeImmuneEvent(string element) { Element = element; }

        /// <summary>
        /// Copy constructor for cloning an existing TypeImmuneEvent.
        /// </summary>
        /// <param name="other">The TypeImmuneEvent to clone.</param>
        protected TypeImmuneEvent(TypeImmuneEvent other) { Element = other.Element; }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new TypeImmuneEvent(this); }

        /// <inheritdoc/>
        public override void Apply(GameEventOwner owner, Character ownerChar, string moveType, string targetType, ref int effectiveness)
        {
            if (moveType == Element)
                effectiveness = PreTypeEvent.N_E;
        }
    }

    /// <summary>
    /// Event that removes immunity to a specific element type, making it deal neutral damage.
    /// Used to bypass specific type immunities.
    /// </summary>
    [Serializable]
    public class TypeVulnerableEvent : ElementEffectEvent
    {
        /// <summary>
        /// The element type to remove immunity from.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string Element;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public TypeVulnerableEvent() { Element = ""; }

        /// <summary>
        /// Initializes a new instance with the specified element.
        /// </summary>
        /// <param name="element">The element type to remove immunity from.</param>
        public TypeVulnerableEvent(string element) { Element = element; }

        /// <summary>
        /// Copy constructor for cloning an existing TypeVulnerableEvent.
        /// </summary>
        /// <param name="other">The TypeVulnerableEvent to clone.</param>
        protected TypeVulnerableEvent(TypeVulnerableEvent other) { Element = other.Element; }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new TypeVulnerableEvent(this); }

        /// <inheritdoc/>
        public override void Apply(GameEventOwner owner, Character ownerChar, string moveType, string targetType, ref int effectiveness)
        {
            if (moveType == Element && effectiveness == 0)
                effectiveness = PreTypeEvent.NRM;
        }
    }

    /// <summary>
    /// Event that allows two element types to hit normally against immune targets.
    /// Implements the Scrappy ability effect for Normal and Fighting vs Ghost.
    /// </summary>
    [Serializable]
    public class ScrappyEvent : ElementEffectEvent
    {
        /// <summary>
        /// The first element type that can bypass immunities.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string Element1;

        /// <summary>
        /// The second element type that can bypass immunities.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string Element2;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public ScrappyEvent() { Element1 = ""; Element2 = ""; }

        /// <summary>
        /// Initializes a new instance with the specified elements.
        /// </summary>
        /// <param name="element1">The first element type.</param>
        /// <param name="element2">The second element type.</param>
        public ScrappyEvent(string element1, string element2) { Element1 = element1; Element2 = element2; }

        /// <summary>
        /// Copy constructor for cloning an existing ScrappyEvent.
        /// </summary>
        /// <param name="other">The ScrappyEvent to clone.</param>
        protected ScrappyEvent(ScrappyEvent other) { Element1 = other.Element1; Element2 = other.Element2; }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ScrappyEvent(this); }

        /// <inheritdoc/>
        public override void Apply(GameEventOwner owner, Character ownerChar, string moveType, string targetType, ref int effectiveness)
        {
            if ((moveType == Element1 || moveType == Element2) && effectiveness == 0)
                effectiveness = PreTypeEvent.NRM;
        }
    }

    /// <summary>
    /// Event that forces super effective damage against a specific element type.
    /// Used by abilities that create artificial weaknesses.
    /// </summary>
    [Serializable]
    public class TypeSuperEvent : ElementEffectEvent
    {
        /// <summary>
        /// The element type to become super effective against.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string Element;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public TypeSuperEvent() { Element = ""; }

        /// <summary>
        /// Initializes a new instance with the specified element.
        /// </summary>
        /// <param name="element">The element type to become super effective against.</param>
        public TypeSuperEvent(string element) { Element = element; }

        /// <summary>
        /// Copy constructor for cloning an existing TypeSuperEvent.
        /// </summary>
        /// <param name="other">The TypeSuperEvent to clone.</param>
        protected TypeSuperEvent(TypeSuperEvent other) { Element = other.Element; }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new TypeSuperEvent(this); }

        /// <inheritdoc/>
        public override void Apply(GameEventOwner owner, Character ownerChar, string moveType, string targetType, ref int effectiveness)
        {
            if (targetType == Element)
                effectiveness = PreTypeEvent.S_E;
        }
    }

    /// <summary>
    /// Event that adds an additional element's matchup to the effectiveness calculation.
    /// Used by abilities like Forest's Curse that add types to targets.
    /// </summary>
    [Serializable]
    public class TypeAddEvent : ElementEffectEvent
    {
        /// <summary>
        /// The additional element type to factor into matchups.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string Element;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public TypeAddEvent() { Element = ""; }

        /// <summary>
        /// Initializes a new instance with the specified element.
        /// </summary>
        /// <param name="element">The element type to add to matchups.</param>
        public TypeAddEvent(string element) { Element = element; }

        /// <summary>
        /// Copy constructor for cloning an existing TypeAddEvent.
        /// </summary>
        /// <param name="other">The TypeAddEvent to clone.</param>
        protected TypeAddEvent(TypeAddEvent other) { Element = other.Element; }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new TypeAddEvent(this); }

        /// <inheritdoc/>
        public override void Apply(GameEventOwner owner, Character ownerChar, string moveType, string targetType, ref int effectiveness)
        {
            int secondMatchup = PreTypeEvent.CalculateTypeMatchup(Element, targetType);
            if (secondMatchup == PreTypeEvent.N_E)
                effectiveness = PreTypeEvent.N_E;
            else
            {
                int diff = secondMatchup - PreTypeEvent.NRM;
                effectiveness = Math.Clamp(effectiveness + diff, PreTypeEvent.NVE, PreTypeEvent.S_E);
            }
        }
    }

    /// <summary>
    /// Event that forces all type matchups to be neutral effectiveness.
    /// Used by effects that remove type advantages and disadvantages.
    /// </summary>
    [Serializable]
    public class NormalizeEvent : ElementEffectEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone()
        {
            return new NormalizeEvent();
        }

        /// <inheritdoc/>
        public override void Apply(GameEventOwner owner, Character ownerChar, string moveType, string targetType, ref int effectiveness)
        {
            effectiveness = PreTypeEvent.NRM;
        }
    }

    /// <summary>
    /// Event that inverts type effectiveness values.
    /// Immunities and resistances become super effective, super effective becomes resisted.
    /// </summary>
    [Serializable]
    public class InverseEvent : ElementEffectEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone()
        {
            return new InverseEvent();
        }

        /// <inheritdoc/>
        public override void Apply(GameEventOwner owner, Character ownerChar, string moveType, string targetType, ref int effectiveness)
        {
            if (effectiveness == PreTypeEvent.N_E || effectiveness == PreTypeEvent.NVE)
                effectiveness = PreTypeEvent.S_E;
            else if (effectiveness == PreTypeEvent.S_E)
                effectiveness = PreTypeEvent.NVE;
        }
    }

    /// <summary>
    /// Abstract base event for sharing equipped item's element effects with allies.
    /// Used by abilities that pass item effects to teammates.
    /// </summary>
    [Serializable]
    public abstract class ShareEquipElementEvent : ElementEffectEvent
    {
        /// <inheritdoc/>
        public override void Apply(GameEventOwner owner, Character ownerChar, string moveType, string targetType, ref int effectiveness)
        {
            if (!String.IsNullOrEmpty(ownerChar.EquippedItem.ID))
            {
                ItemData entry = (ItemData)ownerChar.EquippedItem.GetData();
                if (CheckEquipPassValidityEvent.CanItemEffectBePassed(entry))
                {
                    foreach (var effect in GetEvents(entry))
                        effect.Value.Apply(owner, ownerChar, moveType, targetType, ref effectiveness);
                }
            }
        }

        /// <summary>
        /// Gets the element effect events from the item data.
        /// </summary>
        /// <param name="entry">The item data to get events from.</param>
        /// <returns>The prioritized list of element effect events.</returns>
        protected abstract PriorityList<ElementEffectEvent> GetEvents(ItemData entry);
    }

    /// <summary>
    /// Event that shares the equipped item's target element effects with allies.
    /// Applies defensive type matchup modifications from held items to teammates.
    /// </summary>
    [Serializable]
    public class ShareTargetElementEvent : ShareEquipElementEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone()
        {
            return new ShareTargetElementEvent();
        }

        /// <inheritdoc/>
        protected override PriorityList<ElementEffectEvent> GetEvents(ItemData entry)
        {
            return entry.TargetElementEffects;
        }
    }

    /// <summary>
    /// Event that shares the equipped item's user element effects with allies.
    /// Applies offensive type matchup modifications from held items to teammates.
    /// </summary>
    [Serializable]
    public class ShareUserElementEvent : ShareEquipElementEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone()
        {
            return new ShareUserElementEvent();
        }

        /// <inheritdoc/>
        protected override PriorityList<ElementEffectEvent> GetEvents(ItemData entry)
        {
            return entry.UserElementEffects;
        }
    }
}

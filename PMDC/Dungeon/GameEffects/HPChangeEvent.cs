using System;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Data;
using RogueEssence.Dev;
using System.Collections.Generic;
using RogueElements;

namespace PMDC.Dungeon
{
    /// <summary>
    /// HP change event that multiplies healing amounts by a ratio.
    /// Used for abilities and items that boost healing received.
    /// </summary>
    [Serializable]
    public class HealMultEvent : HPChangeEvent
    {
        /// <summary>
        /// The numerator of the healing multiplier ratio.
        /// </summary>
        public int Numerator;

        /// <summary>
        /// The denominator of the healing multiplier ratio.
        /// </summary>
        public int Denominator;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public HealMultEvent() { }

        /// <summary>
        /// Initializes a new instance with the specified multiplier ratio.
        /// </summary>
        /// <param name="numerator">The numerator of the ratio.</param>
        /// <param name="denominator">The denominator of the ratio.</param>
        public HealMultEvent(int numerator, int denominator)
        {
            Numerator = numerator;
            Denominator = denominator;
        }

        /// <summary>
        /// Copy constructor for cloning an existing HealMultEvent.
        /// </summary>
        protected HealMultEvent(HealMultEvent other)
        {
            Numerator = other.Numerator;
            Denominator = other.Denominator;
        }

        /// <inheritdoc/>
        public override void Apply(GameEventOwner owner, Character ownerChar, ref int hpChange)
        {
            if (hpChange > 0)
            {
                hpChange *= Numerator;
                hpChange /= Denominator;
            }
        }

        /// <summary>
        /// Creates a clone of this HealMultEvent instance.
        /// </summary>
        /// <returns>A new HealMultEvent with the same multiplier values.</returns>
        public override GameEvent Clone() { return new HealMultEvent(this); }
    }


    /// <summary>
    /// HP change event wrapper that only applies when the owner belongs to a specific family.
    /// Used for family-exclusive item healing effects.
    /// </summary>
    [Serializable]
    public class FamilyHPEvent : HPChangeEvent
    {
        /// <summary>
        /// The HP change event to apply when the family condition is met.
        /// </summary>
        public HPChangeEvent BaseEvent;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public FamilyHPEvent() { }

        /// <summary>
        /// Initializes a new instance with the specified base event.
        /// </summary>
        /// <param name="baseEvent">The HP change event to wrap.</param>
        public FamilyHPEvent(HPChangeEvent baseEvent) { BaseEvent = baseEvent; }

        /// <summary>
        /// Copy constructor for cloning an existing FamilyHPEvent.
        /// </summary>
        protected FamilyHPEvent(FamilyHPEvent other)
        {
            BaseEvent = (HPChangeEvent)other.BaseEvent.Clone();
        }

        /// <inheritdoc/>
        public override void Apply(GameEventOwner owner, Character ownerChar, ref int hpChange)
        {
            ItemData entry = DataManager.Instance.GetItem(owner.GetID());
            FamilyState family;
            if (!entry.ItemStates.TryGet<FamilyState>(out family))
                return;
            if (family.Members.Contains(ownerChar.BaseForm.Species))
                BaseEvent.Apply(owner, ownerChar, ref hpChange);
        }

        /// <summary>
        /// Creates a clone of this FamilyHPEvent instance.
        /// </summary>
        /// <returns>A new FamilyHPEvent with the same wrapped base event.</returns>
        public override GameEvent Clone() { return new FamilyHPEvent(this); }
    }
    /// <summary>
    /// Abstract base class for sharing equipped item HP change effects to characters.
    /// Used for abilities that share held item benefits with teammates.
    /// </summary>
    [Serializable]
    public abstract class ShareEquipHPEvent : HPChangeEvent
    {
        /// <inheritdoc/>
        public override void Apply(GameEventOwner owner, Character ownerChar, ref int hpChange)
        {
            if (!String.IsNullOrEmpty(ownerChar.EquippedItem.ID))
            {
                ItemData entry = (ItemData)ownerChar.EquippedItem.GetData();
                if (CheckEquipPassValidityEvent.CanItemEffectBePassed(entry))
                {
                    foreach (var effect in GetEvents(entry))
                        effect.Value.Apply(owner, ownerChar, ref hpChange);
                }
            }
        }

        /// <summary>
        /// Gets the HP change event list from the item data to apply.
        /// </summary>
        /// <param name="entry">The item data to retrieve events from.</param>
        /// <returns>The priority list of HP change events.</returns>
        protected abstract PriorityList<HPChangeEvent> GetEvents(ItemData entry);
    }

    /// <summary>
    /// Shares the equipped item's ModifyHPs events with the character.
    /// ModifyHPs events modify HP changes before they are applied.
    /// </summary>
    [Serializable]
    public class ShareModifyHPsEvent : ShareEquipHPEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new ShareModifyHPsEvent(); }

        /// <inheritdoc/>
        protected override PriorityList<HPChangeEvent> GetEvents(ItemData entry) => entry.ModifyHPs;
    }

    /// <summary>
    /// Shares the equipped item's RestoreHPs events with the character.
    /// RestoreHPs events modify HP restoration amounts.
    /// </summary>
    [Serializable]
    public class ShareRestoreHPsEvent : ShareEquipHPEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new ShareRestoreHPsEvent(); }

        /// <inheritdoc/>
        protected override PriorityList<HPChangeEvent> GetEvents(ItemData entry) => entry.RestoreHPs;
    }
}

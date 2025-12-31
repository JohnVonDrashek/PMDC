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
    // Battle events that are passed from the owner of the effect to the target

    /// <summary>
    /// Abstract base class for events that share equipped item effects with other characters.
    /// Allows nearby allies to benefit from the owner's equipped item passive effects.
    /// </summary>
    [Serializable]
    public abstract class ShareEquipBattleEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (!String.IsNullOrEmpty(ownerChar.EquippedItem.ID))
            {
                ItemData entry = (ItemData)ownerChar.EquippedItem.GetData();
                if (CheckEquipPassValidityEvent.CanItemEffectBePassed(entry))
                {
                    foreach (var effect in GetEvents(entry))
                        yield return CoroutineManager.Instance.StartCoroutine(effect.Value.Apply(owner, ownerChar, context));
                }
            }
        }

        /// <summary>
        /// Gets the priority list of battle events from the item data to share.
        /// </summary>
        /// <param name="entry">The item data to get events from.</param>
        /// <returns>The priority list of battle events.</returns>
        protected abstract PriorityList<BattleEvent> GetEvents(ItemData entry);
    }

    /// <summary>
    /// Event that applies the target with the AfterActions passive effects of the original character's item.
    /// This event should usually be used in proximity events.
    /// </summary>
    [Serializable]
    public class ShareAfterActionsEvent : ShareEquipBattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new ShareAfterActionsEvent(); }

        /// <inheritdoc/>
        protected override PriorityList<BattleEvent> GetEvents(ItemData entry) => entry.AfterActions;
    }

    /// <summary>
    /// Event that applies the target with the AfterBeingHits passive effects of the original character's item.
    /// This event should usually be used in proximity events.
    /// </summary>
    [Serializable]
    public class ShareAfterBeingHitsEvent : ShareEquipBattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new ShareAfterBeingHitsEvent(); }

        /// <inheritdoc/>
        protected override PriorityList<BattleEvent> GetEvents(ItemData entry) => entry.AfterBeingHits;
    }


    /// <summary>
    /// Event that applies the target with the AfterHittings passive effects of the original character's item.
    /// This event should usually be used in proximity events.
    /// </summary>
    [Serializable]
    public class ShareAfterHittingsEvent : ShareEquipBattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new ShareAfterHittingsEvent(); }

        /// <inheritdoc/>
        protected override PriorityList<BattleEvent> GetEvents(ItemData entry) => entry.AfterHittings;
    }


    /// <summary>
    /// Event that applies the target with the BeforeActions passive effects of the original character's item.
    /// This event should usually be used in proximity events.
    /// </summary>
    [Serializable]
    public class ShareBeforeActionsEvent : ShareEquipBattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new ShareBeforeActionsEvent(); }

        /// <inheritdoc/>
        protected override PriorityList<BattleEvent> GetEvents(ItemData entry) => entry.BeforeActions;
    }

    /// <summary>
    /// Event that applies the target with the BeforeBeingHits passive effects of the original character's item.
    /// This event should usually be used in proximity events.
    /// </summary>
    [Serializable]
    public class ShareBeforeBeingHitsEvent : ShareEquipBattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new ShareBeforeBeingHitsEvent(); }

        /// <inheritdoc/>
        protected override PriorityList<BattleEvent> GetEvents(ItemData entry) => entry.BeforeBeingHits;
    }

    /// <summary>
    /// Event that applies the target with the BeforeHittings passive effects of the original character's item.
    /// This event should usually be used in proximity events.
    /// </summary>
    [Serializable]
    public class ShareBeforeHittingsEvent : ShareEquipBattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new ShareBeforeHittingsEvent(); }

        /// <inheritdoc/>
        protected override PriorityList<BattleEvent> GetEvents(ItemData entry) => entry.BeforeHittings;
    }

    /// <summary>
    /// Event that applies the target with the BeforeTryActions passive effects of the original character's item.
    /// This event should usually be used in proximity events.
    /// </summary>
    [Serializable]
    public class ShareBeforeTryActionsEvent : ShareEquipBattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new ShareBeforeTryActionsEvent(); }

        /// <inheritdoc/>
        protected override PriorityList<BattleEvent> GetEvents(ItemData entry) => entry.BeforeTryActions;
    }


    /// <summary>
    /// Event that applies the target with the OnActions passive effects of the original character's item.
    /// This event should usually be used in proximity events.
    /// </summary>
    [Serializable]
    public class ShareOnActionsEvent : ShareEquipBattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new ShareOnActionsEvent(); }

        /// <inheritdoc/>
        protected override PriorityList<BattleEvent> GetEvents(ItemData entry) => entry.OnActions;
    }


    /// <summary>
    /// Event that applies the target with the OnHitTiles passive effects of the original character's item.
    /// This event should usually be used in proximity events.
    /// </summary>
    [Serializable]
    public class ShareOnHitTilesEvent : ShareEquipBattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new ShareOnHitTilesEvent(); }

        /// <inheritdoc/>
        protected override PriorityList<BattleEvent> GetEvents(ItemData entry) => entry.OnHitTiles;
    }
}

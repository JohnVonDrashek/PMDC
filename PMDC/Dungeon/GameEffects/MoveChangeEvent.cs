using System;
using RogueEssence;
using RogueEssence.Dungeon;

namespace PMDC.Dungeon
{
    /// <summary>
    /// Skill change event that updates slot-based status effects when move indices change.
    /// Called when moves are rearranged, ensuring slot-tracking statuses (like Disable)
    /// continue to reference the correct move after reordering.
    /// </summary>
    [Serializable]
    public class UpdateIndicesEvent : SkillChangeEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateIndicesEvent"/> class.
        /// </summary>
        public UpdateIndicesEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateIndicesEvent"/> class by copying from another instance.
        /// </summary>
        /// <param name="other">The <see cref="UpdateIndicesEvent"/> instance to copy.</param>
        protected UpdateIndicesEvent(UpdateIndicesEvent other) { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new UpdateIndicesEvent(this); }

        /// <summary>
        /// Updates the status effect's slot reference based on the new move indices.
        /// Removes the status if the tracked move is no longer present.
        /// </summary>
        /// <param name="owner">The status effect that owns this event.</param>
        /// <param name="character">The character whose moves were rearranged.</param>
        /// <param name="moveIndices">Array mapping old move indices to new positions (-1 if removed).</param>
        public override void Apply(GameEventOwner owner, Character character, int[] moveIndices)
        {
            SlotState statusState = ((StatusEffect)owner).StatusStates.GetWithDefault<SlotState>();
            statusState.Slot = moveIndices[statusState.Slot];
            if (statusState.Slot == -1)
                character.SilentRemoveStatus(((StatusEffect)owner).ID);
        }
    }
}

using System;
using RogueElements;
using RogueEssence.LevelGen;
using PMDC.Dungeon;
using RogueEssence;
using System.Runtime.Serialization;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Names all floors of the dungeon segment according to a naming convention,
    /// and adds a title drop effect when entering each floor.
    /// This step extends <see cref="FloorNameIDZoneStep"/> to display floor names via a visual fade effect
    /// when the player enters each floor.
    /// </summary>
    [Serializable]
    public class FloorNameDropZoneStep : FloorNameIDZoneStep
    {
        /// <summary>
        /// Gets or sets the priority at which the title drop (floor name display) effect occurs.
        /// Determines the order of execution relative to other generation steps.
        /// </summary>
        public Priority DropPriority;

        /// <summary>
        /// Initializes a new instance of the <see cref="FloorNameDropZoneStep"/> class with default values.
        /// </summary>
        public FloorNameDropZoneStep()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FloorNameDropZoneStep"/> class with the specified configuration.
        /// </summary>
        /// <param name="priority">The priority at which to apply the floor naming step in the generation pipeline.</param>
        /// <param name="name">The base name template for floors, which may include format placeholders.</param>
        /// <param name="dropPriority">The priority at which the title drop (visual floor name display) effect occurs.</param>
        public FloorNameDropZoneStep(Priority priority, LocalText name, Priority dropPriority) : base(priority, name)
        {
            DropPriority = dropPriority;
        }

        /// <summary>
        /// Copy constructor for cloning an existing instance with a new random seed.
        /// </summary>
        /// <param name="other">The instance to copy field values from.</param>
        /// <param name="seed">The random seed to use for this cloned instance.</param>
        protected FloorNameDropZoneStep(FloorNameDropZoneStep other, ulong seed) : base(other, seed)
        {
            DropPriority = other.DropPriority;
        }

        /// <inheritdoc/>
        public override ZoneStep Instantiate(ulong seed) { return new FloorNameDropZoneStep(this, seed); }

        /// <summary>
        /// Applies floor naming and title drop effects to the zone generation context.
        /// First calls the base implementation to apply floor naming conventions,
        /// then enqueues a <see cref="MapTitleDropStep{T}"/> to handle the visual display of floor names.
        /// </summary>
        /// <param name="zoneContext">The zone generation context containing zone-level data.</param>
        /// <param name="context">The generic generation context for the current zone structure.</param>
        /// <param name="queue">The priority queue of generation steps to execute in order.</param>
        /// <inheritdoc/>
        public override void Apply(ZoneGenContext zoneContext, IGenContext context, StablePriorityQueue<Priority, IGenStep> queue)
        {
            base.Apply(zoneContext, context, queue);

            MapTitleDropStep<BaseMapGenContext> fade = new MapTitleDropStep<BaseMapGenContext>(DropPriority);
            queue.Enqueue(Priority, fade);
        }

        /// <summary>
        /// Called after deserialization to handle version upgrades and data migration.
        /// Initializes <see cref="DropPriority"/> to a default value for old save files
        /// that were created before version 0.7.0.
        /// </summary>
        /// <param name="context">The streaming context from the deserialization process.</param>
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            //TODO: Remove in v1.1
            if (RogueEssence.Data.Serializer.OldVersion < new Version(0, 7, 0))
                DropPriority = new Priority(-15);
        }
    }
}

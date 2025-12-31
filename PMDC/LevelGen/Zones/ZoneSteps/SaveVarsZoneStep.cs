using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Dungeon;
using RogueEssence.Data;
using RogueEssence.LevelGen;

namespace PMDC.LevelGen
{
    /// <summary>
    /// A zone-level generation step that enqueues rescue spawning logic when a rescue mission is active.
    /// </summary>
    /// <remarks>
    /// This step checks if the current zone matches an active rescue mission's target zone, segment, and structure ID.
    /// If all conditions are met, it enqueues a <see cref="RescueSpawner{TContext}"/> to handle spawning the rescue objective
    /// and related entities. If no rescue is active, this step performs no action.
    /// </remarks>
    [Serializable]
    public class SaveVarsZoneStep : ZoneStep
    {
        /// <summary>
        /// Gets or sets the priority at which this step executes during the zone generation process.
        /// </summary>
        /// <remarks>
        /// The priority determines the execution order relative to other zone steps in the generation queue.
        /// Lower priority values execute before higher priority values.
        /// </remarks>
        public Priority Priority;

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveVarsZoneStep"/> class with the specified priority.
        /// </summary>
        /// <param name="priority">The priority at which this step should execute during zone generation.</param>
        public SaveVarsZoneStep(Priority priority)
        {
            Priority = priority;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveVarsZoneStep"/> class by copying another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        /// <param name="seed">The random seed for instantiation (unused in this implementation).</param>
        protected SaveVarsZoneStep(SaveVarsZoneStep other, ulong seed)
        {
            Priority = other.Priority;
        }

        /// <inheritdoc/>
        public override ZoneStep Instantiate(ulong seed) { return new SaveVarsZoneStep(this, seed); }

        /// <summary>
        /// Applies the rescue spawning logic if the current zone matches an active rescue mission's target.
        /// </summary>
        /// <param name="zoneContext">The zone generation context containing information about the current zone, segment, and structure.</param>
        /// <param name="context">The general generation context (unused in this implementation).</param>
        /// <param name="queue">The priority queue where generation steps are enqueued. If a matching rescue is active,
        /// a <see cref="RescueSpawner{TContext}"/> will be enqueued at this step's priority.</param>
        /// <remarks>
        /// This method retrieves the current game progress and checks if a rescue mission is active. If the rescue mission's
        /// target location (zone ID, segment, and structure ID) matches the current generation context, a rescue spawner
        /// is enqueued to handle placement of rescue-related entities.
        /// </remarks>
        public override void Apply(ZoneGenContext zoneContext, IGenContext context, StablePriorityQueue<Priority, IGenStep> queue)
        {
            GameProgress progress = DataManager.Instance.Save;
            if (progress != null && progress.Rescue != null && progress.Rescue.Rescuing)
            {
                if (progress.Rescue.SOS.Goal.ID == zoneContext.CurrentZone
                    && progress.Rescue.SOS.Goal.StructID.Segment == zoneContext.CurrentSegment
                    && progress.Rescue.SOS.Goal.StructID.ID == zoneContext.CurrentID)
                {
                    queue.Enqueue(Priority, new RescueSpawner<BaseMapGenContext>());
                }
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0}", this.GetType().GetFormattedTypeName());
        }
    }
}

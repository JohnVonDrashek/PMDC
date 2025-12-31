using System;
using RogueElements;
using RogueEssence.Dev;
using PMDC.Dungeon;
using RogueEssence.LevelGen;
using System.Runtime.Serialization;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Configures the enemy spawn settings for the map including respawn behavior and maximum foe count.
    /// This step sets up the respawn event that runs each turn to potentially spawn new enemies.
    /// </summary>
    /// <typeparam name="T">The map generation context type.</typeparam>
    [Serializable]
    public class MobSpawnSettingsStep<T> : GenStep<T> where T : BaseMapGenContext
    {
        /// <summary>
        /// Priority of execution in turn end operations.
        /// </summary>
        public Priority Priority;

        /// <summary>
        /// The respawn event that controls enemy spawning behavior.
        /// </summary>
        [SubGroup]
        public RespawnBaseEvent Respawn;

        /// <summary>
        /// OBSOLETE: Use Respawn.MaxFoes instead.
        /// </summary>
        [NonEdited]
        public int MaxFoes;

        /// <summary>
        /// OBSOLETE: Use Respawn.RespawnTime instead.
        /// </summary>
        [NonEdited]
        public int RespawnTime;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MobSpawnSettingsStep()
        {

        }

        /// <summary>
        /// Initializes a new instance with the specified priority and respawn event.
        /// </summary>
        /// <param name="priority">The priority at which the respawn event executes.</param>
        /// <param name="respawn">The respawn event to use for enemy spawning.</param>
        public MobSpawnSettingsStep(Priority priority, RespawnBaseEvent respawn)
        {
            Priority = priority;
            Respawn = respawn;
        }

        /// <inheritdoc/>
        public override void Apply(T map)
        {
            map.Map.MapEffect.OnMapTurnEnds.Add(Priority, Respawn.Copy());
        }


        /// <summary>
        /// Returns a string representation of this step, displaying the respawn time and maximum foes count.
        /// </summary>
        /// <returns>A formatted string showing the step type, respawn time, and maximum foes, or "[EMPTY]" if no respawn event is set.</returns>
        public override string ToString()
        {
            if (this.Respawn == null)
                return String.Format("{0}: [EMPTY]", this.GetType().GetFormattedTypeName());
            return String.Format("{0}: Turns: {1} Max: {2}", this.GetType().GetFormattedTypeName(), Respawn.RespawnTime, Respawn.MaxFoes);
        }

        /// <summary>
        /// Called after deserialization to handle backwards compatibility with obsolete fields.
        /// Migrates legacy MaxFoes and RespawnTime fields to the new Respawn event if needed.
        /// </summary>
        /// <param name="context">The streaming context provided by the deserialization process.</param>
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            //TODO: Remove in v1.1
            if (Respawn == null)
            {
                Priority = new Priority(15);
                Respawn = new RespawnFromEligibleEvent(MaxFoes, RespawnTime);
            }
        }
    }
}

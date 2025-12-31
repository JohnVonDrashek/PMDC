using System;
using RogueElements;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Data;
using Newtonsoft.Json;
using RogueEssence.Dev;

namespace PMDC.Dungeon
{
    /// <summary>
    /// AI plan that causes the character to wait until they receive a specific status effect.
    /// Once the status is applied (typically from being attacked), the plan defers to the next behavior.
    /// Used for passive enemies that only become aggressive when provoked.
    /// </summary>
    [Serializable]
    public class WaitUntilAttackedPlan : AIPlan
    {
        /// <summary>
        /// The status effect ID that triggers the character to stop waiting.
        /// When this status is present, the plan defers to the next behavior.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string StatusIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitUntilAttackedPlan"/> class with default values.
        /// </summary>
        public WaitUntilAttackedPlan() : base()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitUntilAttackedPlan"/> class.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        /// <param name="status">The status effect ID that triggers activation.</param>
        public WaitUntilAttackedPlan(AIFlags iq, string status) : base(iq)
        {
            StatusIndex = status;
        }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        protected WaitUntilAttackedPlan(WaitUntilAttackedPlan other) : base(other) { StatusIndex = other.StatusIndex; }

        /// <inheritdoc/>
        public override BasePlan CreateNew() { return new WaitUntilAttackedPlan(this); }

        /// <summary>
        /// Waits until the trigger status is applied, then defers to the next plan.
        /// </summary>
        /// <param name="controlledChar">The character being controlled by this AI.</param>
        /// <param name="preThink">Whether this is a pre-think evaluation.</param>
        /// <param name="rand">Random number generator for decision-making.</param>
        /// <returns>Wait action if status not present, null if triggered.</returns>
        public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
        {
            if (controlledChar.GetStatusEffect(StatusIndex) == null)
                return new GameAction(GameAction.ActionType.Wait, Dir8.None);
            return null;
        }
    }

    /// <summary>
    /// AI plan that causes the character to wait until a specific map status is active.
    /// Once the map status appears, the plan defers to the next behavior.
    /// Used for enemies that activate based on environmental triggers.
    /// </summary>
    [Serializable]
    public class WaitUntilMapStatusPlan : AIPlan
    {
        /// <summary>
        /// The map status ID that triggers the character to stop waiting.
        /// When this map status is active, the plan defers to the next behavior.
        /// </summary>
        [JsonConverter(typeof(MapStatusConverter))]
        [DataType(0, DataManager.DataType.MapStatus, false)]
        public string StatusIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitUntilMapStatusPlan"/> class with default values.
        /// </summary>
        public WaitUntilMapStatusPlan() : base()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitUntilMapStatusPlan"/> class.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        /// <param name="status">The map status ID that triggers activation.</param>
        public WaitUntilMapStatusPlan(AIFlags iq, string status) : base(iq)
        {
            StatusIndex = status;
        }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        protected WaitUntilMapStatusPlan(WaitUntilMapStatusPlan other) : base(other) { StatusIndex = other.StatusIndex; }

        /// <inheritdoc/>
        public override BasePlan CreateNew() { return new WaitUntilMapStatusPlan(this); }

        /// <summary>
        /// Waits until the trigger map status is active, then defers to the next plan.
        /// </summary>
        /// <param name="controlledChar">The character being controlled by this AI.</param>
        /// <param name="preThink">Whether this is a pre-think evaluation.</param>
        /// <param name="rand">Random number generator for decision-making.</param>
        /// <returns>Wait action if map status not active, null if triggered.</returns>
        public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
        {
            if (!ZoneManager.Instance.CurrentMap.Status.ContainsKey(StatusIndex))
                return new GameAction(GameAction.ActionType.Wait, Dir8.None);
            return null;
        }
    }
}

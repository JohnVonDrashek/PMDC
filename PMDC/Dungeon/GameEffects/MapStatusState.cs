using System;
using RogueElements;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.LevelGen;

namespace PMDC.Dungeon
{
    /// <summary>
    /// Map status state that tracks a counter for timed map effects.
    /// Used by effects that count down or accumulate over turns.
    /// </summary>
    [Serializable]
    public class MapTickState : MapStatusState
    {
        /// <summary>
        /// The current tick counter value.
        /// </summary>
        public int Counter;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MapTickState() { }

        /// <summary>
        /// Initializes a new instance with the specified counter value.
        /// </summary>
        /// <param name="counter">The initial counter value.</param>
        public MapTickState(int counter) { Counter = counter; }

        /// <summary>
        /// Copy constructor for cloning an existing MapTickState.
        /// </summary>
        protected MapTickState(MapTickState other) { Counter = other.Counter; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new MapTickState(this); }
    }

    /// <summary>
    /// Map status state that tracks shop pricing and cart information.
    /// Used to manage Kecleon shop transactions on the floor.
    /// </summary>
    [Serializable]
    public class ShopPriceState : MapStatusState
    {
        /// <summary>
        /// The total price of items in the current transaction.
        /// </summary>
        public int Amount;

        /// <summary>
        /// The number of items currently in the shopping cart.
        /// </summary>
        public int Cart;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public ShopPriceState() { }

        /// <summary>
        /// Initializes a new instance with the specified amount.
        /// </summary>
        /// <param name="amt">The initial price amount.</param>
        public ShopPriceState(int amt) { Amount = amt; }

        /// <summary>
        /// Copy constructor for cloning an existing ShopPriceState.
        /// </summary>
        protected ShopPriceState(ShopPriceState other) { Amount = other.Amount; Cart = other.Cart; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new ShopPriceState(this); }
    }

    /// <summary>
    /// Map status state that defines the shop security spawn pool.
    /// Used to spawn Kecleon guards when items are stolen.
    /// </summary>
    [Serializable]
    public class ShopSecurityState : MapStatusState
    {
        /// <summary>
        /// Weighted list of security monster spawns to summon when theft occurs.
        /// </summary>
        public SpawnList<MobSpawn> Security;

        /// <summary>
        /// Initializes a new instance with an empty security list.
        /// </summary>
        public ShopSecurityState() { Security = new SpawnList<MobSpawn>(); }

        /// <summary>
        /// Copy constructor for cloning an existing ShopSecurityState.
        /// </summary>
        protected ShopSecurityState(ShopSecurityState other) : this()
        {
            for (int ii = 0; ii < other.Security.Count; ii++)
                Security.Add(other.Security.GetSpawn(ii).Copy(), other.Security.GetSpawnRate(ii));
        }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new ShopSecurityState(this); }
    }
}

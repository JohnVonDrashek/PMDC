using System;
using RogueEssence.Data;
using RogueEssence.Dev;
using RogueEssence.Dungeon;

namespace PMDC.Dungeon
{
    /// <summary>
    /// Character state that prevents items from being knocked off or stolen.
    /// Used by abilities like Sticky Hold.
    /// </summary>
    [Serializable]
    public class StickyHoldState : CharState
    {
        /// <summary>
        /// Initializes a new instance of the StickyHoldState class with default values.
        /// </summary>
        public StickyHoldState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new StickyHoldState(); }
    }

    /// <summary>
    /// Character state that prevents the character from being moved or displaced.
    /// Used by abilities like Suction Cups.
    /// </summary>
    [Serializable]
    public class AnchorState : CharState
    {
        /// <summary>
        /// Initializes a new instance of the AnchorState class with default values.
        /// </summary>
        public AnchorState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new AnchorState(); }
    }

    /// <summary>
    /// Character state that enables hit-and-run behavior, allowing retreat after attacking.
    /// Tracks the originating item that granted this state.
    /// </summary>
    [Serializable]
    public class HitAndRunState : CharState
    {
        /// <summary>
        /// The item ID that granted this hit-and-run capability.
        /// </summary>
        [DataType(0, DataManager.DataType.Item, false)]
        public string OriginItem;

        /// <summary>
        /// Initializes a new instance of the HitAndRunState class with default values.
        /// </summary>
        public HitAndRunState() { OriginItem = ""; }

        /// <summary>
        /// Initializes a new instance of the HitAndRunState class with the specified origin item.
        /// </summary>
        /// <param name="origin">The item ID that grants this state.</param>
        public HitAndRunState(string origin) { OriginItem = origin; }

        /// <summary>
        /// Copy constructor for cloning an existing HitAndRunState.
        /// </summary>
        /// <param name="other">The state to copy.</param>
        public HitAndRunState(HitAndRunState other) { OriginItem = other.OriginItem; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new HitAndRunState(this); }
    }

    /// <summary>
    /// Character state that allows movement while asleep.
    /// Used for sleepwalking abilities.
    /// </summary>
    [Serializable]
    public class SleepWalkerState : CharState
    {
        /// <summary>
        /// Initializes a new instance of the SleepWalkerState class with default values.
        /// </summary>
        public SleepWalkerState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new SleepWalkerState(); }
    }

    /// <summary>
    /// Character state that allows movement while charging an attack.
    /// Enables charging moves that don't immobilize the user.
    /// </summary>
    [Serializable]
    public class ChargeWalkerState : CharState
    {
        /// <summary>
        /// Initializes a new instance of the ChargeWalkerState class with default values.
        /// </summary>
        public ChargeWalkerState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new ChargeWalkerState(); }
    }

    /// <summary>
    /// Character state that modifies HP drain effectiveness.
    /// Used by abilities that enhance or reduce drain moves.
    /// </summary>
    [Serializable]
    public class DrainDamageState : CharState
    {
        /// <summary>
        /// The multiplier applied to HP drain effects.
        /// </summary>
        public int Mult;

        /// <summary>
        /// Initializes a new instance of the DrainDamageState class with default values.
        /// </summary>
        public DrainDamageState() { }

        /// <summary>
        /// Initializes a new instance of the DrainDamageState class with the specified multiplier.
        /// </summary>
        /// <param name="mult">The drain multiplier.</param>
        public DrainDamageState(int mult) { Mult = mult; }

        /// <summary>
        /// Copy constructor for cloning an existing DrainDamageState.
        /// </summary>
        /// <param name="other">The state to copy.</param>
        public DrainDamageState(DrainDamageState other) { Mult = other.Mult; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new DrainDamageState(this); }
    }

    /// <summary>
    /// Character state that prevents recoil damage from attacks.
    /// Used by abilities like Rock Head.
    /// </summary>
    [Serializable]
    public class NoRecoilState : CharState
    {
        /// <summary>
        /// Initializes a new instance of the NoRecoilState class with default values.
        /// </summary>
        public NoRecoilState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new NoRecoilState(); }
    }

    /// <summary>
    /// Character state that reduces fire and burn damage.
    /// Used by the Heatproof ability.
    /// </summary>
    [Serializable]
    public class HeatproofState : CharState
    {
        /// <summary>
        /// Initializes a new instance of the HeatproofState class with default values.
        /// </summary>
        public HeatproofState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new HeatproofState(); }
    }

    /// <summary>
    /// Character state indicating immunity to lava terrain damage.
    /// Used by fire-type or magma-dwelling characters.
    /// </summary>
    [Serializable]
    public class LavaState : CharState
    {
        /// <summary>
        /// Initializes a new instance of the LavaState class with default values.
        /// </summary>
        public LavaState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new LavaState(); }
    }

    /// <summary>
    /// Character state indicating immunity to poison damage.
    /// Used by poison-type or immune characters.
    /// </summary>
    [Serializable]
    public class PoisonState : CharState
    {
        /// <summary>
        /// Initializes a new instance of the PoisonState class with default values.
        /// </summary>
        public PoisonState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new PoisonState(); }
    }

    /// <summary>
    /// Character state that prevents indirect damage (weather, status, etc.).
    /// Used by the Magic Guard ability.
    /// </summary>
    [Serializable]
    public class MagicGuardState : CharState
    {
        /// <summary>
        /// Initializes a new instance of the MagicGuardState class with default values.
        /// </summary>
        public MagicGuardState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new MagicGuardState(); }
    }

    /// <summary>
    /// Character state indicating immunity to sandstorm damage.
    /// Used by rock, ground, and steel types.
    /// </summary>
    [Serializable]
    public class SandState : CharState
    {
        /// <summary>
        /// Initializes a new instance of the SandState class with default values.
        /// </summary>
        public SandState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new SandState(); }
    }

    /// <summary>
    /// Character state indicating immunity to hail damage.
    /// Used by ice types.
    /// </summary>
    [Serializable]
    public class HailState : CharState
    {
        /// <summary>
        /// Initializes a new instance of the HailState class with default values.
        /// </summary>
        public HailState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new HailState(); }
    }

    /// <summary>
    /// Character state that enhances critical hit damage.
    /// Used by the Sniper ability.
    /// </summary>
    [Serializable]
    public class SnipeState : CharState
    {
        /// <summary>
        /// Initializes a new instance of the SnipeState class with default values.
        /// </summary>
        public SnipeState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new SnipeState(); }
    }

    /// <summary>
    /// Character state that converts poison damage into healing.
    /// Used by the Poison Heal ability.
    /// </summary>
    [Serializable]
    public class PoisonHealState : CharState
    {
        /// <summary>
        /// Initializes a new instance of the PoisonHealState class with default values.
        /// </summary>
        public PoisonHealState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new PoisonHealState(); }
    }

    /// <summary>
    /// Character state indicating the character is heavy.
    /// Affects weight-based moves and abilities.
    /// </summary>
    [Serializable]
    public class HeavyWeightState : CharState
    {
        /// <summary>
        /// Initializes a new instance of the HeavyWeightState class with default values.
        /// </summary>
        public HeavyWeightState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new HeavyWeightState(); }
    }

    /// <summary>
    /// Character state indicating the character is light.
    /// Affects weight-based moves and abilities.
    /// </summary>
    [Serializable]
    public class LightWeightState : CharState
    {
        /// <summary>
        /// Initializes a new instance of the LightWeightState class with default values.
        /// </summary>
        public LightWeightState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new LightWeightState(); }
    }

    /// <summary>
    /// Character state indicating the character can avoid or detect traps.
    /// Used for trap-immunity abilities.
    /// </summary>
    [Serializable]
    public class TrapState : CharState
    {
        /// <summary>
        /// Initializes a new instance of the TrapState class with default values.
        /// </summary>
        public TrapState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new TrapState(); }
    }

    /// <summary>
    /// Character state that prevents slipping or being pushed.
    /// Used for grip-enhancing abilities.
    /// </summary>
    [Serializable]
    public class GripState : CharState
    {
        /// <summary>
        /// Initializes a new instance of the GripState class with default values.
        /// </summary>
        public GripState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new GripState(); }
    }

    /// <summary>
    /// Character state that extends weather duration when the character creates weather.
    /// Used by weather-extending abilities.
    /// </summary>
    [Serializable]
    public class ExtendWeatherState : CharState
    {
        /// <summary>
        /// Initializes a new instance of the ExtendWeatherState class with default values.
        /// </summary>
        public ExtendWeatherState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new ExtendWeatherState(); }
    }

    /// <summary>
    /// Character state that enhances binding/trapping moves.
    /// Used for grip-based abilities.
    /// </summary>
    [Serializable]
    public class BindState : CharState
    {
        /// <summary>
        /// Initializes a new instance of the BindState class with default values.
        /// </summary>
        public BindState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new BindState(); }
    }

    /// <summary>
    /// Character state that enhances damage when using type-boosting gems.
    /// Used by gem-related abilities.
    /// </summary>
    [Serializable]
    public class GemBoostState : CharState
    {
        /// <summary>
        /// Initializes a new instance of the GemBoostState class with default values.
        /// </summary>
        public GemBoostState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new GemBoostState(); }
    }


    /// <summary>
    /// Generation state that modifies coin/money spawn rates during floor generation.
    /// Used for luck-based abilities that affect treasure.
    /// </summary>
    [Serializable]
    public class CoinModGenState : ModGenState
    {
        /// <summary>
        /// Initializes a new instance of the CoinModGenState class with default values.
        /// </summary>
        public CoinModGenState() { }

        /// <summary>
        /// Initializes a new instance of the CoinModGenState class with the specified modifier.
        /// </summary>
        /// <param name="mod">The spawn rate modifier.</param>
        public CoinModGenState(int mod) : base(mod) { }

        /// <summary>
        /// Copy constructor for cloning an existing CoinModGenState.
        /// </summary>
        /// <param name="other">The state to copy.</param>
        protected CoinModGenState(CoinModGenState other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new CoinModGenState(this); }
    }

    /// <summary>
    /// Generation state that modifies stairs spawn during floor generation.
    /// Used for abilities that affect dungeon exits.
    /// </summary>
    [Serializable]
    public class StairsModGenState : ModGenState
    {
        /// <summary>
        /// Initializes a new instance of the StairsModGenState class with default values.
        /// </summary>
        public StairsModGenState() { }

        /// <summary>
        /// Initializes a new instance of the StairsModGenState class with the specified modifier.
        /// </summary>
        /// <param name="mod">The spawn rate modifier.</param>
        public StairsModGenState(int mod) : base(mod) { }

        /// <summary>
        /// Copy constructor for cloning an existing StairsModGenState.
        /// </summary>
        /// <param name="other">The state to copy.</param>
        protected StairsModGenState(StairsModGenState other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new StairsModGenState(this); }
    }

    /// <summary>
    /// Generation state that modifies chest spawn rates during floor generation.
    /// Used for luck-based abilities that affect treasure chests.
    /// </summary>
    [Serializable]
    public class ChestModGenState : ModGenState
    {
        /// <summary>
        /// Initializes a new instance of the ChestModGenState class with default values.
        /// </summary>
        public ChestModGenState() { }

        /// <summary>
        /// Initializes a new instance of the ChestModGenState class with the specified modifier.
        /// </summary>
        /// <param name="mod">The spawn rate modifier.</param>
        public ChestModGenState(int mod) : base(mod) { }

        /// <summary>
        /// Copy constructor for cloning an existing ChestModGenState.
        /// </summary>
        /// <param name="other">The state to copy.</param>
        protected ChestModGenState(ChestModGenState other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new ChestModGenState(this); }
    }

    /// <summary>
    /// Generation state that modifies shop spawn rates during floor generation.
    /// Used for abilities that affect shop appearance.
    /// </summary>
    [Serializable]
    public class ShopModGenState : ModGenState
    {
        /// <summary>
        /// Initializes a new instance of the ShopModGenState class with default values.
        /// </summary>
        public ShopModGenState() { }

        /// <summary>
        /// Initializes a new instance of the ShopModGenState class with the specified modifier.
        /// </summary>
        /// <param name="mod">The spawn rate modifier.</param>
        public ShopModGenState(int mod) : base(mod) { }

        /// <summary>
        /// Copy constructor for cloning an existing ShopModGenState.
        /// </summary>
        /// <param name="other">The state to copy.</param>
        protected ShopModGenState(ShopModGenState other) : base(other) { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new ShopModGenState(this); }
    }
}

using System;
using RogueEssence.Data;
using RogueEssence.Dungeon;
using System.Collections.Generic;
using RogueEssence.Dev;

namespace PMDC.Dungeon
{
    /// <summary>
    /// Enumeration of exclusive item cosmetic types.
    /// Used to categorize exclusive items by their visual/thematic classification.
    /// </summary>
    public enum ExclusiveItemType
    {
        /// <summary>No exclusive type.</summary>
        None,

        /// <summary>Claw-type exclusive item.</summary>
        Claw,
        /// <summary>Fang-type exclusive item.</summary>
        Fang,
        /// <summary>Tooth-type exclusive item.</summary>
        Tooth,
        /// <summary>Card-type exclusive item.</summary>
        Card,
        /// <summary>Hair-type exclusive item.</summary>
        Hair,
        /// <summary>Tail-type exclusive item.</summary>
        Tail,
        /// <summary>Wing-type exclusive item.</summary>
        Wing,
        /// <summary>Dew-type exclusive item.</summary>
        Dew,
        /// <summary>Drool-type exclusive item.</summary>
        Drool,
        /// <summary>Sweat-type exclusive item.</summary>
        Sweat,
        /// <summary>Gasp-type exclusive item.</summary>
        Gasp,
        /// <summary>Foam-type exclusive item.</summary>
        Foam,
        /// <summary>Song-type exclusive item.</summary>
        Song,
        /// <summary>Beam-type exclusive item.</summary>
        Beam,
        /// <summary>Thorn-type exclusive item.</summary>
        Thorn,
        /// <summary>Shoot-type exclusive item.</summary>
        Shoot,
        /// <summary>Branch-type exclusive item.</summary>
        Branch,
        /// <summary>Twig-type exclusive item.</summary>
        Twig,
        /// <summary>Root-type exclusive item.</summary>
        Root,
        /// <summary>Seed-type exclusive item.</summary>
        Seed,
        /// <summary>Mud-type exclusive item.</summary>
        Mud,
        /// <summary>Leaf-type exclusive item.</summary>
        Leaf,
        /// <summary>Horn-type exclusive item.</summary>
        Horn,

        /// <summary>Tag-type exclusive item.</summary>
        Tag,
        /// <summary>Jaw-type exclusive item.</summary>
        Jaw,
        /// <summary>Dust-type exclusive item.</summary>
        Dust,
        /// <summary>Jewel-type exclusive item.</summary>
        Jewel,
        /// <summary>Crest-type exclusive item.</summary>
        Crest,
        /// <summary>Seal-type exclusive item.</summary>
        Seal,
        /// <summary>Charm-type exclusive item.</summary>
        Charm,
        /// <summary>Rock-type exclusive item.</summary>
        Rock,
        /// <summary>Pebble-type exclusive item.</summary>
        Pebble,
        /// <summary>Ore-type exclusive item.</summary>
        Ore,
        /// <summary>Shard-type exclusive item.</summary>
        Shard,
        /// <summary>Coin-type exclusive item.</summary>
        Coin,
        /// <summary>Key-type exclusive item.</summary>
        Key,
        /// <summary>Heart-type exclusive item.</summary>
        Heart,
        /// <summary>Aroma-type exclusive item.</summary>
        Aroma,
        /// <summary>Medal-type exclusive item.</summary>
        Medal,
        /// <summary>Ring-type exclusive item.</summary>
        Ring,
        /// <summary>Earring-type exclusive item.</summary>
        Earring,
        /// <summary>Brooch-type exclusive item.</summary>
        Brooch,
        /// <summary>Guard-type exclusive item.</summary>
        Guard,

        /// <summary>Blade-type exclusive item.</summary>
        Blade,
        /// <summary>Band-type exclusive item.</summary>
        Band,
        /// <summary>Belt-type exclusive item.</summary>
        Belt,
        /// <summary>Choker-type exclusive item.</summary>
        Choker,
        /// <summary>Bow-type exclusive item.</summary>
        Bow,
        /// <summary>Scarf-type exclusive item.</summary>
        Scarf,
        /// <summary>Torc-type exclusive item.</summary>
        Torc,
        /// <summary>Sash-type exclusive item.</summary>
        Sash,
        /// <summary>Hat-type exclusive item.</summary>
        Hat,
        /// <summary>Ruff-type exclusive item.</summary>
        Ruff,
        /// <summary>Crown-type exclusive item.</summary>
        Crown,
        /// <summary>Tiara-type exclusive item.</summary>
        Tiara,
        /// <summary>Collar-type exclusive item.</summary>
        Collar,
        /// <summary>Bangle-type exclusive item.</summary>
        Bangle,
        /// <summary>Armlet-type exclusive item.</summary>
        Armlet,
        /// <summary>Tie-type exclusive item.</summary>
        Tie,
        /// <summary>Cape-type exclusive item.</summary>
        Cape,
        /// <summary>Mantle-type exclusive item.</summary>
        Mantle,
        /// <summary>Cap-type exclusive item.</summary>
        Cap,
        /// <summary>Mask-type exclusive item.</summary>
        Mask,
        /// <summary>Helmet-type exclusive item.</summary>
        Helmet,
        /// <summary>Armor-type exclusive item.</summary>
        Armor,
        /// <summary>Shield-type exclusive item.</summary>
        Shield,
        /// <summary>Drill-type exclusive item.</summary>
        Drill,
        /// <summary>Apron-type exclusive item.</summary>
        Apron,
        /// <summary>Poncho-type exclusive item.</summary>
        Poncho,
        /// <summary>Veil-type exclusive item.</summary>
        Veil,
        /// <summary>Robe-type exclusive item.</summary>
        Robe,
        /// <summary>Specs-type exclusive item.</summary>
        Specs,
        /// <summary>Glasses-type exclusive item.</summary>
        Glasses,
        /// <summary>Scope-type exclusive item.</summary>
        Scope,
        /// <summary>Float-type exclusive item.</summary>
        Float,
        /// <summary>Dress-type exclusive item.</summary>
        Dress,
        /// <summary>Coat-type exclusive item.</summary>
        Coat
    }

    /// <summary>
    /// Item state that marks an item as exclusive to specific monster families.
    /// Stores the cosmetic item type classification.
    /// </summary>
    [Serializable]
    public class ExclusiveState : ItemState
    {
        /// <summary>
        /// The cosmetic type classification of this exclusive item.
        /// </summary>
        public ExclusiveItemType ItemType;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public ExclusiveState() { }

        /// <summary>
        /// Initializes a new instance with the specified item type.
        /// </summary>
        /// <param name="itemType">The exclusive item type classification.</param>
        public ExclusiveState(ExclusiveItemType itemType) { ItemType = itemType; }

        /// <summary>
        /// Copy constructor for cloning an existing ExclusiveState.
        /// </summary>
        protected ExclusiveState(ExclusiveState other) { ItemType = other.ItemType; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new ExclusiveState(this); }
    }

    /// <summary>
    /// Item state that defines which monster species can use this item.
    /// Used for family-exclusive items that only work for certain species.
    /// </summary>
    [Serializable]
    public class FamilyState : ItemState
    {
        /// <summary>
        /// List of monster species IDs that can use this item.
        /// </summary>
        [DataType(1, DataManager.DataType.Monster, false)]
        public List<string> Members;

        /// <summary>
        /// Initializes a new instance with an empty member list.
        /// </summary>
        public FamilyState() { Members = new List<string>(); }

        /// <summary>
        /// Initializes a new instance with the specified species IDs.
        /// </summary>
        /// <param name="dexNums">Array of monster species IDs.</param>
        public FamilyState(string[] dexNums) : this()
        {
            Members.AddRange(dexNums);
        }

        /// <summary>
        /// Copy constructor for cloning an existing FamilyState.
        /// </summary>
        protected FamilyState(FamilyState other) : this()
        {
            Members.AddRange(other.Members);
        }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new FamilyState(this); }
    }

    /// <summary>
    /// Marker item state indicating the item can be eaten.
    /// Used by items that can be consumed for effects.
    /// </summary>
    [Serializable]
    public class EdibleState : ItemState
    {
        /// <inheritdoc/>
        public override GameplayState Clone() { return new EdibleState(); }
    }

    /// <summary>
    /// Marker item state indicating the item is a food item.
    /// Used to categorize edible items that restore belly.
    /// </summary>
    [Serializable]
    public class FoodState : ItemState
    {
        /// <inheritdoc/>
        public override GameplayState Clone() { return new FoodState(); }
    }

    /// <summary>
    /// Marker item state indicating the item is a berry.
    /// Berries have special interactions with certain abilities.
    /// </summary>
    [Serializable]
    public class BerryState : ItemState
    {
        /// <inheritdoc/>
        public override GameplayState Clone() { return new BerryState(); }
    }

    /// <summary>
    /// Marker item state indicating the item is a seed.
    /// Seeds typically have immediate effects when consumed.
    /// </summary>
    [Serializable]
    public class SeedState : ItemState
    {
        /// <inheritdoc/>
        public override GameplayState Clone() { return new SeedState(); }
    }

    /// <summary>
    /// Marker item state indicating the item is an herb.
    /// Herbs provide healing or restorative effects.
    /// </summary>
    [Serializable]
    public class HerbState : ItemState
    {
        /// <inheritdoc/>
        public override GameplayState Clone() { return new HerbState(); }
    }

    /// <summary>
    /// Marker item state indicating the item is a gummi.
    /// Gummis raise IQ/stats when eaten by matching types.
    /// </summary>
    [Serializable]
    public class GummiState : ItemState
    {
        /// <inheritdoc/>
        public override GameplayState Clone() { return new GummiState(); }
    }

    /// <summary>
    /// Marker item state indicating the item is a drink/elixir.
    /// Drinks typically restore PP or provide status effects.
    /// </summary>
    [Serializable]
    public class DrinkState : ItemState
    {
        /// <inheritdoc/>
        public override GameplayState Clone() { return new DrinkState(); }
    }

    /// <summary>
    /// Marker item state indicating the item is a wand.
    /// Wands have limited uses and fire projectile effects.
    /// </summary>
    [Serializable]
    public class WandState : ItemState
    {
        /// <inheritdoc/>
        public override GameplayState Clone() { return new WandState(); }
    }

    /// <summary>
    /// Marker item state indicating the item is an orb.
    /// Orbs have room-wide or floor-wide effects when used.
    /// </summary>
    [Serializable]
    public class OrbState : ItemState
    {
        /// <inheritdoc/>
        public override GameplayState Clone() { return new OrbState(); }
    }

    /// <summary>
    /// Marker item state indicating the item is throwable ammunition.
    /// Ammo items deal damage when thrown at targets.
    /// </summary>
    [Serializable]
    public class AmmoState : ItemState
    {
        /// <inheritdoc/>
        public override GameplayState Clone() { return new AmmoState(); }
    }

    /// <summary>
    /// Marker item state indicating the item is a utility item.
    /// Utility items provide non-combat functionality.
    /// </summary>
    [Serializable]
    public class UtilityState : ItemState
    {
        /// <inheritdoc/>
        public override GameplayState Clone() { return new UtilityState(); }
    }

    /// <summary>
    /// Marker item state indicating the item can be held for passive effects.
    /// Held items provide bonuses while equipped.
    /// </summary>
    [Serializable]
    public class HeldState : ItemState
    {
        /// <inheritdoc/>
        public override GameplayState Clone() { return new HeldState(); }
    }

    /// <summary>
    /// Marker item state indicating the item is equipment.
    /// Equipment items are worn and provide stat bonuses.
    /// </summary>
    [Serializable]
    public class EquipState : ItemState
    {
        /// <inheritdoc/>
        public override GameplayState Clone() { return new EquipState(); }
    }

    /// <summary>
    /// Marker item state indicating the item triggers evolution.
    /// Evolution items allow monsters to evolve when used.
    /// </summary>
    [Serializable]
    public class EvoState : ItemState
    {
        /// <inheritdoc/>
        public override GameplayState Clone() { return new EvoState(); }
    }

    /// <summary>
    /// Marker item state indicating the item is a TM/HM machine.
    /// Machines teach moves to compatible monsters.
    /// </summary>
    [Serializable]
    public class MachineState : ItemState
    {
        /// <inheritdoc/>
        public override GameplayState Clone() { return new MachineState(); }
    }

    /// <summary>
    /// Marker item state indicating the item is used for recruitment.
    /// Recruit items are thrown to capture wild monsters.
    /// </summary>
    [Serializable]
    public class RecruitState : ItemState
    {
        /// <inheritdoc/>
        public override GameplayState Clone() { return new RecruitState(); }
    }

    /// <summary>
    /// Marker item state indicating the item cures status conditions.
    /// Curer items remove negative status effects when used.
    /// </summary>
    [Serializable]
    public class CurerState : ItemState
    {
        /// <inheritdoc/>
        public override GameplayState Clone() { return new CurerState(); }
    }

}

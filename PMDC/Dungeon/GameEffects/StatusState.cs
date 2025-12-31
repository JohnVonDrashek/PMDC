using System;
using RogueEssence.Data;
using RogueEssence.Dungeon;
using RogueEssence.Dev;
using Newtonsoft.Json;

namespace PMDC.Dungeon
{
    /// <summary>
    /// Status state that stores an HP value, used for effects that deal or heal a specific amount of HP.
    /// </summary>
    [Serializable]
    public class HPState : StatusState
    {
        /// <summary>
        /// The HP value associated with this status effect.
        /// </summary>
        public int HP;

        /// <summary>
        /// Initializes a new instance of the <see cref="HPState"/> class with a default HP value of 0.
        /// </summary>
        public HPState() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="HPState"/> class with the specified HP value.
        /// </summary>
        /// <param name="hp">The HP value to store.</param>
        public HPState(int hp) { HP = hp; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HPState"/> class by copying another HPState instance.
        /// </summary>
        /// <param name="other">The HPState instance to copy from.</param>
        protected HPState(HPState other) { HP = other.HP; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new HPState(this); }
    }
    /// <summary>
    /// Marker status state indicating the status was recently applied.
    /// Used to track if a status effect is new this turn.
    /// </summary>
    [Serializable]
    public class RecentState : StatusState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RecentState"/> class.
        /// </summary>
        public RecentState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new RecentState(); }
    }

    /// <summary>
    /// Status state that stores a skill slot index.
    /// Used for effects that target or affect a specific move slot.
    /// </summary>
    [Serializable]
    public class SlotState : StatusState
    {
        /// <summary>
        /// The skill slot index (0-3 typically).
        /// </summary>
        public int Slot;

        /// <summary>
        /// Initializes a new instance of the <see cref="SlotState"/> class with a default slot value of 0.
        /// </summary>
        public SlotState() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SlotState"/> class by copying another SlotState instance.
        /// </summary>
        /// <param name="other">The SlotState instance to copy from.</param>
        protected SlotState(SlotState other) { Slot = other.Slot; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new SlotState(this); }
    }

    /// <summary>
    /// Status state that stores a generic integer index.
    /// Used for tracking numeric values within status effects.
    /// </summary>
    [Serializable]
    public class IndexState : StatusState
    {
        /// <summary>
        /// The index value.
        /// </summary>
        public int Index;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexState"/> class with a default index value of 0.
        /// </summary>
        public IndexState() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexState"/> class with the specified index value.
        /// </summary>
        /// <param name="index">The index value to store.</param>
        public IndexState(int index) { Index = index; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexState"/> class by copying another IndexState instance.
        /// </summary>
        /// <param name="other">The IndexState instance to copy from.</param>
        protected IndexState(IndexState other) { Index = other.Index; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new IndexState(this); }
    }

    /// <summary>
    /// Status state that stores a string identifier.
    /// Used for referencing other game data entries by ID.
    /// </summary>
    [Serializable]
    public class IDState : StatusState
    {
        /// <summary>
        /// The string identifier for referencing game data.
        /// </summary>
        public string ID;

        /// <summary>
        /// Initializes a new instance of the <see cref="IDState"/> class with an empty ID string.
        /// </summary>
        public IDState() { ID = ""; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IDState"/> class with the specified ID string.
        /// </summary>
        /// <param name="index">The ID string to store.</param>
        public IDState(string index) { ID = index; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IDState"/> class by copying another IDState instance.
        /// </summary>
        /// <param name="other">The IDState instance to copy from.</param>
        protected IDState(IDState other) { ID = other.ID; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new IDState(this); }
    }

    /// <summary>
    /// Status state that indicates which stat is being modified by a stat change status.
    /// Used with stat boost/drop effects to identify the affected stat.
    /// </summary>
    [Serializable]
    public class StatChangeState : StatusState
    {
        /// <summary>
        /// The stat being modified (Attack, Defense, Speed, etc.).
        /// </summary>
        public Stat ChangeStat;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatChangeState"/> class with a default stat value.
        /// </summary>
        public StatChangeState() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatChangeState"/> class with the specified stat.
        /// </summary>
        /// <param name="stat">The stat being modified.</param>
        public StatChangeState(Stat stat) { ChangeStat = stat; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatChangeState"/> class by copying another StatChangeState instance.
        /// </summary>
        /// <param name="other">The StatChangeState instance to copy from.</param>
        protected StatChangeState(StatChangeState other) { ChangeStat = other.ChangeStat; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new StatChangeState(this); }
    }

    /// <summary>
    /// Marker status state indicating a negative/harmful status effect.
    /// Used to categorize status effects for immunity and cure checks.
    /// </summary>
    [Serializable]
    public class BadStatusState : StatusState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BadStatusState"/> class.
        /// </summary>
        public BadStatusState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new BadStatusState(); }
    }

    /// <summary>
    /// Marker status state indicating a positive/beneficial status effect.
    /// Used to categorize status effects for buff handling.
    /// </summary>
    [Serializable]
    public class GoodStatusState : StatusState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GoodStatusState"/> class.
        /// </summary>
        public GoodStatusState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new GoodStatusState(); }
    }

    /// <summary>
    /// Marker status state indicating a status effect that can be transferred.
    /// Used by effects like Baton Pass that pass certain statuses to allies.
    /// </summary>
    [Serializable]
    public class TransferStatusState : StatusState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransferStatusState"/> class.
        /// </summary>
        public TransferStatusState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new TransferStatusState(); }
    }

    /// <summary>
    /// Marker status state indicating a major status condition (sleep, poison, burn, etc.).
    /// Characters can typically only have one major status at a time.
    /// </summary>
    [Serializable]
    public class MajorStatusState : StatusState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MajorStatusState"/> class.
        /// </summary>
        public MajorStatusState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new MajorStatusState(); }
    }

    /// <summary>
    /// Status state for paralysis effects that tracks if the character was recently paralyzed.
    /// The Recent flag determines if the character should skip their turn.
    /// </summary>
    [Serializable]
    public class ParalyzeState : StatusState
    {
        /// <summary>
        /// Whether the paralysis effect just triggered, causing the character to be fully paralyzed this turn.
        /// </summary>
        public bool Recent;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParalyzeState"/> class with a default recent value of false.
        /// </summary>
        public ParalyzeState() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParalyzeState"/> class with the specified recent flag.
        /// </summary>
        /// <param name="recent">Whether the paralysis effect just triggered this turn.</param>
        public ParalyzeState(bool recent) { Recent = recent; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParalyzeState"/> class by copying another ParalyzeState instance.
        /// </summary>
        /// <param name="other">The ParalyzeState instance to copy from.</param>
        protected ParalyzeState(ParalyzeState other) { Recent = other.Recent; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new ParalyzeState(this); }
    }

    /// <summary>
    /// Status state that tracks whether a character attacked during this turn.
    /// Used by effects that trigger based on combat activity.
    /// </summary>
    [Serializable]
    public class AttackedThisTurnState : StatusState
    {
        /// <summary>
        /// Whether the character has attacked this turn.
        /// </summary>
        public bool Attacked;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttackedThisTurnState"/> class with a default attacked value of false.
        /// </summary>
        public AttackedThisTurnState() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttackedThisTurnState"/> class with the specified attacked flag.
        /// </summary>
        /// <param name="attacked">Whether the character has attacked this turn.</param>
        public AttackedThisTurnState(bool attacked) { Attacked = attacked; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttackedThisTurnState"/> class by copying another AttackedThisTurnState instance.
        /// </summary>
        /// <param name="other">The AttackedThisTurnState instance to copy from.</param>
        protected AttackedThisTurnState(AttackedThisTurnState other) { Attacked = other.Attacked; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new AttackedThisTurnState(this); }
    }

    /// <summary>
    /// Status state that tracks whether a character walked/moved during this turn.
    /// Used by effects that trigger based on movement activity.
    /// </summary>
    [Serializable]
    public class WalkedThisTurnState : StatusState
    {
        /// <summary>
        /// Whether the character has walked this turn.
        /// </summary>
        public bool Walked;

        /// <summary>
        /// Initializes a new instance of the <see cref="WalkedThisTurnState"/> class with a default walked value of false.
        /// </summary>
        public WalkedThisTurnState() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WalkedThisTurnState"/> class with the specified walked flag.
        /// </summary>
        /// <param name="walked">Whether the character has walked this turn.</param>
        public WalkedThisTurnState(bool walked) { Walked = walked; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WalkedThisTurnState"/> class by copying another WalkedThisTurnState instance.
        /// </summary>
        /// <param name="other">The WalkedThisTurnState instance to copy from.</param>
        protected WalkedThisTurnState(WalkedThisTurnState other) { Walked = other.Walked; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new WalkedThisTurnState(this); }
    }

    /// <summary>
    /// Status state that stores a skill category (Physical, Special, Status).
    /// Used by effects that depend on or modify skill categories.
    /// </summary>
    [Serializable]
    public class CategoryState : StatusState
    {
        /// <summary>
        /// The skill category (Physical, Special, or Status).
        /// </summary>
        public BattleData.SkillCategory Category;

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryState"/> class with a default category value.
        /// </summary>
        public CategoryState() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryState"/> class with the specified skill category.
        /// </summary>
        /// <param name="category">The skill category to store.</param>
        public CategoryState(BattleData.SkillCategory category) { Category = category; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryState"/> class by copying another CategoryState instance.
        /// </summary>
        /// <param name="other">The CategoryState instance to copy from.</param>
        protected CategoryState(CategoryState other) { Category = other.Category; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new CategoryState(this); }
    }

    /// <summary>
    /// Status state that stores an element/type.
    /// Used by effects that grant, check, or modify elemental types.
    /// </summary>
    [Serializable]
    public class ElementState : StatusState
    {
        /// <summary>
        /// The element type ID (e.g., "fire", "water", "electric").
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string Element;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementState"/> class with a default element value.
        /// </summary>
        public ElementState() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementState"/> class with the specified element ID.
        /// </summary>
        /// <param name="element">The element type ID to store (e.g., "fire", "water").</param>
        public ElementState(string element) { Element = element; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementState"/> class by copying another ElementState instance.
        /// </summary>
        /// <param name="other">The ElementState instance to copy from.</param>
        protected ElementState(ElementState other) { Element = other.Element; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new ElementState(this); }
    }

    /// <summary>
    /// Status state that stores a monster form identifier.
    /// Used by transformation and illusion effects to store the target appearance.
    /// </summary>
    [Serializable]
    public class MonsterIDState : StatusState
    {
        /// <summary>
        /// The monster form identifier including species, form, gender, and skin.
        /// </summary>
        public MonsterID MonID;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonsterIDState"/> class with a default monster ID.
        /// </summary>
        public MonsterIDState() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonsterIDState"/> class with the specified monster ID.
        /// </summary>
        /// <param name="id">The monster form identifier to store.</param>
        public MonsterIDState(MonsterID id) { MonID = id; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonsterIDState"/> class by copying another MonsterIDState instance.
        /// </summary>
        /// <param name="other">The MonsterIDState instance to copy from.</param>
        protected MonsterIDState(MonsterIDState other) { MonID = other.MonID; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new MonsterIDState(this); }
    }
}

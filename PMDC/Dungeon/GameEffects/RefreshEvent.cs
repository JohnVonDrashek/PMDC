using System;
using RogueEssence.Data;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Dev;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using PMDC.Dev;

namespace PMDC.Dungeon
{
    /// <summary>
    /// Refresh event that grants terrain mobility based on the character's elemental types.
    /// For example, Water types can traverse water tiles, Fire types can traverse lava.
    /// </summary>
    [Serializable]
    public class ElementMobilityEvent : RefreshEvent
    {
        /// <summary>
        /// Maps element type IDs to the terrain mobility they grant.
        /// </summary>
        [DataType(1, DataManager.DataType.Element, false)]
        public Dictionary<string, TerrainData.Mobility> ElementPair;

        public ElementMobilityEvent()
        {
            ElementPair = new Dictionary<string, TerrainData.Mobility>();
        }
        protected ElementMobilityEvent(ElementMobilityEvent other)
            : this()
        {
            foreach (string element in other.ElementPair.Keys)
                ElementPair.Add(element, other.ElementPair[element]);
        }
        public override GameEvent Clone() { return new ElementMobilityEvent(this); }

        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            TerrainData.Mobility terrain1, terrain2;
            if (ElementPair.TryGetValue(character.Element1, out terrain1))
                character.Mobility |= terrain1;
            if (ElementPair.TryGetValue(character.Element2, out terrain2))
                character.Mobility |= terrain2;
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (Serializer.OldVersion < new Version(0, 6, 4))
            {
                ElementPair["water"] = TerrainData.Mobility.Water;
                ElementPair["fire"] = TerrainData.Mobility.Lava;
                ElementPair["dragon"] = TerrainData.Mobility.Water | TerrainData.Mobility.Lava;
                ElementPair["flying"] = TerrainData.Mobility.Water | TerrainData.Mobility.Lava | TerrainData.Mobility.Abyss;
                ElementPair["ghost"] = TerrainData.Mobility.Block;
            }
        }
    }

    /// <summary>
    /// Refresh event that grants terrain mobility based on the character's species/form.
    /// Used for species-specific movement abilities like Magikarp's water traversal.
    /// </summary>
    [Serializable]
    public class SpeciesMobilityEvent : RefreshEvent
    {
        /// <summary>
        /// Maps monster species/form IDs to their terrain mobility capabilities.
        /// </summary>
        [JsonConverter(typeof(MobilityTableConverter))]
        [RogueEssence.Dev.MonsterID(1, false, true, true, true)]
        public Dictionary<MonsterID, TerrainData.Mobility> IDPair;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeciesMobilityEvent"/> class.
        /// </summary>

        public SpeciesMobilityEvent()
        {
            IDPair = new Dictionary<MonsterID, TerrainData.Mobility>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeciesMobilityEvent"/> class as a copy of another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected SpeciesMobilityEvent(SpeciesMobilityEvent other)
            : this()
        {
            foreach (MonsterID id in other.IDPair.Keys)
                IDPair.Add(id, other.IDPair[id]);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new SpeciesMobilityEvent(this); }

        /// <summary>
        /// Applies the terrain mobility to the character based on their current form.
        /// Tries progressively less specific form matches until a match is found.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to apply mobility to.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            TerrainData.Mobility mobility;
            MonsterID testID = character.CurrentForm;
            if (IDPair.TryGetValue(testID, out mobility))
            {
                character.Mobility = mobility;
                return;
            }
            testID.Gender = Gender.Unknown;
            if (IDPair.TryGetValue(testID, out mobility))
            {
                character.Mobility = mobility;
                return;
            }
            testID.Skin = "";
            if (IDPair.TryGetValue(testID, out mobility))
            {
                character.Mobility = mobility;
                return;
            }
            testID.Form = -1;
            if (IDPair.TryGetValue(testID, out mobility))
            {
                character.Mobility = mobility;
                return;
            }
        }
    }

    /// <summary>
    /// Refresh event wrapper that only applies when the item owner belongs to a specific family.
    /// Used for family-exclusive item effects.
    /// </summary>
    [Serializable]
    public class FamilyRefreshEvent : RefreshEvent
    {
        /// <summary>
        /// The refresh event to apply when the family condition is met.
        /// </summary>
        public RefreshEvent BaseEvent;

        /// <summary>
        /// Initializes a new instance of the <see cref="FamilyRefreshEvent"/> class.
        /// </summary>
        public FamilyRefreshEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FamilyRefreshEvent"/> class with a base event.
        /// </summary>
        /// <param name="baseEvent">The refresh event to apply when family condition is met.</param>
        public FamilyRefreshEvent(RefreshEvent baseEvent) { BaseEvent = baseEvent; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FamilyRefreshEvent"/> class as a copy of another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected FamilyRefreshEvent(FamilyRefreshEvent other)
        {
            BaseEvent = (RefreshEvent)other.BaseEvent.Clone();
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new FamilyRefreshEvent(this); }

        /// <summary>
        /// Applies the base event only if the item owner belongs to the specified family.
        /// </summary>
        /// <param name="owner">The item that owns this event.</param>
        /// <param name="ownerChar">The character that owns the item.</param>
        /// <param name="character">The character to potentially apply the event to.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            ItemData entry = DataManager.Instance.GetItem(owner.GetID());
            FamilyState family;
            if (!entry.ItemStates.TryGet<FamilyState>(out family))
                return;
            if (family.Members.Contains(ownerChar.BaseForm.Species))
                BaseEvent.Apply(owner, ownerChar, character);
        }
    }

    /// <summary>
    /// Refresh event that adds specific terrain mobility to a character.
    /// Used for abilities and items that grant movement through specific terrain.
    /// </summary>
    [Serializable]
    public class AddMobilityEvent : RefreshEvent
    {
        /// <summary>
        /// The terrain mobility flags to add to the character.
        /// </summary>
        public TerrainData.Mobility Mobility;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddMobilityEvent"/> class.
        /// </summary>
        public AddMobilityEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddMobilityEvent"/> class with specified mobility.
        /// </summary>
        /// <param name="mobility">The terrain mobility flags to grant.</param>
        public AddMobilityEvent(TerrainData.Mobility mobility)
        {
            Mobility = mobility;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddMobilityEvent"/> class as a copy of another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected AddMobilityEvent(AddMobilityEvent other)
        {
            Mobility = other.Mobility;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new AddMobilityEvent(this); }

        /// <summary>
        /// Applies the terrain mobility to the character.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to add mobility to.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            character.Mobility |= Mobility;
        }
    }

    /// <summary>
    /// Refresh event wrapper that only applies to characters of a specific faction.
    /// Used for faction-conditional effects like ally-only buffs.
    /// </summary>
    [Serializable]
    public class FactionRefreshEvent : RefreshEvent
    {
        /// <summary>
        /// The faction that must match for the event to apply.
        /// </summary>
        public Faction Faction;

        /// <summary>
        /// The refresh event to apply when the faction condition is met.
        /// </summary>
        public RefreshEvent BaseEvent;

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionRefreshEvent"/> class.
        /// </summary>
        public FactionRefreshEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionRefreshEvent"/> class with specified faction and event.
        /// </summary>
        /// <param name="faction">The faction that must match for the event to apply.</param>
        /// <param name="baseEvent">The refresh event to apply when the faction condition is met.</param>
        public FactionRefreshEvent(Faction faction, RefreshEvent baseEvent)
        {
            Faction = faction;
            BaseEvent = baseEvent;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionRefreshEvent"/> class as a copy of another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected FactionRefreshEvent(FactionRefreshEvent other)
        {
            Faction = other.Faction;
            BaseEvent = (RefreshEvent)other.BaseEvent.Clone();
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new FactionRefreshEvent(this); }

        /// <summary>
        /// Applies the base event only if the character belongs to the specified faction.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to check faction for and potentially apply the event to.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            Faction charFaction = ZoneManager.Instance.CurrentMap.GetCharFaction(character);
            if (charFaction == Faction)
                BaseEvent.Apply(owner, ownerChar, character);
        }
    }

    /// <summary>
    /// Refresh event that sets a character's sight range for either characters or tiles.
    /// Used for vision-modifying effects.
    /// </summary>
    [Serializable]
    public class SetSightEvent : RefreshEvent
    {
        /// <summary>
        /// If true, modifies character sight (seeing other characters). If false, modifies tile sight (seeing terrain).
        /// </summary>
        public bool CharSight;

        /// <summary>
        /// The sight range to set.
        /// </summary>
        public Map.SightRange Sight;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetSightEvent"/> class.
        /// </summary>
        public SetSightEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SetSightEvent"/> class with specified sight parameters.
        /// </summary>
        /// <param name="charSight">If true, modifies character sight; if false, modifies tile sight.</param>
        /// <param name="sight">The sight range to set.</param>
        public SetSightEvent(bool charSight, Map.SightRange sight)
        {
            CharSight = charSight;
            Sight = sight;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SetSightEvent"/> class as a copy of another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected SetSightEvent(SetSightEvent other)
        {
            CharSight = other.CharSight;
            Sight = other.Sight;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new SetSightEvent(this); }

        /// <summary>
        /// Applies the sight range to the character.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to set sight range for.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            if (CharSight)
                character.CharSight = Sight;
            else
                character.TileSight = Sight;
        }
    }
    /// <summary>
    /// Refresh event that allows the character to see all other characters on the map.
    /// Used for abilities like Keen Eye.
    /// </summary>
    [Serializable]
    public class SeeCharsEvent : RefreshEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new SeeCharsEvent(); }

        /// <summary>
        /// Applies the ability to see all characters to the specified character.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to grant all-character sight to.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            character.SeeAllChars = true;
        }
    }

    /// <summary>
    /// Refresh event that allows the character to see hidden traps.
    /// Used for trap-detection abilities.
    /// </summary>
    [Serializable]
    public class SeeTrapsEvent : RefreshEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new SeeTrapsEvent(); }

        /// <summary>
        /// Applies the ability to see traps to the specified character.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to grant trap sight to.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            character.SeeTraps = true;
        }
    }

    /// <summary>
    /// Refresh event that allows the character to see items on the floor or in walls.
    /// Used for item-detection abilities.
    /// </summary>
    [Serializable]
    public class SeeItemsEvent : RefreshEvent
    {
        /// <summary>
        /// If true, allows seeing items buried in walls. If false, allows seeing floor items.
        /// </summary>
        public bool WallItems;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeeItemsEvent"/> class.
        /// </summary>
        public SeeItemsEvent()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SeeItemsEvent"/> class with wall items setting.
        /// </summary>
        /// <param name="wallItems">If true, allows seeing wall items; if false, allows seeing floor items.</param>
        public SeeItemsEvent(bool wallItems)
        {
            WallItems = wallItems;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SeeItemsEvent"/> class as a copy of another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected SeeItemsEvent(SeeItemsEvent other)
        {
            WallItems = other.WallItems;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new SeeItemsEvent(this); }

        /// <summary>
        /// Applies the item sight capability to the character.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to grant item sight to.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            if (WallItems)
                character.SeeWallItems = true;
            else
                character.SeeItems = true;
        }
    }

    /// <summary>
    /// Refresh event that blinds the character, preventing them from seeing tiles or characters.
    /// Used for blind status effects.
    /// </summary>
    [Serializable]
    public class BlindEvent : RefreshEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new BlindEvent(); }

        /// <summary>
        /// Applies blindness to the character.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to blind.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            character.CharSight = Map.SightRange.Blind;
            character.TileSight = Map.SightRange.Blind;
        }
    }

    /// <summary>
    /// Refresh event that hides the character's name, displaying "???" instead.
    /// Used for disguise and mystery effects.
    /// </summary>
    [Serializable]
    public class NoNameEvent : RefreshEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new NoNameEvent(); }

        /// <summary>
        /// Applies the name-hiding effect to the character.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character whose name to hide.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            character.ProxyName = "???";
        }
    }

    /// <summary>
    /// Refresh event that makes the character unidentifiable and unlocatable.
    /// Used for invisibility and vanishing effects.
    /// </summary>
    [Serializable]
    public class VanishEvent : RefreshEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new VanishEvent(); }

        /// <summary>
        /// Applies the vanish effect to the character.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to make unidentifiable and unlocatable.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            character.Unidentifiable = true;
            character.Unlocatable = true;
        }
    }

    /// <summary>
    /// Refresh event that applies an illusion, changing the character's apparent sprite and name.
    /// Used by Transform and Illusion ability.
    /// </summary>
    [Serializable]
    public class IllusionEvent : RefreshEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new IllusionEvent(); }

        /// <summary>
        /// Applies the illusion effect to the character using the monster ID stored in the status effect.
        /// </summary>
        /// <param name="owner">The status effect that owns this event.</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to apply the illusion to.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            MonsterID proxy = ((StatusEffect)owner).StatusStates.GetWithDefault<MonsterIDState>().MonID;
            character.ProxySprite = proxy;
            character.ProxyName = Character.GetFullFormName(character.Appearance);
        }
    }

    /// <summary>
    /// Refresh event that changes the character's visual appearance to a specified form.
    /// Used for cosmetic transformations.
    /// </summary>
    [Serializable]
    public class AppearanceEvent : RefreshEvent
    {
        /// <summary>
        /// The monster form to display as the character's appearance.
        /// </summary>
        MonsterID Appearance;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppearanceEvent"/> class.
        /// </summary>
        public AppearanceEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppearanceEvent"/> class with specified appearance.
        /// </summary>
        /// <param name="appearance">The monster form to use as appearance.</param>
        public AppearanceEvent(MonsterID appearance) { Appearance = appearance; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppearanceEvent"/> class as a copy of another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected AppearanceEvent(AppearanceEvent other)
        {
            Appearance = other.Appearance;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new AppearanceEvent(this); }

        /// <summary>
        /// Applies the appearance change to the character.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to change the appearance of.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            character.ProxySprite = Appearance;
        }
    }
    /// <summary>
    /// Refresh event that adds speed to characters of a specific element type.
    /// Only applies if the character has no bad status conditions.
    /// </summary>
    [Serializable]
    public class AddTypeSpeedEvent : RefreshEvent
    {
        /// <summary>
        /// State to set on the character to prevent duplicate speed boosts.
        /// </summary>
        public CharState NoDupeEffect;

        /// <summary>
        /// The element type required to receive the speed boost.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        public string Element;

        /// <summary>
        /// The speed bonus to add.
        /// </summary>
        public int Speed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddTypeSpeedEvent"/> class.
        /// </summary>
        public AddTypeSpeedEvent() { Element = ""; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddTypeSpeedEvent"/> class with specified parameters.
        /// </summary>
        /// <param name="element">The element type required to receive the speed boost.</param>
        /// <param name="speed">The speed bonus to add.</param>
        /// <param name="effect">The character state to prevent duplicate effects.</param>
        public AddTypeSpeedEvent(string element, int speed, CharState effect)
        {
            Element = element;
            Speed = speed;
            NoDupeEffect = effect;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddTypeSpeedEvent"/> class as a copy of another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected AddTypeSpeedEvent(AddTypeSpeedEvent other)
        {
            Element = other.Element;
            Speed = other.Speed;
            NoDupeEffect = other.NoDupeEffect;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new AddTypeSpeedEvent(this); }

        /// <summary>
        /// Applies the speed boost to the character if they match the element type and have no bad statuses.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to potentially add speed to.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            if (character.HasElement(Element) && !character.CharStates.Contains(NoDupeEffect.GetType()))
            {
                bool hasStatus = false;
                foreach (string statusID in character.StatusEffects.Keys)
                {
                    if (character.StatusEffects[statusID].StatusStates.Contains<BadStatusState>())
                    {
                        hasStatus = true;
                        break;
                    }
                }
                if (!hasStatus)
                {
                    character.MovementSpeed += Speed;
                    character.CharStates.Set(NoDupeEffect.Clone<CharState>());
                }
            }
        }
    }
    /// <summary>
    /// Refresh event that modifies a character's movement speed.
    /// Positive values increase speed, negative values decrease it.
    /// </summary>
    [Serializable]
    public class AddSpeedEvent : RefreshEvent
    {
        /// <summary>
        /// The speed modifier to add to the character's movement speed.
        /// </summary>
        public int Speed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddSpeedEvent"/> class.
        /// </summary>
        public AddSpeedEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddSpeedEvent"/> class with specified speed modifier.
        /// </summary>
        /// <param name="speed">The speed modifier to add (positive increases, negative decreases).</param>
        public AddSpeedEvent(int speed) { Speed = speed; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddSpeedEvent"/> class as a copy of another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected AddSpeedEvent(AddSpeedEvent other)
        {
            Speed = other.Speed;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new AddSpeedEvent(this); }

        /// <summary>
        /// Applies the speed modifier to the character.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to modify speed for.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            character.MovementSpeed += Speed;
        }
    }

    /// <summary>
    /// Refresh event that limits the character's speed to zero or below.
    /// Used for speed-limiting effects that prevent speed boosts.
    /// </summary>
    [Serializable]
    public class SpeedLimitEvent : RefreshEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new SpeedLimitEvent(); }

        /// <summary>
        /// Applies the speed limit to the character.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to limit speed for.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            character.MovementSpeed = Math.Min(0, character.MovementSpeed);
        }
    }

    /// <summary>
    /// Refresh event that reverses the character's speed modifier.
    /// Speed boosts become drops and vice versa.
    /// </summary>
    [Serializable]
    public class SpeedReverseEvent : RefreshEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new SpeedReverseEvent(); }

        /// <summary>
        /// Applies the speed reversal to the character.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to reverse speed for.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            character.MovementSpeed = character.MovementSpeed * -1;
        }
    }

    /// <summary>
    /// Refresh event that boosts speed when the character has no held item.
    /// Implements the Unburden ability.
    /// </summary>
    [Serializable]
    public class UnburdenEvent : RefreshEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnburdenEvent"/> class.
        /// </summary>
        public UnburdenEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new UnburdenEvent(); }

        /// <summary>
        /// Applies the unburden bonus if the character has no equipped item.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to potentially boost speed for.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            if (String.IsNullOrEmpty(character.EquippedItem.ID))
                character.MovementSpeed += 1;
        }
    }
    /// <summary>
    /// Refresh event that boosts speed when a specific weather condition is active.
    /// Used by weather-based speed abilities like Swift Swim and Chlorophyll.
    /// </summary>
    [Serializable]
    public class WeatherSpeedEvent : RefreshEvent
    {
        /// <summary>
        /// The weather/map status ID that triggers the speed boost.
        /// </summary>
        [JsonConverter(typeof(MapStatusConverter))]
        [DataType(0, DataManager.DataType.MapStatus, false)]
        public string WeatherID;

        /// <summary>
        /// Initializes a new instance of the <see cref="WeatherSpeedEvent"/> class.
        /// </summary>
        public WeatherSpeedEvent() { WeatherID = ""; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeatherSpeedEvent"/> class with specified weather ID.
        /// </summary>
        /// <param name="id">The map status ID that triggers the speed boost.</param>
        public WeatherSpeedEvent(string id) { WeatherID = id; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeatherSpeedEvent"/> class as a copy of another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected WeatherSpeedEvent(WeatherSpeedEvent other)
        {
            WeatherID = other.WeatherID;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new WeatherSpeedEvent(this); }

        /// <summary>
        /// Applies the speed boost if the specified weather is active.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to potentially boost speed for.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            if (ZoneManager.Instance.CurrentMap != null && ZoneManager.Instance.CurrentMap.Status.ContainsKey(WeatherID))
                character.MovementSpeed += 1;
        }
    }
    /// <summary>
    /// Refresh event that boosts speed when the character has a major status effect.
    /// Major status effects trigger the speed boost.
    /// </summary>
    [Serializable]
    public class StatusSpeedEvent : RefreshEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StatusSpeedEvent"/> class.
        /// </summary>
        public StatusSpeedEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new StatusSpeedEvent(); }

        /// <summary>
        /// Applies the speed boost if the character has a major status effect.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to potentially boost speed for.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            foreach (StatusEffect status in character.IterateStatusEffects())
            {
                if (status.StatusStates.Contains<MajorStatusState>())
                {
                    character.MovementSpeed += 1;
                    break;
                }
            }
        }
    }
    /// <summary>
    /// Refresh event that prevents movement if the character has any of the specified states.
    /// </summary>
    [Serializable]
    public class ImmobilizationEvent : RefreshEvent
    {
        /// <summary>
        /// List of character states that trigger immobilization.
        /// </summary>
        [StringTypeConstraint(1, typeof(CharState))]
        public List<FlagType> States;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmobilizationEvent"/> class.
        /// </summary>
        public ImmobilizationEvent() { States = new List<FlagType>(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmobilizationEvent"/> class with a specific character state.
        /// </summary>
        /// <param name="state">The character state type to track for immobilization.</param>
        public ImmobilizationEvent(Type state) : this() { States.Add(new FlagType(state)); }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmobilizationEvent"/> class as a copy of another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected ImmobilizationEvent(ImmobilizationEvent other) : this()
        {
            States.AddRange(other.States);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ImmobilizationEvent(this); }

        /// <summary>
        /// Applies immobilization if the character has any of the tracked states.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to potentially immobilize.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            bool hasState = false;
            foreach (FlagType state in States)
            {
                if (character.CharStates.Contains(state.FullType))
                    hasState = true;
            }
            if (!hasState)
                character.CantWalk = true;
        }
    }

    /// <summary>
    /// Refresh event that prevents all interactions except for using moves and normal attack.
    /// </summary>
    [Serializable]
    public class AttackOnlyEvent : RefreshEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new AttackOnlyEvent(); }

        /// <summary>
        /// Applies the attack-only restriction to the character.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to restrict interactions for.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            character.CantInteract = true;
            character.WaitToAttack = true;
        }
    }

    /// <summary>
    /// Refresh event that paralyzes the character on recent turns, preventing actions.
    /// </summary>
    [Serializable]
    public class ParaPauseEvent : RefreshEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new ParaPauseEvent(); }

        /// <summary>
        /// Applies paralysis restrictions if the status is recent.
        /// </summary>
        /// <param name="owner">The status effect that owns this event.</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to potentially paralyze.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            ParalyzeState para = ((StatusEffect)owner).StatusStates.GetWithDefault<ParalyzeState>();
            if (para.Recent)
            {
                character.CantWalk = true;
                character.CantInteract = true;
                character.WaitToAttack = true;
            }
        }
    }

    /// <summary>
    /// Refresh event that allows the character to remove stuck item conditions.
    /// </summary>
    [Serializable]
    public class NoStickItemEvent : RefreshEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new NoStickItemEvent(); }

        /// <summary>
        /// Applies the ability to remove stuck items to the character.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to grant stuck-removal ability to.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            character.CanRemoveStuck = true;
        }
    }

    /// <summary>
    /// Refresh event that disables the character's held item.
    /// </summary>
    [Serializable]
    public class NoHeldItemEvent : RefreshEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new NoHeldItemEvent(); }

        /// <summary>
        /// Applies the held item disable to the character.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to disable held item for.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            character.ItemDisabled = true;
        }
    }

    /// <summary>
    /// Refresh event that disables the character's ability (intrinsic).
    /// </summary>
    [Serializable]
    public class NoAbilityEvent : RefreshEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new NoAbilityEvent(); }

        /// <summary>
        /// Applies the ability disable to the character.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to disable ability for.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            character.IntrinsicDisabled = true;
        }
    }

    /// <summary>
    /// Refresh event that boosts speed based on a stack count from the status effect.
    /// </summary>
    [Serializable]
    public class SpeedStackEvent : RefreshEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpeedStackEvent"/> class.
        /// </summary>
        public SpeedStackEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new SpeedStackEvent(); }

        /// <summary>
        /// Applies the speed boost based on the current stack count.
        /// </summary>
        /// <param name="owner">The status effect that owns this event.</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to boost speed for.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            int boost = ((StatusEffect)owner).StatusStates.GetWithDefault<StackState>().Stack;
            character.MovementSpeed += boost;
        }
    }

    /// <summary>
    /// Refresh event that disables a specific move slot.
    /// </summary>
    [Serializable]
    public class DisableEvent : RefreshEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DisableEvent"/> class.
        /// </summary>
        public DisableEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new DisableEvent(); }

        /// <summary>
        /// Applies the move disable to the character's specified move slot.
        /// </summary>
        /// <param name="owner">The status effect that owns this event.</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to disable a move for.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            character.Skills[((StatusEffect)owner).StatusStates.GetWithDefault<SlotState>().Slot].Element.Sealed = true;
        }
    }
    /// <summary>
    /// Refresh event that locks either a specific move slot or all others.
    /// </summary>
    [Serializable]
    public class MoveLockEvent : RefreshEvent
    {
        /// <summary>
        /// If true, locks all moves except the specified slot. If false, locks only the specified slot.
        /// </summary>
        public bool LockOthers;

        /// <summary>
        /// The status effect ID that stores the slot to lock/unlock.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string LastSlotStatusID;

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveLockEvent"/> class.
        /// </summary>
        public MoveLockEvent() { LastSlotStatusID = ""; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveLockEvent"/> class with specified parameters.
        /// </summary>
        /// <param name="statusID">The status effect ID that stores the move slot.</param>
        /// <param name="lockOthers">If true, locks other moves; if false, locks only the specified move.</param>
        public MoveLockEvent(string statusID, bool lockOthers)
        {
            LastSlotStatusID = statusID;
            LockOthers = lockOthers;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveLockEvent"/> class as a copy of another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected MoveLockEvent(MoveLockEvent other)
        {
            LockOthers = other.LockOthers;
            LastSlotStatusID = other.LastSlotStatusID;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new MoveLockEvent(this); }

        /// <summary>
        /// Applies the move lock to the character's moves based on the specified slot.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to lock moves for.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            StatusEffect status = character.GetStatusEffect(LastSlotStatusID);
            if (status != null)
            {
                int slot = status.StatusStates.GetWithDefault<SlotState>().Slot;
                for (int ii = 0; ii < character.Skills.Count; ii++)
                {
                    if (!String.IsNullOrEmpty(character.Skills[ii].Element.SkillNum) && ((ii == slot) != LockOthers))
                        character.Skills[ii].Element.Sealed = true;
                }
            }
        }
    }
    /// <summary>
    /// Refresh event that prevents the use of status-category moves (taunt effect).
    /// </summary>
    [Serializable]
    public class TauntEvent : RefreshEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new TauntEvent(); }

        /// <summary>
        /// Applies taunt by disabling all status-category moves.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to taunt.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            for (int ii = 0; ii < character.Skills.Count; ii++)
            {
                if (!String.IsNullOrEmpty(character.Skills[ii].Element.SkillNum) && DataManager.Instance.GetSkill(character.Skills[ii].Element.SkillNum).Data.Category == BattleData.SkillCategory.Status)
                    character.Skills[ii].Element.Sealed = true;
            }
        }
    }

    /// <summary>
    /// Refresh event that adds charge boost to a character.
    /// Charge affects how quickly moves charge for release.
    /// </summary>
    [Serializable]
    public class AddChargeEvent : RefreshEvent
    {
        /// <summary>
        /// The amount of charge boost to add.
        /// </summary>
        public int AddCharge;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddChargeEvent"/> class.
        /// </summary>
        public AddChargeEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddChargeEvent"/> class with specified charge.
        /// </summary>
        /// <param name="addCharge">The amount of charge boost to add.</param>
        public AddChargeEvent(int addCharge)
        {
            AddCharge = addCharge;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddChargeEvent"/> class as a copy of another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected AddChargeEvent(AddChargeEvent other)
        {
            AddCharge = other.AddCharge;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new AddChargeEvent(this); }

        /// <summary>
        /// Applies the charge boost to the character.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to add charge boost to.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            character.ChargeBoost += AddCharge;
        }
    }

    /// <summary>
    /// Refresh event that unlocks all the character's moves.
    /// </summary>
    [Serializable]
    public class FreeMoveEvent : RefreshEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new FreeMoveEvent(); }

        /// <summary>
        /// Applies the free move unlock to all of the character's moves.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to unlock moves for.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            for (int ii = 0; ii < character.Skills.Count; ii++)
            {
                if (!String.IsNullOrEmpty(character.Skills[ii].Element.SkillNum))
                    character.Skills[ii].Element.Sealed = false;
            }
        }
    }

    /// <summary>
    /// Refresh event that bans a specific move from being used by any character on the map.
    /// </summary>
    [Serializable]
    public class MoveBanEvent : RefreshEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new MoveBanEvent(); }

        /// <summary>
        /// Applies the move ban to the character for the specified move.
        /// </summary>
        /// <param name="owner">The map status that owns this event.</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to ban the move for.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            for (int ii = 0; ii < character.Skills.Count; ii++)
            {
                if (character.Skills[ii].Element.SkillNum == ((MapStatus)owner).StatusStates.GetWithDefault<MapIDState>().ID)
                    character.Skills[ii].Element.Sealed = true;
            }
        }
    }

    /// <summary>
    /// Refresh event that scrambles the character's move order.
    /// </summary>
    [Serializable]
    public class MovementScrambleEvent : RefreshEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new MovementScrambleEvent(); }

        /// <summary>
        /// Applies the move scramble effect to the character.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to scramble moves for.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            character.MovesScrambled = true;
        }
    }

    /// <summary>
    /// Refresh event that reduces move charge cost (PP Saver ability).
    /// </summary>
    [Serializable]
    public class PPSaverEvent : RefreshEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new PPSaverEvent(); }

        /// <summary>
        /// Applies the charge save effect to the character.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to enable charge saving for.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            character.ChargeSaver = true;
        }
    }

    /// <summary>
    /// Refresh event that stops thrown items at the target rather than traveling through.
    /// </summary>
    [Serializable]
    public class ThrownItemBarrierEvent : RefreshEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new ThrownItemBarrierEvent(); }

        /// <summary>
        /// Applies the thrown item barrier effect to the character.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to enable item barrier for.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            character.StopItemAtHit = true;
        }
    }

    /// <summary>
    /// Refresh event that allows the character to attack allies.
    /// </summary>
    [Serializable]
    public class FriendlyFireToEvent : RefreshEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new FriendlyFireToEvent(); }

        /// <summary>
        /// Applies the friendly fire effect to the character.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to enable friendly fire for.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            character.AttackFriend = true;
        }
    }

    /// <summary>
    /// Refresh event that makes the character an enemy to their allies.
    /// Allies may attack them.
    /// </summary>
    [Serializable]
    public class FriendlyFiredEvent : RefreshEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new FriendlyFiredEvent(); }

        /// <summary>
        /// Applies the enemy status to the character.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to mark as enemy of allies.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            character.EnemyOfFriend = true;
        }
    }
    /// <summary>
    /// Refresh event that applies a miscellaneous character state effect.
    /// Generic event for applying any character state.
    /// </summary>
    [Serializable]
    public class MiscEvent : RefreshEvent
    {
        /// <summary>
        /// The character state to apply to the character.
        /// </summary>
        public CharState Effect;

        /// <summary>
        /// Initializes a new instance of the <see cref="MiscEvent"/> class.
        /// </summary>
        public MiscEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MiscEvent"/> class with specified effect.
        /// </summary>
        /// <param name="effect">The character state to apply.</param>
        public MiscEvent(CharState effect)
        {
            Effect = effect;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MiscEvent"/> class as a copy of another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected MiscEvent(MiscEvent other)
        {
            Effect = other.Effect.Clone<CharState>();
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new MiscEvent(this); }

        /// <summary>
        /// Applies the character state to the target character.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character to apply the state to.</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            character.CharStates.Set(Effect.Clone<CharState>());
        }
    }



    /// <summary>
    /// Refresh event that prevents team member switching on the map.
    /// </summary>
    [Serializable]
    public class MapNoSwitchEvent : RefreshEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new MapNoSwitchEvent(); }

        /// <summary>
        /// Applies the no-switch restriction to the current map.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character that owns the event (not used).</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            ZoneManager.Instance.CurrentMap.NoSwitching = true;
        }
    }

    /// <summary>
    /// Refresh event that prevents rescue team from being called on the map.
    /// </summary>
    [Serializable]
    public class MapNoRescueEvent : RefreshEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new MapNoRescueEvent(); }

        /// <summary>
        /// Applies the no-rescue restriction to the current map.
        /// </summary>
        /// <param name="owner">The event owner (not used).</param>
        /// <param name="ownerChar">The character that owns the event (not used).</param>
        /// <param name="character">The character that owns the event (not used).</param>
        public override void Apply(GameEventOwner owner, Character ownerChar, Character character)
        {
            ZoneManager.Instance.CurrentMap.NoRescue = true;
        }
    }
}

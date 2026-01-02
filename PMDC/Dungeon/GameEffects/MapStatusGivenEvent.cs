using System;
using System.Collections.Generic;
using RogueEssence.Data;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Dev;
using RogueEssence.LevelGen;
using RogueElements;
using Newtonsoft.Json;

namespace PMDC.Dungeon
{
    /// <summary>
    /// Map status given event that wraps a SingleCharEvent to execute in a map status context.
    /// Allows single character events to be triggered by map status changes.
    /// </summary>
    [Serializable]
    public class MapStatusCharEvent : MapStatusGivenEvent
    {
        /// <summary>
        /// The single character event to execute when the map status changes.
        /// </summary>
        public SingleCharEvent BaseEvent;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MapStatusCharEvent() { }

        /// <summary>
        /// Initializes a new instance with the specified base event.
        /// </summary>
        /// <param name="effect">The single character event to wrap.</param>
        public MapStatusCharEvent(SingleCharEvent effect)
        {
            BaseEvent = effect;
        }

        /// <summary>
        /// Copy constructor for cloning an existing MapStatusCharEvent.
        /// </summary>
        protected MapStatusCharEvent(MapStatusCharEvent other)
        {
            BaseEvent = (SingleCharEvent)other.BaseEvent.Clone();
        }

        /// <inheritdoc/>
        public override GameEvent Clone() => new MapStatusCharEvent(this);

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, Character character, MapStatus status, bool msg)
        {
            if (status != owner || character == null)
                yield break;

            SingleCharContext singleContext = new SingleCharContext(character);
            yield return CoroutineManager.Instance.StartCoroutine(BaseEvent.Apply(owner, ownerChar, singleContext));
        }
    }



    /// <summary>
    /// Map status event that changes a character's form based on active weather conditions.
    /// Used for form-changing abilities like Castform that respond to weather.
    /// </summary>
    [Serializable]
    public class WeatherFormeChangeEvent : MapStatusGivenEvent
    {
        /// <summary>
        /// The species ID that this event applies to.
        /// </summary>
        [JsonConverter(typeof(MonsterConverter))]
        [DataType(0, DataManager.DataType.Monster, false)]
        public string ReqSpecies;

        /// <summary>
        /// The default form to use when no matching weather is active.
        /// </summary>
        public int DefaultForme;

        /// <summary>
        /// Maps weather/map status IDs to the form indices they trigger.
        /// </summary>
        [JsonConverter(typeof(MapStatusIntDictConverter))]
        public Dictionary<string, int> WeatherPair;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public WeatherFormeChangeEvent() { WeatherPair = new Dictionary<string, int>(); ReqSpecies = ""; }

        /// <summary>
        /// Initializes a new instance with the specified parameters.
        /// </summary>
        /// <param name="reqSpecies">The species this event affects.</param>
        /// <param name="defaultForme">The default form index.</param>
        /// <param name="weather">The weather-to-form mapping.</param>
        public WeatherFormeChangeEvent(string reqSpecies, int defaultForme, Dictionary<string, int> weather)
        {
            ReqSpecies = reqSpecies;
            DefaultForme = defaultForme;
            WeatherPair = weather;
        }

        /// <summary>
        /// Copy constructor for cloning an existing WeatherFormeChangeEvent.
        /// </summary>
        protected WeatherFormeChangeEvent(WeatherFormeChangeEvent other) : this()
        {
            ReqSpecies = other.ReqSpecies;
            DefaultForme = other.DefaultForme;

            foreach (string weather in other.WeatherPair.Keys)
                WeatherPair.Add(weather, other.WeatherPair[weather]);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() => new WeatherFormeChangeEvent(this);

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, Character character, MapStatus status, bool msg)
        {
            if (character == null)
                yield break;

            if (character.CurrentForm.Species != ReqSpecies)
                yield break;
            
            //get the forme it should be in
            int forme = DefaultForme;

            foreach (string weather in WeatherPair.Keys)
            {
                if (ZoneManager.Instance.CurrentMap.Status.ContainsKey(weather))
                {
                    forme = WeatherPair[weather];
                    break;
                }
            }

            if (forme < 0)
                yield break;

            if (forme != character.CurrentForm.Form)
            {
                //transform it
                character.Transform(new MonsterID(character.CurrentForm.Species, forme, character.CurrentForm.Skin, character.CurrentForm.Gender));
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_FORM_CHANGE").ToLocal(), character.GetDisplayName(false)));
            }

            yield break;
        }
    }


    /// <summary>
    /// Map status event that removes other map statuses in the same group when applied.
    /// Used for mutually exclusive effects like weather conditions.
    /// </summary>
    [Serializable]
    public class ReplaceStatusGroupEvent : MapStatusGivenEvent
    {
        /// <summary>
        /// List of MapStatusState types that define the status group to replace.
        /// </summary>
        [StringTypeConstraint(1, typeof(MapStatusState))]
        public List<FlagType> States;

        /// <summary>
        /// Whether to display a message when removing other statuses.
        /// </summary>
        public bool Msg;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public ReplaceStatusGroupEvent() { States = new List<FlagType>(); }

        /// <summary>
        /// Initializes a new instance with the specified state type.
        /// </summary>
        /// <param name="state">The state type that defines the group.</param>
        public ReplaceStatusGroupEvent(Type state) : this() { States.Add(new FlagType(state)); }

        /// <summary>
        /// Initializes a new instance with the specified state type and message setting.
        /// </summary>
        /// <param name="state">The state type that defines the group.</param>
        /// <param name="msg">Whether to show messages when replacing.</param>
        public ReplaceStatusGroupEvent(Type state, bool msg) : this() { States.Add(new FlagType(state)); Msg = msg; }

        /// <summary>
        /// Copy constructor for cloning an existing ReplaceStatusGroupEvent.
        /// </summary>
        protected ReplaceStatusGroupEvent(ReplaceStatusGroupEvent other) : this()
        {
            States.AddRange(other.States);
            Msg = other.Msg;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() => new ReplaceStatusGroupEvent(this);

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, Character character, MapStatus status, bool msg)
        {
            //the owner must not be the newly added status
            if (status.ID != owner.GetID() || character != null)
                yield break;

            //remove all other weather effects
            List<string> removingIDs = new List<string>();
            foreach (MapStatus removeStatus in ZoneManager.Instance.CurrentMap.Status.Values)
            {
                bool hasState = false;
                foreach (FlagType state in States)
                {
                    if (removeStatus.StatusStates.Contains(state.FullType))
                        hasState = true;
                }
                if (hasState && removeStatus.ID != owner.GetID())
                    removingIDs.Add(removeStatus.ID);
            }
            foreach (string removeID in removingIDs)
                yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.RemoveMapStatus(removeID, Msg && msg));
            yield break;
        }

    }


    /// <summary>
    /// Map status event that logs a message to the battle log when the status is applied.
    /// Used for announcing weather changes and other floor-wide effects.
    /// </summary>
    [Serializable]
    public class MapStatusBattleLogEvent : MapStatusGivenEvent
    {
        /// <summary>
        /// The message key to display in the battle log.
        /// </summary>
        public StringKey Message;

        /// <summary>
        /// Whether to add a delay after displaying the message.
        /// </summary>
        public bool Delay;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MapStatusBattleLogEvent() { }

        /// <summary>
        /// Initializes a new instance with the specified message.
        /// </summary>
        /// <param name="message">The message key to display.</param>
        public MapStatusBattleLogEvent(StringKey message) : this(message, false) { }

        /// <summary>
        /// Initializes a new instance with the specified message and delay setting.
        /// </summary>
        /// <param name="message">The message key to display.</param>
        /// <param name="delay">Whether to pause after the message.</param>
        public MapStatusBattleLogEvent(StringKey message, bool delay)
        {
            Message = message;
            Delay = delay;
        }

        /// <summary>
        /// Copy constructor for cloning an existing MapStatusBattleLogEvent.
        /// </summary>
        protected MapStatusBattleLogEvent(MapStatusBattleLogEvent other)
        {
            Message = other.Message;
            Delay = other.Delay;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() => new MapStatusBattleLogEvent(this);

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, Character character, MapStatus status, bool msg)
        {
            if (status != owner || character != null)
                yield break;

            if (msg)
            {
                DungeonScene.Instance.LogMsg(Message.ToLocal());
                if (Delay)
                    yield return new WaitForFrames(GameManager.Instance.ModifyBattleSpeed(10));
            }
        }
    }

    /// <summary>
    /// Map status event that logs a message including the move name that caused the status.
    /// Used for move-triggered map effects like Trick Room.
    /// </summary>
    [Serializable]
    public class MapStatusMoveLogEvent : MapStatusGivenEvent
    {
        /// <summary>
        /// The message key to display, with placeholder for move name.
        /// </summary>
        public StringKey Message;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MapStatusMoveLogEvent() { }

        /// <summary>
        /// Initializes a new instance with the specified message.
        /// </summary>
        /// <param name="message">The message key to display.</param>
        public MapStatusMoveLogEvent(StringKey message)
        {
            Message = message;
        }

        /// <summary>
        /// Copy constructor for cloning an existing MapStatusMoveLogEvent.
        /// </summary>
        protected MapStatusMoveLogEvent(MapStatusMoveLogEvent other)
        {
            Message = other.Message;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() => new MapStatusMoveLogEvent(this);

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, Character character, MapStatus status, bool msg)
        {
            if (status != owner || character != null)
                yield break;

            if (msg)
            {
                SkillData entry = DataManager.Instance.GetSkill(status.StatusStates.GetWithDefault<MapIDState>().ID);
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), entry.GetIconName()));
                yield return new WaitForFrames(GameManager.Instance.ModifyBattleSpeed(10));
            }
        }
    }

    /// <summary>
    /// Map status event that plays a sound effect when the status is applied.
    /// Used for audio feedback on weather and other map effects.
    /// </summary>
    [Serializable]
    public class MapStatusSoundEvent : MapStatusGivenEvent
    {
        /// <summary>
        /// The sound effect ID to play.
        /// </summary>
        [Sound(0)]
        public string Sound;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MapStatusSoundEvent() { }

        /// <summary>
        /// Initializes a new instance with the specified sound.
        /// </summary>
        /// <param name="sound">The sound effect ID to play.</param>
        public MapStatusSoundEvent(string sound)
        {
            Sound = sound;
        }

        /// <summary>
        /// Copy constructor for cloning an existing MapStatusSoundEvent.
        /// </summary>
        protected MapStatusSoundEvent(MapStatusSoundEvent other)
        {
            Sound = other.Sound;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() => new MapStatusSoundEvent(this);

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, Character character, MapStatus status, bool msg)
        {
            if (status != owner || character != null)
                yield break;

            GameManager.Instance.BattleSE(Sound);
            yield break;
        }
    }

    /// <summary>
    /// Map status event that makes the status visible only if it has a countdown timer.
    /// Used for timed effects that should show remaining turns.
    /// </summary>
    [Serializable]
    public class MapStatusVisibleIfCountdownEvent : MapStatusGivenEvent
    {
        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MapStatusVisibleIfCountdownEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() => new MapStatusVisibleIfCountdownEvent();

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, Character character, MapStatus status, bool msg)
        {
            if (status != owner || character != null)
                yield break;

            if (status.StatusStates.GetWithDefault<MapCountDownState>().Counter > -1)
                status.Hidden = false;

            yield break;
        }
    }

    /// <summary>
    /// Map status event that spawns shop security guards when triggered.
    /// Used when theft occurs to spawn Kecleon guards throughout the floor.
    /// </summary>
    [Serializable]
    public class MapStatusSpawnStartGuardsEvent : MapStatusGivenEvent
    {
        /// <summary>
        /// The status effect ID to apply to spawned guards.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string GuardStatus;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MapStatusSpawnStartGuardsEvent() { GuardStatus = ""; }

        /// <summary>
        /// Initializes a new instance with the specified guard status.
        /// </summary>
        /// <param name="guardStatus">The status effect ID for guards.</param>
        public MapStatusSpawnStartGuardsEvent(string guardStatus) { GuardStatus = guardStatus; }

        /// <summary>
        /// Copy constructor for cloning an existing MapStatusSpawnStartGuardsEvent.
        /// </summary>
        public MapStatusSpawnStartGuardsEvent(MapStatusSpawnStartGuardsEvent other) { GuardStatus = other.GuardStatus; }

        /// <inheritdoc/>
        public override GameEvent Clone() => new MapStatusSpawnStartGuardsEvent(this);

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, Character character, MapStatus status, bool msg)
        {
            if (status != owner || character != null)
                yield break;

            ////remove existing spawns
            //ZoneManager.Instance.CurrentMap.TeamSpawns.Clear();

            ShopSecurityState securityState = status.StatusStates.Get<ShopSecurityState>();

            ////add guard spawns
            //for (int ii = 0; ii < securityState.Security.Count; ii++)
            //{
            //    SpecificTeamSpawner post_team = new SpecificTeamSpawner(securityState.Security.GetSpawn(ii).Copy());
            //    ZoneManager.Instance.CurrentMap.TeamSpawns.Add(post_team, securityState.Security.GetSpawnRate(ii));
            //}

            //remove the end of turn respawn
            foreach (SingleCharEvent priority in ZoneManager.Instance.CurrentMap.MapEffect.OnMapTurnEnds.EnumerateInOrder())
            {
                RespawnBaseEvent respawn = priority as RespawnBaseEvent;
                if (respawn != null)
                {
                    respawn.MaxFoes = 0;
                    respawn.RespawnTime = 0;
                }
            }

            //spawn 10 times
            List<Loc> randLocs = ZoneManager.Instance.CurrentMap.GetFreeToSpawnTiles();
            for (int ii = 0; ii < 10; ii++)
            {
                if (randLocs.Count == 0)
                    break;

                int randIndex = DataManager.Instance.Save.Rand.Next(randLocs.Count);
                Loc dest = randLocs[randIndex];
                MobSpawn spawn = securityState.Security.Pick(DataManager.Instance.Save.Rand);
                yield return CoroutineManager.Instance.StartCoroutine(PeriodicSpawnEntranceGuards.PlaceGuard(spawn, dest, GuardStatus));
                randLocs.RemoveAt(randIndex);
            }

            List<Loc> exitLocs = WarpToEndEvent.FindExits();
            //spawn once specifically on the stairs
            foreach(Loc exitLoc in exitLocs)
            {
                Loc? dest = ZoneManager.Instance.CurrentMap.GetClosestTileForChar(null, exitLoc);
                if (!dest.HasValue)
                    continue;

                MobSpawn spawn = securityState.Security.Pick(DataManager.Instance.Save.Rand);
                yield return CoroutineManager.Instance.StartCoroutine(PeriodicSpawnEntranceGuards.PlaceGuard(spawn, dest.Value, GuardStatus));
            }
        }
    }


    /// <summary>
    /// Map status event that changes the background music when the status is applied.
    /// Used for dramatic music changes during boss battles or special events.
    /// </summary>
    [Serializable]
    public class MapStatusBGMEvent : MapStatusGivenEvent
    {
        /// <summary>
        /// The background music track ID to play.
        /// </summary>
        [Music(0)]
        public string BGM;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MapStatusBGMEvent() { }

        /// <summary>
        /// Initializes a new instance with the specified BGM.
        /// </summary>
        /// <param name="bgm">The music track ID to play.</param>
        public MapStatusBGMEvent(string bgm)
        {
            BGM = bgm;
        }

        /// <summary>
        /// Copy constructor for cloning an existing MapStatusBGMEvent.
        /// </summary>
        protected MapStatusBGMEvent(MapStatusBGMEvent other)
        {
            BGM = other.BGM;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() => new MapStatusBGMEvent(this);

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, Character character, MapStatus status, bool msg)
        {
            if (status != owner || character != null)
                yield break;

            GameManager.Instance.BGM(BGM, true);
            yield break;
        }
    }

    /// <summary>
    /// Map status event that combines check events from multiple status applications.
    /// Used when map statuses need to accumulate additional check conditions.
    /// </summary>
    [Serializable]
    public class MapStatusCombineCheckEvent : MapStatusGivenEvent
    {
        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MapStatusCombineCheckEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() => new MapStatusCombineCheckEvent();

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, Character character, MapStatus status, bool msg)
        {
            if (character != null)
                yield break;

            MapCheckState destChecks = ((MapStatus)owner).StatusStates.GetWithDefault<MapCheckState>();
            MapCheckState srcChecks = status.StatusStates.GetWithDefault<MapCheckState>();
            foreach (SingleCharEvent effect in srcChecks.CheckEvents)
                destChecks.CheckEvents.Add(effect);
        }
    }


    /// <summary>
    /// Map status event that refreshes the countdown timer when the same status is reapplied.
    /// Extends the duration if the new countdown is longer than the current one.
    /// </summary>
    [Serializable]
    public class MapStatusRefreshEvent : MapStatusGivenEvent
    {
        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MapStatusRefreshEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new MapStatusRefreshEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, Character character, MapStatus status, bool msg)
        {
            if (character != null)
                yield break;

            if (((MapStatus)owner).StatusStates.GetWithDefault<MapCountDownState>().Counter > -1 &&
                ((MapStatus)owner).StatusStates.GetWithDefault<MapCountDownState>().Counter < status.StatusStates.GetWithDefault<MapCountDownState>().Counter)
                ((MapStatus)owner).StatusStates.GetWithDefault<MapCountDownState>().Counter = status.StatusStates.GetWithDefault<MapCountDownState>().Counter;
            yield break;
        }
    }

    /// <summary>
    /// Map status event that toggles a map status off when reapplied.
    /// Used for effects that can be turned on and off by using the same move.
    /// </summary>
    [Serializable]
    public class MapStatusToggleEvent : MapStatusGivenEvent
    {
        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MapStatusToggleEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new MapStatusToggleEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, Character character, MapStatus status, bool msg)
        {
            if (character != null)
                yield break;

            yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.RemoveMapStatus(((MapStatus)owner).ID));
            yield break;
        }
    }

    /// <summary>
    /// Map status event that replaces the existing status with the new one.
    /// Used for statuses that should completely reset when reapplied.
    /// </summary>
    [Serializable]
    public class MapStatusReplaceEvent : MapStatusGivenEvent
    {
        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MapStatusReplaceEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new MapStatusReplaceEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, Character character, MapStatus status, bool msg)
        {
            if (character != null)
                yield break;

            yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.RemoveMapStatus(status.ID));
            ZoneManager.Instance.CurrentMap.Status.Add(status.ID, status);
            status.StartEmitter(DungeonScene.Instance.Anims);
            yield break;
        }
    }

    /// <summary>
    /// Map status event that ignores all status applications.
    /// Used for statuses that should not be reapplied or stacked.
    /// </summary>
    [Serializable]
    public class MapStatusIgnoreEvent : MapStatusGivenEvent
    {
        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MapStatusIgnoreEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new MapStatusIgnoreEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, Character character, MapStatus status, bool msg)
        {
            yield break;
        }
    }




    /// <summary>
    /// Abstract base class for sharing equipped item map status effects to characters.
    /// Used for abilities that share held item benefits for map status reactions.
    /// </summary>
    [Serializable]
    public abstract class ShareEquipMapStatusEvent : MapStatusGivenEvent
    {
        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, Character character, MapStatus status, bool msg)
        {
            if (!String.IsNullOrEmpty(ownerChar.EquippedItem.ID))
            {
                ItemData entry = (ItemData)ownerChar.EquippedItem.GetData();
                if (CheckEquipPassValidityEvent.CanItemEffectBePassed(entry))
                {
                    foreach (var effect in GetEvents(entry))
                        yield return CoroutineManager.Instance.StartCoroutine(effect.Value.Apply(owner, ownerChar, character, status, msg));
                }
            }
            yield break;
        }

        /// <summary>
        /// Gets the map status event list from the item data to apply.
        /// </summary>
        /// <param name="entry">The item data to retrieve events from.</param>
        /// <returns>The priority list of map status events.</returns>
        protected abstract PriorityList<MapStatusGivenEvent> GetEvents(ItemData entry);
    }

    /// <summary>
    /// Shares the equipped item's OnMapStatusAdds events with the character.
    /// Triggered when map statuses are added to the floor.
    /// </summary>
    [Serializable]
    public class ShareOnMapStatusAddsEvent : ShareEquipMapStatusEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new ShareOnMapStatusAddsEvent(); }

        /// <inheritdoc/>
        protected override PriorityList<MapStatusGivenEvent> GetEvents(ItemData entry) => entry.OnMapStatusAdds;
    }

    /// <summary>
    /// Shares the equipped item's OnMapStatusRemoves events with the character.
    /// Triggered when map statuses are removed from the floor.
    /// </summary>
    [Serializable]
    public class ShareOnMapStatusRemovesEvent : ShareEquipMapStatusEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new ShareOnMapStatusRemovesEvent(); }

        /// <inheritdoc/>
        protected override PriorityList<MapStatusGivenEvent> GetEvents(ItemData entry) => entry.OnMapStatusRemoves;
    }
}
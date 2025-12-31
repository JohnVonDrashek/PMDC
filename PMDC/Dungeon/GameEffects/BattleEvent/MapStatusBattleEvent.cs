using System;
using System.Collections.Generic;
using RogueEssence.Data;
using RogueEssence.Menu;
using RogueElements;
using RogueEssence.Content;
using RogueEssence.LevelGen;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Dev;
using PMDC.Dev;
using PMDC.Data;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using NLua;
using RogueEssence.Script;
using System.Linq;

namespace PMDC.Dungeon
{
    /// <summary>
    /// Event that sets the specified map status.
    /// </summary>
    [Serializable]
    public class GiveMapStatusEvent : BattleEvent
    {
        /// <summary>
        /// The map status to add.
        /// </summary>
        [JsonConverter(typeof(MapStatusConverter))]
        [DataType(0, DataManager.DataType.MapStatus, false)]
        public string StatusID;

        /// <summary>
        /// The amount of turns the map status will last.
        /// </summary>
        public int Counter;

        /// <summary>
        /// The message displayed in the dungeon log when the map status is added.
        /// </summary>
        [StringKey(0, true)]
        public StringKey MsgOverride;

        /// <summary>
        /// If the user contains one of the specified CharStates, then the weather is extended by the multiplier.
        /// </summary>
        [StringTypeConstraint(1, typeof(CharState))]
        public List<FlagType> States;

        /// <summary>
        /// Initializes a new instance of the <see cref="GiveMapStatusEvent"/> class.
        /// </summary>
        public GiveMapStatusEvent() { States = new List<FlagType>(); StatusID = ""; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GiveMapStatusEvent"/> class with specified status ID.
        /// </summary>
        /// <param name="id">The map status ID to apply.</param>
        public GiveMapStatusEvent(string id)
        {
            States = new List<FlagType>();
            StatusID = id;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GiveMapStatusEvent"/> class with status ID and duration.
        /// </summary>
        /// <param name="id">The map status ID to apply.</param>
        /// <param name="counter">The duration in turns.</param>
        public GiveMapStatusEvent(string id, int counter)
        {
            States = new List<FlagType>();
            StatusID = id;
            Counter = counter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GiveMapStatusEvent"/> class with status ID, duration, and message.
        /// </summary>
        /// <param name="id">The map status ID to apply.</param>
        /// <param name="counter">The duration in turns.</param>
        /// <param name="msg">The message override to display.</param>
        public GiveMapStatusEvent(string id, int counter, StringKey msg)
        {
            States = new List<FlagType>();
            StatusID = id;
            Counter = counter;
            MsgOverride = msg;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GiveMapStatusEvent"/> class with extension state.
        /// </summary>
        /// <param name="id">The map status ID to apply.</param>
        /// <param name="counter">The duration in turns.</param>
        /// <param name="msg">The message override to display.</param>
        /// <param name="state">The CharState type that extends the duration.</param>
        public GiveMapStatusEvent(string id, int counter, StringKey msg, Type state)
        {
            States = new List<FlagType>();
            StatusID = id;
            Counter = counter;
            MsgOverride = msg;
            States.Add(new FlagType(state));
        }
        /// <summary>
        /// Copy constructor.
        /// </summary>
        protected GiveMapStatusEvent(GiveMapStatusEvent other)
            : this()
        {
            StatusID = other.StatusID;
            Counter = other.Counter;
            MsgOverride = other.MsgOverride;
            States.AddRange(other.States);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new GiveMapStatusEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            //add the map status
            MapStatus status = new MapStatus(StatusID);
            status.LoadFromData();
            if (Counter != 0)
                status.StatusStates.GetWithDefault<MapCountDownState>().Counter = Counter;

            bool hasState = false;
            foreach (FlagType state in States)
            {
                if (context.User.CharStates.Contains(state.FullType))
                    hasState = true;
            }
            if (hasState)
                status.StatusStates.GetWithDefault<MapCountDownState>().Counter = status.StatusStates.GetWithDefault<MapCountDownState>().Counter * 5;

            if (!MsgOverride.IsValid())
                yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.AddMapStatus(status));
            else
            {
                //message only if the status isn't already there
                MapStatus statusToCheck;
                if (!ZoneManager.Instance.CurrentMap.Status.TryGetValue(status.ID, out statusToCheck))
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(MsgOverride.ToLocal(), ownerChar.GetDisplayName(false), owner.GetDisplayName()));
                yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.AddMapStatus(status, false));
            }
        }
    }


    /// <summary>
    /// Event that removes all the map statuses with the MapWeatherState.
    /// </summary>
    [Serializable]
    public class RemoveWeatherEvent : BattleEvent
    {
        /// <inheritdoc/>
        public RemoveWeatherEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new RemoveWeatherEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            //remove all other weather effects
            List<string> removingIDs = new List<string>();
            foreach (MapStatus removeStatus in ZoneManager.Instance.CurrentMap.Status.Values)
            {
                if (removeStatus.StatusStates.Contains<MapWeatherState>())
                    removingIDs.Add(removeStatus.ID);
            }
            foreach (string removeID in removingIDs)
                yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.RemoveMapStatus(removeID));
        }
    }

    /// <summary>
    /// Event that sets the map status depending on the user's type.
    /// </summary>
    [Serializable]
    public class TypeWeatherEvent : BattleEvent
    {
        /// <summary>
        /// The element that maps to a map status.
        /// </summary>
        [JsonConverter(typeof(ElementMapStatusDictConverter))]
        [DataType(1, DataManager.DataType.Element, false)]
        [DataType(2, DataManager.DataType.MapStatus, false)]
        public Dictionary<string, string> WeatherPair;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeWeatherEvent"/> class.
        /// </summary>
        public TypeWeatherEvent() { WeatherPair = new Dictionary<string, string>(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeWeatherEvent"/> class with specified mappings.
        /// </summary>
        /// <param name="weather">Dictionary mapping element types to map status IDs.</param>
        public TypeWeatherEvent(Dictionary<string, string> weather)
        {
            WeatherPair = weather;
        }
        /// <summary>
        /// Copy constructor.
        /// </summary>
        protected TypeWeatherEvent(TypeWeatherEvent other)
            : this()
        {
            foreach (string element in other.WeatherPair.Keys)
                WeatherPair.Add(element, other.WeatherPair[element]);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new TypeWeatherEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            string weather;
            if (WeatherPair.TryGetValue(context.User.Element1, out weather))
            {
                //add the map status
                MapStatus status = new MapStatus(weather);
                status.LoadFromData();
                status.StatusStates.GetWithDefault<MapCountDownState>().Counter = -1;
                ElementData elementData = DataManager.Instance.GetElement(context.User.Element1);
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_ELEMENT_WEATHER").ToLocal(), context.User.GetDisplayName(false), elementData.GetIconName(), ((MapStatusData)status.GetData()).GetColoredName()));
                yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.AddMapStatus(status));
            }
            else if (WeatherPair.TryGetValue(context.User.Element2, out weather))
            {
                //add the map status
                MapStatus status = new MapStatus(weather);
                status.LoadFromData();
                status.StatusStates.GetWithDefault<MapCountDownState>().Counter = -1;
                ElementData elementData = DataManager.Instance.GetElement(context.User.Element2);
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_ELEMENT_WEATHER").ToLocal(), context.User.GetDisplayName(false), elementData.GetIconName(), ((MapStatusData)status.GetData()).GetColoredName()));
                yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.AddMapStatus(status));
            }
            else//clear weather
            {
                //add the map status
                MapStatus status = new MapStatus(DataManager.Instance.DefaultMapStatus);
                status.LoadFromData();
                status.StatusStates.GetWithDefault<MapCountDownState>().Counter = -1;
                yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.AddMapStatus(status));
            }
        }
    }

    /// <summary>
    /// Event that bans the last move the character used by setting the move ID in the MapIDState.
    /// </summary>
    [Serializable]
    public class BanMoveEvent : BattleEvent
    {
        /// <summary>
        /// The status that will store the move ID in MapIDState.
        /// This should usually be "move_ban".
        /// </summary>
        [JsonConverter(typeof(MapStatusConverter))]
        [DataType(0, DataManager.DataType.MapStatus, false)]
        public string BanStatusID;

        /// <summary>
        /// The status that contains the last used move in IDState status state.
        /// This should usually be "last_used_move".
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string LastMoveStatusID;

        /// <summary>
        /// Initializes a new instance of the <see cref="BanMoveEvent"/> class.
        /// </summary>
        public BanMoveEvent() { BanStatusID = ""; LastMoveStatusID = ""; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BanMoveEvent"/> class with specified status IDs.
        /// </summary>
        /// <param name="banStatusID">The map status ID for storing banned moves.</param>
        /// <param name="prevMoveID">The status ID that tracks the last used move.</param>
        public BanMoveEvent(string banStatusID, string prevMoveID)
        {
            BanStatusID = banStatusID;
            LastMoveStatusID = prevMoveID;
        }
        /// <summary>
        /// Copy constructor.
        /// </summary>
        protected BanMoveEvent(BanMoveEvent other)
        {
            BanStatusID = other.BanStatusID;
            LastMoveStatusID = other.LastMoveStatusID;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new BanMoveEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            StatusEffect testStatus = context.Target.GetStatusEffect(LastMoveStatusID);
            if (testStatus != null)
            {
                //add disable move based on the last move used
                string lockedMove = testStatus.StatusStates.GetWithDefault<IDState>().ID;
                //add the map status
                MapStatus status = new MapStatus(BanStatusID);
                status.LoadFromData();
                status.StatusStates.GetWithDefault<MapIDState>().ID = lockedMove;
                yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.AddMapStatus(status));
            }
            else
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_BAN_FAIL").ToLocal(), context.Target.GetDisplayName(false)));
        }
    }


}


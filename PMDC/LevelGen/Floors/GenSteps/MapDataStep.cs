using System;
using RogueEssence.Dungeon;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Data;
using RogueEssence.LevelGen;
using System.Text;
using RogueEssence.Dev;
using PMDC.Dungeon;
using Newtonsoft.Json;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Sets various attributes about the map including the music, time limit, and darkness of the floor.
    /// This step configures the fundamental gameplay settings for the generated map.
    /// </summary>
    /// <typeparam name="T">The map generation context type.</typeparam>
    [Serializable]
    public class MapDataStep<T> : GenStep<T> where T : BaseMapGenContext
    {
        /// <summary>
        /// The default map music.
        /// </summary>
        [Music(0)]
        public string Music;

        /// <summary>
        /// How many turns the player can spend on the map before an instant game over.
        /// </summary>
        public int TimeLimit;

        /// <summary>
        /// The darkness level for map exploration.
        /// </summary>
        public Map.SightRange TileSight;

        /// <summary>
        /// The darkness level for character viewing.
        /// </summary>
        public Map.SightRange CharSight;

        /// <summary>
        /// Clamps the map edges so that the camera does not scroll past them. Does not work on wrapped-around maps.
        /// </summary>
        public bool ClampCamera;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MapDataStep()
        {
            Music = "";
        }

        /// <summary>
        /// Initializes a new instance with the specified map settings.
        /// </summary>
        /// <param name="music">The background music track for the map.</param>
        /// <param name="timeLimit">The turn limit before game over, or 0 for no limit.</param>
        /// <param name="tileSight">The sight range for tile visibility.</param>
        /// <param name="charSight">The sight range for character visibility.</param>
        public MapDataStep(string music, int timeLimit, Map.SightRange tileSight, Map.SightRange charSight)
        {
            Music = music;
            TimeLimit = timeLimit;
            TileSight = tileSight;
            CharSight = charSight;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Applies the map data settings including music, time limit, sight ranges, and camera clamping.
        /// If TimeLimit is greater than 0, adds a countdown map status.
        /// </remarks>
        public override void Apply(T map)
        {
            map.Map.Music = Music;

            if (TimeLimit > 0)
            {
                MapStatus timeStatus = new MapStatus("somethings_stirring");
                timeStatus.LoadFromData();
                MapCountDownState timeState = timeStatus.StatusStates.GetWithDefault<MapCountDownState>();
                timeState.Counter = TimeLimit;
                map.Map.Status.Add("somethings_stirring", timeStatus);
            }


            map.Map.TileSight = TileSight;
            map.Map.CharSight = CharSight;

            if (map.Map.EdgeView != BaseMap.ScrollEdge.Wrap)
                map.Map.EdgeView = ClampCamera ? BaseMap.ScrollEdge.Clamp : BaseMap.ScrollEdge.Blank;
        }

        /// <summary>
        /// Returns a formatted string representation of the step and its settings.
        /// </summary>
        /// <returns>A string containing the class name, time limit, music, and sight range settings.</returns>
        public override string ToString()
        {
            return String.Format("{0}: Time:{1} Song:{2} TileSight:{3} CharSight:{4}", this.GetType().GetFormattedTypeName(), TimeLimit, Music, TileSight, CharSight);
        }
    }

    /// <summary>
    /// Makes the map name show up before fading in.
    /// This step adds a title drop effect when the player enters the floor.
    /// </summary>
    /// <typeparam name="T">The map generation context type.</typeparam>
    [Serializable]
    public class MapTitleDropStep<T> : GenStep<T> where T : BaseMapGenContext
    {
        /// <summary>
        /// The priority at which the title drop effect occurs during map start.
        /// </summary>
        public Priority DropPriority;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MapTitleDropStep()
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified drop priority.
        /// </summary>
        /// <param name="dropPriority">The priority at which the title drop occurs.</param>
        public MapTitleDropStep(Priority dropPriority)
        {
            DropPriority = dropPriority;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Adds a fade title event to the map that displays the floor name when the map starts.
        /// </remarks>
        public override void Apply(T map)
        {
            map.Map.MapEffect.OnMapStarts.Add(DropPriority, new FadeTitleEvent());
        }

        /// <summary>
        /// Returns a formatted string representation of the step and its priority.
        /// </summary>
        /// <returns>A string containing the class name and the drop priority.</returns>
        public override string ToString()
        {
            return String.Format("{0}: At:{1}", this.GetType().GetFormattedTypeName(), DropPriority);
        }
    }

    /// <summary>
    /// Sets only the time limit for the map without changing other map settings.
    /// Use this step when you only need to configure the turn limit for a floor.
    /// </summary>
    /// <typeparam name="T">The map generation context type.</typeparam>
    [Serializable]
    public class MapTimeLimitStep<T> : GenStep<T> where T : BaseMapGenContext
    {
        /// <summary>
        /// How many turns the player can spend on the map before an instant game over.
        /// </summary>
        public int TimeLimit;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MapTimeLimitStep()
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified time limit.
        /// </summary>
        /// <param name="timeLimit">The number of turns before game over.</param>
        public MapTimeLimitStep(int timeLimit)
        {
            TimeLimit = timeLimit;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Applies the time limit to the map by creating and configuring a countdown map status.
        /// Only creates the status if TimeLimit is greater than 0.
        /// </remarks>
        public override void Apply(T map)
        {
            if (TimeLimit > 0)
            {
                MapStatus timeStatus = new MapStatus("somethings_stirring");
                timeStatus.LoadFromData();
                MapCountDownState timeState = timeStatus.StatusStates.GetWithDefault<MapCountDownState>();
                timeState.Counter = TimeLimit;
                map.Map.Status.Add("somethings_stirring", timeStatus);
            }
        }

        /// <summary>
        /// Returns a formatted string representation of the step and its time limit.
        /// </summary>
        /// <returns>A string containing the class name and the time limit value.</returns>
        public override string ToString()
        {
            return String.Format("{0}: Time:{1}", this.GetType().GetFormattedTypeName(), TimeLimit);
        }
    }

    /// <summary>
    /// Adds a map status that is considered the "default" for that map.
    /// The map will always revert back to this status even if replaced, waiting for the replacing status to expire.
    /// </summary>
    /// <typeparam name="T">The map generation context type.</typeparam>
    [Serializable]
    public class DefaultMapStatusStep<T> : GenStep<T> where T : BaseMapGenContext
    {
        /// <summary>
        /// The map status used to set the default map status.
        /// </summary>
        [JsonConverter(typeof(MapStatusConverter))]
        [DataType(0, DataManager.DataType.MapStatus, false)]
        public string SetterID;

        /// <summary>
        /// The possible default map statuses to randomly choose from.
        /// </summary>
        [JsonConverter(typeof(MapStatusArrayConverter))]
        [DataType(1, DataManager.DataType.MapStatus, false)]
        public string[] DefaultMapStatus;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public DefaultMapStatusStep()
        {

        }

        /// <summary>
        /// Initializes a new instance with the specified status setter and default statuses.
        /// </summary>
        /// <param name="statusSetter">The status ID used to set the default status.</param>
        /// <param name="defaultStatus">One or more possible default statuses to choose from.</param>
        public DefaultMapStatusStep(string statusSetter, params string[] defaultStatus)
        {
            SetterID = statusSetter;
            DefaultMapStatus = defaultStatus;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Randomly selects a default status from the available options and applies it to the map
        /// using the specified status setter. The chosen status is stored in the setter's ID state.
        /// </remarks>
        public override void Apply(T map)
        {
            string chosenStatus = DefaultMapStatus[map.Rand.Next(DefaultMapStatus.Length)];
            MapStatus statusSetter = new MapStatus(SetterID);
            statusSetter.LoadFromData();
            MapIDState indexState = statusSetter.StatusStates.GetWithDefault<MapIDState>();
            indexState.ID = chosenStatus;
            map.Map.Status.Add(SetterID, statusSetter);
        }

        /// <summary>
        /// Returns a formatted string representation of the step and its default statuses.
        /// </summary>
        /// <returns>
        /// A string containing the class name and the default status name if there's only one,
        /// or the class name and the count of statuses if there are multiple.
        /// </returns>
        public override string ToString()
        {
            if (DefaultMapStatus.Length == 1)
                return String.Format("{0}: {1}", this.GetType().GetFormattedTypeName(), DataManager.Instance.DataIndices[DataManager.DataType.MapStatus].Get(DefaultMapStatus[0]).Name.ToLocal());
            return String.Format("{0}[{1}]", this.GetType().GetFormattedTypeName(), DefaultMapStatus.Length);
        }
    }


    /// <summary>
    /// Adds a map status to the map with the specified MapStatusStates.
    /// Use this step to apply custom map status effects with specific state configurations.
    /// </summary>
    /// <typeparam name="T">The map generation context type.</typeparam>
    [Serializable]
    public class StateMapStatusStep<T> : GenStep<T> where T : BaseMapGenContext
    {
        /// <summary>
        /// The map status to apply.
        /// </summary>
        [JsonConverter(typeof(MapStatusConverter))]
        public string MapStatus;

        /// <summary>
        /// The collection of status states to configure on the map status.
        /// </summary>
        public StateCollection<MapStatusState> States;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public StateMapStatusStep()
        {
            States = new StateCollection<MapStatusState>();
        }

        /// <summary>
        /// Initializes a new instance with the specified map status and state.
        /// </summary>
        /// <param name="mapStatus">The map status ID to apply.</param>
        /// <param name="state">The status state to set on the map status.</param>
        public StateMapStatusStep(string mapStatus, MapStatusState state) : this()
        {
            MapStatus = mapStatus;
            States.Set(state);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Creates a new map status instance, loads its default data, applies all configured status states,
        /// and adds it to the map.
        /// </remarks>
        public override void Apply(T map)
        {
            MapStatus status = new MapStatus(MapStatus);
            status.LoadFromData();
            foreach(MapStatusState state in States)
                status.StatusStates.Set((MapStatusState)state.Clone());
            map.Map.Status.Add(MapStatus, status);
        }
    }

    /// <summary>
    /// Sets only the music for the map without changing other map settings.
    /// Use this step when you only need to configure the background music for a floor.
    /// </summary>
    /// <typeparam name="T">The map generation context type.</typeparam>
    [Serializable]
    public class MapMusicStep<T> : GenStep<T> where T : BaseMapGenContext
    {
        /// <summary>
        /// The default map music.
        /// </summary>
        [Music(0)]
        public string Music;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public MapMusicStep()
        {
            Music = "";
        }

        /// <summary>
        /// Initializes a new instance with the specified music track.
        /// </summary>
        /// <param name="music">The background music track for the map.</param>
        public MapMusicStep(string music)
        {
            Music = music;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Sets the background music track for the map.
        /// </remarks>
        public override void Apply(T map)
        {
            map.Map.Music = Music;
        }

        /// <summary>
        /// Returns a formatted string representation of the step and its music track.
        /// </summary>
        /// <returns>A string containing the class name and the music track ID.</returns>
        public override string ToString()
        {
            return String.Format("{0}: Song:{1}", this.GetType().GetFormattedTypeName(), Music);
        }
    }
}

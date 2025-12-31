using System;
using RogueEssence.LevelGen;
using System.Collections.Generic;
using RogueElements;
using RogueEssence;
using RogueEssence.Dev;
using RogueEssence.Dungeon;
using Newtonsoft.Json;

namespace PMDC.Dungeon
{
    /// <summary>
    /// Marker tile state indicating a tile is currently being triggered.
    /// Prevents recursive triggering of tile effects.
    /// </summary>
    [Serializable]
    public class TriggeringState : TileState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TriggeringState"/> class.
        /// </summary>
        public TriggeringState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new TriggeringState(); }
    }

    /// <summary>
    /// Tile state storing a destination for warp/stairs tiles.
    /// Used by tiles that transport characters to other floors or zones.
    /// </summary>
    [Serializable]
    public class DestState : TileState
    {
        /// <summary>
        /// The destination segment and floor location.
        /// </summary>
        public SegLoc Dest;

        /// <summary>
        /// Whether the destination is relative to the current position (for multi-floor jumps).
        /// </summary>
        public bool Relative;

        /// <summary>
        /// Whether to preserve the current music when transitioning to the destination.
        /// </summary>
        public bool PreserveMusic;

        /// <summary>
        /// Initializes a new instance of the <see cref="DestState"/> class.
        /// </summary>
        public DestState() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DestState"/> class with a destination.
        /// </summary>
        /// <param name="dest">The destination segment and floor location.</param>
        public DestState(SegLoc dest) { Dest = dest; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DestState"/> class with a destination and relative flag.
        /// </summary>
        /// <param name="dest">The destination segment and floor location.</param>
        /// <param name="relative">Whether the destination is relative to the current position.</param>
        public DestState(SegLoc dest, bool relative) { Dest = dest; Relative = relative; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DestState"/> class by copying another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected DestState(DestState other) { Dest = other.Dest; Relative = other.Relative; PreserveMusic = other.PreserveMusic; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new DestState(this); }
    }

    /// <summary>
    /// Tile state indicating whether a tile represents a dangerous area.
    /// Used for UI warnings and pathfinding decisions.
    /// </summary>
    [Serializable]
    public class DangerState : TileState
    {
        /// <summary>
        /// Whether this tile is considered dangerous.
        /// </summary>
        public bool Danger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DangerState"/> class.
        /// </summary>
        public DangerState() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DangerState"/> class with a danger level.
        /// </summary>
        /// <param name="danger">Whether the tile is dangerous.</param>
        public DangerState(bool danger) { Danger = danger; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DangerState"/> class by copying another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected DangerState(DangerState other) { Danger = other.Danger; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new DangerState(this); }
    }

    /// <summary>
    /// Tile state that stores a music track to play when the tile is activated.
    /// Used for tiles that change the background music.
    /// </summary>
    [Serializable]
    public class SongState : TileState
    {
        /// <summary>
        /// The music track identifier to play.
        /// </summary>
        [Music(0)]
        public string Song;

        /// <summary>
        /// Initializes a new instance of the <see cref="SongState"/> class.
        /// </summary>
        public SongState() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SongState"/> class with a music track.
        /// </summary>
        /// <param name="song">The music track identifier to play.</param>
        public SongState(string song) { Song = song; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SongState"/> class by copying another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected SongState(SongState other) { Song = other.Song; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new SongState(this); }
    }

    /// <summary>
    /// Tile state for locked tiles that require a specific key item to unlock.
    /// Used by locked doors and chests.
    /// </summary>
    [Serializable]
    public class UnlockState : TileState
    {
        /// <summary>
        /// The item ID required to unlock this tile.
        /// </summary>
        [JsonConverter(typeof(ItemConverter))]
        [DataType(0, RogueEssence.Data.DataManager.DataType.Item, false)]
        public string UnlockItem;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnlockState"/> class.
        /// </summary>
        public UnlockState() { UnlockItem = ""; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnlockState"/> class with a required unlock item.
        /// </summary>
        /// <param name="unlockItem">The item ID required to unlock this tile.</param>
        public UnlockState(string unlockItem) { UnlockItem = unlockItem; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnlockState"/> class by copying another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected UnlockState(UnlockState other) { UnlockItem = other.UnlockItem; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new UnlockState(this); }
    }

    /// <summary>
    /// Tile state for notice/sign tiles that display text when interacted with.
    /// Used by signs, plaques, and informational tiles.
    /// </summary>
    [Serializable]
    public class NoticeState : TileState
    {
        /// <summary>
        /// The title text to display.
        /// </summary>
        public LocalFormat Title;

        /// <summary>
        /// The main content text to display.
        /// </summary>
        public LocalFormat Content;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoticeState"/> class.
        /// </summary>
        public NoticeState() { Title = new LocalFormatSimple(); Content = new LocalFormatSimple(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoticeState"/> class with content text.
        /// </summary>
        /// <param name="content">The main content text to display.</param>
        public NoticeState(LocalFormat content) { Title = new LocalFormatSimple(); Content = content; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoticeState"/> class with title and content text.
        /// </summary>
        /// <param name="title">The title text to display.</param>
        /// <param name="content">The main content text to display.</param>
        public NoticeState(LocalFormat title, LocalFormat content) { Title = title; Content = content; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoticeState"/> class by copying another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected NoticeState(NoticeState other) { Title = other.Title.Clone(); Content = other.Content.Clone(); }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new NoticeState(this); }
    }

    /// <summary>
    /// Tile state that stores a list of tile locations.
    /// Used for tiles that affect or reference multiple other tiles (e.g., switch targets).
    /// </summary>
    [Serializable]
    public class TileListState : TileState
    {
        /// <summary>
        /// The list of tile locations referenced by this tile.
        /// </summary>
        public List<Loc> Tiles;

        /// <summary>
        /// Initializes a new instance of the <see cref="TileListState"/> class.
        /// </summary>
        public TileListState() { Tiles = new List<Loc>(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="TileListState"/> class by copying another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected TileListState(TileListState other)
            : this()
        {
            foreach (Loc item in other.Tiles)
                Tiles.Add(item);
        }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new TileListState(this); }
    }

    /// <summary>
    /// Tile state that stores required tile locations for activation.
    /// Used for tiles that require multiple switches or conditions to be met.
    /// </summary>
    [Serializable]
    public class TileReqListState : TileState
    {
        /// <summary>
        /// The list of tile locations that must be activated for this tile to function.
        /// </summary>
        public List<Loc> Tiles;

        /// <summary>
        /// Initializes a new instance of the <see cref="TileReqListState"/> class.
        /// </summary>
        public TileReqListState() { Tiles = new List<Loc>(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="TileReqListState"/> class by copying another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected TileReqListState(TileReqListState other)
            : this()
        {
            foreach (Loc item in other.Tiles)
                Tiles.Add(item);
        }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new TileReqListState(this); }
    }

    /// <summary>
    /// Tile state that stores items to spawn when the tile is triggered.
    /// Used by treasure chests and item-spawning tiles.
    /// </summary>
    [Serializable]
    public class ItemSpawnState : TileState
    {
        /// <summary>
        /// The list of items to spawn.
        /// </summary>
        public List<MapItem> Spawns;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemSpawnState"/> class.
        /// </summary>
        public ItemSpawnState() { Spawns = new List<MapItem>(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemSpawnState"/> class by copying another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected ItemSpawnState(ItemSpawnState other)
            : this()
        {
            foreach (MapItem item in other.Spawns)
                Spawns.Add(new MapItem(item));
        }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new ItemSpawnState(this); }
    }

    /// <summary>
    /// Tile state that stores enemy spawns to create when the tile is triggered.
    /// Used by monster houses, traps, and ambush tiles.
    /// </summary>
    [Serializable]
    public class MobSpawnState : TileState
    {
        /// <summary>
        /// The list of monster spawns to create.
        /// </summary>
        public List<MobSpawn> Spawns;

        /// <summary>
        /// Initializes a new instance of the <see cref="MobSpawnState"/> class.
        /// </summary>
        public MobSpawnState() { Spawns = new List<MobSpawn>(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="MobSpawnState"/> class by copying another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected MobSpawnState(MobSpawnState other)
            : this()
        {
            foreach (MobSpawn mob in other.Spawns)
                Spawns.Add(mob.Copy());
        }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new MobSpawnState(this); }
    }

    /// <summary>
    /// Tile state that stores events to execute when the map starts.
    /// Used for tiles with special initialization behavior.
    /// </summary>
    [Serializable]
    public class MapStartEventState : TileState
    {
        /// <summary>
        /// The prioritized list of events to execute on map start.
        /// </summary>
        public PriorityList<SingleCharEvent> OnMapStarts;

        /// <summary>
        /// Initializes a new instance of the <see cref="MapStartEventState"/> class.
        /// </summary>
        public MapStartEventState() { OnMapStarts = new PriorityList<SingleCharEvent>(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapStartEventState"/> class by copying another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected MapStartEventState(MapStartEventState other)
            : this()
        {
            foreach (Priority priority in other.OnMapStarts.GetPriorities())
            {
                foreach (SingleCharEvent step in other.OnMapStarts.GetItems(priority))
                    OnMapStarts.Add(priority, step);
            }
        }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new MapStartEventState(this); }
    }

    /// <summary>
    /// Tile state that stores events to execute as a result of tile activation.
    /// Used for tiles with custom result behavior.
    /// </summary>
    [Serializable]
    public class ResultEventState : TileState
    {
        /// <summary>
        /// The list of events to execute as results.
        /// </summary>
        public List<SingleCharEvent> ResultEvents;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultEventState"/> class.
        /// </summary>
        public ResultEventState() { ResultEvents = new List<SingleCharEvent>(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultEventState"/> class by copying another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected ResultEventState(ResultEventState other)
            : this()
        {
            foreach (SingleCharEvent mob in other.ResultEvents)
                ResultEvents.Add((SingleCharEvent)mob.Clone());
        }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new ResultEventState(this); }
    }

    /// <summary>
    /// Tile state that stores a rectangular bounds area.
    /// Used for tiles that affect or reference a specific region of the map.
    /// </summary>
    [Serializable]
    public class BoundsState : TileState
    {
        /// <summary>
        /// The rectangular bounds of the affected area.
        /// </summary>
        public Rect Bounds;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundsState"/> class.
        /// </summary>
        public BoundsState() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundsState"/> class with bounds.
        /// </summary>
        /// <param name="bounds">The rectangular bounds of the affected area.</param>
        public BoundsState(Rect bounds) { Bounds = bounds; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundsState"/> class by copying another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected BoundsState(BoundsState other) { Bounds = other.Bounds; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new BoundsState(this); }
    }

    /// <summary>
    /// Tile state that stores a Lua script to execute when the tile is triggered.
    /// Used for custom tile behavior defined in Lua.
    /// </summary>
    [Serializable]
    public class TileScriptState : TileState
    {
        /// <summary>
        /// The Lua script function name to call.
        /// </summary>
        [RogueEssence.Dev.Sanitize(0)]
        public string Script;

        /// <summary>
        /// The Lua table of arguments to pass to the script.
        /// </summary>
        public string ArgTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="TileScriptState"/> class.
        /// </summary>
        public TileScriptState() { Script = ""; ArgTable = "{}"; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TileScriptState"/> class with script and arguments.
        /// </summary>
        /// <param name="script">The Lua script function name to call.</param>
        /// <param name="argTable">The Lua table of arguments to pass to the script.</param>
        public TileScriptState(string script, string argTable) { Script = script; ArgTable = argTable; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TileScriptState"/> class by copying another instance.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected TileScriptState(TileScriptState other) { Script = other.Script; ArgTable = other.ArgTable; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new TileScriptState(this); }
    }
}

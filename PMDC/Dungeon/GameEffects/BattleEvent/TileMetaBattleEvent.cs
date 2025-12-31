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
    /// Battle events that affect tiles, including traps, terrain, and discovery.
    /// </summary>

    /// <summary>
    /// Event that sets the ground tile with the specified trap.
    /// </summary>
    [Serializable]
    public class SetTrapEvent : BattleEvent
    {
        /// <summary>
        /// The trap being added.
        /// </summary>
        [JsonConverter(typeof(TileConverter))]
        [DataType(0, DataManager.DataType.Tile, false)]
        public string TrapID;

        /// <inheritdoc/>
        public SetTrapEvent() { }

        /// <summary>
        /// Creates a new SetTrapEvent with the specified trap ID.
        /// </summary>
        /// <param name="trapID">The ID of the trap to set.</param>
        public SetTrapEvent(string trapID)
        {
            TrapID = trapID;
        }

        /// <inheritdoc/>
        protected SetTrapEvent(SetTrapEvent other)
        {
            TrapID = other.TrapID;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new SetTrapEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Tile tile = ZoneManager.Instance.CurrentMap.GetTile(context.TargetTile);
            if (tile == null)
                yield break;

            if (((TerrainData)tile.Data.GetData()).BlockType == TerrainData.Mobility.Passable && String.IsNullOrEmpty(tile.Effect.ID))
            {
                tile.Effect = new EffectTile(TrapID, true, tile.Effect.TileLoc);
                tile.Effect.Owner = ZoneManager.Instance.CurrentMap.GetTileOwner(context.User);
            }
        }
    }

    /// <summary>
    /// Event that sets the ground tile with the specified trap at the character's location.
    /// </summary>
    [Serializable]
    public class CounterTrapEvent : BattleEvent
    {
        /// <summary>
        /// The trap being added.
        /// </summary>
        [JsonConverter(typeof(TileConverter))]
        [DataType(0, DataManager.DataType.Tile, false)]
        public string TrapID;

        /// <summary>
        /// The particle VFX emitter.
        /// </summary>
        public FiniteEmitter Emitter;

        /// <summary>
        /// The sound effect of the VFX.
        /// </summary>
        [Sound(0)]
        public string Sound;

        /// <inheritdoc/>
        public CounterTrapEvent() { Emitter = new EmptyFiniteEmitter(); }

        /// <summary>
        /// Creates a new CounterTrapEvent with the specified parameters.
        /// </summary>
        /// <param name="trapID">The ID of the trap to set.</param>
        /// <param name="emitter">The particle VFX emitter.</param>
        /// <param name="sound">The sound effect to play.</param>
        public CounterTrapEvent(string trapID, FiniteEmitter emitter, string sound)
        {
            TrapID = trapID;
            Emitter = emitter;
            Sound = sound;
        }

        /// <inheritdoc/>
        protected CounterTrapEvent(CounterTrapEvent other)
        {
            TrapID = other.TrapID;
            Emitter = (FiniteEmitter)other.Emitter.Clone();
            Sound = other.Sound;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new CounterTrapEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (!Collision.InBounds(ZoneManager.Instance.CurrentMap.Width, ZoneManager.Instance.CurrentMap.Height, context.Target.CharLoc))
                yield break;

            bool dropped = false;
            Loc baseLoc = context.Target.CharLoc;
            foreach (Dir4 dir in DirExt.VALID_DIR4)
            {
                Loc endLoc = baseLoc + dir.GetLoc();
                Tile tile = ZoneManager.Instance.CurrentMap.Tiles[endLoc.X][endLoc.Y];
                if (((TerrainData)tile.Data.GetData()).BlockType == TerrainData.Mobility.Passable && String.IsNullOrEmpty(tile.Effect.ID))
                {
                    tile.Effect = new EffectTile(TrapID, true, endLoc);
                    tile.Effect.Owner = ZoneManager.Instance.CurrentMap.GetTileOwner(context.Target);

                    GameManager.Instance.BattleSE(Sound);
                    FiniteEmitter endEmitter = (FiniteEmitter)Emitter.Clone();
                    endEmitter.SetupEmit(endLoc * GraphicsManager.TileSize + new Loc(GraphicsManager.TileSize / 2), endLoc * GraphicsManager.TileSize + new Loc(GraphicsManager.TileSize / 2), context.Target.CharDir);
                    DungeonScene.Instance.CreateAnim(endEmitter, DrawLayer.NoDraw);
                    dropped = true;
                }
            }
            if (dropped)
            {
                TileData tileData = DataManager.Instance.GetTile(TrapID);
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_SPIKE_DROPPER").ToLocal(), context.Target.GetDisplayName(false), owner.GetDisplayName(), tileData.Name.ToLocal()));
            }
        }
    }

    /// <summary>
    /// Event that triggers the effects of the trap tile.
    /// </summary>
    [Serializable]
    public class TriggerTrapEvent : BattleEvent
    {
        /// <summary>
        /// The trap to ignore triggering.
        /// </summary>
        [DataType(0, DataManager.DataType.Tile, false)]
        public string ExceptID;

        /// <inheritdoc/>
        public TriggerTrapEvent() { }

        /// <summary>
        /// Creates a new TriggerTrapEvent that ignores the specified trap.
        /// </summary>
        /// <param name="exceptID">The trap ID to exclude from triggering.</param>
        public TriggerTrapEvent(string exceptID) { ExceptID = exceptID; }

        /// <inheritdoc/>
        public TriggerTrapEvent(TriggerTrapEvent other)
        {
            ExceptID = other.ExceptID;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new TriggerTrapEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Tile tile = ZoneManager.Instance.CurrentMap.GetTile(context.TargetTile);
            if (tile == null)
                yield break;

            if (!String.IsNullOrEmpty(tile.Effect.ID) && tile.Effect.ID != ExceptID)
            {
                TileData entry = DataManager.Instance.GetTile(tile.Effect.GetID());
                if (entry.StepType == TileData.TriggerType.Trap)
                {
                    SingleCharContext singleContext = new SingleCharContext(context.User);
                    yield return CoroutineManager.Instance.StartCoroutine(tile.Effect.InteractWithTile(singleContext));
                }
            }
        }
    }

    /// <summary>
    /// Event that makes the trap revealed.
    /// </summary>
    [Serializable]
    public class RevealTrapEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new RevealTrapEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Tile tile = ZoneManager.Instance.CurrentMap.GetTile(context.TargetTile);
            if (tile == null)
                yield break;

            if (!String.IsNullOrEmpty(tile.Effect.ID))
                tile.Effect.Revealed = true;
        }
    }

    /// <summary>
    /// Event that removes the trap.
    /// </summary>
    [Serializable]
    public class RemoveTrapEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new RemoveTrapEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Tile tile = ZoneManager.Instance.CurrentMap.GetTile(context.TargetTile);
            if (tile == null)
                yield break;

            if (!String.IsNullOrEmpty(tile.Effect.ID))
            {
                TileData entry = DataManager.Instance.GetTile(tile.Effect.GetID());
                if (entry.StepType == TileData.TriggerType.Trap)
                    tile.Effect = new EffectTile(tile.Effect.TileLoc);
            }
        }
    }


    /// <summary>
    /// Event that changes terrain of one type to another type.
    /// </summary>
    [Serializable]
    public class ChangeTerrainEvent : BattleEvent
    {
        /// <summary>
        /// The terrain type to change from.
        /// </summary>
        [DataType(0, DataManager.DataType.Terrain, false)]
        public string TerrainFrom;

        /// <summary>
        /// The terrain type to change to.
        /// </summary>
        [DataType(0, DataManager.DataType.Terrain, false)]
        public string TerrainTo;

        /// <inheritdoc/>
        public ChangeTerrainEvent()
        {
            TerrainFrom = "";
            TerrainTo = "";
        }

        /// <summary>
        /// Creates a new ChangeTerrainEvent with the specified terrain types.
        /// </summary>
        /// <param name="terrainFrom">The terrain type to change from.</param>
        /// <param name="terrainTo">The terrain type to change to.</param>
        public ChangeTerrainEvent(string terrainFrom, string terrainTo)
        {
            TerrainFrom = "";
            TerrainTo = "";
        }

        /// <inheritdoc/>
        protected ChangeTerrainEvent(ChangeTerrainEvent other)
        {
            TerrainFrom = other.TerrainFrom;
            TerrainTo = other.TerrainTo;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ChangeTerrainEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Tile tile = ZoneManager.Instance.CurrentMap.GetTile(context.TargetTile);
            if (tile.ID != TerrainFrom)
                yield break;

            tile.Data = new TerrainTile(TerrainTo);
            int distance = 0;
            Loc startLoc = context.TargetTile - new Loc(distance + 2);
            Loc sizeLoc = new Loc((distance + 2) * 2 + 1);
            ZoneManager.Instance.CurrentMap.MapModified(startLoc, sizeLoc);
        }
    }


    /// <summary>
    /// Abstract base class for events that remove terrain and replace it with floor tiles.
    /// </summary>
    [Serializable]
    public abstract class RemoveTerrainBaseEvent : BattleEvent
    {
        /// <summary>
        /// The sound effect played when terrain is removed.
        /// </summary>
        [Sound(0)]
        public string RemoveSound;

        /// <summary>
        /// The particle VFX emitter for terrain removal.
        /// </summary>
        public FiniteEmitter RemoveAnim;

        /// <inheritdoc/>
        public RemoveTerrainBaseEvent()
        {
            RemoveAnim = new EmptyFiniteEmitter();
        }

        /// <summary>
        /// Creates a new RemoveTerrainBaseEvent with the specified sound and animation.
        /// </summary>
        /// <param name="removeSound">The sound effect to play.</param>
        /// <param name="removeAnim">The particle VFX emitter.</param>
        public RemoveTerrainBaseEvent(string removeSound, FiniteEmitter removeAnim)
            : this()
        {
            RemoveSound = removeSound;
            RemoveAnim = removeAnim;
        }

        /// <inheritdoc/>
        protected RemoveTerrainBaseEvent(RemoveTerrainBaseEvent other) : this()
        {
            RemoveSound = other.RemoveSound;
            RemoveAnim = (FiniteEmitter)other.RemoveAnim.Clone();
        }

        /// <summary>
        /// Determines whether the specified tile should be removed.
        /// </summary>
        /// <param name="tile">The tile to check.</param>
        /// <returns>True if the tile should be removed; otherwise, false.</returns>
        protected abstract bool ShouldRemove(Tile tile);

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Tile tile = ZoneManager.Instance.CurrentMap.GetTile(context.TargetTile);
            if (!ShouldRemove(tile))
                yield break;

            if (context.Target == null)
            {
                GameManager.Instance.BattleSE(RemoveSound);
                FiniteEmitter emitter = (FiniteEmitter)RemoveAnim.Clone();
                emitter.SetupEmit(context.TargetTile * GraphicsManager.TileSize + new Loc(GraphicsManager.TileSize / 2), context.TargetTile * GraphicsManager.TileSize + new Loc(GraphicsManager.TileSize / 2), context.User.CharDir);
                DungeonScene.Instance.CreateAnim(emitter, DrawLayer.NoDraw);
            }

            tile.Data = new TerrainTile(DataManager.Instance.GenFloor);
            int distance = 0;
            Loc startLoc = context.TargetTile - new Loc(distance + 2);
            Loc sizeLoc = new Loc((distance + 2) * 2 + 1);
            ZoneManager.Instance.CurrentMap.MapModified(startLoc, sizeLoc);
        }
    }

    /// <summary>
    /// Event that removes the specified terrain and replaces it with a floor tile.
    /// </summary>
    [Serializable]
    public class RemoveTerrainEvent : RemoveTerrainBaseEvent
    {
        /// <summary>
        /// The set of terrain types that can be removed.
        /// </summary>
        [JsonConverter(typeof(TerrainSetConverter))]
        public HashSet<string> TileTypes;

        /// <inheritdoc/>
        public RemoveTerrainEvent()
        {
            TileTypes = new HashSet<string>();
        }

        /// <summary>
        /// Creates a new RemoveTerrainEvent with the specified sound, animation, and terrain types.
        /// </summary>
        /// <param name="removeSound">The sound effect to play.</param>
        /// <param name="removeAnim">The particle VFX emitter.</param>
        /// <param name="tileTypes">The terrain types that can be removed.</param>
        public RemoveTerrainEvent(string removeSound, FiniteEmitter removeAnim, params string[] tileTypes)
            : base(removeSound, removeAnim)
        {
            TileTypes = new HashSet<string>();
            foreach (string tileType in tileTypes)
                TileTypes.Add(tileType);
        }

        /// <inheritdoc/>
        protected RemoveTerrainEvent(RemoveTerrainEvent other) : base(other)
        {
            TileTypes = new HashSet<string>();
            foreach (string tileType in other.TileTypes)
                TileTypes.Add(tileType);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new RemoveTerrainEvent(this); }

        /// <inheritdoc/>
        protected override bool ShouldRemove(Tile tile)
        {
            if (tile == null)
                return false;
            return TileTypes.Contains(tile.Data.ID);
        }
    }

    /// <summary>
    /// Event that removes the terrain if it contains one of the specified TerrainStates, replacing it with a floor tile.
    /// </summary>
    [Serializable]
    public class RemoveTerrainStateEvent : RemoveTerrainBaseEvent
    {
        /// <summary>
        /// The list of terrain states that qualify for removal.
        /// </summary>
        [StringTypeConstraint(1, typeof(TerrainState))]
        public List<FlagType> States;

        /// <inheritdoc/>
        public RemoveTerrainStateEvent()
        {
            States = new List<FlagType>();
        }

        /// <summary>
        /// Creates a new RemoveTerrainStateEvent with the specified sound, animation, and terrain states.
        /// </summary>
        /// <param name="removeSound">The sound effect to play.</param>
        /// <param name="removeAnim">The particle VFX emitter.</param>
        /// <param name="flagTypes">The terrain states that qualify for removal.</param>
        public RemoveTerrainStateEvent(string removeSound, FiniteEmitter removeAnim, params FlagType[] flagTypes)
            : base(removeSound, removeAnim)
        {
            States = new List<FlagType>();
            States.AddRange(flagTypes);
        }

        /// <inheritdoc/>
        protected RemoveTerrainStateEvent(RemoveTerrainStateEvent other) : base(other)
        {
            States = new List<FlagType>();
            States.AddRange(other.States);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new RemoveTerrainStateEvent(this); }

        /// <inheritdoc/>
        protected override bool ShouldRemove(Tile tile)
        {
            if (tile == null)
                return false;

            TerrainData terrain = DataManager.Instance.GetTerrain(tile.Data.ID);

            foreach (FlagType state in States)
            {
                if (terrain.TerrainStates.Contains(state.FullType))
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Event that removes the specified terrain and the area around it, replacing it with a floor tile.
    /// </summary>
    [Serializable]
    public class ShatterTerrainEvent : BattleEvent
    {
        /// <summary>
        /// The set of terrain types that can be shattered.
        /// </summary>
        [JsonConverter(typeof(TerrainSetConverter))]
        public HashSet<string> TileTypes;

        /// <inheritdoc/>
        public ShatterTerrainEvent() { TileTypes = new HashSet<string>(); }

        /// <summary>
        /// Creates a new ShatterTerrainEvent with the specified terrain types.
        /// </summary>
        /// <param name="tileTypes">The terrain types that can be shattered.</param>
        public ShatterTerrainEvent(params string[] tileTypes)
            : this()
        {
            foreach (string tileType in tileTypes)
                TileTypes.Add(tileType);
        }

        /// <inheritdoc/>
        protected ShatterTerrainEvent(ShatterTerrainEvent other)
        {
            TileTypes = new HashSet<string>();
            foreach (string tileType in other.TileTypes)
                TileTypes.Add(tileType);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ShatterTerrainEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Tile tile = ZoneManager.Instance.CurrentMap.GetTile(context.TargetTile);
            if (tile == null)
                yield break;

            if (!TileTypes.Contains(tile.Data.ID))
                yield break;

            if (context.Target == null)
            {
                GameManager.Instance.BattleSE("DUN_Rollout");
                SingleEmitter emitter = new SingleEmitter(new AnimData("Rock_Smash", 2));
                emitter.SetupEmit(context.TargetTile * GraphicsManager.TileSize + new Loc(GraphicsManager.TileSize / 2), context.TargetTile * GraphicsManager.TileSize + new Loc(GraphicsManager.TileSize / 2), context.User.CharDir);
                DungeonScene.Instance.CreateAnim(emitter, DrawLayer.NoDraw);
            }

            //destroy the wall
            tile.Data = new TerrainTile(DataManager.Instance.GenFloor);
            for (int ii = 0; ii < DirExt.DIR4_COUNT; ii++)
            {
                Loc moveLoc = context.TargetTile + ((Dir4)ii).GetLoc();
                Tile sideTile = ZoneManager.Instance.CurrentMap.GetTile(moveLoc);
                if (sideTile != null && TileTypes.Contains(sideTile.Data.ID))
                    sideTile.Data = new TerrainTile(DataManager.Instance.GenFloor);
            }

            int distance = 0;
            Loc startLoc = context.TargetTile - new Loc(distance + 3);
            Loc sizeLoc = new Loc((distance + 3) * 2 + 1);
            ZoneManager.Instance.CurrentMap.MapModified(startLoc, sizeLoc);
        }
    }



    /// <summary>
    /// Event that hints all unexplored locations on the map.
    /// </summary>
    [Serializable]
    public class MapOutEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new MapOutEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Loc testTile = context.TargetTile;
            if (!ZoneManager.Instance.CurrentMap.GetLocInMapBounds(ref testTile))
                yield break;

            if (ZoneManager.Instance.CurrentMap.DiscoveryArray[testTile.X][testTile.Y] == Map.DiscoveryState.None)
                ZoneManager.Instance.CurrentMap.DiscoveryArray[testTile.X][testTile.Y] = Map.DiscoveryState.Hinted;

        }
    }

    /// <summary>
    /// Event that hints all unexplored locations on the map within the specified radius.
    /// </summary>
    [Serializable]
    public class MapOutRadiusEvent : BattleEvent
    {
        /// <summary>
        /// The radius around the user to hint.
        /// </summary>
        public int Radius;

        /// <inheritdoc/>
        public MapOutRadiusEvent() { }

        /// <summary>
        /// Creates a new MapOutRadiusEvent with the specified radius.
        /// </summary>
        /// <param name="radius">The radius around the user to hint.</param>
        public MapOutRadiusEvent(int radius)
        {
            Radius = radius;
        }

        /// <inheritdoc/>
        protected MapOutRadiusEvent(MapOutRadiusEvent other)
        {
            Radius = other.Radius;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new MapOutRadiusEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {

            for (int ii = 1; ii <= 25; ii++)
            {
                int limitSquared = Radius * Radius * ii * ii / 25 / 25;
                for (int xx = -Radius; xx <= Radius; xx++)
                {
                    for (int yy = -Radius; yy <= Radius; yy++)
                    {
                        Loc diff = new Loc(xx, yy);
                        if (diff.DistSquared() < limitSquared)
                        {
                            Loc loc = context.User.CharLoc + diff;
                            if (!ZoneManager.Instance.CurrentMap.GetLocInMapBounds(ref loc))
                                continue;
                            if (ZoneManager.Instance.CurrentMap.DiscoveryArray[loc.X][loc.Y] == Map.DiscoveryState.None)
                                ZoneManager.Instance.CurrentMap.DiscoveryArray[loc.X][loc.Y] = Map.DiscoveryState.Hinted;
                        }
                    }
                }
                yield return new WaitForFrames(GameManager.Instance.ModifyBattleSpeed(2));
            }

        }
    }
}

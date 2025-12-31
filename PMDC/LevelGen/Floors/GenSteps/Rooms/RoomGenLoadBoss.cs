using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Dungeon;
using RogueEssence.Dev;
using RogueEssence.LevelGen;
using RogueEssence.Data;
using RogueEssence.Content;
using RogueEssence.Ground;
using PMDC.Dungeon;
using Newtonsoft.Json;


namespace PMDC.LevelGen
{

    /// <summary>
    /// Generates a boss room by loading a predefined map as the room layout.
    /// This room type includes tiles, items, enemies, and map start events from a loaded map file.
    /// The boss is spawned on a special trigger tile that initiates the boss battle.
    /// Room borders are determined by examining the loaded map's terrain.
    /// </summary>
    /// <typeparam name="T">The type of map generation context used for this room.</typeparam>
    [Serializable]
    public class RoomGenLoadBoss<T> : RoomGenLoadMapBase<T> where T : BaseMapGenContext
    {
        /// <summary>
        /// The terrain tile type that counts as part of the room interior.
        /// Halls and connections will only attach to tiles matching this terrain type or tiles specified in Borders.
        /// </summary>
        public ITile RoomTerrain { get; set; }

        /// <summary>
        /// The ID of the tile used to trigger the boss battle when stepped on by the player.
        /// This tile is placed at the first entry point of the loaded map.
        /// </summary>
        [JsonConverter(typeof(TileConverter))]
        [DataType(0, DataManager.DataType.Tile, false)]
        public string TriggerTile;

        /// <summary>
        /// Additional spawn features to apply to all boss mob spawns.
        /// This allows customization of boss spawning behavior such as level ranges or special conditions.
        /// NOTE: This is a temporary feature that will be removed when boss rooms can accept full mob spawn configurations.
        /// </summary>
        public List<MobSpawnExtra> SpawnDetails;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoomGenLoadBoss{T}"/> class.
        /// </summary>
        public RoomGenLoadBoss()
        {
            TriggerTile = "";
            SpawnDetails = new List<MobSpawnExtra>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoomGenLoadBoss{T}"/> class with the specified parameters.
        /// </summary>
        /// <param name="mapID">The ID of the map file to load as the boss room layout.</param>
        /// <param name="roomTerrain">The terrain tile type that defines the interior of the room.</param>
        /// <param name="triggerTile">The ID of the tile used to trigger the boss battle when entered.</param>
        public RoomGenLoadBoss(string mapID, ITile roomTerrain, string triggerTile)
        {
            MapID = mapID;
            this.RoomTerrain = roomTerrain;
            TriggerTile = triggerTile;
            SpawnDetails = new List<MobSpawnExtra>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoomGenLoadBoss{T}"/> class by copying another instance.
        /// </summary>
        /// <param name="other">The instance to copy. All properties and spawn details are deep-copied.</param>
        protected RoomGenLoadBoss(RoomGenLoadBoss<T> other) : base(other)
        {
            MapID = other.MapID;
            this.RoomTerrain = other.RoomTerrain;
            TriggerTile = other.TriggerTile;
            SpawnDetails = new List<MobSpawnExtra>();
            foreach (MobSpawnExtra extra in other.SpawnDetails)
                SpawnDetails.Add(extra.Copy());
        }

        /// <inheritdoc/>
        /// <returns>A new deep copy of this room generator with all properties and spawn details cloned.</returns>
        public override RoomGen<T> Copy() { return new RoomGenLoadBoss<T>(this); }

        /// <summary>
        /// Draws the loaded boss room map onto the provided map generation context.
        /// This method handles:
        /// - Copying tiles from the loaded map to the floor
        /// - Placing decorations and items
        /// - Converting loaded map characters into mob spawn configurations
        /// - Creating a trigger tile at the first entry point to initiate the boss battle
        /// - Setting up map start events and boss room boundaries
        /// - Locking the room from further modifications via post-processing
        /// </summary>
        /// <param name="map">The map generation context where this room will be drawn.</param>
        /// <remarks>
        /// If the room dimensions don't match the loaded map dimensions, falls back to default map drawing.
        /// The boss trigger tile is placed at the first entry point and configured with danger status,
        /// mob spawn state, bounds, and music from the loaded map.
        /// </remarks>
        /// <inheritdoc/>
        public override void DrawOnMap(T map)
        {
            if (this.Draw.Width != this.roomMap.Width || this.Draw.Height != this.roomMap.Height)
            {
                this.DrawMapDefault(map);
                return;
            }

            //no copying is needed here since the map is disposed of after use

            DrawTiles(map);

            DrawDecorations(map);

            DrawItems(map);

            for (int xx = 0; xx < this.Draw.Width; xx++)
            {
                for (int yy = 0; yy < this.Draw.Height; yy++)
                {
                    map.SetTile(new Loc(this.Draw.X + xx, this.Draw.Y + yy), this.roomMap.Tiles[xx][yy]);
                }
            }

            if (this.roomMap.EntryPoints.Count < 1)
                throw new InvalidOperationException("Could not find an entry point!");


            MobSpawnState mobSpawnState = new MobSpawnState();
            foreach (Team team in this.roomMap.MapTeams)
            {
                foreach (Character member in team.EnumerateChars())
                {
                    MobSpawn newSpawn = new MobSpawn();
                    newSpawn.BaseForm = member.BaseForm;
                    newSpawn.Level = new RandRange(member.Level);
                    foreach (SlotSkill skill in member.BaseSkills)
                    {
                        if (!String.IsNullOrEmpty(skill.SkillNum))
                            newSpawn.SpecifiedSkills.Add(skill.SkillNum);
                    }
                    newSpawn.Intrinsic = member.BaseIntrinsics[0];

                    newSpawn.Tactic = member.Tactic.ID;

                    MobSpawnLoc setLoc = new MobSpawnLoc(this.Draw.Start + member.CharLoc);
                    newSpawn.SpawnFeatures.Add(setLoc);

                    foreach (MobSpawnExtra extra in SpawnDetails)
                        newSpawn.SpawnFeatures.Add(extra.Copy());

                    mobSpawnState.Spawns.Add(newSpawn);
                }
            }

            Loc triggerLoc = this.roomMap.EntryPoints[0].Loc;
            EffectTile newEffect = new EffectTile(TriggerTile, true, triggerLoc + this.Draw.Start);
            newEffect.TileStates.Set(new DangerState(true));
            newEffect.TileStates.Set(mobSpawnState);
            newEffect.TileStates.Set(new BoundsState(new Rect(this.Draw.Start - new Loc(1), this.Draw.Size + new Loc(2))));
            newEffect.TileStates.Set(new SongState(this.roomMap.Music));

            MapStartEventState beginEvent = new MapStartEventState();
            foreach (Priority priority in this.roomMap.MapEffect.OnMapStarts.GetPriorities())
            {
                foreach (SingleCharEvent step in this.roomMap.MapEffect.OnMapStarts.GetItems(priority))
                    beginEvent.OnMapStarts.Add(priority, step);
            }
            newEffect.TileStates.Set(beginEvent);
            ((IPlaceableGenContext<EffectTile>)map).PlaceItem(triggerLoc + this.Draw.Start, newEffect);
            map.GetPostProc(triggerLoc + this.Draw.Start).Status |= (PostProcType.Panel | PostProcType.Item | PostProcType.Terrain);

            //this.FulfillRoomBorders(map, this.FulfillAll);
            this.SetRoomBorders(map);

            for (int xx = 0; xx < Draw.Width; xx++)
            {
                for (int yy = 0; yy < Draw.Height; yy++)
                    map.GetPostProc(new Loc(Draw.X + xx, Draw.Y + yy)).AddMask(new PostProcTile(PreventChanges));
            }
        }

        /// <summary>
        /// Prepares which edges of the room can be filled with connecting hallways by analyzing the loaded map's border tiles.
        /// This method determines which border positions match the RoomTerrain and are therefore valid attachment points for halls.
        /// </summary>
        /// <param name="rand">Random number generator for generation (unused in this implementation).</param>
        /// <remarks>
        /// If the room dimensions don't match the loaded map dimensions, all border positions are marked as fulfillable.
        /// Otherwise, each edge is analyzed to check if border tiles match the RoomTerrain specification.
        /// NOTE: The tile ID for openings must be specified on this class instead of through the context
        /// because the context is not available during border preparation.
        /// </remarks>
        /// <inheritdoc/>
        protected override void PrepareFulfillableBorders(IRandom rand)
        {
            // NOTE: Because the context is not passed in when preparing borders,
            // the tile ID representing an opening must be specified on this class instead.
            if (this.Draw.Width != this.roomMap.Width || this.Draw.Height != this.roomMap.Height)
            {
                foreach (Dir4 dir in DirExt.VALID_DIR4)
                {
                    for (int jj = 0; jj < this.FulfillableBorder[dir].Length; jj++)
                        this.FulfillableBorder[dir][jj] = true;
                }
            }
            else
            {
                for (int ii = 0; ii < this.Draw.Width; ii++)
                {
                    this.FulfillableBorder[Dir4.Up][ii] = this.roomMap.Tiles[ii][0].TileEquivalent(this.RoomTerrain);
                    this.FulfillableBorder[Dir4.Down][ii] = this.roomMap.Tiles[ii][this.Draw.Height - 1].TileEquivalent(this.RoomTerrain);
                }

                for (int ii = 0; ii < this.Draw.Height; ii++)
                {
                    this.FulfillableBorder[Dir4.Left][ii] = this.roomMap.Tiles[0][ii].TileEquivalent(this.RoomTerrain);
                    this.FulfillableBorder[Dir4.Right][ii] = this.roomMap.Tiles[this.Draw.Width - 1][ii].TileEquivalent(this.RoomTerrain);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using PMDC.Dungeon;
using RogueElements;
using RogueEssence.Dungeon;
using RogueEssence.LevelGen;

namespace PMDC.LevelGen
{
    /// <summary>
    /// Spawns a team by taking X amount of individual mob spawns from the map's respawn table, and adding a mob spawn extra to the leader.
    /// </summary>
    /// <typeparam name="T">The type of map generation context, derived from <see cref="BaseMapGenContext"/>.</typeparam>
    [Serializable]
    public class BossBandContextSpawner<T> : IMultiTeamSpawner<T>, IBossBandContextSpawner
        where T : BaseMapGenContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BossBandContextSpawner{T}"/> class with an empty team size.
        /// </summary>
        public BossBandContextSpawner()
        {
            TeamSize = RandRange.Empty;
            LeaderFeatures = new List<MobSpawnExtra>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BossBandContextSpawner{T}"/> class with the specified team size.
        /// </summary>
        /// <param name="amount">A <see cref="RandRange"/> specifying the total number of team members to spawn.</param>
        public BossBandContextSpawner(RandRange amount)
        {
            TeamSize = amount;
            LeaderFeatures = new List<MobSpawnExtra>();
        }

        /// <summary>
        /// This amount is in total team members.
        /// </summary>
        public RandRange TeamSize { get; set; }

        /// <summary>
        /// If true, creates an ExplorerTeam instead of a MonsterTeam.
        /// </summary>
        public bool Explorer { get; set; }


        /// <summary>
        /// Additional alterations made to the leader after it is created but before it is spawned.
        /// </summary>
        public List<MobSpawnExtra> LeaderFeatures { get; set; }


        /// <summary>
        /// Creates the team with a leader and subordinates from the map's spawn table.
        /// </summary>
        /// <param name="map">The map generation context.</param>
        /// <returns>A list containing the spawned team.</returns>
        public List<Team> GetSpawns(T map)
        {
            int chosenAmount = TeamSize.Pick(map.Rand);

            List<MobSpawn> flatSpawnList = new List<MobSpawn>();
            for (int ii = 0; ii < map.TeamSpawns.Count; ii++)
            {
                SpawnList<MobSpawn> memberSpawns = map.TeamSpawns.GetSpawn(ii).GetPossibleSpawns();
                for (int jj = 0; jj < memberSpawns.Count; jj++)
                {
                    MobSpawn spawn = memberSpawns.GetSpawn(jj);
                    flatSpawnList.Add(spawn);
                }
            }

            //pick the chosen amount
            List<MobSpawn> chosenSpawns = new List<MobSpawn>();
            for (int ii = 0; ii < chosenAmount; ii++)
            {
                if (ii == 0)
                {
                    //add a leader, with the extra mob spawn step
                    MobSpawn chosenSpawn = flatSpawnList[map.Rand.Next(flatSpawnList.Count)];
                    MobSpawn leaderSpawn = chosenSpawn.Copy();
                    leaderSpawn.SpawnFeatures.AddRange(LeaderFeatures);
                    chosenSpawns.Add(leaderSpawn);
                }
                else
                {
                    //add the subordinates
                    MobSpawn chosenSpawn = flatSpawnList[map.Rand.Next(flatSpawnList.Count)];
                    chosenSpawns.Add(chosenSpawn);
                }
            }

            Team team;
            if (Explorer)
                team = new ExplorerTeam();
            else
                team = new MonsterTeam();
            foreach (MobSpawn chosenSpawn in chosenSpawns)
                chosenSpawn.Spawn(team, map);

            List<Team> resultList = new List<Team>();
            resultList.Add(team);
            return resultList;
        }

        /// <summary>
        /// Returns a string representation of this spawner, including its type name and team size range.
        /// </summary>
        /// <returns>A formatted string containing the type name and team size range.</returns>
        public override string ToString()
        {
            return string.Format("{0}[{1}]", this.GetType().GetFormattedTypeName(), TeamSize.ToString());
        }
    }

    /// <summary>
    /// Interface for boss band context spawners that create teams with a leader and subordinates.
    /// </summary>
    public interface IBossBandContextSpawner
    {
        /// <summary>
        /// Gets or sets the total number of team members to spawn.
        /// </summary>
        RandRange TeamSize { get; set; }
    }
}

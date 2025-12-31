using System;
using System.Collections.Generic;
using RogueEssence.Dungeon;
using RogueElements;
using PMDC.LevelGen;

namespace RogueEssence.Dev
{
    /// <summary>
    /// Editor for MobSpawnWeak objects that reduce spawned monsters' PP and belly.
    /// </summary>
    public class MobSpawnWeakEditor : Editor<MobSpawnWeak>
    {
        /// <summary>
        /// Gets a display string describing the weak spawn effect.
        /// </summary>
        /// <param name="obj">The weak spawn modifier.</param>
        /// <param name="type">The object type.</param>
        /// <param name="attributes">Custom attributes.</param>
        /// <returns>A description of the effect.</returns>
        public override string GetString(MobSpawnWeak obj, Type type, object[] attributes)
        {
            return "Half PP and 35% belly";
        }
        /// <summary>
        /// Gets the type display string for this editor.
        /// </summary>
        /// <returns>The display name for the type.</returns>
        public override string GetTypeString()
        {
            return "Low PP and Belly";
        }
    }

    /// <summary>
    /// Editor for MobSpawnAltColor objects that give spawned monsters a chance to be shiny.
    /// </summary>
    public class MobSpawnAltColorEditor : Editor<MobSpawnAltColor>
    {
        /// <summary>
        /// Gets a display string showing the shiny chance.
        /// </summary>
        /// <param name="obj">The alt color spawn modifier.</param>
        /// <param name="type">The object type.</param>
        /// <param name="attributes">Custom attributes.</param>
        /// <returns>A description showing the chance ratio.</returns>
        public override string GetString(MobSpawnAltColor obj, Type type, object[] attributes)
        {
            return String.Format("Shiny Chance: {0} in {1}", obj.Chance.Numerator, obj.Chance.Denominator);
        }
        /// <summary>
        /// Gets the type display string for this editor.
        /// </summary>
        /// <returns>The display name for the type.</returns>
        public override string GetTypeString()
        {
            return "Shiny Chance";
        }
    }

    /// <summary>
    /// Editor for MobSpawnMovesOff objects that disable move slots on spawned monsters.
    /// </summary>
    public class MobSpawnMovesOffEditor : Editor<MobSpawnMovesOff>
    {
        /// <summary>
        /// Gets a display string showing which move slots are disabled.
        /// </summary>
        /// <param name="obj">The moves off spawn modifier.</param>
        /// <param name="type">The object type.</param>
        /// <param name="attributes">Custom attributes.</param>
        /// <returns>A description of which slots are disabled.</returns>
        public override string GetString(MobSpawnMovesOff obj, Type type, object[] attributes)
        {
            return String.Format("Moves disabled after slot {0}", obj.StartAt);
        }
        /// <summary>
        /// Gets the type display string for this editor.
        /// </summary>
        /// <returns>The display name for the type.</returns>
        public override string GetTypeString()
        {
            return "Disable Moves";
        }
    }

    /// <summary>
    /// Editor for MobSpawnBoost objects that add flat stat bonuses to spawned monsters.
    /// </summary>
    public class MobSpawnBoostEditor : Editor<MobSpawnBoost>
    {
        /// <summary>
        /// Gets a display string showing all stat boosts applied.
        /// </summary>
        /// <param name="obj">The stat boost spawn modifier.</param>
        /// <param name="type">The object type.</param>
        /// <param name="attributes">Custom attributes.</param>
        /// <returns>A formatted list of stat boosts.</returns>
        public override string GetString(MobSpawnBoost obj, Type type, object[] attributes)
        {
            List<Tuple<String, int>> stats = new List<Tuple<String, int>>
            {
                new Tuple<String,int>("HP",obj.MaxHPBonus),
                new Tuple<String,int>("Atk",obj.AtkBonus),
                new Tuple<String,int>("Def",obj.DefBonus),
                new Tuple<String,int>("SpAtk",obj.SpAtkBonus),
                new Tuple<String,int>("SpDef",obj.SpDefBonus),
                new Tuple<String,int>("Speed",obj.SpeedBonus),
            };
            List<String> statBoosts = new List<String>();
            foreach ((String statName, int bonus) in stats)
            {
                if (bonus != 0)
                {
                    statBoosts.Add(String.Format("{0} {1}", bonus.ToString("+0;-#"), statName));
                }
            }
            return String.Format("Stat boosts: {0}", String.Join(", ", statBoosts));
        }
        /// <summary>
        /// Gets the type display string for this editor.
        /// </summary>
        /// <returns>The display name for the type.</returns>
        public override string GetTypeString()
        {
            return "Stat Boosts";
        }
    }

    /// <summary>
    /// Editor for MobSpawnScaledBoost objects that add level-scaled stat bonuses to spawned monsters.
    /// </summary>
    public class MobSpawnScaledBoostEditor : Editor<MobSpawnScaledBoost>
    {
        /// <summary>
        /// Gets a display string showing all level-scaled stat boost ranges.
        /// </summary>
        /// <param name="obj">The scaled boost spawn modifier.</param>
        /// <param name="type">The object type.</param>
        /// <param name="attributes">Custom attributes.</param>
        /// <returns>A formatted list of stat boost ranges.</returns>
        public override string GetString(MobSpawnScaledBoost obj, Type type, object[] attributes)
        {
            
            List<Tuple<String, IntRange>> stats = new List<Tuple<String, IntRange>>
            {
                new Tuple<String,IntRange>("Lvl",obj.LevelRange),
                new Tuple<String,IntRange>("HP",obj.MaxHPBonus),
                new Tuple<String,IntRange>("Atk",obj.AtkBonus),
                new Tuple<String,IntRange>("Def",obj.DefBonus),
                new Tuple<String,IntRange>("SpAtk",obj.SpAtkBonus),
                new Tuple<String,IntRange>("SpDef",obj.SpDefBonus),
                new Tuple<String,IntRange>("Speed",obj.SpeedBonus),
            };
            List<String> statBoosts = new List<String>();
            foreach ((String statName, IntRange bonusRange) in stats)
            {
                if ((bonusRange.Min != 0) | (bonusRange.Max != 0))
                {
                    statBoosts.Add(String.Format("{0}: [{1}, {2}]", statName, bonusRange.Min, bonusRange.Max));
                }
            }
            return String.Format("Level-scaled stat boosts: {0}", String.Join(", ", statBoosts));
        }
        /// <summary>
        /// Gets the type display string for this editor.
        /// </summary>
        /// <returns>The display name for the type.</returns>
        public override string GetTypeString()
        {
            return "Level-Scaled Stat Boosts";
        }
    }

    /// <summary>
    /// Editor for MobSpawnItem objects that give spawned monsters a held item.
    /// </summary>
    public class MobSpawnItemEditor : Editor<MobSpawnItem>
    {
        /// <summary>
        /// Gets a display string showing the held item.
        /// </summary>
        /// <param name="obj">The item spawn modifier.</param>
        /// <param name="type">The object type.</param>
        /// <param name="attributes">Custom attributes.</param>
        /// <returns>A description of the held item.</returns>
        public override string GetString(MobSpawnItem obj, Type type, object[] attributes)
        {
            String Item = "";
            if (obj.Items.Count == 1)
            {
                Item = obj.Items.GetSpawn(0).ToString();
            } 
            else
            {
                Item = obj.Items.ToString();
            }
            return String.Format("Item: {0}", Item);
        }
        /// <summary>
        /// Gets the type display string for this editor.
        /// </summary>
        /// <returns>The display name for the type.</returns>
        public override string GetTypeString()
        {
            return "Held Item";
        }
    }

    /// <summary>
    /// Editor for MobSpawnInv objects that give spawned monsters inventory items.
    /// </summary>
    public class MobSpawnInvEditor : Editor<MobSpawnInv>
    {
        /// <summary>
        /// Gets a display string showing the inventory items.
        /// </summary>
        /// <param name="obj">The inventory spawn modifier.</param>
        /// <param name="type">The object type.</param>
        /// <param name="attributes">Custom attributes.</param>
        /// <returns>A list of inventory items.</returns>
        public override string GetString(MobSpawnInv obj, Type type, object[] attributes)
        {
            List<String> inventory = new List<String>();
            foreach (InvItem item in obj.Items)
            {
                inventory.Add(item.ToString());
            }
            return String.Format("Inventory: {0}", String.Join(", ", inventory));
        }
        /// <summary>
        /// Gets the type display string for this editor.
        /// </summary>
        /// <returns>The display name for the type.</returns>
        public override string GetTypeString()
        {
            return "Inventory";
        }
    }

    /// <summary>
    /// Editor for MobSpawnLevelScale objects that scale monster level based on floor depth.
    /// </summary>
    public class MobSpawnLevelScaleEditor : Editor<MobSpawnLevelScale>
    {
        /// <summary>
        /// Gets a display string showing the floor scaling configuration.
        /// </summary>
        /// <param name="obj">The level scale spawn modifier.</param>
        /// <param name="type">The object type.</param>
        /// <param name="attributes">Custom attributes.</param>
        /// <returns>A description of the level scaling.</returns>
        public override string GetString(MobSpawnLevelScale obj, Type type, object[] attributes)
        {
            return String.Format("Scale level to floor starting at floor {0}", obj.StartFromID + 1);
        }
        /// <summary>
        /// Gets the type display string for this editor.
        /// </summary>
        /// <returns>The display name for the type.</returns>
        public override string GetTypeString()
        {
            return "Floor-Scaled Level";
        }
    }

    /// <summary>
    /// Editor for MobSpawnLoc objects that set a specific spawn position and facing direction.
    /// </summary>
    public class MobSpawnLocEditor : Editor<MobSpawnLoc>
    {
        /// <summary>
        /// Gets a display string showing the spawn position and direction.
        /// </summary>
        /// <param name="obj">The location spawn modifier.</param>
        /// <param name="type">The object type.</param>
        /// <param name="attributes">Custom attributes.</param>
        /// <returns>A description of the spawn location.</returns>
        public override string GetString(MobSpawnLoc obj, Type type, object[] attributes)
        {
            return String.Format("Spawn at X:{0}, Y:{1}, facing {2}", obj.Loc.X, obj.Loc.Y, obj.Dir);
        }
        /// <summary>
        /// Gets the type display string for this editor.
        /// </summary>
        /// <returns>The display name for the type.</returns>
        public override string GetTypeString()
        {
            return "Position and Orientation";
        }
    }

    /// <summary>
    /// Editor for MobSpawnUnrecruitable objects that prevent monsters from being recruited.
    /// </summary>
    public class MobSpawnUnrecruitableEditor : Editor<MobSpawnUnrecruitable>
    {
        /// <summary>
        /// Gets a display string indicating the monster is unrecruitable.
        /// </summary>
        /// <param name="obj">The unrecruitable spawn modifier.</param>
        /// <param name="type">The object type.</param>
        /// <param name="attributes">Custom attributes.</param>
        /// <returns>The string "Unrecruitable".</returns>
        public override string GetString(MobSpawnUnrecruitable obj, Type type, object[] attributes)
        {
            return "Unrecruitable";
        }
        /// <summary>
        /// Gets the type display string for this editor.
        /// </summary>
        /// <returns>The display name for the type.</returns>
        public override string GetTypeString()
        {
            return "Unrecruitable";
        }
    }

    /// <summary>
    /// Editor for MobSpawnFoeConflict objects that make spawned monsters attack other enemies.
    /// </summary>
    public class MobSpawnFoeConflictEditor : Editor<MobSpawnFoeConflict>
    {
        /// <summary>
        /// Gets a display string indicating the monster attacks enemies.
        /// </summary>
        /// <param name="obj">The foe conflict spawn modifier.</param>
        /// <param name="type">The object type.</param>
        /// <param name="attributes">Custom attributes.</param>
        /// <returns>The string "Attacks Enemies".</returns>
        public override string GetString(MobSpawnFoeConflict obj, Type type, object[] attributes)
        {
            return "Attacks Enemies";
        }
        /// <summary>
        /// Gets the type display string for this editor.
        /// </summary>
        /// <returns>The display name for the type.</returns>
        public override string GetTypeString()
        {
            return "Aggressive";
        }
    }

    /// <summary>
    /// Editor for MobSpawnInteractable objects that add interaction events to spawned monsters.
    /// </summary>
    public class MobSpawnInteractableEditor : Editor<MobSpawnInteractable>
    {
        /// <summary>
        /// Gets a display string listing the interaction events.
        /// </summary>
        /// <param name="obj">The interactable spawn modifier.</param>
        /// <param name="type">The object type.</param>
        /// <param name="attributes">Custom attributes.</param>
        /// <returns>A list of interaction event names.</returns>
        public override string GetString(MobSpawnInteractable obj, Type type, object[] attributes)
        {
            List<String> interactionEventNames = new List<String>();
            foreach (BattleEvent battleEvent in obj.CheckEvents)
            {
                interactionEventNames.Add(battleEvent.ToString());
            }
            return String.Format("Interactions: {0}", String.Join(", ", interactionEventNames));
        }
        /// <summary>
        /// Gets the type display string for this editor.
        /// </summary>
        /// <returns>The display name for the type.</returns>
        public override string GetTypeString()
        {
            return "Interactions";
        }
    }

    /// <summary>
    /// Editor for MobSpawnLuaTable objects that attach custom Lua data to spawned monsters.
    /// </summary>
    public class MobSpawnLuaTableEditor : Editor<MobSpawnLuaTable>
    {
        /// <summary>
        /// Gets a display string indicating custom Lua scripting is attached.
        /// </summary>
        /// <param name="obj">The Lua table spawn modifier.</param>
        /// <param name="type">The object type.</param>
        /// <param name="attributes">Custom attributes.</param>
        /// <returns>The string "Custom Lua Script".</returns>
        public override string GetString(MobSpawnLuaTable obj, Type type, object[] attributes)
        {
            return "Custom Lua Script";
        }
        /// <summary>
        /// Gets the type display string for this editor.
        /// </summary>
        /// <returns>The display name for the type.</returns>
        public override string GetTypeString()
        {
            return "Lua Scripting";
        }
    }

    /// <summary>
    /// Editor for MobSpawnDiscriminator objects that assign a unique discriminator ID to spawned monsters.
    /// </summary>
    public class MobSpawnDiscriminatorEditor : Editor<MobSpawnDiscriminator>
    {
        /// <summary>
        /// Gets a display string showing the discriminator ID.
        /// </summary>
        /// <param name="obj">The discriminator spawn modifier.</param>
        /// <param name="type">The object type.</param>
        /// <param name="attributes">Custom attributes.</param>
        /// <returns>A description showing the discriminator ID.</returns>
        public override string GetString(MobSpawnDiscriminator obj, Type type, object[] attributes)
        {
            return String.Format("Descriminator ID: {0}", obj.Discriminator);
        }
        /// <summary>
        /// Gets the type display string for this editor.
        /// </summary>
        /// <returns>The display name for the type.</returns>
        public override string GetTypeString()
        {
            return "Descriminator";
        }
    }

    /// <summary>
    /// Editor for Intrinsic3Chance objects that give spawned monsters a chance to have their hidden ability.
    /// </summary>
    public class Intrinsic3ChanceEditor : Editor<Intrinsic3Chance>
    {
        /// <summary>
        /// Gets a display string indicating the hidden ability roll.
        /// </summary>
        /// <param name="obj">The hidden ability chance modifier.</param>
        /// <param name="type">The object type.</param>
        /// <param name="attributes">Custom attributes.</param>
        /// <returns>The string "Roll for Hidden Ability".</returns>
        public override string GetString(Intrinsic3Chance obj, Type type, object[] attributes)
        {
            return "Roll for Hidden Ability";
        }
        /// <summary>
        /// Gets the type display string for this editor.
        /// </summary>
        /// <returns>The display name for the type.</returns>
        public override string GetTypeString()
        {
            return "Roll for Hidden Ability";
        }
    }
}

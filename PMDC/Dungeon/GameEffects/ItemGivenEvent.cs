using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence.Data;
using RogueEssence.Content;
using RogueEssence;
using RogueEssence.Dungeon;
using PMDC.Dev;
using RogueEssence.Dev;
using RogueEssence.LevelGen;
using Newtonsoft.Json;

namespace PMDC.Dungeon
{
    /// <summary>
    /// Item given event that automatically curses an item when equipped.
    /// Used for items with the auto-curse property.
    /// </summary>
    [Serializable]
    public class AutoCurseItemEvent : ItemGivenEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() => new AutoCurseItemEvent();

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, ItemCheckContext context)
        {
            if (!context.User.EquippedItem.Cursed)
            {
                GameManager.Instance.SE(GraphicsManager.CursedSE);
                if (!context.User.CanRemoveStuck)
                    DungeonScene.Instance.LogMsg(Text.FormatKey("MSG_EQUIP_AUTOCURSE", context.User.EquippedItem.GetDisplayName(), context.User.GetDisplayName(false)));
                else
                    DungeonScene.Instance.LogMsg(Text.FormatKey("MSG_EQUIP_AUTOCURSE_AVOID", context.User.EquippedItem.GetDisplayName(), context.User.GetDisplayName(false)));
                context.User.EquippedItem.Cursed = true;
            }
            context.User.RefreshTraits();
            yield break;
        }
    }

    /// <summary>
    /// Item given event that displays a warning message when equipping a cursed item.
    /// Notifies the player that the item is cursed and cannot be removed normally.
    /// </summary>
    [Serializable]
    public class CurseWarningEvent : ItemGivenEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() => new CurseWarningEvent();

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, ItemCheckContext context)
        {
            if (context.User.EquippedItem.Cursed && !context.User.CanRemoveStuck)
            {
                GameManager.Instance.SE(GraphicsManager.CursedSE);
                DungeonScene.Instance.LogMsg(Text.FormatKey("MSG_EQUIP_CURSED", context.User.EquippedItem.GetDisplayName(), context.User.GetDisplayName(false)));
            }
            yield break;
        }
    }

    /// <summary>
    /// Structure representing a fake item configuration that transforms into a monster when picked up.
    /// Associates an item ID with the species that disguises as that item.
    /// </summary>
    [Serializable]
    public struct ItemFake
    {
        /// <summary>
        /// The item ID that this fake item appears as.
        /// </summary>
        [DataType(0, DataManager.DataType.Item, false)]
        public string Item;

        /// <summary>
        /// The monster species that is disguised as the item.
        /// </summary>
        [DataType(0, DataManager.DataType.Monster, false)]
        public string Species;

        /// <summary>
        /// Initializes a new ItemFake with the specified item and species.
        /// </summary>
        /// <param name="item">The item ID to disguise as.</param>
        /// <param name="species">The monster species hiding as this item.</param>
        public ItemFake(string item, string species)
        {
            Item = item;
            Species = species;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return (obj is ItemFake) && Equals((ItemFake)obj);
        }

        /// <summary>
        /// Determines whether this ItemFake equals another ItemFake.
        /// </summary>
        /// <param name="other">The ItemFake to compare with.</param>
        /// <returns>True if the item and species match.</returns>
        public bool Equals(ItemFake other)
        {
            if (Species != other.Species)
                return false;
            if (Item != other.Item)
                return false;

            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return String.GetHashCode(Species) ^ String.GetHashCode(Item);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return String.Format("{0}:{1}", Item, Species);
        }
    }

    /// <summary>
    /// Item given event that transforms fake items into enemy monsters when picked up.
    /// Implements the mimic-like behavior where disguised monsters reveal themselves.
    /// </summary>
    [Serializable]
    public class FakeItemEvent : ItemGivenEvent
    {
        /// <summary>
        /// Maps fake item configurations to the monster spawns that appear when the item is picked up.
        /// </summary>
        [JsonConverter(typeof(ItemFakeTableConverter))]
        public Dictionary<ItemFake, MobSpawn> SpawnTable;

        /// <summary>
        /// Initializes a new instance with an empty spawn table.
        /// </summary>
        public FakeItemEvent()
        {
            SpawnTable = new Dictionary<ItemFake, MobSpawn>();
        }

        /// <summary>
        /// Initializes a new instance with the specified spawn table.
        /// </summary>
        /// <param name="spawnTable">The mapping of fake items to monster spawns.</param>
        public FakeItemEvent(Dictionary<ItemFake, MobSpawn> spawnTable)
        {
            this.SpawnTable = spawnTable;
        }

        /// <summary>
        /// Copy constructor for cloning an existing FakeItemEvent.
        /// </summary>
        /// <param name="other">The FakeItemEvent to clone.</param>
        public FakeItemEvent(FakeItemEvent other)
        {
            this.SpawnTable = new Dictionary<ItemFake, MobSpawn>();
            foreach (ItemFake fake in other.SpawnTable.Keys)
                this.SpawnTable.Add(fake, other.SpawnTable[fake].Copy());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() => new FakeItemEvent(this);

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, ItemCheckContext context)
        {
            ItemFake fake = new ItemFake(context.Item.Value, context.Item.HiddenValue);
            MobSpawn spawn;
            if (SpawnTable.TryGetValue(fake, out spawn))
            {
                deleteFakeItem(context.User, fake);

                if (context.User.MemberTeam == DungeonScene.Instance.ActiveTeam)
                {
                    yield return CoroutineManager.Instance.StartCoroutine(SpawnFake(context.User, context.Item.MakeInvItem(), spawn));
                }
                else
                {
                    //enemies might pick up the item, just silently put it back down.

                    //spawn the item directly below
                    DungeonScene.Instance.DropMapItem(new MapItem(context.Item), context.User.CharLoc, context.User.CharLoc, true);
                }

                //cancel the pickup
                context.CancelState.Cancel = true;
            }
            yield break;
        }

        /// <summary>
        /// Spawns a fake item monster near the specified character with animation.
        /// </summary>
        /// <param name="chara">The character who triggered the fake item.</param>
        /// <param name="item">The item that was revealed to be fake.</param>
        /// <param name="spawn">The monster spawn configuration to use.</param>
        /// <returns>A coroutine for the spawn sequence.</returns>
        public static IEnumerator<YieldInstruction> SpawnFake(Character chara, InvItem item, MobSpawn spawn)
        {
            //pause
            yield return new WaitForFrames(GameManager.Instance.ModifyBattleSpeed(30));

            //gasp!
            EmoteData emoteData = DataManager.Instance.GetEmote("shock");
            chara.StartEmote(new Emote(emoteData.Anim, emoteData.LocHeight, 1));
            GameManager.Instance.BattleSE("EVT_Emote_Shock_2");

            //spawn the enemy
            MonsterTeam team = new MonsterTeam();
            Character mob = spawn.Spawn(team, ZoneManager.Instance.CurrentMap);
            Loc? dest = ZoneManager.Instance.CurrentMap.GetClosestTileForChar(mob, chara.CharLoc + chara.CharDir.GetLoc());
            Loc endLoc;
            if (dest.HasValue)
                endLoc = dest.Value;
            else
                endLoc = chara.CharLoc;

            DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_FAKE_ITEM").ToLocal(), item.GetDisplayName(), mob.GetDisplayName(false)));

            ZoneManager.Instance.CurrentMap.MapTeams.Add(team);
            mob.RefreshTraits();
            CharAnimJump jumpTo = new CharAnimJump();
            jumpTo.FromLoc = chara.CharLoc;
            jumpTo.CharDir = mob.CharDir;
            jumpTo.ToLoc = endLoc;
            jumpTo.MajorAnim = true;
            Dir8 dir = ZoneManager.Instance.CurrentMap.ApproximateClosestDir8(endLoc, chara.CharLoc);
            if (dir > Dir8.None)
                jumpTo.CharDir = dir;

            yield return CoroutineManager.Instance.StartCoroutine(mob.StartAnim(jumpTo));
            mob.Tactic.Initialize(mob);

            yield return CoroutineManager.Instance.StartCoroutine(mob.OnMapStart());
            ZoneManager.Instance.CurrentMap.UpdateExploration(mob);
        }

        /// <summary>
        /// Removes the fake item from the character's equipped item or inventory.
        /// Searches both the equipped slot and all inventory slots for a matching item.
        /// </summary>
        /// <param name="chara">The character to remove the fake item from.</param>
        /// <param name="fake">The fake item configuration to match and delete.</param>
        private static void deleteFakeItem(Character chara, ItemFake fake)
        {
            //delete the item from held items and inventory (just check all slots for the an item that matches and delete it)
            //later maybe make a more watertight way to check??
            if (chara.EquippedItem.ID == fake.Item && chara.EquippedItem.HiddenValue == fake.Species)
            {
                chara.SilentDequipItem();
                return;
            }

            for (int ii = 0; ii < chara.MemberTeam.GetInvCount(); ii++)
            {
                InvItem item = chara.MemberTeam.GetInv(ii);
                if (item.ID == fake.Item && item.HiddenValue == fake.Species)
                {
                    chara.MemberTeam.RemoveFromInv(ii);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Item given event that checks and notifies when an equipped item's effects can be shared.
    /// Used by abilities that allow held item benefits to extend to teammates.
    /// </summary>
    [Serializable]
    public class CheckEquipPassValidityEvent : ItemGivenEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() => new CheckEquipPassValidityEvent();

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, ItemCheckContext context)
        {
            if (!String.IsNullOrEmpty(context.User.EquippedItem.ID))
            {
                ItemData entry = (ItemData)context.User.EquippedItem.GetData();

                if (CanItemEffectBePassed(entry))
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_EQUIP_SHARE").ToLocal(), context.User.EquippedItem.GetDisplayName(), context.User.GetDisplayName(false)));
            }
            yield break;
        }

        /// <summary>
        /// Determines whether an item's effects can be shared with other characters.
        /// Items with certain event types or non-zero priority effects cannot be passed.
        /// </summary>
        /// <param name="entry">The item data to check.</param>
        /// <returns>True if the item's effects can be shared.</returns>
        public static bool CanItemEffectBePassed(ItemData entry)
        {
            //no refresh events allowed
            if (entry.OnRefresh.Count > 0)
                return false;

            //no proximity events allowed
            if (entry.ProximityEvent.Radius > -1)
                return false;

            //for every other event list, the priority must be 0
            //foreach (var effect in entry.OnEquips)
            //    if (effect.Key != Priority.Zero)
            //        return false;
            //foreach (var effect in entry.OnPickups)
            //    if (effect.Key != Priority.Zero)
            //    return false;

            foreach (var effect in entry.BeforeStatusAdds)
                if (effect.Key != Priority.Zero)
                    return false;
            foreach (var effect in entry.BeforeStatusAddings)
                if (effect.Key != Priority.Zero)
                    return false;
            foreach (var effect in entry.OnStatusAdds)
                if (effect.Key != Priority.Zero)
                    return false;
            foreach (var effect in entry.OnStatusRemoves)
                if (effect.Key != Priority.Zero)
                    return false;
            foreach (var effect in entry.OnMapStatusAdds)
                if (effect.Key != Priority.Zero)
                    return false;
            foreach (var effect in entry.OnMapStatusRemoves)
                if (effect.Key != Priority.Zero)
                    return false;

            foreach (var effect in entry.OnMapStarts)
                if (effect.Key != Priority.Zero)
                    return false;
            foreach (var effect in entry.OnTurnStarts)
                if (effect.Key != Priority.Zero)
                    return false;
            foreach (var effect in entry.OnTurnEnds)
                if (effect.Key != Priority.Zero)
                    return false;
            foreach (var effect in entry.OnMapTurnEnds)
                if (effect.Key != Priority.Zero)
                    return false;
            foreach (var effect in entry.OnWalks)
                if (effect.Key != Priority.Zero)
                    return false;
            foreach (var effect in entry.OnDeaths)
                if (effect.Key != Priority.Zero)
                    return false;

            foreach (var effect in entry.BeforeTryActions)
                if (effect.Key != Priority.Zero)
                    return false;
            foreach (var effect in entry.BeforeActions)
                if (effect.Key != Priority.Zero)
                    return false;
            foreach (var effect in entry.OnActions)
                if (effect.Key != Priority.Zero)
                    return false;
            foreach (var effect in entry.BeforeHittings)
                if (effect.Key != Priority.Zero)
                    return false;
            foreach (var effect in entry.BeforeBeingHits)
                if (effect.Key != Priority.Zero)
                    return false;
            foreach (var effect in entry.AfterHittings)
                if (effect.Key != Priority.Zero)
                    return false;
            foreach (var effect in entry.AfterBeingHits)
                if (effect.Key != Priority.Zero)
                    return false;
            foreach (var effect in entry.OnHitTiles)
                if (effect.Key != Priority.Zero)
                    return false;
            foreach (var effect in entry.AfterActions)
                if (effect.Key != Priority.Zero)
                    return false;

            foreach (var effect in entry.UserElementEffects)
                if (effect.Key != Priority.Zero)
                    return false;
            foreach (var effect in entry.TargetElementEffects)
                if (effect.Key != Priority.Zero)
                    return false;
            foreach (var effect in entry.ModifyHPs)
                if (effect.Key != Priority.Zero)
                    return false;
            foreach (var effect in entry.RestoreHPs)
                if (effect.Key != Priority.Zero)
                    return false;

            return true;
        }
    }
}

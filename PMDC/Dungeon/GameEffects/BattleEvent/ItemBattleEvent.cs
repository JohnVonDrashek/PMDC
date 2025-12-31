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
    /// Battle events related to actions using or throwing items.
    /// </summary>


    /// <summary>
    /// Event that modifies the battle data if the character is hit by an item.
    /// When a thrown item hits this target, the item is destroyed and replaced with alternate battle data.
    /// </summary>
    [Serializable]
    public class ThrowItemDestroyEvent : BattleEvent
    {
        /// <summary>
        /// The replacement battle data to use when the item is destroyed on impact.
        /// Defines hit effects, damage, etc.
        /// </summary>
        public BattleData NewData;

        /// <inheritdoc/>
        public ThrowItemDestroyEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThrowItemDestroyEvent"/> class with specified battle data.
        /// </summary>
        /// <param name="moveData">The battle data to use when the item is destroyed.</param>
        public ThrowItemDestroyEvent(BattleData moveData)
        {
            NewData = moveData;
        }

        /// <inheritdoc/>
        protected ThrowItemDestroyEvent(ThrowItemDestroyEvent other)
        {
            NewData = new BattleData(other.NewData);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ThrowItemDestroyEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Throw)
            {
                ItemData entry = DataManager.Instance.GetItem(context.Item.ID);
                if (!entry.ItemStates.Contains<RecruitState>())
                {
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_INCINERATE").ToLocal(), context.Item.GetDisplayName()));

                    string id = context.Data.ID;
                    DataManager.DataType dataType = context.Data.DataType;
                    context.Data = new BattleData(NewData);
                    context.Data.ID = id;
                    context.Data.DataType = dataType;

                    context.GlobalContextStates.Set(new ItemDestroyed());
                }
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that prevents an item from dropping by setting the ItemDestroyed global context state.
    /// When applied, thrown items will not land on the ground after hitting a target.
    /// </summary>
    [Serializable]
    public class ThrowItemPreventDropEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new ThrowItemPreventDropEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Throw)
            {
                context.GlobalContextStates.Set(new ItemDestroyed());
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that removes the user's held item and sets it in the battle context.
    /// Used for moves that transfer the user's held item to a target.
    /// </summary>
    [Serializable]
    public class HeldItemMoveEvent : BattleEvent
    {
        /// <inheritdoc/>
        public HeldItemMoveEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new HeldItemMoveEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            bool attackerCannotDrop = false;
            if (!String.IsNullOrEmpty(context.User.EquippedItem.ID))
            {
                ItemData entry = DataManager.Instance.GetItem(context.User.EquippedItem.ID);
                attackerCannotDrop = entry.CannotDrop;
            }

            if (!String.IsNullOrEmpty(context.User.EquippedItem.ID) && !attackerCannotDrop)
            {
                context.Item = context.User.EquippedItem;
                yield return CoroutineManager.Instance.StartCoroutine(context.User.DequipItem());
            }
            else
            {
                context.CancelState.Cancel = true;
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_BESTOW_ITEM_FAIL").ToLocal(), context.User.GetDisplayName(false)));
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that causes the user to pass the held item to the target.
    /// Handles cases where the target already has an item or a full inventory.
    /// </summary>
    [Serializable]
    public class BestowItemEvent : BattleEvent
    {
        /// <inheritdoc/>
        public BestowItemEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new BestowItemEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (!String.IsNullOrEmpty(context.Target.EquippedItem.ID) && context.Target.CharStates.Contains<StickyHoldState>())
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_STICKY_HOLD").ToLocal(), context.Target.GetDisplayName(false)));

                //bestowed item slides off
                Loc endLoc = DungeonScene.Instance.MoveShotUntilBlocked(context.User, context.Target.CharLoc, context.User.CharDir, 2, Alignment.None, false, false);
                yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.DropItem(context.Item, endLoc, context.Target.CharLoc));
            }
            else if (!String.IsNullOrEmpty(context.Item.ID))
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_BESTOW_ITEM").ToLocal(), context.Target.GetDisplayName(false), context.Item.GetDisplayName()));

                if (!String.IsNullOrEmpty(context.Target.EquippedItem.ID))
                {
                    //held item slides off
                    InvItem heldItem = context.Target.EquippedItem;
                    yield return CoroutineManager.Instance.StartCoroutine(context.Target.DequipItem());
                    Loc endLoc = DungeonScene.Instance.MoveShotUntilBlocked(context.User, context.Target.CharLoc, context.User.CharDir, 2, Alignment.None, false, false);
                    yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.DropItem(heldItem, endLoc, context.Target.CharLoc));

                    //give the target the item
                    yield return CoroutineManager.Instance.StartCoroutine(context.Target.EquipItem(new InvItem(context.Item)));
                }
                else if (context.Target.MemberTeam is ExplorerTeam && ((ExplorerTeam)context.Target.MemberTeam).GetInvCount() >= ((ExplorerTeam)context.Target.MemberTeam).GetMaxInvSlots(ZoneManager.Instance.CurrentZone))
                {
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_INV_FULL").ToLocal(), context.Target.GetDisplayName(false), context.Item.GetDisplayName()));
                    //check if inventory is full.  If so, make the bestowed item slide off
                    Loc endLoc = context.Target.CharLoc + context.Target.CharDir.Reverse().GetLoc();
                    yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.DropItem(context.Item, endLoc, context.Target.CharLoc));

                }
                else
                {
                    //give the target the item
                    yield return CoroutineManager.Instance.StartCoroutine(context.Target.EquipItem(new InvItem(context.Item)));
                }

            }
        }
    }

    /// <summary>
    /// Event that causes the character to equip the item in the BattleContext.
    /// Used when a character catches a thrown item.
    /// </summary>
    [Serializable]
    public class CatchItemEvent : BattleEvent
    {
        /// <inheritdoc/>
        public CatchItemEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new CatchItemEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (!String.IsNullOrEmpty(context.Item.ID))
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_CATCH_ITEM").ToLocal(), context.Target.GetDisplayName(false), context.Item.GetDisplayName()));
                //give the target the item
                yield return CoroutineManager.Instance.StartCoroutine(context.Target.EquipItem(new InvItem(context.Item)));
            }
            yield break;
        }
    }


    /// <summary>
    /// Event that turns the target into an item from the current map's spawn pool.
    /// The target is removed from the map and replaced with a randomly spawned item.
    /// </summary>
    [Serializable]
    public class ItemizerEvent : BattleEvent
    {
        /// <inheritdoc/>
        public ItemizerEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ItemizerEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Target.Dead)
                yield break;

            if (ZoneManager.Instance.CurrentMap.ItemSpawns.CanPick)
            {
                //remove the target
                yield return CoroutineManager.Instance.StartCoroutine(context.Target.DieSilent());

                //drop an item
                InvItem item = ZoneManager.Instance.CurrentMap.ItemSpawns.Pick(DataManager.Instance.Save.Rand);
                yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.DropItem(item, context.Target.CharLoc));
            }
            else
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_NOTHING_HAPPENED").ToLocal()));
        }
    }

    /// <summary>
    /// Event that causes an item to land where the strike hitbox ended.
    /// Used to handle item drop behavior after a throw action misses or completes.
    /// </summary>
    [Serializable]
    public class LandItemEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new LandItemEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if ((context.GetContextStateInt<AttackHitTotal>(true, 0) == 0) && !context.GlobalContextStates.Contains<ItemDestroyed>())
            {
                foreach (Loc tile in context.StrikeLandTiles)
                    yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.DropItem(context.Item, tile));
            }
        }
    }

}


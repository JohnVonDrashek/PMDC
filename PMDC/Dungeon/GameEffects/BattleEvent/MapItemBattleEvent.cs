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
    /// Battle events that affect items on the dungeon map or deal with money.
    /// </summary>


    /// <summary>
    /// Event that destroys the item on the target tile.
    /// Can be blocked by terrain if BlockedByTerrain is true.
    /// </summary>
    [Serializable]
    public class RemoveItemEvent : BattleEvent
    {
        /// <summary>
        /// Whether the item isn't destroyed if the tile has a terrain.
        /// When true, items on terrain tiles (non-floor) will not be removed.
        /// </summary>
        public bool BlockedByTerrain;

        /// <inheritdoc/>
        public RemoveItemEvent()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveItemEvent"/> class with terrain blocking option.
        /// </summary>
        /// <param name="blockable">If true, items on terrain tiles will not be destroyed.</param>
        public RemoveItemEvent(bool blockable)
        {
            BlockedByTerrain = blockable;
        }

        /// <inheritdoc/>
        protected RemoveItemEvent(RemoveItemEvent other)
        {
            BlockedByTerrain = other.BlockedByTerrain;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new RemoveItemEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Tile tile = ZoneManager.Instance.CurrentMap.GetTile(context.TargetTile);
            if (tile == null)
                yield break;

            if (!BlockedByTerrain || tile.Data.ID == DataManager.Instance.GenFloor)
            {
                Loc wrappedLoc = ZoneManager.Instance.CurrentMap.WrapLoc(context.TargetTile);
                for (int ii = ZoneManager.Instance.CurrentMap.Items.Count - 1; ii >= 0; ii--)
                {
                    bool delete = true;
                    if (!ZoneManager.Instance.CurrentMap.Items[ii].IsMoney)
                    {
                        ItemData itemData = DataManager.Instance.GetItem(ZoneManager.Instance.CurrentMap.Items[ii].Value);
                        if (itemData.CannotDrop)
                            delete = false;
                    }
                    if (!delete)
                        continue;

                    if (ZoneManager.Instance.CurrentMap.Items[ii].TileLoc == wrappedLoc)
                        ZoneManager.Instance.CurrentMap.Items.RemoveAt(ii);
                }
            }
        }
    }


    /// <summary>
    /// Event that pulls unclaimed items on the floor to the user's location.
    /// Items within range that can be reached without crossing blocking terrain are moved near the user.
    /// </summary>
    [Serializable]
    public class TrawlEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new TrawlEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Dictionary<Loc, int> itemLocs = new Dictionary<Loc, int>();
            for (int ii = 0; ii < ZoneManager.Instance.CurrentMap.Items.Count; ii++)
                itemLocs.Add(ZoneManager.Instance.CurrentMap.Items[ii].TileLoc, ii);
            Loc?[] chosenItems = new Loc?[ZoneManager.Instance.CurrentMap.Items.Count];

            TerrainData.Mobility mobility = TerrainData.Mobility.Lava | TerrainData.Mobility.Water | TerrainData.Mobility.Abyss;

            Grid.AffectConnectedTiles(context.User.CharLoc - new Loc(CharAction.MAX_RANGE), new Loc(CharAction.MAX_RANGE * 2 + 1),
                (Loc effectLoc) =>
                {
                    if (ZoneManager.Instance.CurrentMap.TileBlocked(effectLoc, mobility))
                        return;

                    Loc wrapLoc = ZoneManager.Instance.CurrentMap.WrapLoc(effectLoc);
                    if (itemLocs.ContainsKey(wrapLoc))
                        chosenItems[itemLocs[wrapLoc]] = effectLoc;
                },
                (Loc testLoc) =>
                {
                    return ZoneManager.Instance.CurrentMap.TileBlocked(testLoc, true);
                },
                (Loc testLoc) =>
                {
                    return ZoneManager.Instance.CurrentMap.TileBlocked(testLoc, true, true);
                },
                context.User.CharLoc);

            for (int ii = ZoneManager.Instance.CurrentMap.Items.Count - 1; ii >= 0; ii--)
            {
                if (chosenItems[ii] != null)
                {
                    MapItem item = ZoneManager.Instance.CurrentMap.Items[ii];
                    Loc? newLoc = ZoneManager.Instance.CurrentMap.FindItemlessTile(context.User.CharLoc, CharAction.MAX_RANGE, true);
                    if (newLoc != null)
                    {
                        ItemAnim itemAnim = new ItemAnim(chosenItems[ii].Value * GraphicsManager.TileSize + new Loc(GraphicsManager.TileSize / 2), newLoc.Value * GraphicsManager.TileSize + new Loc(GraphicsManager.TileSize / 2), item.IsMoney ? GraphicsManager.MoneySprite : DataManager.Instance.GetItem(item.Value).Sprite, GraphicsManager.TileSize / 2, 1);
                        DungeonScene.Instance.CreateAnim(itemAnim, DrawLayer.Normal);
                        item.TileLoc = ZoneManager.Instance.CurrentMap.WrapLoc(newLoc.Value);
                    }
                    else
                        chosenItems[ii] = null;
                }
            }
            List<MapItem> unclaimed_items = new List<MapItem>();
            for (int ii = ZoneManager.Instance.CurrentMap.Items.Count - 1; ii >= 0; ii--)
            {
                if (chosenItems[ii] != null)
                {
                    MapItem item = ZoneManager.Instance.CurrentMap.Items[ii];
                    unclaimed_items.Add(item);
                    ZoneManager.Instance.CurrentMap.Items.RemoveAt(ii);
                }
            }
            DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_TRAWL").ToLocal(), context.User.GetDisplayName(false)));
            yield return new WaitForFrames(ItemAnim.ITEM_ACTION_TIME);
            foreach (MapItem item in unclaimed_items)
                ZoneManager.Instance.CurrentMap.Items.Add(item);
        }
    }



    /// <summary>
    /// Event that causes the target to drop money when defeated.
    /// The amount is based on the target's EXP yield and level.
    /// </summary>
    [Serializable]
    public class DefeatedMoneyEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new DefeatedMoneyEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            bool knockedOut = context.ContextStates.Contains<Knockout>();
            if (knockedOut)
            {
                MonsterData monsterData = DataManager.Instance.GetMonster(context.Target.BaseForm.Species);
                MonsterFormData monsterForm = (MonsterFormData)monsterData.Forms[context.Target.BaseForm.Form];
                int exp = expFormula(monsterForm.ExpYield, context.Target.Level);
                if (context.Target.MemberTeam is ExplorerTeam)
                    exp *= 2;
                int gainedMoney = exp;
                if (gainedMoney > 0)
                {
                    yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.DropMoney(gainedMoney, context.Target.CharLoc, context.Target.CharLoc));
                }
            }
        }

        /// <summary>
        /// Calculates EXP based on yield and level.
        /// </summary>
        /// <param name="expYield">The base EXP yield of the monster.</param>
        /// <param name="level">The level of the monster.</param>
        /// <returns>The calculated EXP value.</returns>
        private int expFormula(int expYield, int level)
        {
            return (int)((ulong)expYield * (ulong)level / 5) + 1;
        }
    }

    /// <summary>
    /// Event that causes the target to drop money based on the damage dealt.
    /// Money amount is calculated using: level * damage * Numerator / Denominator.
    /// </summary>
    [Serializable]
    public class DamageMoneyEvent : BattleEvent
    {

        /// <summary>
        /// The numerator for the money drop formula: level * damage * Numerator / Denominator.
        /// </summary>
        public int Numerator;

        /// <summary>
        /// The denominator for the money drop formula: level * damage * Numerator / Denominator.
        /// </summary>
        public int Denominator;

        /// <inheritdoc/>
        public DamageMoneyEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DamageMoneyEvent"/> class with specified multiplier.
        /// </summary>
        /// <param name="numerator">The numerator for money calculation.</param>
        /// <param name="denominator">The denominator for money calculation.</param>
        public DamageMoneyEvent(int numerator, int denominator) { Numerator = numerator; Denominator = denominator; }

        /// <inheritdoc/>
        protected DamageMoneyEvent(DamageMoneyEvent other)
        {
            Numerator = other.Numerator;
            Denominator = other.Denominator;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new DamageMoneyEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            int damage = context.GetContextStateInt<DamageDealt>(false, 0);
            int gainedMoney = damage * context.Target.Level * Numerator / Denominator;
            if (gainedMoney > 0)
            {
                Loc endLoc = DungeonScene.Instance.MoveShotUntilBlocked(context.User, context.Target.CharLoc, context.User.CharDir, 2, Alignment.None, false, false);
                yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.DropMoney(gainedMoney, endLoc, context.Target.CharLoc));
            }
        }
    }

    /// <summary>
    /// Event that causes the target to drop a portion of their money.
    /// The money lost is calculated as: Money * (1 - (Multiplier-1)/Multiplier).
    /// </summary>
    [Serializable]
    public class KnockMoneyEvent : BattleEvent
    {

        /// <summary>
        /// The money loss multiplier. Money lost = Money * (1 - (Multiplier-1)/Multiplier).
        /// A higher multiplier means less money is lost (e.g., Multiplier=4 means 25% is dropped).
        /// </summary>
        public int Multiplier;

        /// <inheritdoc/>
        public KnockMoneyEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="KnockMoneyEvent"/> class with specified multiplier.
        /// </summary>
        /// <param name="multiplier">The divisor for money calculation.</param>
        public KnockMoneyEvent(int multiplier) { Multiplier = multiplier; }

        /// <inheritdoc/>
        protected KnockMoneyEvent(KnockMoneyEvent other)
        {
            Multiplier = other.Multiplier;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new KnockMoneyEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Target.CharStates.Contains<StickyHoldState>())
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_STICKY_HOLD_MONEY").ToLocal(), context.Target.GetDisplayName(false)));
                yield break;
            }

            if (context.Target.MemberTeam is ExplorerTeam)
            {
                ExplorerTeam team = (ExplorerTeam)context.Target.MemberTeam;
                int moneyLost = team.Money - team.Money * (Multiplier - 1) / Multiplier;

                if (moneyLost > 0)
                {
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_KNOCK_MONEY").ToLocal(), context.Target.GetDisplayName(false), Text.FormatKey("MONEY_AMOUNT", moneyLost.ToString())));
                    team.LoseMoney(context.Target, moneyLost);
                    Loc endLoc = DungeonScene.Instance.MoveShotUntilBlocked(context.User, context.Target.CharLoc, context.User.CharDir, 2, Alignment.None, false, false);
                    yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.DropMoney(moneyLost, endLoc, context.Target.CharLoc));
                }
            }
        }
    }

}


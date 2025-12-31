using System;
using RogueElements;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Data;
using Newtonsoft.Json;
using RogueEssence.Dev;

namespace PMDC.Dungeon
{
    /// <summary>
    /// AI plan that causes the character to dance around a specific item until triggered.
    /// The character will circle the target item continuously until either:
    /// - A team member receives a specific status effect (indicating they were attacked)
    /// - The target item is no longer visible
    /// Used for creating ritual-like behavior around special items.
    /// </summary>
    [Serializable]
    public class CultDancePlan : AIPlan
    {
        /// <summary>
        /// The status effect ID that triggers the character to go berserk and stop dancing.
        /// When any team member has this status, the plan defers to the next behavior.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string StatusIndex;

        /// <summary>
        /// The item ID that the character will dance around.
        /// The character circles this item while it remains visible.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Item, false)]
        public string ItemIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="CultDancePlan"/> class with default values.
        /// </summary>
        public CultDancePlan() : base()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CultDancePlan"/> class.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior.</param>
        /// <param name="item">The item ID to dance around.</param>
        /// <param name="status">The status effect ID that triggers aggressive behavior.</param>
        public CultDancePlan(AIFlags iq, string item, string status) : base(iq)
        {
            StatusIndex = status;
            ItemIndex = item;
        }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        protected CultDancePlan(CultDancePlan other) : base(other) { StatusIndex = other.StatusIndex; ItemIndex = other.ItemIndex; }

        /// <inheritdoc/>
        public override BasePlan CreateNew() { return new CultDancePlan(this); }

        /// <summary>
        /// Evaluates whether to continue dancing or go berserk.
        /// </summary>
        /// <param name="controlledChar">The character being controlled by this AI.</param>
        /// <param name="preThink">Whether this is a pre-think evaluation.</param>
        /// <param name="rand">Random number generator for decision-making.</param>
        /// <returns>The action to take, or null to defer to the next plan (when triggered).</returns>
        public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
        {
            MapItem item;
            if (goBeserk(controlledChar, out item))
                return null;

            return dance(controlledChar, preThink, item);
        }

        /// <summary>
        /// Calculates the next dance movement around the target item.
        /// Attempts to move in the direction of the item, and if blocked, tries adjacent directions.
        /// Falls back to waiting if all directions around the item are blocked.
        /// </summary>
        /// <param name="controlledChar">The character performing the dance.</param>
        /// <param name="preThink">Whether this is a pre-think evaluation, affecting peer collision checking.</param>
        /// <param name="targetItem">The item to dance around.</param>
        /// <returns>A GameAction representing either a move in a valid direction or a wait action.</returns>
        private GameAction dance(Character controlledChar, bool preThink, MapItem targetItem)
        {
            //move in the approximate direction of the item
            Dir8 moveDir = (targetItem.TileLoc - controlledChar.CharLoc).ApproximateDir8();

            for (int ii = 0; ii < DirExt.DIR8_COUNT; ii++)
            {
                if (canWalk(controlledChar, !preThink, targetItem, moveDir))
                    return new GameAction(GameAction.ActionType.Move, moveDir, ((IQ & AIFlags.ItemGrabber) != AIFlags.None) ? 1 : 0);
                //if you can't or the tile IS the item, try a different direction
                moveDir = DirExt.AddAngles(moveDir, Dir8.DownRight);
            }

            //last resort, just stay there
            return new GameAction(GameAction.ActionType.Wait, Dir8.None);
        }

        /// <summary>
        /// Determines if the character can walk in a given direction while dancing.
        /// Checks for terrain obstacles, other characters, and ensures the movement circles the item rather than reaching it.
        /// </summary>
        /// <param name="controlledChar">The character attempting to move.</param>
        /// <param name="respectPeers">If true, blocks movement if other characters are in the way (unless at the target item location).</param>
        /// <param name="targetItem">The item being circled.</param>
        /// <param name="testDir">The direction to test for walkability.</param>
        /// <returns>True if the character can walk in the given direction while maintaining proper dance behavior; false otherwise.</returns>
        private bool canWalk(Character controlledChar, bool respectPeers, MapItem targetItem, Dir8 testDir)
        {
            Loc endLoc = controlledChar.CharLoc + testDir.GetLoc();

            //check to see if it's possible to move in this direction
            bool blocked = Grid.IsDirBlocked(controlledChar.CharLoc, testDir,
                (Loc testLoc) =>
                {
                    if (IsPathBlocked(controlledChar, testLoc))
                        return true;

                    if (ZoneManager.Instance.CurrentMap.WrapLoc(testLoc) != targetItem.TileLoc && respectPeers)
                    {
                        Character destChar = ZoneManager.Instance.CurrentMap.GetCharAtLoc(testLoc);
                        if (!canPassChar(controlledChar, destChar, true))
                            return true;
                    }

                    return false;
                },
                (Loc testLoc) =>
                {
                    return (ZoneManager.Instance.CurrentMap.TileBlocked(testLoc, controlledChar.Mobility, true));
                },
                1);

            //if that direction is good, send the command to move in that direction
            if (blocked)
                return false;

            //check to see if moving in this direction will get to the target item
            if (ZoneManager.Instance.CurrentMap.WrapLoc(endLoc) == targetItem.TileLoc)
                return false;

            //not blocked
            return true;
        }

        /// <summary>
        /// Checks if the trigger conditions are met to stop dancing, or if the target item is still visible.
        /// Returns true if the character should stop dancing (either triggered or item lost sight).
        /// </summary>
        /// <param name="controlledChar">The character to check.</param>
        /// <param name="seeItem">The target item if it's currently visible; null if out of sight.</param>
        /// <returns>True if the character should stop dancing (triggered or item out of sight); false if dancing should continue.</returns>
        private bool goBeserk(Character controlledChar, out MapItem seeItem)
        {
            seeItem = null;
            foreach (Character chara in controlledChar.MemberTeam.Players)
            {
                if (chara.GetStatusEffect(StatusIndex) != null)
                    return true;
            }

            Loc seen = Character.GetSightDims();
            Loc mapStart = controlledChar.CharLoc - seen;
            Loc mapSize = seen * 2 + new Loc(1);
            foreach (MapItem item in ZoneManager.Instance.CurrentMap.Items)
            {
                if (item.Value == ItemIndex && ZoneManager.Instance.CurrentMap.InBounds(new Rect(mapStart, mapSize), item.TileLoc))
                {
                    seeItem = item;
                    break;
                }
            }
            return seeItem == null;
        }
    }

}

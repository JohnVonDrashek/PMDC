using System;
using System.Collections.Generic;
using RogueElements;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Data;

namespace PMDC.Dungeon
{
    /// <summary>
    /// AI plan that causes the character to seek out and collect items on the floor.
    /// Extends <see cref="ExplorePlan"/> to use item locations as destinations instead of exits.
    /// The plan will only activate if the character has inventory space available.
    /// </summary>
    [Serializable]
    public class FindItemPlan : ExplorePlan
    {
        /// <summary>
        /// Whether to also seek out money drops, not just regular items.
        /// When <c>true</c>, money items will be included as valid destinations.
        /// When <c>false</c>, only non-money items will be targeted.
        /// </summary>
        public bool IncludeMoney;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindItemPlan"/> class.
        /// </summary>
        /// <param name="iq">The intelligence flags controlling AI behavior and decision-making.</param>
        /// <param name="includeMoney">Whether to also collect money drops in addition to regular items.</param>
        public FindItemPlan(AIFlags iq, bool includeMoney) : base(iq)
        {
            IncludeMoney = includeMoney;
        }

        /// <summary>
        /// Copy constructor for cloning an existing plan.
        /// Creates a new instance with the same configuration as the source plan.
        /// </summary>
        /// <param name="other">The plan to copy from.</param>
        protected FindItemPlan(FindItemPlan other) : base(other)
        {
            IncludeMoney = other.IncludeMoney;
        }

        /// <inheritdoc/>
        public override BasePlan CreateNew() { return new FindItemPlan(this); }

        /// <summary>
        /// Evaluates whether to seek items. Only activates if the character's inventory has available space.
        /// For <see cref="ExplorerTeam"/>, checks if inventory count is below maximum.
        /// For other team types, checks if no item is currently equipped.
        /// </summary>
        /// <param name="controlledChar">The character being controlled by this AI plan.</param>
        /// <param name="preThink">Whether this is a pre-think evaluation (performed before normal action.</param>
        /// <param name="rand">Random number generator for decision-making.</param>
        /// <returns>The action to take, or <c>null</c> if inventory is full or no items are within sight range.</returns>
        public override GameAction Think(Character controlledChar, bool preThink, IRandom rand)
        {
            if (controlledChar.MemberTeam is ExplorerTeam)
            {
                ExplorerTeam explorerTeam = (ExplorerTeam)controlledChar.MemberTeam;
                if (explorerTeam.GetInvCount() >= explorerTeam.GetMaxInvSlots(ZoneManager.Instance.CurrentZone))
                    return null;
            }
            else
            {
                //already holding an item
                if (!String.IsNullOrEmpty(controlledChar.EquippedItem.ID))
                    return null;
            }

            return base.Think(controlledChar, preThink, rand);
        }

        /// <summary>
        /// Gets item locations within sight range as exploration destinations.
        /// Filters out shop items (items with a price greater than 0) and optionally filters out money items.
        /// Only items visible within the character's sight radius are considered as valid destinations.
        /// </summary>
        /// <param name="controlledChar">The character being controlled, whose sight range is used as the search area.</param>
        /// <returns>A list of item tile locations within sight range that should be navigated toward, excluding shop items and optionally excluding money.</returns>
        protected override List<Loc> GetDestinations(Character controlledChar)
        {
            //get all tiles that are within the border of sight range, or within the border of the screen
            Loc seen = Character.GetSightDims();
            Loc mapStart = controlledChar.CharLoc - seen;
            Loc mapSize = seen * 2 + new Loc(1);

            List<Loc> loc_list = new List<Loc>();
            //currently, CPU sight cheats by knowing items up to the bounds, instead of individual tiles at the border of FOV.
            //fix later
            foreach (MapItem item in ZoneManager.Instance.CurrentMap.Items)
            {
                if (item.IsMoney)
                {
                    if (!IncludeMoney)
                        continue;
                }
                else
                {
                    if (item.Price > 0)
                        continue;
                }
                if (ZoneManager.Instance.CurrentMap.InBounds(new Rect(mapStart, mapSize), item.TileLoc))
                    TryAddDest(controlledChar, loc_list, item.TileLoc);
            }
            return loc_list;
        }
    }
}

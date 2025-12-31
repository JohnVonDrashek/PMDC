using System;
using RogueEssence.Data;
using RogueEssence.Dungeon;
using PMDC.Dungeon;

namespace PMDC.Data
{
    /// <summary>
    /// Universal effect handler for active game effects that need range calculations.
    /// Extends the base universal effect to provide AI-aware range modifications
    /// based on character passives and battlefield conditions.
    /// </summary>
    public class UniversalActiveEffect : UniversalBaseEffect
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UniversalActiveEffect"/> class.
        /// </summary>
        public UniversalActiveEffect() { }

        /// <summary>
        /// Attempts to extract a battle event of the specified type from a potentially wrapped event.
        /// Handles conditional events like <see cref="FamilyBattleEvent"/> by checking conditions and unwrapping.
        /// If the effect is already of type T, it is returned directly. If it is a FamilyBattleEvent,
        /// the method checks if the controlled character's species is in the family members list and
        /// recursively unwraps the base event.
        /// </summary>
        /// <typeparam name="T">The type of battle event to extract. Must be a <see cref="BattleEvent"/> subclass.</typeparam>
        /// <param name="controlledChar">The character being controlled, used to check family membership.</param>
        /// <param name="passiveContext">The passive context containing the effect and its data.</param>
        /// <param name="effect">The effect to unwrap and check for the desired type.</param>
        /// <returns>The extracted event of type T, or <c>null</c> if the effect is not of the requested type or conditions are not met.</returns>
        private T getConditionalEvent<T>(Character controlledChar, PassiveContext passiveContext, BattleEvent effect) where T : BattleEvent
        {
            if (effect is T)
                return (T)effect;

            //TODO: add other conditions
            FamilyBattleEvent familyEffect = effect as FamilyBattleEvent;
            if (familyEffect != null)
            {
                ItemData entry = (ItemData)passiveContext.Passive.GetData();
                FamilyState family;
                if (!entry.ItemStates.TryGet<FamilyState>(out family))
                    return null;
                if (family.Members.Contains(controlledChar.BaseForm.Species))
                    return getConditionalEvent<T>(controlledChar, passiveContext, familyEffect.BaseEvent);
            }

            return null;
        }

        /// <summary>
        /// Calculates the total range modifier for a skill based on all active character passives.
        /// Iterates through all passive effects on the character and checks for the following event types:
        /// <see cref="AddRangeEvent"/>, <see cref="CategoryAddRangeEvent"/>, <see cref="WeatherAddRangeEvent"/>,
        /// and <see cref="ElementAddRangeEvent"/>. Range modifications are accumulated and the final result is
        /// clamped to the range [-3, 3] to prevent excessive range modifications.
        /// </summary>
        /// <param name="character">The character using the skill, whose passives are evaluated.</param>
        /// <param name="entry">Reference to the skill data being used for range calculation.</param>
        /// <returns>The total range modifier, clamped between -3 and 3 inclusive.</returns>
        public override int GetRange(Character character, ref SkillData entry)
        {
            int rangeMod = 0;
            
            //check for passives that modify range; NOTE: specialized AI code!
            foreach (PassiveContext passive in character.IteratePassives(GameEventPriority.USER_PORT_PRIORITY))
            {
                foreach (BattleEvent effect in passive.EventData.OnActions.EnumerateInOrder())
                {
                    AddRangeEvent addRangeEvent = getConditionalEvent<AddRangeEvent>(character, passive, effect);
                    if (addRangeEvent != null)
                    {
                        rangeMod += addRangeEvent.Range;
                        continue;
                    }

                    CategoryAddRangeEvent categoryRangeEvent = getConditionalEvent<CategoryAddRangeEvent>(character, passive, effect);
                    if (categoryRangeEvent != null)
                    {
                        if (entry.Data.Category == categoryRangeEvent.Category)
                            rangeMod += categoryRangeEvent.Range;
                        continue;
                    }

                    WeatherAddRangeEvent weatherRangeEvent = getConditionalEvent<WeatherAddRangeEvent>(character, passive, effect);
                    if (weatherRangeEvent != null)
                    {
                        if (ZoneManager.Instance.CurrentMap.Status.ContainsKey(weatherRangeEvent.WeatherID))
                            rangeMod += weatherRangeEvent.Range;
                        continue;
                    }
                    
                    ElementAddRangeEvent elementRangeEvent = getConditionalEvent<ElementAddRangeEvent>(character, passive, effect);
                    if (elementRangeEvent != null)
                    {
                        if (elementRangeEvent.Elements.Contains(character.Element1) || elementRangeEvent.Elements.Contains(character.Element2))
                        {
                            rangeMod += elementRangeEvent.Range;
                        }
                        continue;
                    }
                }
            }

            rangeMod = Math.Min(Math.Max(-3, rangeMod), 3);
            return rangeMod;
        }
    }
}

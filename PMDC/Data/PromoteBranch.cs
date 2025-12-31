using System;
using System.Collections.Generic;
using RogueEssence.Dungeon;
using System.Xml.Serialization;
using RogueElements;
using RogueEssence;
using RogueEssence.Data;
using RogueEssence.Dev;
using Newtonsoft.Json;
using PMDC.Dungeon;
using System.Runtime.Serialization;

namespace PMDC.Data
{
    /// <summary>
    /// Evolution requirement based on character level.
    /// Character must reach or exceed the specified level to evolve.
    /// </summary>
    [Serializable]
    public class EvoLevel : PromoteDetail
    {
        /// <summary>
        /// The minimum level required to trigger this evolution.
        /// </summary>
        public int Level;

        /// <summary>
        /// Gets a localized string describing the level requirement for this evolution.
        /// </summary>
        /// <returns>A formatted string describing the minimum level required.</returns>
        /// <inheritdoc/>
        public override string GetReqString() { return Text.FormatGrammar(new StringKey("EVO_REQ_LEVEL").ToLocal(), Level); }

        /// <summary>
        /// Determines whether the character meets the level requirement to evolve.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context.</param>
        /// <returns>true if the character's level is greater than or equal to the required level; otherwise, false.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            return character.Level >= Level;
        }
    }

    /// <summary>
    /// Evolution requirement based on holding or possessing a specific item.
    /// The item is consumed upon evolution.
    /// </summary>
    [Serializable]
    public class EvoItem : PromoteDetail
    {
        /// <summary>
        /// The item ID required for this evolution.
        /// </summary>
        [JsonConverter(typeof(ItemConverter))]
        [DataType(0, DataManager.DataType.Item, false)]
        public string ItemNum;

        /// <summary>
        /// Gets the ID of the item required for this evolution.
        /// </summary>
        /// <param name="character">The character being checked.</param>
        /// <returns>The required item ID.</returns>
        /// <inheritdoc/>
        public override string GetReqItem(Character character) { return ItemNum; }

        /// <summary>
        /// Gets a localized string describing the item requirement for this evolution.
        /// </summary>
        /// <returns>A formatted string describing the required item with its colored name.</returns>
        /// <inheritdoc/>
        public override string GetReqString() { return Text.FormatGrammar(new StringKey("EVO_REQ_ITEM").ToLocal(), ((ItemEntrySummary)DataManager.Instance.DataIndices[DataManager.DataType.Item].Get(ItemNum)).GetColoredName()); }

        /// <summary>
        /// Determines whether the character is holding or possessing the required item.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context. Cursed items do not count in dungeons.</param>
        /// <returns>true if the character has the required item; otherwise, false.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            if (inDungeon)
            {
                if (character.EquippedItem.ID == ItemNum && !character.EquippedItem.Cursed)
                    return true;
            }
            else
            {
                if (character.EquippedItem.ID == ItemNum)
                    return true;

                foreach (InvItem item in character.MemberTeam.EnumerateInv())
                {
                    if (item.ID == ItemNum)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Removes the required item from the character upon evolution.
        /// </summary>
        /// <param name="character">The character that is evolving.</param>
        /// <param name="inDungeon">Whether the evolution is occurring in a dungeon context.</param>
        /// <inheritdoc/>
        public override void OnPromote(Character character, bool inDungeon)
        {
            if (inDungeon)
            {
                if (character.EquippedItem.ID == ItemNum && !character.EquippedItem.Cursed)
                    character.SilentDequipItem();
            }
            else
            {
                if (character.EquippedItem.ID == ItemNum)
                    character.SilentDequipItem();
                else
                {
                    for (int ii = 0; ii < character.MemberTeam.GetInvCount(); ii++)
                    {
                        if (character.MemberTeam.GetInv(ii).ID == ItemNum)
                        {
                            character.MemberTeam.RemoveFromInv(ii);
                            break;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Evolution requirement based on having evolved allies in the party.
    /// Requires a minimum number of team members that have evolved forms.
    /// </summary>
    [Serializable]
    public class EvoFriendship : PromoteDetail
    {
        /// <summary>
        /// The minimum number of evolved allies required in the party.
        /// </summary>
        public int Allies;

        /// <summary>
        /// Initializes a new instance of the EvoFriendship class.
        /// </summary>
        public EvoFriendship() { }

        /// <summary>
        /// Initializes a new instance of the EvoFriendship class with the specified ally count.
        /// </summary>
        /// <param name="allies">The required number of evolved allies.</param>
        public EvoFriendship(int allies)
        {
            Allies = allies;
        }

        /// <summary>
        /// Gets a localized string describing the evolved ally requirement for this evolution.
        /// </summary>
        /// <returns>A formatted string describing the minimum number of evolved allies required.</returns>
        /// <inheritdoc/>
        public override string GetReqString() { return Text.FormatGrammar(new StringKey("EVO_REQ_ALLIES").ToLocal(), Allies); }

        /// <summary>
        /// Determines whether the character's party contains the required number of evolved allies.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context.</param>
        /// <returns>true if the party contains at least the required number of evolved allies; otherwise, false.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            ExplorerTeam team = character.MemberTeam as ExplorerTeam;

            if (team == null)
                return false;

            int count = 0;
            foreach (Character ally in character.MemberTeam.Players)
            {
                if (ally != character)
                {
                    MonsterData data = DataManager.Instance.GetMonster(ally.BaseForm.Species);
                    if (!String.IsNullOrEmpty(data.PromoteFrom))
                        count++;
                }
            }

            return count >= Allies;
        }
    }

    /// <summary>
    /// Evolution requirement based on the current time of day.
    /// Character can only evolve during specific time periods.
    /// </summary>
    [Serializable]
    public class EvoTime : PromoteDetail
    {
        /// <summary>
        /// The time of day required for this evolution.
        /// </summary>
        public TimeOfDay Time;

        /// <summary>
        /// Gets a localized string describing the time of day requirement for this evolution.
        /// </summary>
        /// <returns>A formatted string describing the required time of day.</returns>
        /// <inheritdoc/>
        public override string GetReqString() { return Text.FormatGrammar(new StringKey("EVO_REQ_TIME").ToLocal(), Time.ToLocal()); }

        /// <summary>
        /// Determines whether the current time of day matches the evolution requirement.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context.</param>
        /// <returns>true if the current time matches the required time period; otherwise, false.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            return DataManager.Instance.Save.Time != TimeOfDay.Unknown && (DataManager.Instance.Save.Time == Time || (TimeOfDay)(((int)DataManager.Instance.Save.Time + 1) % 4) == Time);
        }
    }

    /// <summary>
    /// Evolution requirement based on active weather conditions.
    /// Character can only evolve when a specific map status (weather) is active.
    /// </summary>
    [Serializable]
    public class EvoWeather : PromoteDetail
    {
        /// <summary>
        /// The weather/map status ID required for this evolution.
        /// </summary>
        [JsonConverter(typeof(MapStatusConverter))]
        [DataType(0, DataManager.DataType.MapStatus, false)]
        public string Weather;

        /// <summary>
        /// Gets a localized string describing the weather requirement for this evolution.
        /// </summary>
        /// <returns>A formatted string describing the required weather/map status with its colored name.</returns>
        /// <inheritdoc/>
        public override string GetReqString() { return Text.FormatGrammar(new StringKey("EVO_REQ_MAP").ToLocal(), DataManager.Instance.GetMapStatus(Weather).GetColoredName()); }

        /// <summary>
        /// Determines whether the required weather/map status is currently active on the map.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context. Returns false if not in a dungeon.</param>
        /// <returns>true if the required weather is active on the current map; otherwise, false.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            if (!inDungeon)
                return false;

            return ZoneManager.Instance.CurrentMap.Status.ContainsKey(Weather);
        }
    }

    /// <summary>
    /// Evolution requirement based on the comparison between Attack and Defense stats.
    /// Determines evolution branch based on which stat is higher, lower, or if they are equal.
    /// </summary>
    [Serializable]
    public class EvoStats : PromoteDetail
    {
        /// <summary>
        /// The required comparison result: positive if Attack must be greater than Defense,
        /// negative if Attack must be less than Defense, zero if they must be equal.
        /// </summary>
        public int AtkDefComparison;

        /// <summary>
        /// Gets a localized string describing the stat comparison requirement for this evolution.
        /// </summary>
        /// <returns>A formatted string describing whether Attack must be greater than, less than, or equal to Defense.</returns>
        /// <inheritdoc/>
        public override string GetReqString()
        {
            if (AtkDefComparison > 0)
                return Text.FormatGrammar(new StringKey("EVO_REQ_ATK_DEF_GREATER").ToLocal());
            else if (AtkDefComparison < 0)
                return Text.FormatGrammar(new StringKey("EVO_REQ_ATK_DEF_LESS").ToLocal());
            else
                return Text.FormatGrammar(new StringKey("EVO_REQ_ATK_DEF_EQUAL").ToLocal());
        }

        /// <summary>
        /// Determines whether the character's Attack and Defense stats match the required comparison.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context.</param>
        /// <returns>true if the character's stat comparison matches the requirement; otherwise, false.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            return character.BaseAtk.CompareTo(character.BaseDef) == AtkDefComparison;
        }
    }

    /// <summary>
    /// Evolution requirement based on accumulating critical hit stacks.
    /// Character must have landed enough critical hits (tracked via status effect).
    /// </summary>
    [Serializable]
    public class EvoCrits : PromoteDetail
    {
        /// <summary>
        /// The status effect ID that tracks critical hit count.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string CritStatus;

        /// <summary>
        /// The minimum number of critical hit stacks required.
        /// </summary>
        public int Stack;

        /// <summary>
        /// Gets a localized string describing the critical hit requirement for this evolution.
        /// </summary>
        /// <returns>A formatted string describing the minimum number of critical hits required.</returns>
        /// <inheritdoc/>
        public override string GetReqString()
        {
            return Text.FormatGrammar(new StringKey("EVO_REQ_CRITS").ToLocal(), Stack);
        }

        /// <summary>
        /// Determines whether the character has accumulated enough critical hit stacks.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context.</param>
        /// <returns>true if the character has at least the required number of critical hit stacks; otherwise, false.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            StatusEffect status;
            if (character.StatusEffects.TryGetValue(CritStatus, out status))
            {
                StackState state = status.StatusStates.Get<StackState>();
                if (state.Stack >= Stack)
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Evolution requirement based on having a positive stat boost status.
    /// Character must have the specified stat boost status with positive stacks.
    /// </summary>
    [Serializable]
    public class EvoStatBoost : PromoteDetail
    {
        /// <summary>
        /// The stat boost status effect ID that must be active.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string StatBoostStatus;

        /// <summary>
        /// Gets a localized string describing the stat boost requirement for this evolution.
        /// </summary>
        /// <returns>A formatted string describing the required stat boost status with its colored name.</returns>
        /// <inheritdoc/>
        public override string GetReqString()
        {
            return Text.FormatGrammar(new StringKey("EVO_REQ_STAT_BOOST").ToLocal(), DataManager.Instance.GetStatus(StatBoostStatus).GetColoredName());
        }

        /// <summary>
        /// Determines whether the character has the required positive stat boost status.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context.</param>
        /// <returns>true if the character has the stat boost with positive stacks; otherwise, false.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            StatusEffect status;
            if (character.StatusEffects.TryGetValue(StatBoostStatus, out status))
            {
                StackState state = status.StatusStates.Get<StackState>();
                if (state.Stack > 0)
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Evolution requirement based on knowing a specific move/skill.
    /// Character must have the specified move in their moveset.
    /// </summary>
    [Serializable]
    public class EvoMove : PromoteDetail
    {
        /// <summary>
        /// The skill ID that must be known to evolve.
        /// </summary>
        [JsonConverter(typeof(SkillConverter))]
        [DataType(0, DataManager.DataType.Skill, false)]
        public string MoveNum;

        /// <summary>
        /// Gets a localized string describing the skill requirement for this evolution.
        /// </summary>
        /// <returns>A formatted string describing the required skill with its colored name.</returns>
        /// <inheritdoc/>
        public override string GetReqString() { return Text.FormatGrammar(new StringKey("EVO_REQ_SKILL").ToLocal(), DataManager.Instance.GetSkill(MoveNum).GetColoredName()); }

        /// <summary>
        /// Determines whether the character knows the required move.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context.</param>
        /// <returns>true if the character has the required move in their moveset; otherwise, false.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            foreach (SlotSkill move in character.BaseSkills)
            {
                if (move.SkillNum == MoveNum)
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Evolution requirement based on knowing a move of a specific elemental type.
    /// Character must have at least one move matching the required element.
    /// </summary>
    [Serializable]
    public class EvoMoveElement : PromoteDetail
    {
        /// <summary>
        /// The element type ID that at least one known move must have.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string MoveElement;

        /// <summary>
        /// Initializes a new instance of the EvoMoveElement class.
        /// </summary>
        public EvoMoveElement() { MoveElement = ""; }

        /// <summary>
        /// Gets a localized string describing the move element requirement for this evolution.
        /// </summary>
        /// <returns>A formatted string describing the required element type with its colored name.</returns>
        /// <inheritdoc/>
        public override string GetReqString()
        {
            ElementData elementEntry = DataManager.Instance.GetElement(MoveElement);
            return Text.FormatGrammar(new StringKey("EVO_REQ_SKILL_ELEMENT").ToLocal(), elementEntry.GetColoredName());
        }

        /// <summary>
        /// Determines whether the character knows at least one move of the required element.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context.</param>
        /// <returns>true if the character has a move with the required element; otherwise, false.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            foreach (SlotSkill move in character.BaseSkills)
            {
                if (!String.IsNullOrEmpty(move.SkillNum))
                {
                    SkillData data = DataManager.Instance.GetSkill(move.SkillNum);
                    if (data.Data.Element == MoveElement)
                        return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Evolution requirement based on using a specific move multiple times.
    /// Tracks move usage via status effects and requires repeated use.
    /// </summary>
    [Serializable]
    public class EvoMoveUse : PromoteDetail
    {
        /// <summary>
        /// Status effect ID that tracks the last move used.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string LastMoveStatusID;

        /// <summary>
        /// Status effect ID that tracks consecutive move usage count.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string MoveRepeatStatusID;

        /// <summary>
        /// The skill ID that must be used repeatedly.
        /// </summary>
        [JsonConverter(typeof(SkillConverter))]
        [DataType(0, DataManager.DataType.Skill, false)]
        public string MoveNum;

        /// <summary>
        /// The number of times the move must be used consecutively.
        /// </summary>
        public int Amount;

        /// <summary>
        /// Gets a localized string describing the move usage requirement for this evolution.
        /// </summary>
        /// <returns>A formatted string describing the required move and usage count with the skill's colored name.</returns>
        /// <inheritdoc/>
        public override string GetReqString() { return Text.FormatGrammar(new StringKey("EVO_REQ_SKILL_USE").ToLocal(), DataManager.Instance.GetSkill(MoveNum).GetColoredName(), Amount); }

        /// <summary>
        /// Determines whether the character has used the required move enough consecutive times.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context.</param>
        /// <returns>true if the character has used the required move the specified number of consecutive times; otherwise, false.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            StatusEffect moveStatus = character.GetStatusEffect(LastMoveStatusID);
            StatusEffect repeatStatus = character.GetStatusEffect(MoveRepeatStatusID);
            if (moveStatus == null || repeatStatus == null)
                return false;
            if (moveStatus.StatusStates.GetWithDefault<IDState>().ID != MoveNum)
                return false;
            return repeatStatus.StatusStates.GetWithDefault<CountState>().Count >= Amount;
        }
    }

    /// <summary>
    /// Evolution requirement based on defeating a number of enemies.
    /// Currently not fully implemented.
    /// </summary>
    [Serializable]
    public class EvoKillCount : PromoteDetail
    {
        /// <summary>
        /// The number of enemies that must be defeated.
        /// </summary>
        public int Amount;

        /// <summary>
        /// Gets a localized string describing the kill count requirement for this evolution.
        /// Currently returns an empty string as this feature is not fully implemented.
        /// </summary>
        /// <returns>An empty string.</returns>
        /// <inheritdoc/>
        public override string GetReqString() { return ""; }

        /// <summary>
        /// Determines whether the character has defeated the required number of enemies.
        /// Currently always returns false as this feature is not fully implemented.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context.</param>
        /// <returns>Always returns false until this feature is fully implemented.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            //foreach (SlotSkill move in character.BaseSkills)
            //{
            //    if (move.SkillNum == MoveNum)
            //        return true;
            //}
            return false;
        }
    }

    /// <summary>
    /// Evolution requirement based on collecting a certain amount of money.
    /// Currently not fully implemented.
    /// </summary>
    [Serializable]
    public class EvoMoney : PromoteDetail
    {
        /// <summary>
        /// The amount of money required.
        /// </summary>
        public int Amount;

        /// <summary>
        /// Gets a localized string describing the money requirement for this evolution.
        /// Currently returns an empty string as this feature is not fully implemented.
        /// </summary>
        /// <returns>An empty string.</returns>
        /// <inheritdoc/>
        public override string GetReqString() { return ""; }

        /// <summary>
        /// Determines whether the character has collected the required amount of money.
        /// Currently always returns false as this feature is not fully implemented.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context.</param>
        /// <returns>Always returns false until this feature is fully implemented.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            //foreach (SlotSkill move in character.BaseSkills)
            //{
            //    if (move.SkillNum == MoveNum)
            //        return true;
            //}
            return false;
        }
    }

    /// <summary>
    /// Evolution requirement based on walking a certain distance.
    /// Currently not fully implemented.
    /// </summary>
    [Serializable]
    public class EvoWalk : PromoteDetail
    {
        /// <summary>
        /// Gets a localized string describing the walking distance requirement for this evolution.
        /// Currently returns an empty string as this feature is not fully implemented.
        /// </summary>
        /// <returns>An empty string.</returns>
        /// <inheritdoc/>
        public override string GetReqString() { return ""; }

        /// <summary>
        /// Determines whether the character has walked the required distance.
        /// Currently always returns false as this feature is not fully implemented.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context.</param>
        /// <returns>Always returns false until this feature is fully implemented.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            //foreach (SlotSkill move in character.BaseSkills)
            //{
            //    if (move.SkillNum == MoveNum)
            //        return true;
            //}
            return false;
        }
    }

    /// <summary>
    /// Evolution requirement based on completing rescue missions.
    /// Currently not fully implemented.
    /// </summary>
    [Serializable]
    public class EvoRescue : PromoteDetail
    {
        /// <summary>
        /// Gets a localized string describing the rescue mission requirement for this evolution.
        /// Currently returns an empty string as this feature is not fully implemented.
        /// </summary>
        /// <returns>An empty string.</returns>
        /// <inheritdoc/>
        public override string GetReqString() { return ""; }

        /// <summary>
        /// Determines whether the character has completed the required rescue missions.
        /// Currently always returns false as this feature is not fully implemented.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context.</param>
        /// <returns>Always returns false until this feature is fully implemented.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            //foreach (SlotSkill move in character.BaseSkills)
            //{
            //    if (move.SkillNum == MoveNum)
            //        return true;
            //}
            return false;
        }
    }

    /// <summary>
    /// Evolution requirement based on taking a certain amount of damage.
    /// Currently not fully implemented.
    /// </summary>
    [Serializable]
    public class EvoTookDamage : PromoteDetail
    {
        /// <summary>
        /// The amount of damage that must be taken.
        /// </summary>
        public int Amount;

        /// <summary>
        /// Gets a localized string describing the damage requirement for this evolution.
        /// Currently returns an empty string as this feature is not fully implemented.
        /// </summary>
        /// <returns>An empty string.</returns>
        /// <inheritdoc/>
        public override string GetReqString() { return ""; }

        /// <summary>
        /// Determines whether the character has taken the required amount of damage.
        /// Currently always returns false as this feature is not fully implemented.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context.</param>
        /// <returns>Always returns false until this feature is fully implemented.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            //foreach (SlotSkill move in character.BaseSkills)
            //{
            //    if (move.SkillNum == MoveNum)
            //        return true;
            //}
            return false;
        }
    }

    /// <summary>
    /// Evolution requirement based on the character's current form.
    /// Only characters in one of the specified forms can evolve.
    /// This is a hard requirement that cannot be bypassed.
    /// </summary>
    [Serializable]
    public class EvoForm : PromoteDetail
    {
        /// <summary>
        /// Legacy field for single form requirement. Use ReqForms instead.
        /// </summary>
        public int ReqForm;

        /// <summary>
        /// Set of form indices that are valid for this evolution.
        /// </summary>
        public HashSet<int> ReqForms;

        /// <summary>
        /// Initializes a new instance of the EvoForm class.
        /// </summary>
        public EvoForm()
        {
            ReqForms = new HashSet<int>();
        }

        /// <summary>
        /// Initializes a new instance of the EvoForm class with the specified forms.
        /// </summary>
        /// <param name="forms">The form indices that are valid for this evolution.</param>
        public EvoForm(params int[] forms)
        {
            ReqForms = new HashSet<int>();
            foreach(int form in forms)
                ReqForms.Add(form);
        }

        /// <summary>
        /// Indicates that this is a hard requirement that cannot be bypassed.
        /// </summary>
        /// <returns>Always returns true.</returns>
        /// <inheritdoc/>
        public override bool IsHardReq() { return true; }

        /// <summary>
        /// Determines whether the character's current form is in the set of allowed forms.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context.</param>
        /// <returns>true if the character's current form is one of the allowed forms; otherwise, false.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            return ReqForms.Contains(character.BaseForm.Form);
        }

        /// <summary>
        /// Handles deserialization from older save formats.
        /// Migrates legacy ReqForm field to ReqForms set.
        /// </summary>
        /// <param name="context">The streaming context for deserialization.</param>
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            //TODO: remove on v1.1
            if (Serializer.OldVersion < new Version(0, 7, 22))
            {
                ReqForms = new HashSet<int>();
                ReqForms.Add(ReqForm);
            }
        }
    }

    /// <summary>
    /// Evolution requirement based on the character's gender.
    /// Only characters of the specified gender can evolve.
    /// This is a hard requirement that cannot be bypassed.
    /// </summary>
    [Serializable]
    public class EvoGender : PromoteDetail
    {
        /// <summary>
        /// The gender required to trigger this evolution.
        /// </summary>
        public Gender ReqGender;

        /// <summary>
        /// Initializes a new instance of the EvoGender class.
        /// </summary>
        public EvoGender() { }

        /// <summary>
        /// Initializes a new instance of the EvoGender class with the specified gender.
        /// </summary>
        /// <param name="gender">The required gender.</param>
        public EvoGender(Gender gender)
        {
            ReqGender = gender;
        }

        /// <summary>
        /// Indicates that this is a hard requirement that cannot be bypassed.
        /// </summary>
        /// <returns>Always returns true.</returns>
        /// <inheritdoc/>
        public override bool IsHardReq() { return true; }

        /// <summary>
        /// Determines whether the character's gender matches the required gender.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context.</param>
        /// <returns>true if the character's gender matches the required gender; otherwise, false.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            return character.BaseForm.Gender == ReqGender;
        }
    }

    /// <summary>
    /// Evolution requirement based on the character's hunger level.
    /// Requires either very low (hungry) or very high (full) hunger.
    /// </summary>
    [Serializable]
    public class EvoHunger : PromoteDetail
    {
        /// <summary>
        /// If true, requires the character to be hungry (0 fullness).
        /// If false, requires the character to be very full (110+ fullness).
        /// </summary>
        public bool Hungry;

        /// <summary>
        /// Initializes a new instance of the EvoHunger class.
        /// </summary>
        public EvoHunger() { }

        /// <summary>
        /// Gets a localized string describing the hunger requirement for this evolution.
        /// </summary>
        /// <returns>A formatted string describing whether the character must be hungry or very full.</returns>
        /// <inheritdoc/>
        public override string GetReqString()
        {
            if (Hungry)
                return Text.FormatGrammar(new StringKey("EVO_REQ_HUNGER_LOW").ToLocal());
            else
                return Text.FormatGrammar(new StringKey("EVO_REQ_HUNGER_HIGH").ToLocal());
        }

        /// <summary>
        /// Determines whether the character's hunger level matches the requirement.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context.</param>
        /// <returns>true if the character's hunger level matches the requirement (either 0 or 110+); otherwise, false.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            return Hungry ? (character.Fullness == 0) : (character.Fullness >= 110);
        }
    }

    /// <summary>
    /// Evolution requirement based on standing on a tile with a specific element.
    /// Requires the current map to have the specified elemental type.
    /// </summary>
    [Serializable]
    public class EvoLocation : PromoteDetail
    {
        /// <summary>
        /// The element type ID required for the current map/tile.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string TileElement;

        /// <summary>
        /// Initializes a new instance of the EvoLocation class.
        /// </summary>
        public EvoLocation() { TileElement = ""; }

        /// <summary>
        /// Initializes a new instance of the EvoLocation class with the specified element.
        /// </summary>
        /// <param name="element">The required tile element ID.</param>
        public EvoLocation(string element)
        {
            TileElement = element;
        }

        /// <summary>
        /// Gets a localized string describing the location/element requirement for this evolution.
        /// </summary>
        /// <returns>A formatted string describing the required tile element with its colored name.</returns>
        /// <inheritdoc/>
        public override string GetReqString()
        {
            ElementData elementEntry = DataManager.Instance.GetElement(TileElement);
            return Text.FormatGrammar(new StringKey("EVO_REQ_TILE_ELEMENT").ToLocal(), elementEntry.GetColoredName());
        }

        /// <summary>
        /// Determines whether the current map's element matches the requirement.
        /// This check only applies when the character is in a dungeon.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context. Returns false if not in a dungeon.</param>
        /// <returns>true if the current map's element matches the required element; otherwise, false.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            if (!inDungeon)
                return false;

            if (GameManager.Instance.CurrentScene == DungeonScene.Instance)
                return ZoneManager.Instance.CurrentMap.Element == TileElement;
            return false;
        }
    }

    /// <summary>
    /// Evolution requirement based on being at the start of a dungeon map.
    /// Condition: if in a dungeon map and a turn has not passed.
    /// </summary>
    [Serializable]
    public class EvoMapStart : PromoteDetail
    {
        /// <summary>
        /// Initializes a new instance of the EvoMapStart class.
        /// </summary>
        public EvoMapStart() { }

        /// <summary>
        /// Gets a localized string describing the map start requirement for this evolution.
        /// Currently returns an empty string.
        /// </summary>
        /// <returns>An empty string.</returns>
        /// <inheritdoc/>
        public override string GetReqString() { return ""; }

        /// <summary>
        /// Determines whether the character is at the start of the current dungeon map.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context. Returns false if not in a dungeon.</param>
        /// <returns>true if the character is in a dungeon and no turns have passed on the current map; otherwise, false.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            if (!inDungeon)
                return false;

            if (GameManager.Instance.CurrentScene == DungeonScene.Instance)
                return ZoneManager.Instance.CurrentMap.MapTurns == 0;
            return false;
        }
    }

    /// <summary>
    /// Evolution requirement based on having a specific species as a party member.
    /// Requires a certain number of the specified species in the team.
    /// </summary>
    [Serializable]
    public class EvoPartner : PromoteDetail
    {
        /// <summary>
        /// The species ID that must be present in the party.
        /// </summary>
        [JsonConverter(typeof(MonsterConverter))]
        [DataType(0, DataManager.DataType.Monster, false)]
        public string Species;

        /// <summary>
        /// The number of party members of the specified species required.
        /// </summary>
        public int Amount;

        /// <summary>
        /// Gets a localized string describing the partner species requirement for this evolution.
        /// </summary>
        /// <returns>A formatted string describing the required species and count with the species' colored name.</returns>
        /// <inheritdoc/>
        public override string GetReqString()
        {
            if (Amount > 1)
                return Text.FormatGrammar(new StringKey("EVO_REQ_ALLY_SPECIES_MULTI").ToLocal(), DataManager.Instance.GetMonster(Species).GetColoredName(), Amount);
            return Text.FormatGrammar(new StringKey("EVO_REQ_ALLY_SPECIES").ToLocal(), DataManager.Instance.GetMonster(Species).GetColoredName());
        }

        /// <summary>
        /// Determines whether the party contains the required number of the specified species.
        /// The character being evolved is not counted towards this requirement.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context.</param>
        /// <returns>true if the party contains at least the required number of the specified species; otherwise, false.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            int amount = 0;
            foreach (Character partner in character.MemberTeam.Players)
            {
                if (partner.BaseForm.Species == Species && partner != character)
                    amount++;
            }
            return amount >= Amount;
        }
    }

    /// <summary>
    /// Evolution requirement based on having a party member with a specific element.
    /// Any team member (including the evolving character) with the element satisfies this.
    /// </summary>
    [Serializable]
    public class EvoPartnerElement : PromoteDetail
    {
        /// <summary>
        /// The element type ID that a party member must have.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string PartnerElement;

        /// <summary>
        /// Initializes a new instance of the EvoPartnerElement class.
        /// </summary>
        public EvoPartnerElement() { PartnerElement = ""; }

        /// <summary>
        /// Gets a localized string describing the partner element requirement for this evolution.
        /// </summary>
        /// <returns>A formatted string describing the required element with its colored name.</returns>
        /// <inheritdoc/>
        public override string GetReqString()
        {
            ElementData elementEntry = DataManager.Instance.GetElement(PartnerElement);
            return Text.FormatGrammar(new StringKey("EVO_REQ_ALLY_ELEMENT").ToLocal(), elementEntry.GetColoredName());
        }

        /// <summary>
        /// Determines whether any party member has the required element.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context.</param>
        /// <returns>true if any party member (including the character themselves) has the required element; otherwise, false.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            foreach (Character partner in character.MemberTeam.Players)
            {
                if (partner.HasElement(PartnerElement))
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Evolution effect that spawns an additional monster when evolving.
    /// Creates a new party member with the same stats as the evolving character (Shedinja-style).
    /// </summary>
    [Serializable]
    public class EvoShed : PromoteDetail
    {
        /// <summary>
        /// The species ID of the monster to spawn alongside the evolution.
        /// </summary>
        [JsonConverter(typeof(MonsterConverter))]
        [DataType(0, DataManager.DataType.Monster, false)]
        public string ShedSpecies;

        /// <summary>
        /// Spawns a new character of the shed species when evolution occurs.
        /// The new character inherits stats, skills, and other properties from the evolving character.
        /// Only works in dungeon context and if there is an available team slot.
        /// </summary>
        /// <param name="character">The character that is evolving.</param>
        /// <param name="inDungeon">Whether the evolution is occurring in a dungeon context.</param>
        /// <inheritdoc/>
        public override void OnPromote(Character character, bool inDungeon)
        {
            if (!inDungeon)
                return;

            ExplorerTeam team = character.MemberTeam as ExplorerTeam;
            if (team == null)
                return;
            if (character.MemberTeam.Players.Count == team.GetMaxTeam(ZoneManager.Instance.CurrentZone))
                return;

            //if character has an open team slot, spawn the new character based on the stats of the current one

            MonsterID formData = new MonsterID(ShedSpecies, 0, character.BaseForm.Skin, Gender.Genderless);
            MonsterData dex = DataManager.Instance.GetMonster(formData.Species);

            CharData newChar = new CharData();
            newChar.BaseForm = formData;
            newChar.Level = character.Level;

            newChar.MaxHPBonus = character.MaxHPBonus;
            newChar.AtkBonus = character.AtkBonus;
            newChar.DefBonus = character.DefBonus;
            newChar.MAtkBonus = character.MAtkBonus;
            newChar.MDefBonus = character.MDefBonus;
            newChar.SpeedBonus = character.SpeedBonus;

            BaseMonsterForm forme = dex.Forms[formData.Form];

            for (int ii = 0; ii < character.BaseSkills.Count; ii++)
                newChar.BaseSkills[ii] = new SlotSkill(character.BaseSkills[ii]);

            string intrinsic = forme.RollIntrinsic(DataManager.Instance.Save.Rand, 2);
            newChar.SetBaseIntrinsic(intrinsic);

            newChar.Discriminator = character.Discriminator;
            newChar.MetAt = character.MetAt;
            newChar.MetLoc = character.MetLoc;
            foreach (BattleEvent effect in character.ActionEvents)
                newChar.ActionEvents.Add((BattleEvent)effect.Clone());

            Character player = new Character(newChar);
            foreach (BackReference<Skill> move in player.Skills)
            {
                if (!String.IsNullOrEmpty(move.Element.SkillNum))
                {
                    SkillData entry = DataManager.Instance.GetSkill(move.Element.SkillNum);
                    move.Element.Enabled = (entry.Data.Category == BattleData.SkillCategory.Physical || entry.Data.Category == BattleData.SkillCategory.Magical);
                }
            }
            player.Tactic = new AITactic(character.Tactic);
            character.MemberTeam.Players.Add(player);

            Loc? endLoc = ZoneManager.Instance.CurrentMap.GetClosestTileForChar(player, character.CharLoc);
            if (endLoc == null)
                endLoc = character.CharLoc;

            player.CharLoc = endLoc.Value;

            ZoneManager.Instance.CurrentMap.UpdateExploration(player);

            player.RefreshTraits();

            DataManager.Instance.Save.RegisterMonster(newChar.BaseForm);
            DataManager.Instance.Save.RogueUnlockMonster(newChar.BaseForm.Species);
        }
    }

    /// <summary>
    /// Evolution modifier that sets the resulting form based on conditions.
    /// When all conditions are met, the evolution result uses the specified form.
    /// </summary>
    [Serializable]
    public class EvoSetForm : PromoteDetail
    {
        /// <summary>
        /// List of conditions that must all be met to set this form.
        /// </summary>
        public List<PromoteDetail> Conditions;

        /// <summary>
        /// The form index to set if all conditions are met.
        /// </summary>
        public int Form;

        /// <summary>
        /// Initializes a new instance of the EvoSetForm class.
        /// </summary>
        public EvoSetForm()
        {
            Conditions = new List<PromoteDetail>();
        }

        /// <summary>
        /// Initializes a new instance of the EvoSetForm class with the specified form.
        /// </summary>
        /// <param name="form">The form index to use when conditions are met.</param>
        public EvoSetForm(int form)
        {
            Conditions = new List<PromoteDetail>();
            Form = form;
        }

        /// <summary>
        /// Sets the evolution result form if all conditions are met and the form is released.
        /// </summary>
        /// <param name="character">The character that is evolving.</param>
        /// <param name="inDungeon">Whether the evolution is occurring in a dungeon context.</param>
        /// <param name="result">The evolution result to modify, containing the target species and form.</param>
        /// <inheritdoc/>
        public override void BeforePromote(Character character, bool inDungeon, ref MonsterID result)
        {
            MonsterData data = DataManager.Instance.GetMonster(result.Species);
            if (!data.Forms[Form].Released)
                return;

            //set forme depending on condition
            foreach (PromoteDetail detail in Conditions)
            {
                if (!detail.GetReq(character, inDungeon))
                    return;
            }
            result.Form = Form;
        }
    }

    /// <summary>
    /// Evolution modifier that sets form based on capture location origin.
    /// Currently not fully implemented.
    /// </summary>
    [Serializable]
    public class EvoFormLocOrigin : PromoteDetail
    {
        /// <summary>
        /// Sets the evolution form based on the character's capture location.
        /// Currently not fully implemented.
        /// </summary>
        /// <param name="character">The character that is evolving.</param>
        /// <param name="inDungeon">Whether the evolution is occurring in a dungeon context.</param>
        /// <inheritdoc/>
        public override void OnPromote(Character character, bool inDungeon)
        {
            //set forme depending on capture location
        }
    }

    /// <summary>
    /// Evolution modifier for cream/flavor-based form selection.
    /// Currently not fully implemented.
    /// </summary>
    [Serializable]
    public class EvoFormCream : PromoteDetail
    {
        /// <summary>
        /// Determines whether the character meets the cream/flavor evolution requirement.
        /// Currently always returns false as this feature is not fully implemented.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context.</param>
        /// <returns>Always returns false until this feature is fully implemented.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            return false;
        }

        /// <summary>
        /// Modifies the evolution when cream/flavor conditions are met.
        /// Functions as both an item check and form setter.
        /// Currently not fully implemented.
        /// </summary>
        /// <param name="character">The character that is evolving.</param>
        /// <param name="inDungeon">Whether the evolution is occurring in a dungeon context.</param>
        /// <inheritdoc/>
        public override void OnPromote(Character character, bool inDungeon)
        {
            //functions as an item check, AND sets the forme
            //set forme depending on ???
        }
    }

    /// <summary>
    /// Evolution modifier that sets form based on held item from a mapping.
    /// Maps different items to different resulting forms (Lycanroc-style).
    /// </summary>
    [Serializable]
    public class EvoFormDusk : PromoteDetail
    {
        /// <summary>
        /// Dictionary mapping item IDs to resulting form indices.
        /// </summary>
        [DataType(1, DataManager.DataType.Item, false)]
        public Dictionary<string, int> ItemMap;

        /// <summary>
        /// The form index to use if no item matches.
        /// </summary>
        public int DefaultForm;

        /// <summary>
        /// Gets the ID of the character's currently equipped item.
        /// </summary>
        /// <param name="character">The character being checked.</param>
        /// <returns>The ID of the equipped item, or empty string if none equipped.</returns>
        /// <inheritdoc/>
        public override string GetReqItem(Character character) { return character.EquippedItem.ID; }

        /// <summary>
        /// Initializes a new instance of the EvoFormDusk class.
        /// </summary>
        public EvoFormDusk() { ItemMap = new Dictionary<string, int>(); }

        /// <summary>
        /// Gets a localized string describing the item requirement for this evolution.
        /// </summary>
        /// <returns>A formatted string indicating an item is required (currently returns "???" as placeholder).</returns>
        /// <inheritdoc/>
        public override string GetReqString() { return Text.FormatGrammar(new StringKey("EVO_REQ_ITEM").ToLocal(), "???"); }

        /// <summary>
        /// Determines whether the character is holding an item that maps to a form in this evolution.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context. Cursed items do not count in dungeons.</param>
        /// <returns>true if the character holds an item that exists in the item map; otherwise, false.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            foreach (string itemNum in ItemMap.Keys)
            {
                if (inDungeon)
                {
                    if (character.EquippedItem.ID == itemNum && !character.EquippedItem.Cursed)
                        return true;
                }
                else
                {
                    if (character.EquippedItem.ID == itemNum)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Sets the evolution result form based on the item held by the character.
        /// Uses the default form if the held item is not in the item map.
        /// </summary>
        /// <param name="character">The character that is evolving.</param>
        /// <param name="inDungeon">Whether the evolution is occurring in a dungeon context.</param>
        /// <param name="result">The evolution result to modify, containing the target species and form.</param>
        /// <inheritdoc/>
        public override void BeforePromote(Character character, bool inDungeon, ref MonsterID result)
        {
            int resultForm = DefaultForm;

            //set forme depending on condition
            foreach (string itemNum in ItemMap.Keys)
            {
                if (character.EquippedItem.ID == itemNum)
                {
                    resultForm = ItemMap[itemNum];
                    break;
                }
            }

            MonsterData data = DataManager.Instance.GetMonster(result.Species);
            if (data.Forms[resultForm].Released)
                result.Form = resultForm;
        }

        /// <summary>
        /// Removes the held item from the character upon evolution.
        /// </summary>
        /// <param name="character">The character that is evolving.</param>
        /// <param name="inDungeon">Whether the evolution is occurring in a dungeon context.</param>
        /// <inheritdoc/>
        public override void OnPromote(Character character, bool inDungeon)
        {
            character.SilentDequipItem();
        }
    }

    /// <summary>
    /// Evolution modifier that sets form based on terrain type.
    /// Currently not fully implemented.
    /// </summary>
    [Serializable]
    public class EvoFormScroll : PromoteDetail
    {
        /// <summary>
        /// Gets a localized string describing the terrain requirement for this evolution.
        /// </summary>
        /// <returns>A formatted string indicating a terrain/element is required (currently returns "???" as placeholder).</returns>
        /// <inheritdoc/>
        public override string GetReqString()
        {
            return Text.FormatGrammar(new StringKey("EVO_REQ_TILE_ELEMENT").ToLocal(), "???");
        }

        /// <summary>
        /// Determines whether the character meets the terrain evolution requirement.
        /// Currently always returns false as this feature is not fully implemented.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context.</param>
        /// <returns>Always returns false until this feature is fully implemented.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            return false;
        }

        /// <summary>
        /// Modifies the evolution when terrain conditions are met.
        /// Functions as both a terrain check and form setter.
        /// Currently not fully implemented.
        /// </summary>
        /// <param name="character">The character that is evolving.</param>
        /// <param name="inDungeon">Whether the evolution is occurring in a dungeon context.</param>
        /// <inheritdoc/>
        public override void OnPromote(Character character, bool inDungeon)
        {
            //functions as a terrain check, AND sets the forme
        }
    }

    /// <summary>
    /// Evolution requirement based on the character's personality/discriminator value.
    /// Uses modular arithmetic to determine which evolution branch is taken.
    /// This is a hard requirement that cannot be bypassed.
    /// </summary>
    [Serializable]
    public class EvoPersonality : PromoteDetail
    {
        /// <summary>
        /// The required remainder when dividing the discriminator by Divisor.
        /// </summary>
        public int Mod;

        /// <summary>
        /// The divisor used for the modular arithmetic check.
        /// </summary>
        public int Divisor;

        /// <summary>
        /// Indicates that this is a hard requirement that cannot be bypassed.
        /// </summary>
        /// <returns>Always returns true.</returns>
        /// <inheritdoc/>
        public override bool IsHardReq() { return true; }

        /// <summary>
        /// Determines whether the character's personality (discriminator) value matches the required modulo condition.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context.</param>
        /// <returns>true if the character's discriminator modulo divisor equals the required remainder; otherwise, false.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            return character.Discriminator % Divisor == Mod;
        }
    }

    /// <summary>
    /// Evolution requirement based on trading the character.
    /// Currently always returns true (trading not fully implemented).
    /// </summary>
    [Serializable]
    public class EvoTrade : PromoteDetail
    {
        /// <summary>
        /// Gets a localized string describing the trade requirement for this evolution.
        /// </summary>
        /// <returns>A formatted string indicating trading is required.</returns>
        /// <inheritdoc/>
        public override string GetReqString() { return Text.FormatGrammar(new StringKey("EVO_REQ_TRADE").ToLocal()); }

        /// <summary>
        /// Determines whether the character has been traded.
        /// Currently always returns true as the trading system is not fully implemented.
        /// Once implemented, should check whether the character has been traded before.
        /// </summary>
        /// <param name="character">The character being checked for evolution eligibility.</param>
        /// <param name="inDungeon">Whether the check is being performed in a dungeon context.</param>
        /// <returns>Always returns true until the trading system is fully implemented.</returns>
        /// <inheritdoc/>
        public override bool GetReq(Character character, bool inDungeon)
        {
            return true; //character.TradeHistory.Count > 0;
        }
    }
}

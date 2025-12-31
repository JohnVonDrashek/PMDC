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
    /// Battle events that temporarily change the target's attributes, such as species, form, ability, moves, element, base stats, etc.
    /// </summary>

    /// <summary>
    /// Event that makes the user learn the last used move of the target.
    /// </summary>
    [Serializable]
    public class SketchBattleEvent : BattleEvent
    {
        /// <summary>
        /// The status that contains the last used move in IDState status state.
        /// This should usually be "last_used_move".
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, true)]
        public string LastMoveStatusID;

        /// <inheritdoc/>
        public SketchBattleEvent() { LastMoveStatusID = ""; }

        /// <summary>
        /// Creates a new SketchBattleEvent with the specified status ID.
        /// </summary>
        /// <param name="prevMoveID">The status ID containing the last used move.</param>
        public SketchBattleEvent(string prevMoveID)
        {
            LastMoveStatusID = prevMoveID;
        }

        /// <inheritdoc/>
        protected SketchBattleEvent(SketchBattleEvent other)
        {
            LastMoveStatusID = other.LastMoveStatusID;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new SketchBattleEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Target.Dead)
                yield break;

            if (String.IsNullOrEmpty(LastMoveStatusID))
            {
                bool learn = (context.ActionType == BattleActionType.Skill && context.UsageSlot > BattleContext.DEFAULT_ATTACK_SLOT && context.UsageSlot < CharData.MAX_SKILL_SLOTS && context.User.Skills[context.UsageSlot].BackRef > -1);
                for (int ii = CharData.MAX_SKILL_SLOTS - 1; ii >= 0; ii--)
                    sketchMove(context, context.Target.BaseSkills[ii].SkillNum, ii, learn, true);
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_SKETCH").ToLocal(), context.User.GetDisplayName(false), context.Target.GetDisplayName(false)));
                yield break;
            }
            else
            {
                StatusEffect testStatus = context.Target.GetStatusEffect(LastMoveStatusID);
                if (testStatus != null && context.ActionType == BattleActionType.Skill && context.UsageSlot > BattleContext.DEFAULT_ATTACK_SLOT && context.UsageSlot < CharData.MAX_SKILL_SLOTS)
                {
                    sketchMove(context, testStatus.StatusStates.GetWithDefault<IDState>().ID, context.UsageSlot, context.User.Skills[context.UsageSlot].BackRef > -1, false);
                    yield break;
                }
            }

            DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_SKETCH_FAIL").ToLocal(), context.Target.GetDisplayName(false)));
        }

        /// <summary>
        /// Sketches the specified move to the user's moveset.
        /// </summary>
        /// <param name="context">The battle context.</param>
        /// <param name="moveIndex">The move ID to sketch.</param>
        /// <param name="moveSlot">The slot to place the move in.</param>
        /// <param name="learn">Whether to permanently learn the move.</param>
        /// <param name="group">Whether this is part of a group sketch operation.</param>
        private void sketchMove(BattleContext context, string moveIndex, int moveSlot, bool learn, bool group)
        {
            SkillData entry = null;

            if (!String.IsNullOrEmpty(moveIndex))
                entry = DataManager.Instance.GetSkill(moveIndex);

            if (!group)
            {
                foreach (BackReference<Skill> moveState in context.User.Skills)
                {
                    if (moveState.Element.SkillNum == moveIndex)
                    {
                        DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_ALREADY_HAS_SKILL").ToLocal(), context.User.GetDisplayName(false), entry.GetIconName()));
                        return;
                    }
                }
            }
            if (learn)
            {
                if (!String.IsNullOrEmpty(moveIndex))
                    context.User.ReplaceSkill(moveIndex, moveSlot, DataManager.Instance.Save.GetDefaultEnable(moveIndex));
                else
                    context.User.DeleteSkill(moveSlot);
            }
            else
                context.User.ChangeSkill(moveSlot, moveIndex, -1, DataManager.Instance.Save.GetDefaultEnable(moveIndex));
            if (!group)
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_SKETCH").ToLocal(), context.User.GetDisplayName(false), entry.GetIconName()));
        }
    }

    /// <summary>
    /// Event that makes the user learn the last used move of the target until the next floor.
    /// </summary>
    [Serializable]
    public class MimicBattleEvent : BattleEvent
    {
        /// <summary>
        /// The status that contains the last used move in IDState status state.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string LastMoveStatusID;

        /// <summary>
        /// The number of charges the mimicked move will have.
        /// </summary>
        public int NewMoveCharges;

        /// <inheritdoc/>
        public MimicBattleEvent() { LastMoveStatusID = ""; }

        /// <summary>
        /// Creates a new MimicBattleEvent with the specified parameters.
        /// </summary>
        /// <param name="prevMoveID">The status ID containing the last used move.</param>
        /// <param name="newMoveCharges">The number of charges for the mimicked move.</param>
        public MimicBattleEvent(string prevMoveID, int newMoveCharges)
        {
            LastMoveStatusID = prevMoveID;
            NewMoveCharges = newMoveCharges;
        }

        /// <inheritdoc/>
        protected MimicBattleEvent(MimicBattleEvent other)
        {
            NewMoveCharges = other.NewMoveCharges;
            LastMoveStatusID = other.LastMoveStatusID;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new MimicBattleEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Target.Dead)
                yield break;

            StatusEffect testStatus = context.Target.GetStatusEffect(LastMoveStatusID);
            if (testStatus != null && context.ActionType == BattleActionType.Skill && context.UsageSlot > BattleContext.DEFAULT_ATTACK_SLOT && context.UsageSlot < CharData.MAX_SKILL_SLOTS)
            {
                string chosenMove = testStatus.StatusStates.GetWithDefault<IDState>().ID;

                SkillData entry = DataManager.Instance.GetSkill(chosenMove);

                foreach (BackReference<Skill> moveState in context.User.Skills)
                {
                    if (moveState.Element.SkillNum == chosenMove)
                    {
                        DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_ALREADY_HAS_SKILL").ToLocal(), context.User.GetDisplayName(false), entry.GetIconName()));
                        yield break;
                    }
                }
                context.User.ChangeSkill(context.UsageSlot, chosenMove, NewMoveCharges, DataManager.Instance.Save.GetDefaultEnable(chosenMove));
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_MIMIC").ToLocal(), context.User.GetDisplayName(false), entry.GetIconName()));
            }
            else
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_MIMIC_FAIL").ToLocal(), context.Target.GetDisplayName(false)));
        }
    }


    /// <summary>
    /// Event that converts the character's type to resist incoming moves.
    /// </summary>
    [Serializable]
    public class Conversion2Event : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new Conversion2Event(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            List<string> elements = new List<string>();
            string element = DataManager.Instance.DefaultElement;
            foreach (string key in DataManager.Instance.DataIndices[DataManager.DataType.Element].GetOrderedKeys(true))
            {
                int effectiveness = PreTypeEvent.CalculateTypeMatchup(context.Data.Element, key);
                if (effectiveness == PreTypeEvent.N_E)
                {
                    element = key;
                    break;
                }
                else if (effectiveness == PreTypeEvent.NVE)
                    elements.Add(key);
            }

            if (element == DataManager.Instance.DefaultElement && elements.Count > 0)
                element = elements[DataManager.Instance.Save.Rand.Next(0, elements.Count)];

            if (element != DataManager.Instance.DefaultElement)
                yield return CoroutineManager.Instance.StartCoroutine(context.Target.ChangeElement(element, DataManager.Instance.DefaultElement));
        }
    }

    /// <summary>
    /// Event that converts the character's type to the move last used.
    /// </summary>
    [Serializable]
    public class ConversionEvent : BattleEvent
    {
        /// <summary>
        /// Whether to affect the target or user.
        /// </summary>
        public bool AffectTarget;

        /// <inheritdoc/>
        public ConversionEvent() { }

        /// <summary>
        /// Creates a new ConversionEvent with the specified target setting.
        /// </summary>
        /// <param name="affectTarget">Whether to affect the target or user.</param>
        public ConversionEvent(bool affectTarget)
        {
            AffectTarget = affectTarget;
        }

        /// <inheritdoc/>
        protected ConversionEvent(ConversionEvent other)
        {
            AffectTarget = other.AffectTarget;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ConversionEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Character target = (AffectTarget ? context.Target : context.User);
            if (target.Dead)
                yield break;

            if (context.Data.Element != DataManager.Instance.DefaultElement && !(target.Element1 == context.Data.Element && target.Element2 == DataManager.Instance.DefaultElement))
            {
                yield return CoroutineManager.Instance.StartCoroutine(target.ChangeElement(context.Data.Element, DataManager.Instance.DefaultElement));
            }
        }
    }


    /// <summary>
    /// Event that converts the character to the specified type.
    /// </summary>
    [Serializable]
    public class ChangeToElementEvent : BattleEvent
    {
        /// <summary>
        /// The type to convert to.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string TargetElement;

        /// <inheritdoc/>
        public ChangeToElementEvent() { TargetElement = ""; }

        /// <summary>
        /// Creates a new ChangeToElementEvent with the specified element.
        /// </summary>
        /// <param name="element">The element type to change to.</param>
        public ChangeToElementEvent(string element)
        {
            TargetElement = element;
        }

        /// <inheritdoc/>
        protected ChangeToElementEvent(ChangeToElementEvent other)
        {
            TargetElement = other.TargetElement;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ChangeToElementEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (!(TargetElement == context.Target.Element1 && context.Target.Element2 == DataManager.Instance.DefaultElement))
                yield return CoroutineManager.Instance.StartCoroutine(context.Target.ChangeElement(TargetElement, DataManager.Instance.DefaultElement));
            else
            {
                ElementData typeData = DataManager.Instance.GetElement(TargetElement);
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_ALREADY_HAS_ELEMENT").ToLocal(), context.Target.GetDisplayName(false), typeData.GetIconName()));
            }
        }
    }

    /// <summary>
    /// Event that adds the specified type to the target's type.
    /// </summary>
    [Serializable]
    public class AddElementEvent : BattleEvent
    {
        /// <summary>
        /// The type to add.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string TargetElement;

        /// <inheritdoc/>
        public AddElementEvent() { TargetElement = ""; }

        /// <summary>
        /// Creates a new AddElementEvent with the specified element.
        /// </summary>
        /// <param name="element">The element type to add.</param>
        public AddElementEvent(string element)
        {
            TargetElement = element;
        }

        /// <inheritdoc/>
        protected AddElementEvent(AddElementEvent other)
        {
            TargetElement = other.TargetElement;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new AddElementEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (!context.Target.HasElement(TargetElement))
                yield return CoroutineManager.Instance.StartCoroutine(context.Target.ChangeElement(TargetElement, context.Target.Element1));
            else
            {
                ElementData typeData = DataManager.Instance.GetElement(TargetElement);
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_ALREADY_HAS_ELEMENT").ToLocal(), context.Target.GetDisplayName(false), typeData.GetIconName()));
            }
        }
    }

    /// <summary>
    /// Event that removes the specified type from the target.
    /// </summary>
    [Serializable]
    public class RemoveElementEvent : BattleEvent
    {
        /// <summary>
        /// The type to remove.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string TargetElement;

        /// <inheritdoc/>
        public RemoveElementEvent() { TargetElement = ""; }

        /// <summary>
        /// Creates a new RemoveElementEvent with the specified element.
        /// </summary>
        /// <param name="element">The element type to remove.</param>
        public RemoveElementEvent(string element)
        {
            TargetElement = element;
        }

        /// <inheritdoc/>
        protected RemoveElementEvent(RemoveElementEvent other)
        {
            TargetElement = other.TargetElement;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new RemoveElementEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Target.Element2 == TargetElement)
                yield return CoroutineManager.Instance.StartCoroutine(context.Target.ChangeElement(context.Target.Element1, DataManager.Instance.DefaultElement, true, false));
            if (context.Target.Element1 == TargetElement)
                yield return CoroutineManager.Instance.StartCoroutine(context.Target.ChangeElement(context.Target.Element2, DataManager.Instance.DefaultElement, true, false));
        }
    }

    /// <summary>
    /// Event that causes the user to copy the element types of the target.
    /// </summary>
    [Serializable]
    public class ReflectElementEvent : BattleEvent
    {
        /// <inheritdoc/>
        public ReflectElementEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ReflectElementEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            yield return CoroutineManager.Instance.StartCoroutine(context.User.ChangeElement(context.Target.Element1, context.Target.Element2));
        }
    }

    /// <summary>
    /// Event that changes the character's type based on the current map status.
    /// </summary>
    [Serializable]
    public class NatureElementEvent : BattleEvent
    {
        /// <summary>
        /// The map status mapped to a type.
        /// </summary>
        [JsonConverter(typeof(MapStatusElementDictConverter))]
        [DataType(1, DataManager.DataType.MapStatus, false)]
        [DataType(2, DataManager.DataType.Element, false)]
        public Dictionary<string, string> TerrainPair;

        /// <inheritdoc/>
        public NatureElementEvent()
        {
            TerrainPair = new Dictionary<string, string>();
        }

        /// <summary>
        /// Creates a new NatureElementEvent with the specified terrain mappings.
        /// </summary>
        /// <param name="terrain">Dictionary mapping map status IDs to element IDs.</param>
        public NatureElementEvent(Dictionary<string, string> terrain)
        {
            TerrainPair = terrain;
        }

        /// <inheritdoc/>
        protected NatureElementEvent(NatureElementEvent other)
            : this()
        {
            foreach (string terrain in other.TerrainPair.Keys)
                TerrainPair.Add(terrain, other.TerrainPair[terrain]);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new NatureElementEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            foreach (string terrain in TerrainPair.Keys)
            {
                if (ZoneManager.Instance.CurrentMap.Status.ContainsKey(terrain))
                {
                    yield return CoroutineManager.Instance.StartCoroutine(context.Target.ChangeElement(TerrainPair[terrain], DataManager.Instance.DefaultElement));
                    yield break;
                }
            }

            if (ZoneManager.Instance.CurrentMap.Element != DataManager.Instance.DefaultElement)
                yield return CoroutineManager.Instance.StartCoroutine(context.Target.ChangeElement(ZoneManager.Instance.CurrentMap.Element, DataManager.Instance.DefaultElement));
        }
    }

    /// <summary>
    /// Event that changes the character's ability to the specified ability.
    /// </summary>
    [Serializable]
    public class ChangeToAbilityEvent : BattleEvent
    {
        /// <summary>
        /// The ability to change to.
        /// </summary>
        [JsonConverter(typeof(IntrinsicConverter))]
        [DataType(0, DataManager.DataType.Intrinsic, false)]
        public string TargetAbility;

        /// <summary>
        /// Whether to affect the target or user.
        /// </summary>
        public bool AffectTarget;

        /// <summary>
        /// Whether to display a message if the ability failed to change.
        /// </summary>
        public bool SilentCheck;

        /// <inheritdoc/>
        public ChangeToAbilityEvent() { TargetAbility = ""; }

        /// <summary>
        /// Creates a new ChangeToAbilityEvent with the specified parameters.
        /// </summary>
        /// <param name="ability">The ability to change to.</param>
        /// <param name="affectTarget">Whether to affect the target or user.</param>
        public ChangeToAbilityEvent(string ability, bool affectTarget) : this(ability, affectTarget, false)
        { }

        /// <summary>
        /// Creates a new ChangeToAbilityEvent with the specified parameters.
        /// </summary>
        /// <param name="ability">The ability to change to.</param>
        /// <param name="affectTarget">Whether to affect the target or user.</param>
        /// <param name="silentCheck">Whether to suppress failure messages.</param>
        public ChangeToAbilityEvent(string ability, bool affectTarget, bool silentCheck)
        {
            TargetAbility = ability;
            AffectTarget = affectTarget;
            SilentCheck = silentCheck;
        }

        /// <inheritdoc/>
        protected ChangeToAbilityEvent(ChangeToAbilityEvent other)
        {
            TargetAbility = other.TargetAbility;
            AffectTarget = other.AffectTarget;
            SilentCheck = other.SilentCheck;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ChangeToAbilityEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Character target = (AffectTarget ? context.Target : context.User);
            //change to ability
            if (SilentCheck && target.Intrinsics[0].Element.ID == TargetAbility)
                yield break;

            yield return CoroutineManager.Instance.StartCoroutine(target.ReplaceIntrinsic(0, TargetAbility, true, false));
        }
    }

    /// <summary>
    /// Event that removes the specified ability of the character.
    /// </summary>
    [Serializable]
    public class RemoveAbilityEvent : BattleEvent
    {
        /// <summary>
        /// The ability to check for.
        /// </summary>
        [JsonConverter(typeof(IntrinsicConverter))]
        [DataType(0, DataManager.DataType.Intrinsic, false)]
        public string TargetAbility;

        /// <inheritdoc/>
        public RemoveAbilityEvent() { TargetAbility = ""; }

        /// <summary>
        /// Creates a new RemoveAbilityEvent with the specified ability.
        /// </summary>
        /// <param name="ability">The ability to remove.</param>
        public RemoveAbilityEvent(string ability)
        {
            TargetAbility = ability;
        }

        /// <inheritdoc/>
        protected RemoveAbilityEvent(RemoveAbilityEvent other)
        {
            TargetAbility = other.TargetAbility;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new RemoveAbilityEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (TargetAbility == context.Target.Intrinsics[0].Element.ID)
                yield return CoroutineManager.Instance.StartCoroutine(context.Target.ReplaceIntrinsic(0, DataManager.Instance.DefaultIntrinsic, true, false));
        }
    }

    /// <summary>
    /// Event that causes the user to copy the ability of the target.
    /// </summary>
    [Serializable]
    public class ReflectAbilityEvent : BattleEvent
    {
        /// <summary>
        /// Whether the target copies the ability of the user.
        /// </summary>
        public bool AffectTarget;

        /// <summary>
        /// The message displayed in the dungeon log.
        /// </summary>
        [StringKey(0, true)]
        public StringKey Msg;

        /// <inheritdoc/>
        public ReflectAbilityEvent() { }

        /// <summary>
        /// Creates a new ReflectAbilityEvent with the specified target setting.
        /// </summary>
        /// <param name="affectTarget">Whether to affect the target instead of user.</param>
        public ReflectAbilityEvent(bool affectTarget) { AffectTarget = affectTarget; }

        /// <summary>
        /// Creates a new ReflectAbilityEvent with the specified parameters.
        /// </summary>
        /// <param name="affectTarget">Whether to affect the target instead of user.</param>
        /// <param name="msg">The message to display.</param>
        public ReflectAbilityEvent(bool affectTarget, StringKey msg) { AffectTarget = affectTarget; Msg = msg; }

        /// <inheritdoc/>
        protected ReflectAbilityEvent(ReflectAbilityEvent other)
        {
            AffectTarget = other.AffectTarget;
            Msg = other.Msg;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ReflectAbilityEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Character target = (AffectTarget ? context.Target : context.User);
            Character origin = (AffectTarget ? context.User : context.Target);

            if (Msg.IsValid())
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(Msg.ToLocal(), origin.GetDisplayName(false), target.GetDisplayName(false), owner.GetDisplayName()));

            //reflect ability (target to attacker, or vice versa)
            for (int ii = 0; ii < CharData.MAX_INTRINSIC_SLOTS; ii++)
                yield return CoroutineManager.Instance.StartCoroutine(target.ReplaceIntrinsic(ii, origin.Intrinsics[ii].Element.ID));
        }
    }

    /// <summary>
    /// Event that causes the user to swap abilities with the target.
    /// </summary>
    [Serializable]
    public class SwapAbilityEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new SwapAbilityEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            List<string> abilities = new List<string>();
            foreach (BackReference<Intrinsic> ability in context.Target.Intrinsics)
                abilities.Add(ability.Element.ID);

            //reflect ability (target to attacker, or vice versa)
            for (int ii = 0; ii < CharData.MAX_INTRINSIC_SLOTS; ii++)
                yield return CoroutineManager.Instance.StartCoroutine(context.Target.ReplaceIntrinsic(ii, context.User.Intrinsics[ii].Element.ID, true, false));
            for (int ii = 0; ii < CharData.MAX_INTRINSIC_SLOTS; ii++)
                yield return CoroutineManager.Instance.StartCoroutine(context.User.ReplaceIntrinsic(ii, abilities[ii], true, false));
        }
    }

    /// <summary>
    /// Event that causes the character to swap its attack with its defense stats.
    /// </summary>
    [Serializable]
    public class PowerTrickEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new PowerTrickEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            int atk = context.Target.Atk;
            context.Target.ProxyAtk = context.Target.Def;
            context.Target.ProxyDef = atk;
            DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_STAT_SWAP").ToLocal(), context.User.GetDisplayName(false),
                Text.FormatGrammar(new StringKey("BASE_STAT").ToLocal(), Stat.Attack.ToLocal()),
                Text.FormatGrammar(new StringKey("BASE_STAT").ToLocal(), Stat.Defense.ToLocal())));
            yield break;
        }
    }

    /// <summary>
    /// Event that averages the defense or attack stats of the user and target.
    /// </summary>
    [Serializable]
    public class StatSplitEvent : BattleEvent
    {
        /// <summary>
        /// Whether to split the attack stats instead of defense.
        /// </summary>
        public bool AttackStats;

        /// <inheritdoc/>
        public StatSplitEvent() { }

        /// <summary>
        /// Creates a new StatSplitEvent with the specified stat type.
        /// </summary>
        /// <param name="attack">Whether to split attack stats instead of defense.</param>
        public StatSplitEvent(bool attack)
        {
            AttackStats = attack;
        }

        /// <inheritdoc/>
        protected StatSplitEvent(StatSplitEvent other)
        {
            AttackStats = other.AttackStats;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new StatSplitEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            int phys = (AttackStats ? (context.User.Atk + context.Target.Atk) : (context.User.Def + context.Target.Def)) / 2;
            int spec = (AttackStats ? (context.User.MAtk + context.Target.MAtk) : (context.User.MDef + context.Target.MDef)) / 2;
            if (AttackStats)
            {
                context.User.ProxyAtk = phys;
                context.Target.ProxyAtk = phys;
                context.User.ProxyMAtk = spec;
                context.Target.ProxyMAtk = spec;
                string[] stats = new string[2];
                stats[0] = Text.FormatGrammar(new StringKey("BASE_STAT").ToLocal(), Stat.Attack.ToLocal());
                stats[1] = Text.FormatGrammar(new StringKey("BASE_STAT").ToLocal(), Stat.MAtk.ToLocal());
                string list = Text.BuildList(stats);
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_STAT_SPLIT").ToLocal(), context.User.GetDisplayName(false), list, context.Target.GetDisplayName(false)));
            }
            else
            {
                context.User.ProxyDef = phys;
                context.Target.ProxyDef = phys;
                context.User.ProxyMDef = spec;
                context.Target.ProxyMDef = spec;
                string[] stats = new string[2];
                stats[0] = Text.FormatGrammar(new StringKey("BASE_STAT").ToLocal(), Stat.Defense.ToLocal());
                stats[1] = Text.FormatGrammar(new StringKey("BASE_STAT").ToLocal(), Stat.MDef.ToLocal());
                string list = Text.BuildList(stats);
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_STAT_SPLIT").ToLocal(), context.User.GetDisplayName(false), list, context.Target.GetDisplayName(false)));
            }
            yield break;
        }
    }

    /// <summary>
    /// Event that causes the user to swap its speed stat with the target.
    /// </summary>
    [Serializable]
    public class SpeedSwapEvent : BattleEvent
    {
        /// <inheritdoc/>
        public SpeedSwapEvent() { }

        /// <inheritdoc/>
        protected SpeedSwapEvent(SpeedSwapEvent other)
        {
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new SpeedSwapEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            int speed = context.User.Speed;
            context.User.ProxySpeed = context.Target.Speed;
            context.Target.ProxySpeed = speed;
            DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_STAT_SWAP_OTHER").ToLocal(), context.User.GetDisplayName(false),
                Text.FormatGrammar(new StringKey("BASE_STAT").ToLocal(), Stat.Speed.ToLocal()), context.Target.GetDisplayName(false)));
            yield break;
        }
    }

    /// <summary>
    /// Event that causes the user to copy the stat boosts/drops of the target.
    /// </summary>
    [Serializable]
    public class ReflectStatsEvent : BattleEvent
    {
        /// <summary>
        /// The set of status IDs representing stat changes to copy from the target.
        /// </summary>
        [JsonConverter(typeof(StatusSetConverter))]
        [DataType(1, DataManager.DataType.Status, false)]
        public HashSet<string> StatusIDs;

        /// <inheritdoc/>
        public ReflectStatsEvent() { StatusIDs = new HashSet<string>(); }

        /// <summary>
        /// Creates a new ReflectStatsEvent with the specified status IDs.
        /// </summary>
        /// <param name="statusIDs">The set of status IDs to copy.</param>
        public ReflectStatsEvent(HashSet<string> statusIDs)
        {
            StatusIDs = statusIDs;
        }

        /// <inheritdoc/>
        protected ReflectStatsEvent(ReflectStatsEvent other)
            : this()
        {
            foreach (string statusID in other.StatusIDs)
                StatusIDs.Add(statusID);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ReflectStatsEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            foreach (string statusID in StatusIDs)
            {
                //silently remove all stat changes on the user
                yield return CoroutineManager.Instance.StartCoroutine(context.User.RemoveStatusEffect(statusID, false));
                //silently add all stat changes from target to user
                StatusEffect testStatus = context.Target.GetStatusEffect(statusID);

                if (testStatus != null)
                {
                    StatusEffect status = new StatusEffect(statusID);
                    status.LoadFromData();
                    status.StatusStates.GetWithDefault<StackState>().Stack = testStatus.StatusStates.GetWithDefault<StackState>().Stack;
                    yield return CoroutineManager.Instance.StartCoroutine(context.User.AddStatusEffect(context.User, status, false));
                }
            }
            DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_BUFF_COPY").ToLocal(), context.User.GetDisplayName(false), context.Target.GetDisplayName(false)));
        }
    }

    /// <summary>
    /// Event that causes the user to swap stat changes with the target.
    /// </summary>
    [Serializable]
    public class SwapStatsEvent : BattleEvent
    {
        /// <summary>
        /// The set of status IDs representing stat changes to swap with the target.
        /// </summary>
        [JsonConverter(typeof(StatusSetConverter))]
        [DataType(1, DataManager.DataType.Status, false)]
        public HashSet<string> StatusIDs;

        /// <inheritdoc/>
        public SwapStatsEvent() { StatusIDs = new HashSet<string>(); }

        /// <summary>
        /// Creates a new SwapStatsEvent with the specified status IDs.
        /// </summary>
        /// <param name="statusIDs">The set of status IDs to swap.</param>
        public SwapStatsEvent(HashSet<string> statusIDs)
        {
            StatusIDs = statusIDs;
        }

        /// <inheritdoc/>
        protected SwapStatsEvent(SwapStatsEvent other)
            : this()
        {
            foreach (string statusID in other.StatusIDs)
                StatusIDs.Add(statusID);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new SwapStatsEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            foreach (string statusID in StatusIDs)
            {
                //get the stat changes of both sides
                StatusEffect userStatus = context.User.GetStatusEffect(statusID);
                StatusEffect targetStatus = context.Target.GetStatusEffect(statusID);

                int userStack = (userStatus != null) ? userStatus.StatusStates.GetWithDefault<StackState>().Stack : 0;
                int targetStack = (targetStatus != null) ? targetStatus.StatusStates.GetWithDefault<StackState>().Stack : 0;

                //remove the changes
                yield return CoroutineManager.Instance.StartCoroutine(context.User.RemoveStatusEffect(statusID, false));
                yield return CoroutineManager.Instance.StartCoroutine(context.Target.RemoveStatusEffect(statusID, false));

                //grant the changes
                if (userStack != 0)
                {
                    StatusEffect status = new StatusEffect(statusID);
                    status.LoadFromData();
                    status.StatusStates.GetWithDefault<StackState>().Stack = userStack;
                    yield return CoroutineManager.Instance.StartCoroutine(context.Target.AddStatusEffect(context.Target, status, false));
                }
                if (targetStack != 0)
                {
                    StatusEffect status = new StatusEffect(statusID);
                    status.LoadFromData();
                    status.StatusStates.GetWithDefault<StackState>().Stack = targetStack;
                    yield return CoroutineManager.Instance.StartCoroutine(context.User.AddStatusEffect(context.User, status, false));
                }
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_BUFF_SWAP").ToLocal(), context.User.GetDisplayName(false), context.Target.GetDisplayName(false), DataManager.Instance.GetStatus(statusID).GetColoredName()));
            }
        }
    }


    /// <summary>
    /// Event that restores the character back to its original form.
    /// </summary>
    [Serializable]
    public class RestoreFormEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new RestoreFormEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Target.Dead)
                yield break;

            context.Target.RestoreForm();
        }
    }

    /// <summary>
    /// Event that causes the user to transform into the target.
    /// </summary>
    [Serializable]
    public class TransformEvent : BattleEvent
    {
        /// <summary>
        /// Whether the target transforms into the user instead.
        /// </summary>
        public bool AffectTarget;

        /// <summary>
        /// The transformed status to apply.
        /// </summary>
        [DataType(0, DataManager.DataType.Status, false)]
        public string StatusID;

        /// <summary>
        /// The number of charges transformed moves will have.
        /// </summary>
        public int TransformCharges;

        /// <inheritdoc/>
        public TransformEvent() { StatusID = ""; }

        /// <summary>
        /// Creates a new TransformEvent with the specified parameters.
        /// </summary>
        /// <param name="affectTarget">Whether to affect the target instead of user.</param>
        /// <param name="status">The status ID to apply.</param>
        /// <param name="transformCharges">The number of charges for transformed moves.</param>
        public TransformEvent(bool affectTarget, string status, int transformCharges)
        {
            AffectTarget = affectTarget;
            StatusID = status;
            TransformCharges = transformCharges;
        }

        /// <inheritdoc/>
        protected TransformEvent(TransformEvent other)
        {
            AffectTarget = other.AffectTarget;
            StatusID = other.StatusID;
            TransformCharges = other.TransformCharges;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new TransformEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Character target = (AffectTarget ? context.Target : context.User);
            Character user = (AffectTarget ? context.User : context.Target);

            if (target.Dead || user.Dead)
                yield break;

            StatusEffect transform = target.GetStatusEffect(StatusID);
            if (transform != null)
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_ALREADY_TRANSFORMED").ToLocal(), target.GetDisplayName(false)));
                yield break;
            }
            if (target.CurrentForm.Species == user.CurrentForm.Species)
            {
                MonsterData entry = DataManager.Instance.GetMonster(target.CurrentForm.Species);
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_ALREADY_HAS_SPECIES").ToLocal(), target.GetDisplayName(false), entry.GetColoredName()));
                yield break;
            }

            int hp = target.HP;

            target.Transform(user.CurrentForm);

            //proxy stats
            target.ProxyAtk = user.Atk;
            target.ProxyDef = user.Def;
            target.ProxyMAtk = user.MAtk;
            target.ProxyMDef = user.MDef;
            target.ProxySpeed = user.Speed;

            //ability
            for (int ii = 0; ii < CharData.MAX_INTRINSIC_SLOTS; ii++)
                yield return CoroutineManager.Instance.StartCoroutine(target.ReplaceIntrinsic(ii, user.Intrinsics[ii].Element.ID, false, false));

            //type
            yield return CoroutineManager.Instance.StartCoroutine(target.ChangeElement(user.Element1, user.Element2, false, false));

            //moves
            for (int ii = 0; ii < CharData.MAX_SKILL_SLOTS; ii++)
                target.ChangeSkill(ii, user.Skills[ii].Element.SkillNum, TransformCharges, DataManager.Instance.Save.GetDefaultEnable(user.Skills[ii].Element.SkillNum));

            //set the status
            if (!String.IsNullOrEmpty(StatusID))
            {
                StatusEffect setStatus = new StatusEffect(StatusID);
                setStatus.LoadFromData();
                setStatus.StatusStates.Set(new HPState(hp));
                yield return CoroutineManager.Instance.StartCoroutine(target.AddStatusEffect(null, setStatus, false));
            }

            DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_TRANSFORM").ToLocal(), target.GetDisplayName(false), user.GetDisplayName(false)));
        }
    }

    /// <summary>
    /// Event that devolves the target to its pre-evolution form.
    /// </summary>
    [Serializable]
    public class DevolveEvent : BattleEvent
    {
        /// <summary>
        /// Whether to display a message if the target cannot devolve.
        /// </summary>
        public bool SilentCheck;

        /// <summary>
        /// The transformed status to apply.
        /// </summary>
        [DataType(0, DataManager.DataType.Status, false)]
        public string StatusID;

        /// <summary>
        /// The list of battle VFXs played if the condition is met.
        /// </summary>
        public List<BattleAnimEvent> Anims;

        /// <summary>
        /// The number of charges devolved moves will have.
        /// </summary>
        public int TransformCharges;

        /// <inheritdoc/>
        public DevolveEvent() { Anims = new List<BattleAnimEvent>(); }

        /// <summary>
        /// Creates a new DevolveEvent with the specified parameters.
        /// </summary>
        /// <param name="silentCheck">Whether to suppress failure messages.</param>
        /// <param name="status">The status ID to apply.</param>
        /// <param name="transformCharges">The number of charges for devolved moves.</param>
        /// <param name="anims">The battle animations to play.</param>
        public DevolveEvent(bool silentCheck, string status, int transformCharges, params BattleAnimEvent[] anims) : this()
        {
            SilentCheck = silentCheck;
            StatusID = status;
            TransformCharges = transformCharges;
            Anims.AddRange(anims);
        }

        /// <inheritdoc/>
        public DevolveEvent(DevolveEvent other) : this()
        {
            SilentCheck = other.SilentCheck;
            StatusID = other.StatusID;
            TransformCharges = other.TransformCharges;
            foreach (BattleAnimEvent anim in other.Anims)
                Anims.Add((BattleAnimEvent)anim.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new DevolveEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Target.Dead)
                yield break;

            MonsterData candidateDex = DataManager.Instance.GetMonster(context.Target.CurrentForm.Species);
            BaseMonsterForm candidateForm = candidateDex.Forms[context.Target.CurrentForm.Form];

            if (!String.IsNullOrEmpty(candidateDex.PromoteFrom))
            {
                MonsterData dex = DataManager.Instance.GetMonster(candidateDex.PromoteFrom);
                BaseMonsterForm forme = dex.Forms[candidateForm.PromoteForm];

                if (dex.Released && forme.Released)
                {
                    int hp = context.Target.HP;

                    string prevName = context.Target.GetDisplayName(false);
                    MonsterID prevoData = context.Target.CurrentForm;
                    prevoData.Species = candidateDex.PromoteFrom;
                    prevoData.Form = candidateForm.PromoteForm;
                    context.Target.Transform(prevoData);

                    //moves
                    List<string> final_moves = forme.RollLatestSkills(context.Target.Level * 1 / 2 + 1, new List<string>());
                    for (int ii = 0; ii < CharData.MAX_SKILL_SLOTS; ii++)
                    {
                        if (ii < final_moves.Count)
                            context.Target.ChangeSkill(ii, final_moves[ii], TransformCharges, DataManager.Instance.Save.GetDefaultEnable(final_moves[ii]));
                        else
                            context.Target.ChangeSkill(ii, "", -1, false);
                    }

                    //set the status
                    if (!String.IsNullOrEmpty(StatusID))
                    {
                        StatusEffect setStatus = new StatusEffect(StatusID);
                        setStatus.LoadFromData();
                        setStatus.StatusStates.Set(new HPState(hp));
                        yield return CoroutineManager.Instance.StartCoroutine(context.Target.AddStatusEffect(null, setStatus, false));
                    }

                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_DEVOLVE").ToLocal(), prevName, dex.GetColoredName()));

                    foreach (BattleAnimEvent anim in Anims)
                        yield return CoroutineManager.Instance.StartCoroutine(anim.Apply(owner, ownerChar, context));

                    yield break;
                }
            }

            if (!SilentCheck)
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_DEVOLVE_FAIL").ToLocal(), context.Target.GetDisplayName(false)));
        }
    }



    /// <summary>
    /// Event that changes the character's form based on whether they use an attacking or status move.
    /// </summary>
    [Serializable]
    public class StanceChangeEvent : BattleEvent
    {
        /// <summary>
        /// The required species in order for this ability to activate.
        /// </summary>
        [JsonConverter(typeof(MonsterConverter))]
        [DataType(0, DataManager.DataType.Monster, false)]
        public string ReqSpecies;

        /// <summary>
        /// The defense form ID of the species.
        /// </summary>
        public int DefenseForme;

        /// <summary>
        /// The attack form ID of the species.
        /// </summary>
        public int AttackForme;

        /// <inheritdoc/>
        public StanceChangeEvent() { ReqSpecies = ""; }

        /// <summary>
        /// Creates a new StanceChangeEvent with the specified parameters.
        /// </summary>
        /// <param name="reqSpecies">The required species for this event.</param>
        /// <param name="defenseForme">The defense form ID.</param>
        /// <param name="attackForme">The attack form ID.</param>
        public StanceChangeEvent(string reqSpecies, int defenseForme, int attackForme)
        {
            ReqSpecies = reqSpecies;
            DefenseForme = defenseForme;
            AttackForme = attackForme;
        }

        /// <inheritdoc/>
        protected StanceChangeEvent(StanceChangeEvent other) : this()
        {
            ReqSpecies = other.ReqSpecies;
            DefenseForme = other.DefenseForme;
            AttackForme = other.AttackForme;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new StanceChangeEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.User.CurrentForm.Species != ReqSpecies)
                yield break;

            if (context.ActionType == BattleActionType.Skill && context.UsageSlot > BattleContext.DEFAULT_ATTACK_SLOT && context.UsageSlot < CharData.MAX_SKILL_SLOTS)
            {
                //get the forme it should be in
                int forme = -1;

                if (context.Data.Category == BattleData.SkillCategory.Physical || context.Data.Category == BattleData.SkillCategory.Magical)
                {
                    forme = AttackForme;
                }
                else if (context.Data.Category == BattleData.SkillCategory.Status)
                {
                    forme = DefenseForme;
                }

                if (forme != -1 && forme != context.User.CurrentForm.Form)
                {
                    //transform it
                    context.User.Transform(new MonsterID(context.User.CurrentForm.Species, forme, context.User.CurrentForm.Skin, context.User.CurrentForm.Gender));
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_FORM_CHANGE").ToLocal(), context.User.GetDisplayName(false)));
                }
            }
            yield break;
        }
    }


    /// <summary>
    /// Event that protects against an attack if the target is in the specified form, and changes to a different form if so.
    /// </summary>
    [Serializable]
    public class BustFormEvent : BattleEvent
    {
        /// <summary>
        /// Species needed to trigger protection.
        /// </summary>
        [DataType(0, DataManager.DataType.Monster, false)]
        public string ReqSpecies;

        /// <summary>
        /// The form needed to trigger protection.
        /// </summary>
        public int ReqForm;

        /// <summary>
        /// The form to change to after protection.
        /// </summary>
        public int ResultForm;

        /// <summary>
        /// The message displayed in the dungeon log.
        /// </summary>
        public StringKey Msg;

        /// <summary>
        /// The list of battle VFXs played if the protection triggers.
        /// </summary>
        public List<BattleAnimEvent> Anims;

        /// <inheritdoc/>
        public BustFormEvent()
        {
            ReqSpecies = "";
            Anims = new List<BattleAnimEvent>();
        }

        /// <summary>
        /// Creates a new BustFormEvent with the specified parameters.
        /// </summary>
        /// <param name="species">The required species.</param>
        /// <param name="fromForm">The form required to trigger.</param>
        /// <param name="toForm">The form to change to.</param>
        /// <param name="msg">The message to display.</param>
        /// <param name="anims">The battle animations to play.</param>
        public BustFormEvent(string species, int fromForm, int toForm, StringKey msg, params BattleAnimEvent[] anims)
        {
            ReqSpecies = species;
            ReqForm = fromForm;
            ResultForm = toForm;
            Msg = msg;
            Anims = new List<BattleAnimEvent>();
            Anims.AddRange(anims);
        }

        /// <inheritdoc/>
        protected BustFormEvent(BustFormEvent other)
        {
            ReqSpecies = other.ReqSpecies;
            ReqForm = other.ReqForm;
            ResultForm = other.ResultForm;
            Msg = other.Msg;
            Anims = new List<BattleAnimEvent>();
            foreach (BattleAnimEvent anim in other.Anims)
                Anims.Add((BattleAnimEvent)anim.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new BustFormEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Target.CurrentForm.Species == ReqSpecies && context.Target.CurrentForm.Form == ReqForm)
            {
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(Msg.ToLocal(), ownerChar.GetDisplayName(false), owner.GetDisplayName()));

                context.Target.Transform(new MonsterID(context.Target.CurrentForm.Species, ResultForm, context.Target.CurrentForm.Skin, context.Target.CurrentForm.Gender));

                foreach (BattleAnimEvent anim in Anims)
                    yield return CoroutineManager.Instance.StartCoroutine(anim.Apply(owner, ownerChar, context));

                context.AddContextStateMult<AccMult>(false, -1, 1);
            }
        }
    }

}

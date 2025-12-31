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
    /// Battle events that handle recruitment mechanics.
    /// </summary>

    /// <summary>
    /// Abstract base class for events that modify recruitment rates.
    /// </summary>
    [Serializable]
    public abstract class RecruitBoostEvent : BattleEvent
    {
        /// <summary>
        /// Calculates the recruitment rate modifier for this event.
        /// </summary>
        protected abstract int GetRecruitRate(GameEventOwner owner, Character ownerChar, BattleContext context);

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Target != null)
                context.AddContextStateInt<RecruitBoost>(GetRecruitRate(owner, ownerChar, context));
            yield break;
        }
    }

    /// <summary>
    /// Event that sets the additional recruitment rate, not accounting for the species join rate
    /// </summary>
    [Serializable]
    public class FlatRecruitmentEvent : RecruitBoostEvent
    {
        /// <summary>
        /// The additional recruitment rate
        /// </summary>
        public int RecruitRate;

        /// <inheritdoc/>
        public FlatRecruitmentEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlatRecruitmentEvent"/> class with specified rate.
        /// </summary>
        /// <param name="recruitRate">The flat recruitment rate bonus.</param>
        public FlatRecruitmentEvent(int recruitRate) { RecruitRate = recruitRate; }

        /// <inheritdoc/>
        protected FlatRecruitmentEvent(FlatRecruitmentEvent other)
        {
            RecruitRate = other.RecruitRate;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new FlatRecruitmentEvent(this); }

        /// <inheritdoc/>
        protected override int GetRecruitRate(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            return RecruitRate;
        }
    }

    /// <summary>
    /// Event that boosts the recruitment rate if the target's type matches one of the specified type.
    /// Otherwise, it drops the recruitment rate
    /// </summary>
    [Serializable]
    public class TypeRecruitmentEvent : RecruitBoostEvent
    {
        [JsonConverter(typeof(ElementSetConverter))]
        /// <summary>
        /// Set of element types that provide a recruitment bonus.
        /// </summary>
        [DataType(1, DataManager.DataType.Element, false)]
        public HashSet<string> Elements;

        /// <inheritdoc/>
        public TypeRecruitmentEvent() { Elements = new HashSet<string>(); }

        /// <summary>
        /// Initializes a new instance with a single element type.
        /// </summary>
        /// <param name="element">The element type that provides a recruitment bonus.</param>
        public TypeRecruitmentEvent(string element) : this() { Elements.Add(element); }

        /// <summary>
        /// Initializes a new instance with multiple element types.
        /// </summary>
        /// <param name="elements">The set of element types that provide a recruitment bonus.</param>
        public TypeRecruitmentEvent(HashSet<string> elements) { Elements = elements; }

        /// <inheritdoc/>
        protected TypeRecruitmentEvent(TypeRecruitmentEvent other)
            : this()
        {
            foreach (string element in other.Elements)
                Elements.Add(element);
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new TypeRecruitmentEvent(this); }

        /// <inheritdoc/>
        protected override int GetRecruitRate(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            MonsterID formData = context.Target.BaseForm;
            BaseMonsterForm form = DataManager.Instance.GetMonster(formData.Species).Forms[formData.Form];
            if (Elements.Contains(form.Element1) || Elements.Contains(form.Element2))
                return 40;
            else
                return -50;
        }
    }

    /// <summary>
    /// Event that boosts the recruitment rate if the target is not the default skin.
    /// Otherwise, it drops the recruitment rate.
    /// </summary>
    [Serializable]
    public class SkinRecruitmentEvent : RecruitBoostEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new SkinRecruitmentEvent(); }

        /// <inheritdoc/>
        protected override int GetRecruitRate(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            MonsterID formData = context.Target.BaseForm;
            if (formData.Skin != DataManager.Instance.DefaultSkin)
                return 40;
            return -50;
        }
    }

    /// <summary>
    /// Event that modifies the recruitment rate based on the type matchup between the user and target.
    /// </summary>
    [Serializable]
    public class TypeMatchupRecruitmentEvent : RecruitBoostEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new TypeMatchupRecruitmentEvent(); }

        /// <inheritdoc/>
        protected override int GetRecruitRate(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            int matchup1 = PreTypeEvent.CalculateTypeMatchup(context.User.Element1, context.Target.Element1);
            matchup1 += PreTypeEvent.CalculateTypeMatchup(context.User.Element1, context.Target.Element2);

            int matchup2 = PreTypeEvent.CalculateTypeMatchup(context.User.Element1, context.Target.Element1);
            matchup2 += PreTypeEvent.CalculateTypeMatchup(context.User.Element1, context.Target.Element2);

            return PreTypeEvent.GetEffectivenessMult(Math.Max(matchup1, matchup2)) * 20 - 80;//between + and - 80 recruit rate
        }
    }

    /// <summary>
    /// Event that modifies the recruitment rate based on the level difference between the user and target.
    /// </summary>
    [Serializable]
    public class LevelRecruitmentEvent : RecruitBoostEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new LevelRecruitmentEvent(); }

        /// <inheritdoc/>
        protected override int GetRecruitRate(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            return ((context.User.Level - context.Target.Level - 1) / 10 + 1) * 10;//between + and - 100, at max
        }
    }

    /// <summary>
    /// Event that attempts to recruit the target using an item.
    /// If successful, the recruit can be nicknamed and added to the team.
    /// </summary>
    [Serializable]
    public class RecruitmentEvent : BaseRecruitmentEvent
    {
        /// <inheritdoc/>
        public RecruitmentEvent()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecruitmentEvent"/> class with a script event.
        /// </summary>
        /// <param name="scriptEvent">The script event to run when interacting with the recruit.</param>
        public RecruitmentEvent(BattleScriptEvent scriptEvent) : base(scriptEvent)
        {

        }

        /// <inheritdoc/>
        public RecruitmentEvent(RecruitmentEvent other) : base(other)
        {

        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new RecruitmentEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            context.Target.CharDir = context.User.CharDir.Reverse();
            if (DiagManager.Instance.CurSettings.BattleFlow < Settings.BattleSpeed.VeryFast)
            {
                EmoteData emoteData = DataManager.Instance.GetEmote("question");
                context.Target.StartEmote(new Emote(emoteData.Anim, emoteData.LocHeight, 1));
                GameManager.Instance.BattleSE("EVT_Emote_Confused");
                yield return new WaitForFrames(60);
            }

            if (context.Target.Unrecruitable || context.Target.MemberTeam is ExplorerTeam || DataManager.Instance.Save.NoRecruiting)
            {
                EmoteData emoteData = DataManager.Instance.GetEmote("angry");
                context.Target.StartEmote(new Emote(emoteData.Anim, emoteData.LocHeight, 1));
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_CANT_RECRUIT").ToLocal(), context.Target.GetDisplayName(false)));
                yield return CoroutineManager.Instance.StartCoroutine(failRecruit(owner, ownerChar, context));
            }
            else if (context.Target.Level > context.User.Level + 5)
            {
                EmoteData emoteData = DataManager.Instance.GetEmote("angry");
                context.Target.StartEmote(new Emote(emoteData.Anim, emoteData.LocHeight, 1));
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_CANT_RECRUIT_LEVEL").ToLocal(), context.User.GetDisplayName(false), context.Target.GetDisplayName(false)));
                yield return CoroutineManager.Instance.StartCoroutine(failRecruit(owner, ownerChar, context));
            }
            else
            {
                MonsterID formData = context.Target.BaseForm;
                int catchRate = DataManager.Instance.GetMonster(formData.Species).JoinRate;

                int totalRate = catchRate + context.GetContextStateInt<RecruitBoost>(0);
                totalRate = totalRate * (context.Target.MaxHP * 2 - context.Target.HP) / context.Target.MaxHP;

                if (totalRate <= 0)
                {
                    EmoteData emoteData = DataManager.Instance.GetEmote("angry");
                    context.Target.StartEmote(new Emote(emoteData.Anim, emoteData.LocHeight, 1));
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_CANT_RECRUIT_RATE").ToLocal(), context.Target.GetDisplayName(false)));
                    yield return CoroutineManager.Instance.StartCoroutine(failRecruit(owner, ownerChar, context));
                }
                else
                {
                    if (DataManager.Instance.Save.Rand.Next(100) < totalRate)
                    {
                        yield return CoroutineManager.Instance.StartCoroutine(base.Apply(owner, ownerChar, context));
                    }
                    else
                    {
                        DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_RECRUIT_FAIL").ToLocal(), context.Target.GetDisplayName(false)));
                        yield return CoroutineManager.Instance.StartCoroutine(failRecruit(owner, ownerChar, context));
                    }
                }
            }
        }

        /// <summary>
        /// Handles the failure case when recruitment fails.
        /// </summary>
        private IEnumerator<YieldInstruction> failRecruit(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            GameManager.Instance.BattleSE("DUN_Miss");

            // TODO: Not the best way to track where the item landed... ideally use a context but there isn't more than one use case yet.
            MapItem mapItem = new MapItem(context.Item, new Loc(-1));
            yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.DropMapItem(mapItem, context.Target.CharLoc, context.Target.CharLoc, false));

            Loc? resultLoc = null;
            if (mapItem.TileLoc != new Loc(-1))
                resultLoc = mapItem.TileLoc;
            context.ContextStates.Set(new RecruitFail(resultLoc));
        }
    }



    /// <summary>
    /// Event that attempts to recruit a defeated target.
    /// If successful, the recruit can be nicknamed and added to the team.
    /// </summary>
    [Serializable]
    public class DefeatRecruitmentEvent : BaseRecruitmentEvent
    {
        /// <summary>
        /// The numerator of the recruitment rate modifier.
        /// </summary>
        public int Numerator;

        /// <summary>
        /// The denominator of the recruitment rate modifier.
        /// </summary>
        public int Denominator;

        /// <inheritdoc/>
        public DefeatRecruitmentEvent()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefeatRecruitmentEvent"/> class with specified parameters.
        /// </summary>
        /// <param name="numerator">The numerator of the recruitment rate modifier.</param>
        /// <param name="denominator">The denominator of the recruitment rate modifier.</param>
        /// <param name="scriptEvent">The script event to run when interacting with the recruit.</param>
        public DefeatRecruitmentEvent(int numerator, int denominator, BattleScriptEvent scriptEvent) : base(scriptEvent)
        {
            Numerator = numerator;
            Denominator = denominator;
        }

        /// <inheritdoc/>
        public DefeatRecruitmentEvent(DefeatRecruitmentEvent other) : base(other)
        {
            Numerator = other.Numerator;
            Denominator = other.Denominator;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new DefeatRecruitmentEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (!context.Target.Dead)
                yield break;


            if (context.Target.Unrecruitable || context.Target.MemberTeam is ExplorerTeam || DataManager.Instance.Save.NoRecruiting)
            {
                yield break;
            }
            else if (context.Target.Level > context.User.Level + 5)
            {
                yield break;
            }
            else
            {
                MonsterID formData = context.Target.BaseForm;
                int catchRate = DataManager.Instance.GetMonster(formData.Species).JoinRate;

                int totalRate = catchRate + context.GetContextStateInt<RecruitBoost>(0);
                totalRate = totalRate * Numerator / Denominator;

                if (totalRate <= 0)
                {
                    yield break;
                }
                else
                {
                    if (DataManager.Instance.Save.Rand.Next(100) < totalRate)
                    {
                        bool recruit = false;
                        if (DataManager.Instance.CurrentReplay != null)
                        {
                            recruit = DataManager.Instance.CurrentReplay.ReadUI() > 0;
                        }
                        else
                        {
                            yield return CoroutineManager.Instance.StartCoroutine(MenuManager.Instance.ProcessMenuCoroutine(MenuManager.Instance.CreateQuestion(Text.FormatGrammar(new StringKey("MSG_ASK_RECRUIT").ToLocal()),
                                () => { recruit = true; },
                                () => { })));

                            DataManager.Instance.LogUIPlay(recruit ? 1 : 0);
                        }
                        if (recruit)
                            yield return CoroutineManager.Instance.StartCoroutine(base.Apply(owner, ownerChar, context));
                    }
                    else
                    {
                        yield break;
                    }
                }
            }
        }
    }


    /// <summary>
    /// Abstract base class for recruitment events.
    /// Handles the core recruitment logic including adding characters to team and UI flows.
    /// </summary>
    [Serializable]
    public abstract class BaseRecruitmentEvent : BattleEvent
    {
        /// <summary>
        /// The Lua battle script that runs when interacting with the recruit in dungeons.
        /// </summary>
        public BattleScriptEvent ActionScript;

        /// <inheritdoc/>
        public BaseRecruitmentEvent()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRecruitmentEvent"/> class with a script event.
        /// </summary>
        /// <param name="scriptEvent">The script event to run when interacting with the recruit.</param>
        public BaseRecruitmentEvent(BattleScriptEvent scriptEvent)
        {
            ActionScript = scriptEvent;
        }

        /// <inheritdoc/>
        public BaseRecruitmentEvent(BaseRecruitmentEvent other)
        {
            ActionScript = other.ActionScript;
        }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            yield return CoroutineManager.Instance.StartCoroutine(DungeonRecruit(owner, ownerChar, context.Target, ActionScript));
        }

        /// <summary>
        /// Performs the recruitment process for a character in a dungeon.
        /// Handles team joining, nickname selection, and related UI flows.
        /// </summary>
        public static IEnumerator<YieldInstruction> DungeonRecruit(GameEventOwner owner, Character ownerChar, Character targetChar, BattleScriptEvent actionScript)
        {
            GameManager.Instance.Fanfare("Fanfare/JoinTeam");
            DungeonScene.Instance.RemoveChar(targetChar);
            //should be default false, set this just in case
            targetChar.Absentee = false;
            AITactic tactic = DataManager.Instance.GetAITactic(DataManager.Instance.DefaultAI);
            targetChar.Tactic = new AITactic(tactic);
            DungeonScene.Instance.AddCharToTeam(Faction.Player, 0, false, targetChar);
            targetChar.RefreshTraits();
            targetChar.Tactic.Initialize(targetChar);

            int oldFullness = targetChar.Fullness;
            targetChar.FullRestore();
            targetChar.Fullness = oldFullness;
            //restore HP and status problems
            //{
            //    targetChar.HP = targetChar.MaxHP;

            //    List<int> statuses = new List<int>();
            //    foreach (StatusEffect oldStatus in targetChar.IterateStatusEffects())
            //        statuses.Add(oldStatus.ID);

            //    foreach (int statusID in statuses)
            //        yield return CoroutineManager.Instance.StartCoroutine(targetChar.RemoveStatusEffect(statusID, false));
            //}

            foreach (BackReference<Skill> skill in targetChar.Skills)
                skill.Element.Enabled = DataManager.Instance.Save.GetDefaultEnable(skill.Element.SkillNum);


            targetChar.OriginalUUID = DataManager.Instance.Save.UUID;
            targetChar.OriginalTeam = DataManager.Instance.Save.ActiveTeam.Name;
            targetChar.MetAt = ZoneManager.Instance.CurrentMap.GetColoredName();
            targetChar.MetLoc = new ZoneLoc(ZoneManager.Instance.CurrentZoneID, ZoneManager.Instance.CurrentMapID);
            targetChar.ActionEvents.Clear();
            if (actionScript != null)
                targetChar.ActionEvents.Add((BattleEvent)actionScript.Clone());
            ZoneManager.Instance.CurrentMap.UpdateExploration(targetChar);

            EmoteData emoteData = DataManager.Instance.GetEmote("glowing");
            targetChar.StartEmote(new Emote(emoteData.Anim, emoteData.LocHeight, 2));
            yield return new WaitForFrames(40);

            int poseId = 50;
            CharSheet sheet = GraphicsManager.GetChara(targetChar.Appearance.ToCharID());
            int fallbackIndex = sheet.GetReferencedAnimIndex(poseId);
            if (fallbackIndex == poseId)
                yield return CoroutineManager.Instance.StartCoroutine(targetChar.StartAnim(new CharAnimPose(targetChar.CharLoc, targetChar.CharDir, poseId, 0)));

            //check against inventory capacity violation
            if (!String.IsNullOrEmpty(targetChar.EquippedItem.ID) && DungeonScene.Instance.ActiveTeam.MaxInv == DungeonScene.Instance.ActiveTeam.GetInvCount())
            {
                InvItem item = targetChar.EquippedItem;
                yield return CoroutineManager.Instance.StartCoroutine(targetChar.DequipItem());
                yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.DropItem(item, targetChar.CharLoc));
            }

            if (DataManager.Instance.CurrentReplay == null)
            {
                yield return CoroutineManager.Instance.StartCoroutine(MenuManager.Instance.ProcessMenuCoroutine(new MemberFeaturesMenu(DungeonScene.Instance.ActiveTeam, DungeonScene.Instance.ActiveTeam.Players.Count - 1, false, false, false)));

                bool nick = false;
                string name = "";
                yield return CoroutineManager.Instance.StartCoroutine(MenuManager.Instance.ProcessMenuCoroutine(MenuManager.Instance.CreateQuestion(Text.FormatGrammar(new StringKey("MSG_ASK_NICKNAME").ToLocal()),
                    () => { nick = true; },
                    () => { })));
                if (nick)
                    yield return CoroutineManager.Instance.StartCoroutine(MenuManager.Instance.ProcessMenuCoroutine(new NicknameMenu((string text) => { name = text; }, () => { })));
                DataManager.Instance.LogUIStringPlay(name);
                targetChar.Nickname = name;
            }
            else
            {
                //give nickname
                targetChar.Nickname = DataManager.Instance.CurrentReplay.ReadUIString();
            }
            if (DungeonScene.Instance.ActiveTeam.Name != "")
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_RECRUIT").ToLocal(), targetChar.GetDisplayName(true), DungeonScene.Instance.ActiveTeam.GetDisplayName()));
            else
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_RECRUIT_ANY").ToLocal(), targetChar.GetDisplayName(true)));
            DataManager.Instance.Save.RegisterMonster(targetChar.BaseForm);
            DataManager.Instance.Save.RogueUnlockMonster(targetChar.BaseForm.Species);
            yield return CoroutineManager.Instance.StartCoroutine(targetChar.OnMapStart());

            //yield return new WaitForFrames(120);

            if (DungeonScene.Instance.ActiveTeam.Players.Count > DungeonScene.Instance.ActiveTeam.GetMaxTeam(ZoneManager.Instance.CurrentZone))
                yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.AskToSendHome());

            yield return CoroutineManager.Instance.StartCoroutine(targetChar.StartAnim(new CharAnimIdle(targetChar.CharLoc, targetChar.CharDir)));
        }
    }

}


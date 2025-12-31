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
    /// Event that occurs before the user does an action.
    /// Sets attack stats and tracks move usage.
    /// </summary>
    [Serializable]
    public class PreActionEvent : BattleEvent
    {
        /// <summary>
        /// The status that will store the last used slot.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string LastSlotStatusID;

        /// <summary>
        /// The status that will store the last used move.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string LastMoveStatusID;

        /// <summary>
        /// The status that will store how many times the same move was used.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string RepeatStatusID;

        /// <inheritdoc/>
        public PreActionEvent()
        {
            LastSlotStatusID = "";
            LastMoveStatusID = "";
            RepeatStatusID = "";
        }

        /// <summary>
        /// Creates a new PreActionEvent with the specified status IDs.
        /// </summary>
        /// <param name="lastSlotStatusID">Status ID for tracking last used move slot.</param>
        /// <param name="lastMoveStatusID">Status ID for tracking last used move.</param>
        /// <param name="repeatStatusID">Status ID for tracking repeated move usage.</param>
        public PreActionEvent(string lastSlotStatusID, string lastMoveStatusID, string repeatStatusID)
        {
            LastSlotStatusID = lastSlotStatusID;
            LastMoveStatusID = lastMoveStatusID;
            RepeatStatusID = repeatStatusID;
        }

        /// <inheritdoc/>
        public PreActionEvent(PreActionEvent other)
        {
            LastSlotStatusID = other.LastSlotStatusID;
            LastMoveStatusID = other.LastMoveStatusID;
            RepeatStatusID = other.RepeatStatusID;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new PreActionEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            //use the correct phys/special stats
            if (context.Data.Category == BattleData.SkillCategory.Physical)
                context.ContextStates.Set(new UserAtkStat(context.User.Atk));
            else if (context.Data.Category == BattleData.SkillCategory.Magical)
                context.ContextStates.Set(new UserAtkStat(context.User.MAtk));
            context.ContextStates.Set(new UserLevel(context.User.Level));
            context.ContextStates.Set(new UserHitStat(context.User.Speed));

            if (context.ActionType == BattleActionType.Skill && context.UsageSlot > BattleContext.DEFAULT_ATTACK_SLOT && context.UsageSlot < CharData.MAX_SKILL_SLOTS)
            {
                StatusEffect lastSlotStatus = new StatusEffect(LastSlotStatusID);
                lastSlotStatus.LoadFromData();
                lastSlotStatus.StatusStates.GetWithDefault<SlotState>().Slot = context.UsageSlot;
                yield return CoroutineManager.Instance.StartCoroutine(context.User.AddStatusEffect(context.User, lastSlotStatus));

                StatusEffect testStatus = context.User.GetStatusEffect(LastMoveStatusID);
                StatusEffect repeatStatus = context.User.GetStatusEffect(RepeatStatusID);
                if (lastSlotStatus != null && repeatStatus != null &&
                    testStatus.StatusStates.GetWithDefault<IDState>().ID == context.Data.ID &&
                    repeatStatus.StatusStates.GetWithDefault<RecentState>() != null)
                {
                    //fall through
                }
                else
                {
                    //start new repetition
                    StatusEffect newRepeatStatus = new StatusEffect(RepeatStatusID);
                    newRepeatStatus.LoadFromData();
                    yield return CoroutineManager.Instance.StartCoroutine(context.User.AddStatusEffect(context.User, newRepeatStatus));
                }

                StatusEffect lastMoveStatus = new StatusEffect(LastMoveStatusID);
                lastMoveStatus.LoadFromData();
                lastMoveStatus.StatusStates.GetWithDefault<IDState>().ID = context.Data.ID;
                yield return CoroutineManager.Instance.StartCoroutine(context.User.AddStatusEffect(context.User, lastMoveStatus));
            }
            else
            {
                StatusEffect repeatStatus = context.User.GetStatusEffect(RepeatStatusID);
                if (repeatStatus != null)
                    repeatStatus.StatusStates.Remove<RecentState>();
            }
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            //TODO: remove on v1.1
            if (Serializer.OldVersion < new Version(0, 7, 16))
            {
                LastSlotStatusID = "last_used_move_slot";
                LastMoveStatusID = "last_used_move";
                RepeatStatusID = "times_move_used";
            }
        }
    }


    /// <summary>
    /// Event that occurs before the target takes the hit.
    /// This sets the target's level and which defense stat they will use.
    /// </summary>
    [Serializable]
    public class PreHitEvent : BattleEvent
    {
        /// <inheritdoc/>
        public PreHitEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new PreHitEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Data.Category == BattleData.SkillCategory.Physical)
                context.ContextStates.Set(new TargetDefStat(context.Target.Def));
            else if (context.Data.Category == BattleData.SkillCategory.Magical)
                context.ContextStates.Set(new TargetDefStat(context.Target.MDef));
            context.ContextStates.Set(new TargetLevel(context.Target.Level));
            context.ContextStates.Set(new TargetEvadeStat(context.Target.Speed));

            yield break;
        }
    }

    /// <summary>
    /// Event that groups multiple battle events into one event.
    /// </summary>
    [Serializable]
    public class MultiBattleEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle events to apply.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public MultiBattleEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <summary>
        /// Creates a new MultiBattleEvent with the specified events.
        /// </summary>
        /// <param name="effects">The battle events to combine.</param>
        public MultiBattleEvent(params BattleEvent[] effects)
            : this()
        {
            foreach (BattleEvent battleEffect in effects)
                BaseEvents.Add(battleEffect);
        }

        /// <inheritdoc/>
        protected MultiBattleEvent(MultiBattleEvent other)
            : this()
        {
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new MultiBattleEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            foreach (BattleEvent battleEffect in BaseEvents)
                yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
        }
    }

    /// <summary>
    /// Event that calculates whether the target is hit, taking into consideration the move accuracy,
    /// the user's accuracy boost, whether the move missed last turn, etc.
    /// </summary>
    [Serializable]
    public class AttemptHitEvent : BattleEvent
    {
        /// <inheritdoc/>
        public AttemptHitEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new AttemptHitEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            //see if it hits
            int accMod = context.GetContextStateMult<AccMult>().Multiply(0);
            bool hit = false;
            if (accMod == -1) //don't hit, don't say anything
            {
                GameManager.Instance.BattleSE("DUN_Miss_2");
                DungeonScene.Instance.Missed(context.Target.CharLoc);
            }
            else
            {
                if (context.Data.HitRate == -1)
                    hit = true;
                else if (context.GetContextStateMult<AccMult>().IsNeutralized())
                    hit = false;
                else
                {
                    int acc = context.Data.HitRate;
                    HitRateLevelTableState table = DataManager.Instance.UniversalEvent.UniversalStates.GetWithDefault<HitRateLevelTableState>();
                    acc = table.ApplyAccuracyMod(acc, context.GetContextStateInt<UserAccuracyBoost>(0));
                    acc /= table.AccuracyLevels[-table.MinAccuracy];
                    acc = table.ApplyEvasionMod(acc, context.GetContextStateInt<TargetEvasionBoost>(0));
                    acc /= table.EvasionLevels[-table.MinEvasion];
                    acc *= context.GetContextStateInt<UserHitStat>(1);
                    acc /= context.GetContextStateInt<TargetEvadeStat>(1);
                    acc = context.GetContextStateMult<AccMult>().Multiply(acc);

                    //MustHitNext is to ensure that no single character can miss twice in a row
                    if (context.User.MustHitNext || DataManager.Instance.Save.Rand.Next(0, 100) < acc)
                        hit = true;
                }
                if (hit)
                {
                    if (context.User == context.Target)
                    {
                        // Don't reset the miss chain if the target hit is the self
                    }
                    else if (context.ActionType == BattleActionType.Trap)
                    {
                        // Don't reset the miss chain if the action is a trap
                    }
                    else
                        context.User.MissChain = 0;
                }
                else
                    context.User.MissChain++;

                if (hit)
                {
                    if (context.User.MemberTeam == DungeonScene.Instance.ActiveTeam)
                    {
                        if (context.ActionType == BattleActionType.Skill && context.Data.ID != DataManager.Instance.DefaultSkill)
                            context.Target.EXPMarked = true;
                        else if (context.ActionType == BattleActionType.Item)
                            context.Target.EXPMarked = true;
                        else if (context.ActionType == BattleActionType.Throw)
                        {
                            ItemData entry = DataManager.Instance.GetItem(context.Item.ID);
                            if (entry.ItemStates.Contains<RecruitState>())
                            {
                                //failed recruitment items dont count
                            }
                            else if (context.ContextStates.Contains<ItemCaught>())
                            {
                                //items that were caught don't count
                            }
                            else
                                context.Target.EXPMarked = true;
                        }
                    }
                    //play the hit animation here
                    yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.ProcessEndAnim(context.User, context.Target, context.Data));

                    context.Hit = true;
                }
                else
                {
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_MISS").ToLocal(), context.Target.GetDisplayName(false)));
                    GameManager.Instance.BattleSE("DUN_Miss");
                    DungeonScene.Instance.Missed(context.Target.CharLoc);
                }
            }

        }
    }

    /// <summary>
    /// Event that occurs when a move is used from the menu.
    /// This sets the battle context data from the move and checks if the user has enough PP and the move is not disabled.
    /// </summary>
    [Serializable]
    public class PreSkillEvent : BattleEvent
    {
        /// <inheritdoc/>
        public PreSkillEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new PreSkillEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType != BattleActionType.Skill)
                yield break;
            if (context.UsageSlot == BattleContext.FORCED_SLOT)
                yield break;

            string usageIndex = DataManager.Instance.DefaultSkill;
            if (context.UsageSlot > BattleContext.DEFAULT_ATTACK_SLOT && context.UsageSlot < CharData.MAX_SKILL_SLOTS)
                usageIndex = context.User.Skills[context.UsageSlot].Element.SkillNum;

            SkillData entry = DataManager.Instance.GetSkill(usageIndex);
            context.Data = new BattleData(entry.Data);

            context.Data.ID = usageIndex;
            context.Data.DataType = DataManager.DataType.Skill;
            context.Explosion = new ExplosionData(entry.Explosion);
            context.HitboxAction = entry.HitboxAction.Clone();
            context.Item = new InvItem();
            context.Strikes = entry.Strikes;

            context.StartDir = context.User.CharDir;


            if (usageIndex == DataManager.Instance.DefaultSkill)
                context.SetActionMsg(Text.FormatGrammar(new StringKey("MSG_ATTACK_USE").ToLocal(), context.User.GetDisplayName(false)), true);
            else
                context.SetActionMsg(Text.FormatGrammar(new StringKey("MSG_SKILL_USE").ToLocal(), context.User.GetDisplayName(false), entry.GetIconName()));

            if (context.UsageSlot > BattleContext.DEFAULT_ATTACK_SLOT && context.UsageSlot < CharData.MAX_SKILL_SLOTS)
            {
                if (context.User.Skills[context.UsageSlot].Element.Charges <= 0)
                {
                    if (context.User == DungeonScene.Instance.FocusedCharacter)
                        DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_NO_MORE_CHARGES").ToLocal()), false, true);
                    context.CancelState.Cancel = true;
                }
                else if (context.User.Skills[context.UsageSlot].Element.Sealed)
                {
                    if (context.User == DungeonScene.Instance.FocusedCharacter)
                        DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_SKILL_SEALED").ToLocal()), false, true);
                    context.CancelState.Cancel = true;
                }
            }

            yield break;
        }
    }

    /// <summary>
    /// Event that occurs when an item is used from the menu.
    /// This sets the battle context data from the item and checks if the item is sticky.
    /// </summary>
    [Serializable]
    public class PreItemEvent : BattleEvent
    {
        /// <summary>
        /// Mapping of item use types to their display messages.
        /// </summary>
        [StringKey(2, false)]
        public Dictionary<ItemData.UseType, StringKey> UseMsgs;

        /// <inheritdoc/>
        public PreItemEvent()
        {
            UseMsgs = new Dictionary<ItemData.UseType, StringKey>();
        }

        /// <summary>
        /// Creates a new PreItemEvent with the specified message mappings.
        /// </summary>
        /// <param name="useMsgs">Dictionary mapping use types to their messages.</param>
        public PreItemEvent(Dictionary<ItemData.UseType, StringKey> useMsgs)
        {
            UseMsgs = useMsgs;
        }

        /// <inheritdoc/>
        public PreItemEvent(PreItemEvent other)
        {
            UseMsgs = new Dictionary<ItemData.UseType, StringKey>();
            foreach (ItemData.UseType useType in other.UseMsgs.Keys)
                UseMsgs[useType] = other.UseMsgs[useType];
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new PreItemEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType != BattleActionType.Item)
                yield break;
            if (context.UsageSlot == BattleContext.FORCED_SLOT)
                yield break;

            //while attack, use, and throw, will all use the same battle contexts and hitboxes,
            //they will still have different Execution methods, as well as EndEffects
            //Use Item will have its own method, which will have its own charge-up step to play the sound/animation for eating/drinking/using
            //it will still have a BeforeAction and OnAction method call

            context.StartDir = context.User.CharDir;

            InvItem item = null;
            if (context.UsageSlot > BattleContext.EQUIP_ITEM_SLOT)//item in inventory
                item = ((ExplorerTeam)context.User.MemberTeam).GetInv(context.UsageSlot);
            else if (context.UsageSlot == BattleContext.EQUIP_ITEM_SLOT)
                item = context.User.EquippedItem;
            else if (context.UsageSlot == BattleContext.FLOOR_ITEM_SLOT)
            {
                int mapSlot = ZoneManager.Instance.CurrentMap.GetItem(context.User.CharLoc);
                MapItem mapItem = ZoneManager.Instance.CurrentMap.Items[mapSlot];
                item = mapItem.MakeInvItem();
            }

            ItemData entry = DataManager.Instance.GetItem(item.ID);
            context.Data = new BattleData(entry.UseEvent);
            context.Data.ID = item.GetID();
            context.Data.DataType = DataManager.DataType.Item;
            context.Explosion = new ExplosionData(entry.Explosion);
            context.Strikes = 1;
            context.Item = new InvItem(item);
            if (entry.MaxStack > 1)
            {
                //TODO: Price needs to be multiplied by amount instead of dividing
                context.Item.Price = context.Item.Price / context.Item.Amount;
                context.Item.Amount = 1;
            }
            context.HitboxAction = entry.UseAction.Clone();
            StringKey useMsg;
            if (UseMsgs.TryGetValue(entry.UsageType, out useMsg))
                context.SetActionMsg(Text.FormatGrammar(useMsg.ToLocal(), context.User.GetDisplayName(false), context.Item.GetDisplayName()));

            if (item.Cursed)
            {
                GameManager.Instance.BattleSE("DUN_Sticky");
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_USE_CURSED").ToLocal(), item.GetDisplayName()), false, true);
                context.CancelState.Cancel = true;
            }

            yield break;
        }


        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            //TODO: remove on v1.1
            if (Serializer.OldVersion < new Version(0, 8, 9))
            {
                UseMsgs[ItemData.UseType.Eat] = new StringKey("MSG_USE_EAT");
                UseMsgs[ItemData.UseType.Drink] = new StringKey("MSG_USE_DRINK");
                UseMsgs[ItemData.UseType.Learn] = new StringKey("MSG_USE_OPERATE");
                UseMsgs[ItemData.UseType.Use] = new StringKey("MSG_USE");
            }
        }
    }


    /// <summary>
    /// Event that occurs when an item is thrown from the menu.
    /// This sets different battle context data depending if the item is sticky, whether the item is thrown in an arc, etc.
    /// </summary>
    [Serializable]
    public class PreThrowEvent : BattleEvent
    {
        /// <inheritdoc/>
        public PreThrowEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new PreThrowEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType != BattleActionType.Throw)
                yield break;
            if (context.UsageSlot == BattleContext.FORCED_SLOT)
                yield break;
            //Throw Item will have its own method, which will have its own charge-up step for throwing
            //hardcoded into the process is the removal of the item
            //the item class will have its own throw-item data for how the item will fly, which will be used as the char action on execution
            //all given targets will receive the effect of the item (if imported), or a differently specified effect (blast seed)
            //if the item hits no one, it must fall to the floor
            //if the item hits someone, and it's an AMMO type, it must fall to the floor
            //-or, it must not fall to the floor no matter what
            //The hitbox logic will take care of this?  If it does, it needs to be flexible to accomodate all specified cases
            //whenever an item hits a target, and they can catch, the target will catch the item
            //otherwise, the item will inflict its effect on the target
            //keep note: some late-game enemies will appear while holding items (chests?)
            //pierce-throwing makes items uncatchable
            //Friendly Fire: default off; when turned on, attacks that hit enemies will also hit allies and vice-versa
            //Friendly Item Fire: default on; turning it on will have thrown items hit allies as well as enemies

            context.StartDir = context.User.CharDir;

            //while attack, use, and throw, will all use the same battle contexts and hitboxes,
            //they will still have different Execution methods, as well as EndEffects
            InvItem item = null;
            if (context.UsageSlot > BattleContext.EQUIP_ITEM_SLOT)//item in inventory
                item = ((ExplorerTeam)context.User.MemberTeam).GetInv(context.UsageSlot);
            else if (context.UsageSlot == BattleContext.EQUIP_ITEM_SLOT)
                item = context.User.EquippedItem;
            else if (context.UsageSlot == BattleContext.FLOOR_ITEM_SLOT)
            {
                int mapSlot = ZoneManager.Instance.CurrentMap.GetItem(context.User.CharLoc);
                MapItem mapItem = ZoneManager.Instance.CurrentMap.Items[mapSlot];
                item = mapItem.MakeInvItem();
            }

            ItemData entry = DataManager.Instance.GetItem(item.ID);
            bool defaultDmg = false;
            bool catchable = true;


            if (entry.UsageType == ItemData.UseType.None || entry.UsageType == ItemData.UseType.Treasure || entry.UsageType == ItemData.UseType.Use || entry.UsageType == ItemData.UseType.Learn || entry.UsageType == ItemData.UseType.Box || entry.UsageType == ItemData.UseType.UseOther)
                defaultDmg = true;
            else if (entry.ItemStates.Contains<RecruitState>())
                catchable = false;
            //otherwise normally catchable, but depends on the target...

            if (item.Cursed)
                defaultDmg = true;

            if (defaultDmg)
            {
                //these just do damage(create a custom effect in stead of the item's effect)
                context.Data = new BattleData();
                context.Data.Element = DataManager.Instance.DefaultElement;
                context.Data.ID = item.GetID();
                context.Data.DataType = DataManager.DataType.Item;

                context.Data.Category = BattleData.SkillCategory.Physical;
                context.Data.SkillStates.Set(new BasePowerState(30));
                context.Data.OnHits.Add(-1, new DamageFormulaEvent());
            }
            else
            {
                context.Data = new BattleData(entry.UseEvent);
                context.Data.ID = item.GetID();
                context.Data.DataType = DataManager.DataType.Item;
            }

            if (catchable)
                context.Data.BeforeExplosions.Add(-5, new CatchItemSplashEvent());
            if (!entry.BreakOnThrow)
                context.Data.AfterActions.Add(-1, new LandItemEvent());
            context.Item = new InvItem(item);
            if (entry.MaxStack > 1)
            {
                //TODO: Price needs to be multiplied by amount instead of dividing
                context.Item.Price = context.Item.Price / context.Item.Amount;
                context.Item.Amount = 1;
            }
            context.Strikes = 1;
            //create the action from scratch
            if (entry.ArcThrow)
            {
                ThrowAction action = new ThrowAction();
                action.CharAnimData = new CharAnimFrameType(42);//Rotate
                action.Coverage = ThrowAction.ArcCoverage.WideAngle;
                action.TargetAlignments = Alignment.Foe;
                action.Anim = new AnimData(entry.ThrowAnim);
                action.ItemSprite = DataManager.Instance.GetItem(item.ID).Sprite;
                BattleFX newFX = new BattleFX();
                newFX.Sound = "DUN_Throw_Start";
                action.PreActions.Add(newFX);
                action.ActionFX.Sound = "DUN_Throw_Arc";
                action.Speed = 10;
                action.Range = 6;
                context.HitboxAction = action;
                context.Explosion = new ExplosionData(entry.Explosion);
            }
            else
            {
                ProjectileAction action = new ProjectileAction();
                action.CharAnimData = new CharAnimFrameType(42);//Rotate
                action.TargetAlignments = Alignment.Friend | Alignment.Foe;
                action.Anim = new AnimData(entry.ThrowAnim);
                action.ItemSprite = DataManager.Instance.GetItem(item.ID).Sprite;
                BattleFX newFX = new BattleFX();
                newFX.Sound = "DUN_Throw_Start";
                action.PreActions.Add(newFX);
                if (entry.ItemStates.Contains<AmmoState>())
                    action.ActionFX.Sound = "DUN_Throw_Spike";
                else
                    action.ActionFX.Sound = "DUN_Throw_Something";
                action.Speed = 14;
                action.Range = 8;
                action.StopAtHit = true;
                action.StopAtWall = true;
                action.HitTiles = true;
                context.HitboxAction = action;
                context.Explosion = new ExplosionData(entry.Explosion);
                context.Explosion.TargetAlignments = Alignment.Friend | Alignment.Foe | Alignment.Self;
            }

            context.SetActionMsg(Text.FormatGrammar(new StringKey("MSG_THROW").ToLocal(), context.User.GetDisplayName(false), context.Item.GetDisplayName()));

            if (context.UsageSlot == BattleContext.EQUIP_ITEM_SLOT && context.User.EquippedItem.Cursed && !context.User.CanRemoveStuck)
            {
                GameManager.Instance.BattleSE("DUN_Sticky");
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_THROW_CURSED").ToLocal(), context.User.GetDisplayName(false), item.GetDisplayName()), false, true);
                context.CancelState.Cancel = true;
            }
        }
    }


    /// <summary>
    /// Event that converts a SingleCharEvent to a battle event.
    /// </summary>
    [Serializable]
    public class BattlelessEvent : BattleEvent
    {
        /// <summary>
        /// The SingleCharEvent being converted.
        /// </summary>
        public SingleCharEvent BaseEvent;

        /// <summary>
        /// Whether to affect the target or user.
        /// </summary>
        public bool AffectTarget;

        /// <inheritdoc/>
        public BattlelessEvent() { }

        /// <summary>
        /// Creates a new BattlelessEvent with the specified parameters.
        /// </summary>
        /// <param name="affectTarget">Whether to affect the target or user.</param>
        /// <param name="effect">The single character event to wrap.</param>
        public BattlelessEvent(bool affectTarget, SingleCharEvent effect)
        {
            AffectTarget = affectTarget;
            BaseEvent = effect;
        }

        /// <inheritdoc/>
        protected BattlelessEvent(BattlelessEvent other)
        {
            BaseEvent = (SingleCharEvent)other.BaseEvent.Clone();
            AffectTarget = other.AffectTarget;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new BattlelessEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            SingleCharContext singleContext = new SingleCharContext(AffectTarget ? context.Target : context.User);
            yield return CoroutineManager.Instance.StartCoroutine(BaseEvent.Apply(owner, ownerChar, singleContext));
        }
    }


    /// <summary>
    /// Event that groups multiple battle events into one event.
    /// Can be used for hit-consequence effects.
    /// </summary>
    [Serializable]
    public class GroupEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle events that will be applied.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public GroupEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <summary>
        /// Creates a new GroupEvent with the specified events.
        /// </summary>
        /// <param name="effects">The battle events to group.</param>
        public GroupEvent(params BattleEvent[] effects)
        {
            BaseEvents = new List<BattleEvent>();
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected GroupEvent(GroupEvent other)
            : this()
        {
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new GroupEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            foreach (BattleEvent battleEffect in BaseEvents)
                yield return CoroutineManager.Instance.StartCoroutine(battleEffect.Apply(owner, ownerChar, context));
        }
    }

    /// <summary>
    /// Event that chooses a random battle event.
    /// Can be used for hit-consequence effects with randomized outcomes.
    /// </summary>
    [Serializable]
    public class ChooseOneEvent : BattleEvent
    {
        /// <summary>
        /// The list of battle events to choose from.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <inheritdoc/>
        public ChooseOneEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <summary>
        /// Creates a new ChooseOneEvent with the specified events.
        /// </summary>
        /// <param name="effects">The battle events to choose from.</param>
        public ChooseOneEvent(params BattleEvent[] effects)
        {
            BaseEvents = new List<BattleEvent>();
            foreach (BattleEvent effect in effects)
                BaseEvents.Add(effect);
        }

        /// <inheritdoc/>
        protected ChooseOneEvent(ChooseOneEvent other)
            : this()
        {
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new ChooseOneEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            yield return CoroutineManager.Instance.StartCoroutine(BaseEvents[DataManager.Instance.Save.Rand.Next(BaseEvents.Count)].Apply(owner, ownerChar, context));
        }
    }

    /// <summary>
    /// Event that displays the dialogue when interacting with an NPC.
    /// This event should usually be placed inside the NPC's MobSpawnInteractable spawn feature.
    /// </summary>
    [Serializable]
    public class NpcDialogueBattleEvent : BattleEvent
    {
        /// <summary>
        /// The message displayed when interacting with the NPC.
        /// </summary>
        public StringKey Message;

        /// <summary>
        /// Whether to hide the speaker portrait.
        /// </summary>
        public bool HideSpeaker;

        /// <summary>
        /// The portrait emotion.
        /// </summary>
        public EmoteStyle Emote;

        /// <inheritdoc/>
        public NpcDialogueBattleEvent() { }

        /// <summary>
        /// Creates a new NpcDialogueBattleEvent with the specified message.
        /// </summary>
        /// <param name="message">The dialogue message to display.</param>
        public NpcDialogueBattleEvent(StringKey message) : this(message, false) { }

        /// <summary>
        /// Creates a new NpcDialogueBattleEvent with the specified message and speaker visibility.
        /// </summary>
        /// <param name="message">The dialogue message to display.</param>
        /// <param name="hideSpeaker">Whether to hide the speaker portrait.</param>
        public NpcDialogueBattleEvent(StringKey message, bool hideSpeaker)
        {
            Message = message;
            HideSpeaker = hideSpeaker;
        }

        /// <inheritdoc/>
        protected NpcDialogueBattleEvent(NpcDialogueBattleEvent other)
        {
            Message = other.Message;
            HideSpeaker = other.HideSpeaker;
            Emote = other.Emote;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new NpcDialogueBattleEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (DataManager.Instance.CurrentReplay == null)
            {
                if (HideSpeaker)
                    yield return CoroutineManager.Instance.StartCoroutine(MenuManager.Instance.SetDialogue(Message.ToLocal()));
                else
                {
                    Dir8 oldDir = context.Target.CharDir;
                    context.Target.CharDir = context.User.CharDir.Reverse();
                    Character target = context.Target;
                    yield return CoroutineManager.Instance.StartCoroutine(MenuManager.Instance.SetDialogue(target.Appearance, target.GetDisplayName(true), Emote, true, Message.ToLocal()));
                    context.Target.CharDir = oldDir;
                }
                context.CancelState.Cancel = true;
            }
        }
    }

    /// <summary>
    /// Event that logs a StringKey message to the dungeon log.
    /// </summary>
    [Serializable]
    public class BattleLogBattleEvent : BattleEvent
    {
        /// <summary>
        /// The message displayed in the dungeon log.
        /// </summary>
        public StringKey Message;

        /// <summary>
        /// Whether to have a short delay after displaying the message.
        /// </summary>
        public bool Delay;

        /// <summary>
        /// Whether to use the target or user name when formatting the message.
        /// </summary>
        public bool UseTarget;

        /// <inheritdoc/>
        public BattleLogBattleEvent() { }

        /// <summary>
        /// Creates a new BattleLogBattleEvent with the specified message.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public BattleLogBattleEvent(StringKey message) : this(message, false) { }

        /// <summary>
        /// Creates a new BattleLogBattleEvent with the specified message and delay.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="delay">Whether to delay after displaying.</param>
        public BattleLogBattleEvent(StringKey message, bool delay) : this(message, delay, false) { }

        /// <summary>
        /// Creates a new BattleLogBattleEvent with the specified parameters.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="delay">Whether to delay after displaying.</param>
        /// <param name="useTarget">Whether to use target's name instead of user's.</param>
        public BattleLogBattleEvent(StringKey message, bool delay, bool useTarget)
        {
            Message = message;
            Delay = delay;
            UseTarget = useTarget;
        }

        /// <inheritdoc/>
        protected BattleLogBattleEvent(BattleLogBattleEvent other)
        {
            Message = other.Message;
            Delay = other.Delay;
            UseTarget = other.UseTarget;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new BattleLogBattleEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (UseTarget)
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.Target.GetDisplayName(false)));
            else
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), context.User.GetDisplayName(false)));
            if (Delay)
                yield return new WaitForFrames(GameManager.Instance.ModifyBattleSpeed(10));
        }
    }

    /// <summary>
    /// Event that logs a string message to the dungeon log.
    /// </summary>
    [Serializable]
    public class FormatLogLocalEvent : BattleEvent
    {
        /// <summary>
        /// The message displayed in the dungeon log.
        /// </summary>
        public string Message;

        /// <summary>
        /// Whether to have a short delay after displaying the message.
        /// </summary>
        public bool Delay;

        /// <inheritdoc/>
        public FormatLogLocalEvent() { }

        /// <summary>
        /// Creates a new FormatLogLocalEvent with the specified message.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public FormatLogLocalEvent(string message) : this(message, false) { }

        /// <summary>
        /// Creates a new FormatLogLocalEvent with the specified message and delay.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="delay">Whether to delay after displaying.</param>
        public FormatLogLocalEvent(string message, bool delay)
        {
            Message = message;
            Delay = delay;
        }

        /// <inheritdoc/>
        protected FormatLogLocalEvent(FormatLogLocalEvent other)
        {
            Message = other.Message;
            Delay = other.Delay;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new FormatLogLocalEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            DungeonScene.Instance.LogMsg(Message);
            if (Delay)
                yield return new WaitForFrames(GameManager.Instance.ModifyBattleSpeed(10));
        }
    }

    /// <summary>
    /// Event that displays a message to the dungeon log once.
    /// Uses context state to track if the message has already been shown.
    /// </summary>
    [Serializable]
    public class MessageOnceEvent : BattleEvent
    {
        /// <summary>
        /// The context state added to indicate if the message was displayed.
        /// </summary>
        public ContextState AddedState;

        /// <summary>
        /// Whether to add the context state globally.
        /// </summary>
        public bool Global;

        /// <summary>
        /// Whether to display the target or user name in the message.
        /// </summary>
        public bool AffectTarget;

        /// <summary>
        /// The message displayed in the dungeon log if the condition is met.
        /// </summary>
        [StringKey(0, true)]
        public StringKey Message;

        /// <summary>
        /// The list of battle VFXs played if the condition is met.
        /// </summary>
        public List<BattleAnimEvent> Anims;

        /// <inheritdoc/>
        public MessageOnceEvent()
        {
            Anims = new List<BattleAnimEvent>();
        }

        /// <summary>
        /// Creates a new MessageOnceEvent with the specified parameters.
        /// </summary>
        /// <param name="state">The context state to track message display.</param>
        /// <param name="global">Whether to use global context state.</param>
        /// <param name="affectTarget">Whether to use target's name in message.</param>
        /// <param name="anim">The animation to play.</param>
        /// <param name="msg">The message to display.</param>
        public MessageOnceEvent(ContextState state, bool global, bool affectTarget, BattleAnimEvent anim, StringKey msg)
        {
            Anims = new List<BattleAnimEvent>();
            AddedState = state;
            Global = global;
            AffectTarget = affectTarget;
            Anims.Add(anim);
            Message = msg;
        }

        /// <inheritdoc/>
        protected MessageOnceEvent(MessageOnceEvent other)
        {
            Anims = new List<BattleAnimEvent>();
            AddedState = other.AddedState.Clone<ContextState>();
            Global = other.Global;
            AffectTarget = other.AffectTarget;
            Anims.AddRange(other.Anims);
            Message = other.Message;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new MessageOnceEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (Global)
            {
                if (context.GlobalContextStates.Contains(AddedState.GetType()))
                    yield break;
                else
                    context.GlobalContextStates.Set(AddedState.Clone<ContextState>());
            }
            else
            {
                if (context.ContextStates.Contains(AddedState.GetType()))
                    yield break;
                else
                    context.ContextStates.Set(AddedState.Clone<ContextState>());
            }

            Character target = (AffectTarget ? context.Target : context.User);

            if (Message.IsValid())
                DungeonScene.Instance.LogMsg(Text.FormatGrammar(Message.ToLocal(), ownerChar.GetDisplayName(false), target.GetDisplayName(false)));

            foreach (BattleAnimEvent anim in Anims)
                yield return CoroutineManager.Instance.StartCoroutine(anim.Apply(owner, ownerChar, context));

        }
    }

    /// <summary>
    /// Event that applies a VFX on a character
    /// </summary>
    [Serializable]
    public class BattleAnimEvent : BattleEvent
    {
        /// <summary>
        /// The particle VFX 
        /// </summary>
        public FiniteEmitter Emitter;

        //TODO: make this into BattleFX?
        /// <summary>
        /// The sound effect of the VFX
        /// </summary>
        [Sound(0)]
        public string Sound;

        /// <summary>
        /// Whether to apply the VFX on the target or user
        /// </summary>
        public bool AffectTarget;

        /// <summary>
        /// The delay after the VFX
        /// </summary>
        public int Delay;

        public BattleAnimEvent()
        {
            Emitter = new EmptyFiniteEmitter();
        }
        public BattleAnimEvent(FiniteEmitter emitter, string sound, bool affectTarget) : this(emitter, sound, affectTarget, 0) { }
        public BattleAnimEvent(FiniteEmitter emitter, string sound, bool affectTarget, int delay)
        {
            Emitter = emitter;
            Sound = sound;
            AffectTarget = affectTarget;
            Delay = delay;
        }
        protected BattleAnimEvent(BattleAnimEvent other)
        {
            Emitter = (FiniteEmitter)other.Emitter.Clone();
            Sound = other.Sound;
            AffectTarget = other.AffectTarget;
            Delay = other.Delay;
        }
        public override GameEvent Clone() { return new BattleAnimEvent(this); }

        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Character target = (AffectTarget ? context.Target : context.User);

            GameManager.Instance.BattleSE(Sound);
            if (!target.Unidentifiable)
            {
                FiniteEmitter endEmitter = (FiniteEmitter)Emitter.Clone();
                endEmitter.SetupEmit(target.MapLoc, target.MapLoc, target.CharDir);
                DungeonScene.Instance.CreateAnim(endEmitter, DrawLayer.NoDraw);
            }
            yield return new WaitForFrames(GameManager.Instance.ModifyBattleSpeed(Delay));
        }
    }

    /// <summary>
    /// Event that applies a VFX on the owner character only
    /// </summary>
    [Serializable]
    public class OwnerBattleAnimEvent : BattleEvent
    {
        /// <summary>
        /// The particle VFX 
        /// </summary>
        public FiniteEmitter Emitter;

        //TODO: make this into BattleFX?
        /// <summary>
        /// The sound effect of the VFX
        /// </summary>
        [Sound(0)]
        public string Sound;

        /// <summary>
        /// The delay after the VFX
        /// </summary>
        public int Delay;

        public OwnerBattleAnimEvent()
        {
            Emitter = new EmptyFiniteEmitter();
        }
        public OwnerBattleAnimEvent(FiniteEmitter emitter, string sound) : this(emitter, sound, 0) { }
        public OwnerBattleAnimEvent(FiniteEmitter emitter, string sound, int delay)
        {
            Emitter = emitter;
            Sound = sound;
            Delay = delay;
        }
        protected OwnerBattleAnimEvent(OwnerBattleAnimEvent other)
        {
            Emitter = (FiniteEmitter)other.Emitter.Clone();
            Sound = other.Sound;
            Delay = other.Delay;
        }
        public override GameEvent Clone() { return new OwnerBattleAnimEvent(this); }

        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Character target = ownerChar;

            GameManager.Instance.BattleSE(Sound);
            if (!target.Unidentifiable)
            {
                FiniteEmitter endEmitter = (FiniteEmitter)Emitter.Clone();
                endEmitter.SetupEmit(target.MapLoc, target.MapLoc, target.CharDir);
                DungeonScene.Instance.CreateAnim(endEmitter, DrawLayer.NoDraw);
            }
            yield return new WaitForFrames(GameManager.Instance.ModifyBattleSpeed(Delay));
        }
    }


    /// <summary>
    /// Event that increments the AttackHitTotal global context state and tracks combat statistics.
    /// </summary>
    [Serializable]
    public class HitPostEvent : BattleEvent
    {
        /// <summary>
        /// The status that tracks recent hits on this turn.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string RecentHitStatusID;

        /// <summary>
        /// The status that tracks the last hit received.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string LastHitStatusID;

        /// <summary>
        /// The status that stores the ID of the last move that hit the target.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string OtherHitStatusID;

        /// <summary>
        /// The status that tracks who last attacked this character.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string TargetStatusID;

        /// <summary>
        /// The status that tracks critical hits dealt by this character.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string CritStatusID;

        public HitPostEvent()
        {
            RecentHitStatusID = "";
            LastHitStatusID = "";
            OtherHitStatusID = "";
            TargetStatusID = "";
            CritStatusID = "";
        }
        public HitPostEvent(string recentHitStatusID, string lastHitStatusID, string otherHitStatusID, string targetStatusID, string critStatusID)
        {
            RecentHitStatusID = recentHitStatusID;
            LastHitStatusID = lastHitStatusID;
            OtherHitStatusID = otherHitStatusID;
            TargetStatusID = targetStatusID;
            CritStatusID = critStatusID;
        }
        protected HitPostEvent(HitPostEvent other)
        {
            RecentHitStatusID = other.RecentHitStatusID;
            LastHitStatusID = other.LastHitStatusID;
            OtherHitStatusID = other.OtherHitStatusID;
            TargetStatusID = other.TargetStatusID;
            CritStatusID = other.CritStatusID;
        }
        public override GameEvent Clone() { return new HitPostEvent(this); }

        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Trap)
                yield break;

            context.ContextStates.Set(new AttackHit());
            AttackHitTotal totalHit = context.GlobalContextStates.GetWithDefault<AttackHitTotal>();
            if (totalHit != null)
                totalHit.Count++;
            else
                context.GlobalContextStates.Set(new AttackHitTotal(1));

            if (context.Target != context.User)
            {
                int dmg = context.GetContextStateInt<DamageDealt>(0);
                if (dmg > 0)
                {
                    StatusEffect recentHitStatus = context.Target.GetStatusEffect(RecentHitStatusID);
                    if (recentHitStatus == null)
                    {
                        recentHitStatus = new StatusEffect(RecentHitStatusID);
                        recentHitStatus.LoadFromData();
                        recentHitStatus.StatusStates.GetWithDefault<StackState>().Stack = 1;
                        yield return CoroutineManager.Instance.StartCoroutine(context.Target.AddStatusEffect(context.User, recentHitStatus));
                    }
                    else
                        recentHitStatus.StatusStates.GetWithDefault<StackState>().Stack = recentHitStatus.StatusStates.GetWithDefault<StackState>().Stack + 1;

                    StatusEffect lastHitStatus = context.Target.GetStatusEffect(LastHitStatusID);
                    if (lastHitStatus == null)
                    {
                        lastHitStatus = new StatusEffect(LastHitStatusID);
                        lastHitStatus.LoadFromData();
                        lastHitStatus.StatusStates.GetWithDefault<StackState>().Stack = 1;
                        yield return CoroutineManager.Instance.StartCoroutine(context.Target.AddStatusEffect(context.User, lastHitStatus));
                    }
                    else
                        lastHitStatus.StatusStates.GetWithDefault<StackState>().Stack = lastHitStatus.StatusStates.GetWithDefault<StackState>().Stack + 1;
                }

                if (context.ContextStates.Contains<AttackCrit>())
                {
                    StatusEffect recentCritStatus = context.User.GetStatusEffect(CritStatusID);
                    if (recentCritStatus == null)
                    {
                        recentCritStatus = new StatusEffect(CritStatusID);
                        recentCritStatus.LoadFromData();
                        recentCritStatus.StatusStates.GetWithDefault<StackState>().Stack = 1;
                        yield return CoroutineManager.Instance.StartCoroutine(context.User.AddStatusEffect(context.User, recentCritStatus));
                    }
                    else
                        recentCritStatus.StatusStates.GetWithDefault<StackState>().Stack = recentCritStatus.StatusStates.GetWithDefault<StackState>().Stack + 1;
                }

                if (context.ActionType == BattleActionType.Skill && context.Data.ID != DataManager.Instance.DefaultSkill)
                {

                    StatusEffect otherStatus = new StatusEffect(OtherHitStatusID);
                    otherStatus.LoadFromData();
                    otherStatus.StatusStates.GetWithDefault<IDState>().ID = context.Data.ID;
                    yield return CoroutineManager.Instance.StartCoroutine(context.Target.AddStatusEffect(context.User, otherStatus));
                }

                if (context.User.MemberTeam.MapFaction != context.Target.MemberTeam.MapFaction)
                {
                    StatusEffect targetStatus = new StatusEffect(TargetStatusID);
                    targetStatus.LoadFromData();
                    targetStatus.TargetChar = context.User;
                    targetStatus.StatusStates.GetWithDefault<HPState>().HP = dmg;
                    yield return CoroutineManager.Instance.StartCoroutine(context.Target.AddStatusEffect(context.User, targetStatus));
                }
            }
        }
    }

    /// <summary>
    /// Event that tracks action usage statistics after an action is used.
    /// </summary>
    [Serializable]
    public class UsePostEvent : BattleEvent
    {
        /// <summary>
        /// The status that tracks repeated move usage.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string RepeatStatusID;

        /// <summary>
        /// The status that notifies allies of the move used.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string AllyStatusID;

        /// <summary>
        /// The status applied when the user missed all targets.
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [DataType(0, DataManager.DataType.Status, false)]
        public string MissedAllID;

        public UsePostEvent()
        {
            RepeatStatusID = "";
            AllyStatusID = "";
            MissedAllID = "";
        }
        public UsePostEvent(string repeatStatusID, string allyStatusID, string missedAllID)
        {
            RepeatStatusID = repeatStatusID;
            AllyStatusID = allyStatusID;
            MissedAllID = missedAllID;
        }
        protected UsePostEvent(UsePostEvent other)
        {
            RepeatStatusID = other.RepeatStatusID;
            AllyStatusID = other.AllyStatusID;
            MissedAllID = other.MissedAllID;
        }
        public override GameEvent Clone() { return new UsePostEvent(this); }

        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Skill && context.UsageSlot > BattleContext.DEFAULT_ATTACK_SLOT && context.UsageSlot < CharData.MAX_SKILL_SLOTS)
            {
                StatusEffect repeatStatus = context.User.GetStatusEffect(RepeatStatusID);
                if (repeatStatus != null && repeatStatus.StatusStates.GetWithDefault<RecentState>() != null)
                {
                    //increment repetition
                    repeatStatus.StatusStates.GetWithDefault<CountState>().Count++;
                    //reset turn counter
                    repeatStatus.StatusStates.GetWithDefault<CountDownState>().Counter = 0;
                }


                foreach (Character ally in context.User.GetSeenCharacters(Alignment.Friend))
                {
                    StatusEffect allyStatus = new StatusEffect(AllyStatusID);
                    allyStatus.LoadFromData();
                    allyStatus.StatusStates.GetWithDefault<IDState>().ID = context.Data.ID;
                    yield return CoroutineManager.Instance.StartCoroutine(ally.AddStatusEffect(context.User, allyStatus));
                }

                if (context.GetContextStateInt<AttackHitTotal>(true, 0) == 0)
                {
                    StatusEffect missedAllStatus = new StatusEffect(MissedAllID);
                    missedAllStatus.LoadFromData();
                    yield return CoroutineManager.Instance.StartCoroutine(context.User.AddStatusEffect(context.User, missedAllStatus));
                }
                else
                {
                    yield return CoroutineManager.Instance.StartCoroutine(context.User.RemoveStatusEffect(MissedAllID, false));
                }
            }
        }
    }


    /// <summary>
    /// Event that reveals the tile and queues its affects
    /// </summary>
    [Serializable]
    public class TilePostEvent : BattleEvent
    {
        public override GameEvent Clone() { return new TilePostEvent(); }

        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Trap)
                yield break;

            if (context.Data.Category == BattleData.SkillCategory.None)
                yield break;

            Tile tile = ZoneManager.Instance.CurrentMap.GetTile(context.TargetTile);
            if (tile == null)
                yield break;

            if (!String.IsNullOrEmpty(tile.Effect.ID) && ZoneManager.Instance.CurrentMap.GetTileOwner(context.User) != tile.Effect.Owner)
            {
                TileData entry = DataManager.Instance.GetTile(tile.Effect.GetID());
                if (entry.StepType == TileData.TriggerType.Trap)
                {
                    if (context.ActionType == BattleActionType.Skill && context.Data.ID == DataManager.Instance.DefaultSkill)
                        tile.Effect.Revealed = true;
                    else if (tile.Effect.Owner != EffectTile.TileOwner.None && ZoneManager.Instance.CurrentMap.GetTileOwner(context.Target) == tile.Effect.Owner)
                    {
                        //sort of a hack, meant to prevent the following scenario:
                        //character A sets a trap underfoot and owns it, expecting the trap to not hurt it
                        //character B attacks character A, hitting the tile with the owner on it
                        //character A takes the effect of the trap

                        //this is a fall-through case
                    }
                    else
                    {
                        DungeonScene.Instance.QueueTrap(context.TargetTile);
                        //yield return CoroutineManager.Instance.StartCoroutine(tile.Effect.InteractWithTile(context.User));
                    }
                }
                else
                {
                    if (!tile.Effect.Revealed)
                    {
                        GameManager.Instance.BattleSE("DUN_Smokescreen");
                        SingleEmitter emitter = new SingleEmitter(new AnimData("Puff_Brown", 3));
                        emitter.Layer = DrawLayer.Front;
                        emitter.SetupEmit(context.TargetTile * GraphicsManager.TileSize + new Loc(GraphicsManager.TileSize / 2), context.TargetTile * GraphicsManager.TileSize + new Loc(GraphicsManager.TileSize / 2), context.User.CharDir);
                        DungeonScene.Instance.CreateAnim(emitter, DrawLayer.NoDraw);
                        tile.Effect.Revealed = true;
                    }
                }
            }
        }
    }


}


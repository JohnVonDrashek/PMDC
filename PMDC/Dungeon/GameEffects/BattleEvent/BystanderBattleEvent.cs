using System;
using System.Collections.Generic;
using RogueEssence.Data;
using RogueElements;
using RogueEssence.Content;
using RogueEssence;
using RogueEssence.Dungeon;
using RogueEssence.Dev;
using Newtonsoft.Json;

namespace PMDC.Dungeon
{
    // For battle events that occur for characters that aren't the target of the attack

    /// <summary>
    /// Event that boosts the damage of magical attacks if the user has the specified ability.
    /// Used for abilities that enhance magic power when an ally is nearby.
    /// </summary>
    [Serializable]
    public class SupportAbilityEvent : BattleEvent
    {
        /// <summary>
        /// The intrinsic ability ID that qualifies for the damage boost.
        /// </summary>
        [JsonConverter(typeof(IntrinsicConverter))]
        [DataType(0, DataManager.DataType.Intrinsic, false)]
        public string SupportAbility;

        /// <summary>
        /// Initializes a new instance of the <see cref="SupportAbilityEvent"/> class with default values.
        /// </summary>
        public SupportAbilityEvent() { SupportAbility = ""; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SupportAbilityEvent"/> class with the specified ability.
        /// </summary>
        /// <param name="supportAbility">The intrinsic ability ID required for the boost.</param>
        public SupportAbilityEvent(string supportAbility)
        {
            SupportAbility = supportAbility;
        }
        /// <summary>
        /// Copy constructor for cloning an existing SupportAbilityEvent.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected SupportAbilityEvent(SupportAbilityEvent other)
        {
            SupportAbility = other.SupportAbility;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new SupportAbilityEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.Data.Category == BattleData.SkillCategory.Magical
                && context.User.HasIntrinsic(SupportAbility))
                context.AddContextStateMult<DmgMult>(false, 4, 3);
            yield break;
        }
    }

    /// <summary>
    /// Event that allows a bystander to snatch a status move targeting the user and redirect it to themselves.
    /// This is triggered when an ally uses a beneficial status move on themselves.
    /// </summary>
    [Serializable]
    public class SnatchEvent : BattleEvent
    {
        /// <summary>
        /// The visual emitter effect played when snatching the move.
        /// </summary>
        public FiniteEmitter Emitter;

        /// <summary>
        /// The sound effect played when snatching the move.
        /// </summary>
        [Sound(0)]
        public string Sound;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnatchEvent"/> class with default values.
        /// </summary>
        public SnatchEvent() { Emitter = new EmptyFiniteEmitter(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnatchEvent"/> class with the specified emitter and sound.
        /// </summary>
        /// <param name="emitter">The visual emitter effect.</param>
        /// <param name="sound">The sound effect ID.</param>
        public SnatchEvent(FiniteEmitter emitter, string sound)
            : this()
        {
            Emitter = emitter;
            Sound = sound;
        }

        /// <summary>
        /// Copy constructor for cloning an existing SnatchEvent.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected SnatchEvent(SnatchEvent other)
            : this()
        {
            Emitter = (FiniteEmitter)other.Emitter.Clone();
            Sound = other.Sound;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new SnatchEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ContextStates.Contains<Redirected>())
                yield break;

            if (context.ActionType == BattleActionType.Trap || context.ActionType == BattleActionType.Item)
                yield break;

            //must be a status move
            if (context.Data.Category != BattleData.SkillCategory.Status)
                yield break;

            //attacker must be target
            if (context.User != context.Target)
                yield break;


            GameManager.Instance.BattleSE(Sound);
            if (!ownerChar.Unidentifiable)
            {
                FiniteEmitter endEmitter = (FiniteEmitter)Emitter.Clone();
                endEmitter.SetupEmit(ownerChar.MapLoc, ownerChar.MapLoc, ownerChar.CharDir);
                DungeonScene.Instance.CreateAnim(endEmitter, DrawLayer.NoDraw);
            }

            CharAnimAction SpinAnim = new CharAnimAction(ownerChar.CharLoc, ZoneManager.Instance.CurrentMap.ApproximateClosestDir8(ownerChar.CharLoc, context.Target.CharLoc), 05);//Attack
            SpinAnim.MajorAnim = true;

            yield return CoroutineManager.Instance.StartCoroutine(ownerChar.StartAnim(SpinAnim));
            yield return new WaitWhile(ownerChar.OccupiedwithAction);

            DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_SNATCH").ToLocal(), ownerChar.GetDisplayName(false)));
            context.Target = ownerChar;
            context.ContextStates.Set(new Redirected());
        }
    }


    // Below, the effects deal exclusively with explosions

    /// <summary>
    /// Event that replaces the normal explosion effects with alternative effects when the explosion hits an ally.
    /// This allows explosions to have different behavior for friendly targets.
    /// </summary>
    [Serializable]
    public class AllyDifferentExplosionEvent : BattleEvent
    {
        /// <summary>
        /// The alternative battle events to apply when the explosion hits an ally.
        /// These replace the normal OnHits and remove base power.
        /// </summary>
        public List<BattleEvent> BaseEvents;

        /// <summary>
        /// Initializes a new instance of the <see cref="AllyDifferentExplosionEvent"/> class with default values.
        /// </summary>
        public AllyDifferentExplosionEvent() { BaseEvents = new List<BattleEvent>(); }

        /// <summary>
        /// Copy constructor for cloning an existing AllyDifferentExplosionEvent.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected AllyDifferentExplosionEvent(AllyDifferentExplosionEvent other)
            : this()
        {
            foreach (BattleEvent battleEffect in other.BaseEvents)
                BaseEvents.Add((BattleEvent)battleEffect.Clone());
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new AllyDifferentExplosionEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Character targetChar = ZoneManager.Instance.CurrentMap.GetCharAtLoc(context.ExplosionTile);
            if (targetChar == null)
                yield break;

            if (DungeonScene.Instance.GetMatchup(context.User, targetChar) == Alignment.Friend)
            {
                //remove all MoveHit effects (except for the post-effect)
                context.Data.OnHits.Clear();
                context.Data.OnHitTiles.Clear();
                //remove BasePower component
                if (context.Data.SkillStates.Contains<BasePowerState>())
                    context.Data.SkillStates.Remove<BasePowerState>();

                //add the alternative effects
                foreach (BattleEvent battleEffect in BaseEvents)
                    context.Data.OnHits.Add(0, (BattleEvent)battleEffect.Clone());
            }
        }
    }

    /// <summary>
    /// Event that dampens explosions, reducing their range to zero and optionally modifying their damage.
    /// Used for abilities that suppress explosive attacks.
    /// </summary>
    [Serializable]
    public class DampEvent : BattleEvent
    {
        /// <summary>
        /// The damage divisor. Positive values divide damage, negative values multiply it.
        /// </summary>
        public int Div;

        /// <summary>
        /// The message displayed when the explosion is dampened.
        /// </summary>
        StringKey Msg;

        /// <summary>
        /// Initializes a new instance of the <see cref="DampEvent"/> class with default values.
        /// </summary>
        public DampEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DampEvent"/> class with the specified divisor and message.
        /// </summary>
        /// <param name="div">The damage divisor.</param>
        /// <param name="msg">The message to display.</param>
        public DampEvent(int div, StringKey msg)
        {
            Div = div;
            Msg = msg;
        }

        /// <summary>
        /// Copy constructor for cloning an existing DampEvent.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected DampEvent(DampEvent other)
        {
            Div = other.Div;
            Msg = other.Msg;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new DampEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            //only block explosions
            if (context.Explosion.Range == 0)
                yield break;

            //make sure to exempt Round.

            DungeonScene.Instance.LogMsg(Text.FormatGrammar(Msg.ToLocal(), ownerChar.GetDisplayName(false)));
            context.Explosion.Range = 0;
            context.Explosion.ExplodeFX = new BattleFX();
            context.Explosion.Emitter = new EmptyCircleSquareEmitter();
            context.Explosion.TileEmitter = new EmptyFiniteEmitter();
            if (Div > 0)
                context.AddContextStateMult<DmgMult>(false,1, Div);
            else
                context.AddContextStateMult<DmgMult>(false,Div, 1);
        }
    }

    /// <summary>
    /// Event that dampens explosions from thrown items, removing their splash effect.
    /// Does not affect recruit items.
    /// </summary>
    [Serializable]
    public class DampItemEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new DampItemEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Throw)
            {
                ItemData entry = DataManager.Instance.GetItem(context.Item.ID);
                if (!entry.ItemStates.Contains<RecruitState>())
                {
                    context.Explosion.Range = 0;
                    context.Explosion.ExplodeFX = new BattleFX();
                    context.Explosion.Emitter = new EmptyCircleSquareEmitter();
                    context.Explosion.TileEmitter = new EmptyFiniteEmitter();
                }
            }
            yield break;
        }
    }


    /// <summary>
    /// Event that allows characters to catch thrown items before they splash.
    /// Prevents the explosion effect and sets up a catch event on hit.
    /// Items that are already held, edible for allies, or ammo for wild teams cannot be caught.
    /// </summary>
    [Serializable]
    public class CatchItemSplashEvent : BattleEvent
    {
        /// <inheritdoc/>
        public override GameEvent Clone() { return new CatchItemSplashEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ActionType == BattleActionType.Throw)
            {
                //can't catch pierce
                if (context.HitboxAction is LinearAction && !((LinearAction)context.HitboxAction).StopAtHit)
                    yield break;

                Character targetChar = ZoneManager.Instance.CurrentMap.GetCharAtLoc(context.ExplosionTile);
                if (targetChar != null)
                {

                    //can't catch when holding
                    if (!String.IsNullOrEmpty(targetChar.EquippedItem.ID))
                        yield break;

                    ItemData entry = DataManager.Instance.GetItem(context.Item.ID);

                    //can't catch recruit item under any circumstances
                    if (entry.ItemStates.Contains<RecruitState>())
                        yield break;


                    if (targetChar.MemberTeam is MonsterTeam)
                    {
                        //can't catch if it's a wild team, and it's an edible or ammo
                        if (entry.ItemStates.Contains<EdibleState>() || entry.ItemStates.Contains<AmmoState>())
                            yield break;
                    }

                    // throwing edibles at an ally always results in no-catch (eaten)
                    if (DungeonScene.Instance.GetMatchup(context.User, targetChar) == Alignment.Friend && entry.ItemStates.Contains<EdibleState>())
                        yield break;

                    context.Explosion.Range = 0;
                    context.Explosion.ExplodeFX = new BattleFX();
                    context.Explosion.Emitter = new EmptyCircleSquareEmitter();
                    context.Explosion.TileEmitter = new EmptyFiniteEmitter();



                    BattleData catchData = new BattleData();
                    catchData.Element = DataManager.Instance.DefaultElement;
                    catchData.OnHits.Add(0, new CatchItemEvent());
                    catchData.HitFX.Sound = "DUN_Equip";
                    context.Data.BeforeHits.Add(-5, new CatchableEvent(catchData));
                }
            }
        }
    }

    /// <summary>
    /// Event that isolates a character from an explosion of a matching element type.
    /// Reduces the explosion range to zero when the character is at the explosion center.
    /// </summary>
    [Serializable]
    public class IsolateElementEvent : BattleEvent
    {
        /// <summary>
        /// The element type to isolate. If default element, affects all elements.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string Element;

        /// <summary>
        /// Initializes a new instance of the <see cref="IsolateElementEvent"/> class with default values.
        /// </summary>
        public IsolateElementEvent() { Element = ""; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IsolateElementEvent"/> class with the specified element.
        /// </summary>
        /// <param name="element">The element type to isolate.</param>
        public IsolateElementEvent(string element)
        {
            Element = element;
        }
        /// <summary>
        /// Copy constructor for cloning an existing IsolateElementEvent.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected IsolateElementEvent(IsolateElementEvent other)
        {
            Element = other.Element;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new IsolateElementEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (Element != DataManager.Instance.DefaultElement && context.Data.Element != Element)
                yield break;

            if (ZoneManager.Instance.CurrentMap.GetCharAtLoc(context.ExplosionTile) != ownerChar)
                yield break;

            context.Explosion.Range = 0;
        }
    }

    /// <summary>
    /// Event that draws attacks of a specific element type to the owner character.
    /// Redirects the explosion center to the owner's location.
    /// </summary>
    [Serializable]
    public class DrawAttackEvent : BattleEvent
    {
        /// <summary>
        /// The element type to draw. If default element, draws all elements.
        /// </summary>
        [JsonConverter(typeof(ElementConverter))]
        [DataType(0, DataManager.DataType.Element, false)]
        public string Element;

        /// <summary>
        /// The alignment of targets from which to draw attacks.
        /// </summary>
        public Alignment DrawFrom;

        /// <summary>
        /// The message displayed when an attack is drawn.
        /// </summary>
        public StringKey Msg;

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawAttackEvent"/> class with default values.
        /// </summary>
        public DrawAttackEvent() { Element = ""; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawAttackEvent"/> class with the specified parameters.
        /// </summary>
        /// <param name="drawFrom">The alignment of targets from which to draw attacks.</param>
        /// <param name="element">The element type to draw.</param>
        /// <param name="msg">The message to display.</param>
        public DrawAttackEvent(Alignment drawFrom, string element, StringKey msg)
        {
            DrawFrom = drawFrom;
            Element = element;
            Msg = msg;
        }

        /// <summary>
        /// Copy constructor for cloning an existing DrawAttackEvent.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected DrawAttackEvent(DrawAttackEvent other)
        {
            DrawFrom = other.DrawFrom;
            Element = other.Element;
            Msg = other.Msg;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new DrawAttackEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ContextStates.Contains<Redirected>())
                yield break;

            if (context.ActionType == BattleActionType.Trap || context.ActionType == BattleActionType.Item)
                yield break;

            if (Element != DataManager.Instance.DefaultElement && context.Data.Element != Element)
                yield break;

            Character targetChar = ZoneManager.Instance.CurrentMap.GetCharAtLoc(context.ExplosionTile);
            if (targetChar == null)
                yield break;

            //the attack needs to be able to hit foes
            if ((context.HitboxAction.TargetAlignments & Alignment.Foe) == Alignment.None)
                yield break;

            //original target char needs to be a friend of the target char
            if ((DungeonScene.Instance.GetMatchup(ownerChar, targetChar) & DrawFrom) == Alignment.None)
                yield break;

            CharAnimSpin spinAnim = new CharAnimSpin();
            spinAnim.CharLoc = ownerChar.CharLoc;
            spinAnim.CharDir = ownerChar.CharDir;
            spinAnim.MajorAnim = true;

            yield return CoroutineManager.Instance.StartCoroutine(ownerChar.StartAnim(spinAnim));
            yield return new WaitWhile(ownerChar.OccupiedwithAction);

            DungeonScene.Instance.LogMsg(Text.FormatGrammar(Msg.ToLocal(), ownerChar.GetDisplayName(false), owner.GetDisplayName()));
            context.ExplosionTile = ownerChar.CharLoc;
            context.Explosion.Range = 0;
            context.ContextStates.Set(new Redirected());
        }
    }

    /// <summary>
    /// Event that allows a character to pass an attack to a nearby target at the cost of fullness.
    /// Redirects the attack to an adjacent character when the owner is targeted.
    /// </summary>
    [Serializable]
    public class PassAttackEvent : BattleEvent
    {
        /// <summary>
        /// The fullness cost to pass the attack.
        /// </summary>
        public int BellyCost;

        /// <summary>
        /// Initializes a new instance of the <see cref="PassAttackEvent"/> class with default values.
        /// </summary>
        public PassAttackEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PassAttackEvent"/> class with the specified belly cost.
        /// </summary>
        /// <param name="bellyCost">The fullness cost to pass the attack.</param>
        public PassAttackEvent(int bellyCost)
        {
            BellyCost = bellyCost;
        }
        /// <summary>
        /// Copy constructor for cloning an existing PassAttackEvent.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected PassAttackEvent(PassAttackEvent other)
        {
            BellyCost = other.BellyCost;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new PassAttackEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ContextStates.Contains<Redirected>())
                yield break;

            if (context.ActionType == BattleActionType.Trap || context.ActionType == BattleActionType.Item)
                yield break;

            //needs to be an attacking move
            if (context.Data.Category != BattleData.SkillCategory.Physical && context.Data.Category != BattleData.SkillCategory.Magical)
                yield break;

            if (ZoneManager.Instance.CurrentMap.GetCharAtLoc(context.ExplosionTile) != ownerChar)
                yield break;

            if (ownerChar.Fullness < BellyCost)
                yield break;
            
            foreach (Character newTarget in ZoneManager.Instance.CurrentMap.GetCharsInFillRect(ownerChar.CharLoc, Rect.FromPointRadius(ownerChar.CharLoc, 1)))
            {
                if (!newTarget.Dead && newTarget != ownerChar && newTarget != context.User)
                {
                    ownerChar.Fullness -= BellyCost;
                    if (ownerChar.Fullness < 0)
                        ownerChar.Fullness = 0;

                    CharAnimSpin spinAnim = new CharAnimSpin();
                    spinAnim.CharLoc = ownerChar.CharLoc;
                    spinAnim.CharDir = ownerChar.CharDir;
                    spinAnim.MajorAnim = true;

                    yield return CoroutineManager.Instance.StartCoroutine(ownerChar.StartAnim(spinAnim));
                    yield return new WaitWhile(ownerChar.OccupiedwithAction);

                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_PASS_ATTACK").ToLocal(), ownerChar.GetDisplayName(false), newTarget.GetDisplayName(false)));
                    context.ExplosionTile = newTarget.CharLoc;
                    context.Explosion.TargetAlignments |= Alignment.Foe;
                    context.Explosion.TargetAlignments |= Alignment.Friend;
                    context.ContextStates.Set(new Redirected());
                    yield break;
                }
            }
            
        }
    }

    /// <summary>
    /// Event that allows a character to cover for an ally by taking the attack instead.
    /// Only works when the owner has at least 50% HP and the target is a friend.
    /// </summary>
    [Serializable]
    public class CoverAttackEvent : BattleEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CoverAttackEvent"/> class.
        /// </summary>
        public CoverAttackEvent() { }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new CoverAttackEvent(); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ContextStates.Contains<Redirected>())
                yield break;

            if (context.ActionType == BattleActionType.Trap || context.ActionType == BattleActionType.Item)
                yield break;

            Character targetChar = ZoneManager.Instance.CurrentMap.GetCharAtLoc(context.ExplosionTile);
            if (targetChar == null)
                yield break;

            if (ownerChar.HP < ownerChar.MaxHP / 2)
                yield break;

            //char needs to be a friend of the target char
            if (DungeonScene.Instance.GetMatchup(ownerChar, targetChar) != Alignment.Friend)
                yield break;

            CharAnimSpin spinAnim = new CharAnimSpin();
            spinAnim.CharLoc = ownerChar.CharLoc;
            spinAnim.CharDir = ownerChar.CharDir;
            spinAnim.MajorAnim = true;

            yield return CoroutineManager.Instance.StartCoroutine(ownerChar.StartAnim(spinAnim));
            yield return new WaitWhile(ownerChar.OccupiedwithAction);

            DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_COVER_ATTACK").ToLocal(), ownerChar.GetDisplayName(false)));
            context.ExplosionTile = ownerChar.CharLoc;
            context.ContextStates.Set(new Redirected());        
        }
    }


    /// <summary>
    /// Event that allows a character to fetch a failed recruit item and pick it up.
    /// Used for abilities that retrieve thrown items that missed or bounced.
    /// </summary>
    [Serializable]
    public class FetchEvent : BattleEvent
    {
        /// <summary>
        /// The message displayed when fetching the item.
        /// </summary>
        public StringKey Msg;

        /// <summary>
        /// Initializes a new instance of the <see cref="FetchEvent"/> class with default values.
        /// </summary>
        public FetchEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FetchEvent"/> class with the specified message.
        /// </summary>
        /// <param name="msg">The message to display when fetching.</param>
        public FetchEvent(StringKey msg)
        {
            Msg = msg;
        }
        /// <summary>
        /// Copy constructor for cloning an existing FetchEvent.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected FetchEvent(FetchEvent other)
        {
            Msg = other.Msg;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new FetchEvent(this); }

        /// <inheritdoc/>
        public override IEnumerator<YieldInstruction> Apply(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            if (context.ContextStates.Contains<BallFetch>())
                yield break;

            RecruitFail state = context.ContextStates.GetWithDefault<RecruitFail>();
            if (state == null || state.ResultLoc == null)
                yield break;

            //the item needs to be there
            int itemSlot = ZoneManager.Instance.CurrentMap.GetItem(state.ResultLoc.Value);

            //the item needs to match
            if (itemSlot == -1)
                yield break;

            MapItem mapItem = ZoneManager.Instance.CurrentMap.Items[itemSlot];

            //make sure it's the right one
            if (mapItem.Value != context.Item.ID)
                yield break;


            //fetch the ball!
            InvItem item = context.Item;
            Character origin = ownerChar;


            yield return new WaitForFrames(30);

            ZoneManager.Instance.CurrentMap.Items.RemoveAt(itemSlot);

            //item steal animation
            Loc itemStartLoc = mapItem.TileLoc * GraphicsManager.TileSize + new Loc(GraphicsManager.TileSize / 2);
            int MaxDistance = (int)Math.Sqrt((itemStartLoc - origin.MapLoc).DistSquared());
            ItemAnim itemAnim = new ItemAnim(itemStartLoc, origin.MapLoc, DataManager.Instance.GetItem(item.ID).Sprite, MaxDistance / 2, 0);
            DungeonScene.Instance.CreateAnim(itemAnim, DrawLayer.Normal);
            yield return new WaitForFrames(ItemAnim.ITEM_ACTION_TIME);

            GameManager.Instance.SE(GraphicsManager.EquipSE);
            DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_BALL_FETCH").ToLocal(), origin.GetDisplayName(false), item.GetDisplayName()));

            if (origin.MemberTeam is ExplorerTeam)
            {
                if (((ExplorerTeam)origin.MemberTeam).GetInvCount() < ((ExplorerTeam)origin.MemberTeam).GetMaxInvSlots(ZoneManager.Instance.CurrentZone))
                {
                    //attackers already holding an item will have the item returned to the bag
                    if (!String.IsNullOrEmpty(origin.EquippedItem.ID))
                    {
                        InvItem attackerItem = origin.EquippedItem;
                        yield return CoroutineManager.Instance.StartCoroutine(origin.DequipItem());
                        origin.MemberTeam.AddToInv(attackerItem);
                    }
                    yield return CoroutineManager.Instance.StartCoroutine(origin.EquipItem(item));
                }
                else
                {
                    yield return new WaitForFrames(GameManager.Instance.ModifyBattleSpeed(30));
                    DungeonScene.Instance.LogMsg(Text.FormatGrammar(new StringKey("MSG_INV_FULL").ToLocal(), origin.GetDisplayName(false), item.GetDisplayName()));
                    //if the bag is full, or there is no bag, the stolen item will slide off in the opposite direction they're facing
                    Loc endLoc = origin.CharLoc + origin.CharDir.Reverse().GetLoc();
                    yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.DropItem(item, endLoc, origin.CharLoc));
                }
            }
            else
            {
                if (!String.IsNullOrEmpty(origin.EquippedItem.ID))
                {
                    InvItem attackerItem = origin.EquippedItem;
                    yield return CoroutineManager.Instance.StartCoroutine(origin.DequipItem());
                    //if the user is holding an item already, the item will slide off in the opposite direction they're facing
                    Loc endLoc = origin.CharLoc + origin.CharDir.Reverse().GetLoc();
                    yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.DropItem(attackerItem, endLoc, origin.CharLoc));
                }
                yield return CoroutineManager.Instance.StartCoroutine(origin.EquipItem(item));
            }
        }
    }


    /// <summary>
    /// Event that allows an ally to follow up with an additional attack after the user deals damage.
    /// Invokes a specified skill from the bystander's position targeting the original target.
    /// </summary>
    [Serializable]
    public class FollowUpEvent : InvokeBattleEvent
    {
        /// <summary>
        /// The skill ID to invoke as a follow-up attack.
        /// </summary>
        [JsonConverter(typeof(SkillConverter))]
        [DataType(0, DataManager.DataType.Skill, false)]
        public string InvokedMove;

        /// <summary>
        /// Whether to target the original target or the original user.
        /// </summary>
        public bool AffectTarget;

        /// <summary>
        /// The offset in front of the owner from which to aim the attack.
        /// </summary>
        public int FrontOffset;

        /// <summary>
        /// The message displayed when the follow-up attack is triggered.
        /// </summary>
        public StringKey Msg;

        /// <summary>
        /// Initializes a new instance of the <see cref="FollowUpEvent"/> class with default values.
        /// </summary>
        public FollowUpEvent() { InvokedMove = ""; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FollowUpEvent"/> class with the specified parameters.
        /// </summary>
        /// <param name="invokedMove">The skill ID to invoke.</param>
        /// <param name="affectTarget">Whether to target the original target.</param>
        /// <param name="frontOffset">The offset in front of the owner.</param>
        /// <param name="msg">The message to display.</param>
        public FollowUpEvent(string invokedMove, bool affectTarget, int frontOffset, StringKey msg)
        {
            InvokedMove = invokedMove;
            AffectTarget = affectTarget;
            FrontOffset = frontOffset;
            Msg = msg;
        }

        /// <summary>
        /// Copy constructor for cloning an existing FollowUpEvent.
        /// </summary>
        /// <param name="other">The instance to copy from.</param>
        protected FollowUpEvent(FollowUpEvent other)
        {
            InvokedMove = other.InvokedMove;
            AffectTarget = other.AffectTarget;
            FrontOffset = other.FrontOffset;
            Msg = other.Msg;
        }

        /// <inheritdoc/>
        public override GameEvent Clone() { return new FollowUpEvent(this); }

        /// <inheritdoc/>
        protected override BattleContext CreateContext(GameEventOwner owner, Character ownerChar, BattleContext context)
        {
            Character target = (AffectTarget ? context.Target : context.User);
            int damage = context.GetContextStateInt<DamageDealt>(0);
            if (damage > 0 && ownerChar != context.User &&
                DungeonScene.Instance.GetMatchup(context.User, context.Target) == Alignment.Foe &&
                !context.ContextStates.Contains<FollowUp>())
            {
                //the attack needs to face the foe, and *auto-target*
                Dir8 attackDir = ZoneManager.Instance.CurrentMap.GetClosestDir8(ownerChar.CharLoc, target.CharLoc);
                if (attackDir == Dir8.None)
                    attackDir = Dir8.Down;
                ownerChar.CharDir = attackDir;
                Loc frontLoc = ownerChar.CharLoc + attackDir.GetLoc() * FrontOffset;

                SkillData entry = DataManager.Instance.GetSkill(InvokedMove);

                DungeonScene.Instance.LogMsg(Text.FormatGrammar(Msg.ToLocal(), ownerChar.GetDisplayName(false), context.User.GetDisplayName(false)));

                BattleContext newContext = new BattleContext(BattleActionType.Skill);
                newContext.User = ownerChar;
                newContext.UsageSlot = BattleContext.FORCED_SLOT;

                newContext.StartDir = newContext.User.CharDir;

                //fill effects
                newContext.Data = new BattleData(entry.Data);
                newContext.Data.ID = InvokedMove;
                newContext.Data.DataType = DataManager.DataType.Skill;
                newContext.Explosion = new ExplosionData(entry.Explosion);
                newContext.HitboxAction = entry.HitboxAction.Clone();
                //make the attack *autotarget*; set the offset to the space between the front loc and the target
                newContext.HitboxAction.HitOffset = target.CharLoc - frontLoc;
                newContext.Strikes = entry.Strikes;
                newContext.Item = new InvItem();
                //don't set move message, just directly give the message of what the move turned into

                //add a tag that will allow the moves themselves to switch to their offensive versions
                newContext.ContextStates.Set(new FollowUp());


                return newContext;
            }

            return null;
        }
    }

}


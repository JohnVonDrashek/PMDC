using System;
using RogueEssence.Dungeon;

namespace PMDC.Dungeon
{
    /// <summary>
    /// Skill state storing the chance of triggering an additional effect.
    /// Used by moves with secondary effects like flinching or stat changes.
    /// </summary>
    [Serializable]
    public class AdditionalEffectState : SkillState
    {
        /// <summary>
        /// The percentage chance (0-100) of the additional effect triggering.
        /// </summary>
        public int EffectChance;

        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public AdditionalEffectState() { }

        /// <summary>
        /// Initializes a new instance with the specified effect chance.
        /// </summary>
        /// <param name="effectChance">The percentage chance of the effect triggering.</param>
        public AdditionalEffectState(int effectChance) { EffectChance = effectChance; }

        /// <summary>
        /// Copy constructor for cloning an existing AdditionalEffectState.
        /// </summary>
        protected AdditionalEffectState(AdditionalEffectState other) { EffectChance = other.EffectChance; }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new AdditionalEffectState(this); }
    }

    /// <summary>
    /// Marker skill state indicating the move makes physical contact with the target.
    /// Used to trigger contact-based abilities like Static and Rough Skin.
    /// </summary>
    [Serializable]
    public class ContactState : SkillState
    {
        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public ContactState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new ContactState(); }
    }

    /// <summary>
    /// Marker skill state indicating the move is sound-based.
    /// Used by moves like Hyper Voice that bypass Substitute and are blocked by Soundproof.
    /// </summary>
    [Serializable]
    public class SoundState : SkillState
    {
        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public SoundState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new SoundState(); }
    }

    /// <summary>
    /// Marker skill state indicating the move is a healing move.
    /// Used to identify moves affected by Heal Block and boosted by Mega Launcher.
    /// </summary>
    [Serializable]
    public class HealState : SkillState
    {
        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public HealState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new HealState(); }
    }

    /// <summary>
    /// Marker skill state indicating the move is a punching move.
    /// Used by Iron Fist ability to boost punching move damage.
    /// </summary>
    [Serializable]
    public class FistState : SkillState
    {
        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public FistState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new FistState(); }
    }

    /// <summary>
    /// Marker skill state indicating the move is a pulse/aura move.
    /// Used by Mega Launcher ability to boost pulse move damage.
    /// </summary>
    [Serializable]
    public class PulseState : SkillState
    {
        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public PulseState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new PulseState(); }
    }

    /// <summary>
    /// Marker skill state indicating the move is a biting move.
    /// Used by Strong Jaw ability to boost biting move damage.
    /// </summary>
    [Serializable]
    public class JawState : SkillState
    {
        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public JawState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new JawState(); }
    }

    /// <summary>
    /// Marker skill state indicating the move is a slashing/cutting move.
    /// Used by Sharpness ability to boost slashing move damage.
    /// </summary>
    [Serializable]
    public class BladeState : SkillState
    {
        /// <summary>
        /// Initializes a new instance with default values.
        /// </summary>
        public BladeState() { }

        /// <inheritdoc/>
        public override GameplayState Clone() { return new BladeState(); }
    }
}

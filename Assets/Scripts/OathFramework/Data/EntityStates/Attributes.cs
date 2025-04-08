using OathFramework.Data.StatParams;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.States;
using Lookup = OathFramework.Data.EntityStates.StateLookup.Attributes;

namespace OathFramework.Data.EntityStates
{
    public abstract class AttributeState : State
    {
        public override uint Order           => 100;
        public override ushort MaxValue      => 25;
        public override bool NetSync         => true;
        public override bool PersistenceSync => false;
    }
    
    public sealed class ConstitutionAttribute : AttributeState
    {
        public override string LookupKey  => Lookup.Constitution.Key;
        public override ushort? DefaultID => Lookup.Constitution.DefaultID;
        
        public static ConstitutionAttribute Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnApplyStatChanges(Entity entity, bool lateJoin, ushort val)
        {
            Stats stats      = entity.CurStats;
            stats.maxHealth += 16u * val;
            // Status resist + 4%
        }
    }
    
    public sealed class EnduranceAttribute : AttributeState
    {
        public override string LookupKey  => Lookup.Endurance.Key;
        public override ushort? DefaultID => Lookup.Endurance.DefaultID;
        
        public static EnduranceAttribute Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnApplyStatChanges(Entity entity, bool lateJoin, ushort val)
        {
            Stats stats       = entity.CurStats;
            stats.maxHealth  += 2u * val;
            stats.maxStamina += (ushort)(3 * val);
            // Weight + 15
        }
    }
    
    public sealed class AgilityAttribute : AttributeState
    {
        public override string LookupKey  => Lookup.Agility.Key;
        public override ushort? DefaultID => Lookup.Agility.DefaultID;
        
        public static AgilityAttribute Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }
        
        protected override void OnApplyStatChanges(Entity entity, bool lateJoin, ushort val)
        {
            Stats stats       = entity.CurStats;
            stats.speed      += 0.019f * val;
            stats.maxStamina += (ushort)(1 * val);
            stats.SetParam(StaminaRegen.Instance, stats.GetParam(StaminaRegen.Instance) + (1.0f * val));
            stats.SetParam(StaminaRegenDelay.Instance, stats.GetParam(StaminaRegenDelay.Instance) - (0.02f * val));
            stats.SetParam(DodgeSpeedMult.Instance, stats.GetParam(DodgeSpeedMult.Instance) + (0.003f * val));
        }
    }
    
    public sealed class StrengthAttribute : AttributeState
    {
        public override string LookupKey  => Lookup.Strength.Key;
        public override ushort? DefaultID => Lookup.Strength.DefaultID;
        
        public static StrengthAttribute Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }

        protected override void OnApplyStatChanges(Entity entity, bool lateJoin, ushort val)
        {
            Stats stats      = entity.CurStats;
            stats.maxHealth += (uint)(6u * val);
            stats.speed     -= 0.00627f * val;
            stats.poise     += 4u * val;
            stats.SetParam(StaminaRegen.Instance, stats.GetParam(StaminaRegen.Instance) - (0.25f * val));
            stats.SetParam(DodgeSpeedMult.Instance, stats.GetParam(DodgeSpeedMult.Instance) - (0.0025f * val));
            // Melee + 5%
        }
    }
    
    public sealed class ExpertiseAttribute : AttributeState
    {
        public override string LookupKey  => Lookup.Expertise.Key;
        public override ushort? DefaultID => Lookup.Expertise.DefaultID;
        
        public static ExpertiseAttribute Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }
        
        protected override void OnApplyStatChanges(Entity entity, bool lateJoin, ushort val)
        {
            Stats stats = entity.CurStats;
            stats.SetParam(AccuracyMult.Instance, stats.GetParam(AccuracyMult.Instance) + (0.02f * val));
            stats.SetParam(ReloadSpeedMult.Instance, stats.GetParam(ReloadSpeedMult.Instance) + (0.02f * val));
        }
    }
    
    public sealed class IntelligenceAttribute : AttributeState
    {
        public override string LookupKey  => Lookup.Intelligence.Key;
        public override ushort? DefaultID => Lookup.Intelligence.DefaultID;
        
        public static IntelligenceAttribute Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
        }
        
        protected override void OnApplyStatChanges(Entity entity, bool lateJoin, ushort val)
        {
            Stats stats      = entity.CurStats;
            stats.maxHealth -= (uint)(2u * val);
            stats.SetParam(CriticalMult.Instance, stats.GetParam(CriticalMult.Instance) + (0.02f * val));
            stats.SetParam(CriticalChance.Instance, stats.GetParam(CriticalChance.Instance) + (0.0068f * val));
        }
    }
}

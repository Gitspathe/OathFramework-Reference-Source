using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.Data.EntityStates;
using OathFramework.EntitySystem.States;
using OathFramework.Progression;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace OathFramework.EntitySystem.Attributes
{ 

    public sealed class AttributeManager : Subsystem
    {
        public static AttributeManager Instance { get; private set; }

        public override string Name    => "Attribute Manager";
        public override uint LoadOrder => SubsystemLoadOrders.AttributeManager;
        
        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize multiple '{nameof(AttributeManager)}' singletons.");
                Destroy(this);
                return UniTask.CompletedTask;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
            return UniTask.CompletedTask;
        }

        public static void Apply(in PlayerBuildData build, StateHandler handler, bool apply)
        {
            handler.SetState(new EntityState(ConstitutionAttribute.Instance, build.constitution), applyStats: false);
            handler.SetState(new EntityState(EnduranceAttribute.Instance,    build.endurance),    applyStats: false);
            handler.SetState(new EntityState(AgilityAttribute.Instance,      build.agility),      applyStats: false);
            handler.SetState(new EntityState(StrengthAttribute.Instance,     build.strength),     applyStats: false);
            handler.SetState(new EntityState(ExpertiseAttribute.Instance,    build.expertise),    applyStats: false);
            handler.SetState(new EntityState(IntelligenceAttribute.Instance, build.intelligence), applyStats: false);
            if(apply) {
                handler.ApplyStats(true);
            }
        }
    }

    public enum AttributeTypes
    {
        None         = 0,
        Constitution = 1,
        Endurance    = 2,
        Agility      = 3,
        Strength     = 4,
        Expertise    = 5,
        Intelligence = 6,
    }

}

using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace OathFramework.Core.GameEvents
{
    public sealed class EventManager : Subsystem
    {
        private Dictionary<Event.Type, EventBase> events = new();
        private Dictionary<Event.Type, EventBase> active = new();
        
        public static EventManager Instance { get; private set; }

        public override string Name    => "Event Manager";
        public override uint LoadOrder => SubsystemLoadOrders.EventManager;
        
        public override UniTask Initialize(Stopwatch timer)
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize duplicate {nameof(EventManager)} singleton.");
                Destroy(this);
                return UniTask.CompletedTask;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
            foreach(EventBase @event in GetComponentsInChildren<EventBase>(true)) {
                if(!events.TryAdd(@event.EventType, @event)) {
                    Debug.LogError($"Attempted to register duplicate game event for '{@event.EventType}'");
                    continue;
                }
                @event.Initialize();
            }
            return UniTask.CompletedTask;
        }

        public static Event Activate(Event.Type type)
        {
            if(!Instance.events.TryGetValue(type, out EventBase @event)) {
                Debug.LogError($"Couldn't find event of type '{type}'");
                return null;
            }
            Event ev = (Event)@event;
            ev.Activate();
            Instance.active.Add(type, @event);
            return ev;
        }
        
        public static async UniTask<Event> ActivateAsync(Event.Type type)
        {
            Event ev = Activate(type);
            if(ev == null)
                return null;
            
            return await ev.WaitForCompletion();
        }

        public static Event<TInput> Activate<TInput>(Event.Type type, TInput input)
        {
            if(!Instance.events.TryGetValue(type, out EventBase @event)) {
                Debug.LogError($"Couldn't find event of type '{type}'");
                return null;
            }
            Event<TInput> ev = (Event<TInput>)@event;
            ev.Activate(input);
            Instance.active.Add(type, @event);
            return ev;
        }

        public static async UniTask<Event<TInput>> ActivateAsync<TInput>(Event.Type type, TInput input)
        {
            Event<TInput> ev = Activate(type, input);
            if(ev == null)
                return null;
            
            return await ev.WaitForCompletion();
        }
        
        public static void Deactivate(Event.Type type, bool complete = false)
        {
            if(!Instance.events.TryGetValue(type, out EventBase @event)) {
                Debug.LogError($"Couldn't find event of type '{type}'");
                return;
            }
            @event.Deactivate(complete);
        }

        public static void DeactivatedCallback(Event.Type type)
        {
            Instance.active.Remove(type);
        }
    }
}

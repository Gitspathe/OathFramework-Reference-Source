using OathFramework.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.EntitySystem.Players
{
    public sealed class CommandQueue
    {
        private Dictionary<int, Command> actionsDict = new();
        private LockableOrderedList<Command> queue   = new();
        private List<Command.Instance> instances     = new();

        public void Clear()
        {
            instances.Clear();
            queue.Clear();
        }
        
        public void ProcessQueue(float deltaTime)
        {
            int processed = 0;
            while(true) {
                if(queue.Count == 0 || processed >= queue.Count)
                    break;

                Command next = queue.Current[processed];
                if(!next.IsAvailable) {
                    processed++;
                    continue;
                }

                try {
                    next.Execute();
                } finally {
                    RemoveFromQueue(next);
                }
            }
            
            for(int i = instances.Count - 1; i >= 0; i--) {
                Command.Instance inst = new(instances[i].Action, instances[i].BufferTime - deltaTime);
                if(inst.BufferTime <= 0.0f) {
                    RemoveFromQueue(inst.Action);
                    continue;
                }
                instances[i] = inst;
            }
        }
        
        public void RegisterAction(Command command)
        {
            actionsDict.Add(command.ID, command);
        }
        
        public void RegisterActions(params Command[] commands)
        {
            foreach(Command cmd in commands) {
                RegisterAction(cmd);
            }
        }

        private void AddToQueue(Command command)
        {
            bool found = false;
            for(int i = 0; i < instances.Count; i++) {
                if(!instances[i].Action.Equals(command))
                    continue;

                Command.Instance inst = new(instances[i].Action, command.BufferTime);
                instances[i] = inst;
                found = true;
                break;
            }
            if(!found) {
                instances.Add(new Command.Instance(command, command.BufferTime));
            }
            
            queue.Lock();
            foreach(int clear in command.Clears) {
                queue.Remove(actionsDict[clear]);
            }
            queue.Add(command);
            queue.Unlock();
            queue.Sort();
        }

        private void RemoveFromQueue(Command command)
        {
            for(int i = 0; i < instances.Count; i++) {
                if(!instances[i].Action.Equals(command))
                    continue;

                instances.RemoveAt(i);
                break;
            }
            
            queue.Remove(command);
            queue.Sort();
        }
        
        public void Enqueue(Command command)
        {
            if(queue.Contains(command))
                return;
            
            if(command.IsAvailable) {
                queue.Lock();
                try {
                    command.Execute();
                } finally {
                    queue.Unlock();
                }
                return;
            }
            if(command.BufferTime > 0.01f) {
                AddToQueue(command);
            }
        }

        public void Enqueue(int commandID)
        {
            if(!actionsDict.TryGetValue(commandID, out Command command)) {
                Debug.LogError($"No Command with ID '{commandID}' found.");
                return;
            }
            Enqueue(command);
        }
    }
    
    public sealed class Command : IEquatable<Command>, ILockableOrderedListElement
    {
        private readonly Action action;
        private readonly bool invertCondition;

        public ExtBool.Handler BlockFlag { get; set; }
        public int ID                    { get; }
        public uint Order                { get; }
        public float BufferTime          { get; set; }
        public List<int> Clears          { get; }
        
        public bool IsAvailable => BlockFlag == null || (invertCondition ? BlockFlag : !BlockFlag);

        public bool ClearsAction(Command command) => Clears.Contains(command.ID);

        public void Execute()
        {
            action.Invoke();
        }

        public Command(
            int id,
            uint order,
            Action action,
            ExtBool.Handler blockFlag,
            float bufferTime,
            List<int> clears = null,
            bool invertCondition = false)
        {
            ID                   = id;
            Order                = order;
            BufferTime           = bufferTime;
            Clears               = clears ?? new List<int>();
            BlockFlag            = blockFlag;
            this.action          = action;
            this.invertCondition = invertCondition;
        }

        public bool Equals(Command other)
        {
            if(ReferenceEquals(null, other))
                return false;
            if(ReferenceEquals(this, other))
                return true;

            return ID == other.ID;
        }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(null, obj))
                return false;
            if(ReferenceEquals(this, obj))
                return true;
            
            return obj.GetType() == GetType() && Equals((Command)obj);
        }

        public override int GetHashCode() => ID;
        
        public struct Instance
        {
            public Command Action   { get; }
            public float BufferTime { get; }

            public Instance(Command action, float bufferTime)
            {
                Action     = action;
                BufferTime = bufferTime;
            }
        }
    }
}

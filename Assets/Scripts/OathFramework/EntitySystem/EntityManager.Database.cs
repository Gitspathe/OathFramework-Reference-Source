using OathFramework.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace OathFramework.EntitySystem
{
    public sealed partial class EntityManager
    {
        private Database database = new();

        [SerializeField] private List<EntityParams> entites;
        
        public static bool RegisterParams(EntityParams param)
        {
            if(param.DefaultID != 0) {
                if(Instance.database.RegisterWithID(param.LookupKey, param, param.DefaultID)) {
                    param.ID = param.DefaultID;
                    return true;
                }
            }
            if(Instance.database.Register(param.LookupKey, param, out ushort retID)) {
                param.ID = retID;
                return true;
            }
            Debug.LogError($"Failed to register Entity Params '{param.LookupKey}'.");
            return false;
        }
        
        public static bool TryGetParams(string key, out EntityParams param)
        {
            if(Instance.database.TryGet(key, out param, out _))
                return true;
            
            Debug.LogError($"No Entity Params for '{key}' found.");
            return false;
        }
        
        public static bool TryGetParams(ushort netID, out EntityParams param)
        {
            if(Instance.database.TryGet(netID, out param, out _))
                return true;
            
            Debug.LogError($"No Entity Params for netID '{netID}' found.");
            return false;
        }
        
        public static EntityParams GetParams(string key)
        {
            if(Instance.database.TryGet(key, out EntityParams param, out _))
                return param;
            
            Debug.LogError($"No Entity Params for '{key}' found.");
            return null;
        }
        
        public static EntityParams GetParams(ushort netID)
        {
            if(Instance.database.TryGet(netID, out EntityParams param, out _))
                return param;
            
            Debug.LogError($"No Entity Params for netID '{netID}' found.");
            return null;
        }
        
        public static bool TryGetParamsKey(ushort netID, out string key)   => Instance.database.TryGet(netID, out _, out key);
        public static bool TryGetParamsNetID(string key, out ushort netID) => Instance.database.TryGet(key, out _, out netID);
        
        private sealed class Database : Database<string, ushort, EntityParams>
        {
            protected override ushort StartingID => 1;
            protected override void IncrementID() => CurrentID++;

            public override bool IsIDLarger(ushort current, ushort comparison) => comparison > current;
        }
    }

}

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace OathFramework.Core.Service
{

    public class WaveService : MonoBehaviour
    {
        public static WaveService Instance { get; private set; }
        
        public WaveService Initialize()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize multiple {nameof(WaveService)} singletons.");
                return null;
            }

            Instance = this;
            return Instance;
        }
    }

}

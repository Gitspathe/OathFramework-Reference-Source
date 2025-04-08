using OathFramework.Pooling;
using OathFramework.Core;
using UnityEngine;

namespace OathFramework.Audio
{ 

    public class SoundOnEnable : MonoBehaviour
    {
        [field: SerializeField] public AudioParams Params { get; private set; }

        private void OnEnable()
        {
            if(Game.State != GameState.Preload) {
                AudioPool.Retrieve(transform.position, Params);
            }
        }
    }

}

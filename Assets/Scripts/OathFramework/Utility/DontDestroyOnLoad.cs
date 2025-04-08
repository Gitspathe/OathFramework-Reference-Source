using UnityEngine;

namespace OathFramework.Utility
{

    public class DontDestroyOnLoad : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }

}

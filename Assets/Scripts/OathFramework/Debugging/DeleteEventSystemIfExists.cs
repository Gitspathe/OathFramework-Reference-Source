using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OathFramework.Debugging
{
    public class DeleteEventSystemIfExists : MonoBehaviour
    {
        private void Awake()
        {
            if(FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length > 1) {
                Destroy(gameObject);
            }
        }
    }
}

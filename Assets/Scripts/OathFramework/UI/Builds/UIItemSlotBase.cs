using OathFramework.Progression;
using UnityEngine;

namespace OathFramework.UI.Builds
{
    public abstract class UIItemSlotBase : MonoBehaviour
    {
        public abstract string ValueKey        { get; protected set; }
        public abstract EquipSlotType SlotType { get; protected set; }
    }
}

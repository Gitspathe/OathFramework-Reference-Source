using Sirenix.OdinInspector.Editor;
using Unity.Netcode;
using UnityEditor;

namespace OathFramework.Editor
{

    [CustomEditor(typeof(NetworkBehaviour), true)]
    public class OdinNetworkBehaviourEditor : OdinEditor { }

}

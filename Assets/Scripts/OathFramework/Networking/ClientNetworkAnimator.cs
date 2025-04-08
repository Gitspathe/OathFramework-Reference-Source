using Unity.Netcode.Components;

namespace OathFramework.Networking
{ 

    public class OwnerNetworkAnimator : NetworkAnimator
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }

}

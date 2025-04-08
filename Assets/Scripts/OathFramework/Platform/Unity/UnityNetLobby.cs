using OathFramework.Networking;

namespace OathFramework.Platform.Unity
{
    public class UnityNetLobby : NetLobbyBase
    {
        public override ulong OwnerID   { get; protected set; }
        public override ulong LobbyID   { get; protected set; }
        public override int PlayerCount { get; protected set; }
        public override string Code     { get; protected set; }
        public override bool IsPublic   { get; protected set; }

        public override void SetCode(string code)
        {
            
        }

        public override void SetPublic(bool isPublic)
        {
            
        }

        public override void MakeListing()
        {
            
        }

        public override void ClearListing()
        {
            
        }
        
        public override void Leave()
        {
            
        }
    }
}

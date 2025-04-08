#if !UNITY_IOS && !UNITY_ANDROID
using OathFramework.Networking;
using Steamworks.Data;

namespace OathFramework.Platform.Steam
{
    public sealed class SteamNetLobby : NetLobbyBase
    {
        public Lobby SteamLobby         { get; private set; }
        public override ulong OwnerID   { get; protected set; }
        public override ulong LobbyID   { get; protected set; }
        public override int PlayerCount { get; protected set; }
        public override string Code     { get; protected set; }
        public override bool IsPublic   { get; protected set; }

        public SteamNetLobby(Lobby steamLobby)
        {
            SteamLobby  = steamLobby;
            OwnerID     = steamLobby.Owner.Id;
            LobbyID     = steamLobby.Id;
            PlayerCount = steamLobby.MemberCount;
            Code        = steamLobby.GetData("code");
            IsPublic    = steamLobby.GetData("isPublic") == "true";
        }

        public override void SetCode(string code)
        {
            SteamLobby.SetData("code", code);
            Code = code;
        }

        public override void SetPublic(bool isPublic)
        {
            SteamLobby.SetData("isPublic", isPublic ? "true" : "false");
            IsPublic = isPublic;
        }

        public override void MakeListing()
        {
            SteamLobby.SetJoinable(true);
            SteamLobby.SetPublic();
        }

        public override void ClearListing()
        {
            SteamLobby.SetJoinable(false);
            SteamLobby.SetPrivate();
        }
        
        public override void Leave()
        {
            SteamLobby.Leave();
        }
    }
}
#endif

namespace OathFramework.Networking
{
    public abstract class NetLobbyBase
    {
        public abstract ulong OwnerID   { get; protected set; }
        public abstract ulong LobbyID   { get; protected set; }
        public abstract int PlayerCount { get; protected set; }
        public abstract string Code     { get; protected set; }
        public abstract bool IsPublic   { get; protected set; }

        public abstract void SetCode(string code);
        public abstract void SetPublic(bool isPublic);
        public abstract void MakeListing();
        public abstract void ClearListing();
        public abstract void Leave();
    }
}

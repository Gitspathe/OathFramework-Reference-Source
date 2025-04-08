using UnityEngine;
using UnityEngine.Localization;

namespace OathFramework.Networking
{
    public class NetGameMsg : MonoBehaviour
    {
        [field: Header("General")]
        [field: SerializeField] public LocalizedString CreatingLobbyStr         { get; private set; }
        [field: SerializeField] public LocalizedString JoiningLobbyStr          { get; private set; }
        [field: SerializeField] public LocalizedString CancelStr                { get; private set; }
        [field: SerializeField] public LocalizedString LoadingSceneStr          { get; private set; }
        [field: SerializeField] public LocalizedString WaitingForClientsStr     { get; private set; }
        [field: SerializeField] public LocalizedString WaitingForOthersStr      { get; private set; }
        [field: SerializeField] public LocalizedString WaitingForServerStr      { get; private set; }
        [field: SerializeField] public LocalizedString GeneratingMapStr         { get; private set; }
        [field: SerializeField] public LocalizedString GeneratingMapPostInitStr { get; private set; }
        [field: SerializeField] public LocalizedString GeneratingNavMeshStr     { get; private set; }

        [field: Header("Errors")]
        [field: SerializeField] public LocalizedString SinglePlayerHostFailedStr          { get; private set; }
        [field: SerializeField] public LocalizedString MultiPlayerHostFailedStr           { get; private set; }
        [field: SerializeField] public LocalizedString MultiPlayerStartGameFailedStr      { get; private set; }
        [field: SerializeField] public LocalizedString MultiPlayerStartGameFailedSteamStr { get; private set; }
        [field: SerializeField] public LocalizedString ConnectFailedStr                   { get; private set; }
        [field: SerializeField] public LocalizedString ConnectFailedSteamStr              { get; private set; }
        [field: SerializeField] public LocalizedString DisconnectedStr                    { get; private set; }
        [field: SerializeField] public LocalizedString EmptyLobbyCodeStr                  { get; private set; }
        [field: SerializeField] public LocalizedString LobbyFullStr                       { get; private set; }
        [field: SerializeField] public LocalizedString NoRandomLobbyStr                   { get; private set; }
        [field: SerializeField] public LocalizedString LobbyTimedOutStr                   { get; private set; }
        [field: SerializeField] public LocalizedString FailedToCreateLobbyStr             { get; private set; }
        [field: SerializeField] public LocalizedString FailedToJoinLobbyStr               { get; private set; }
        [field: SerializeField] public LocalizedString TimedOutStr                        { get; private set; }
    }
}

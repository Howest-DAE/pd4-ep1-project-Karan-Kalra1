using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class NetworkMatchReporter : NetworkBehaviour
{
    public static NetworkMatchReporter Instance { get; private set; }

    public NetworkVariable<int> MatchId = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Task _createMatchTask;

    public bool HasMatchId => MatchId.Value > 0;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void CreateMatchOnServer()
    {
        if (!IsServer)
        {
            return;
        }

        if (_createMatchTask != null && !_createMatchTask.IsCompleted)
        {
            Debug.Log("Match creation is already running.");
            return;
        }

        if (MatchId.Value > 0)
        {
            Debug.Log($"Match already created. MatchId={MatchId.Value}");
            return;
        }

        _createMatchTask = CreateMatchAsync();
    }

    private async Task CreateMatchAsync()
    {
        try
        {
            if (BackendApiClient.Instance == null)
            {
                Debug.LogError("Cannot create backend match: BackendApiClient.Instance is null.");
                return;
            }

            NetworkPlayerRegistry registry = NetworkPlayerRegistry.Instance;

            if (registry == null || registry.PlayerCount < 2)
            {
                Debug.LogError("Cannot create backend match: not enough registered players.");
                return;
            }

            if (!registry.TryGetHostPlayer(out NetworkPlayerData hostPlayer))
            {
                Debug.LogError("Cannot create backend match: host player not found.");
                return;
            }

            if (!registry.TryGetOtherPlayer(hostPlayer.ClientId, out NetworkPlayerData clientPlayer))
            {
                Debug.LogError("Cannot create backend match: client player not found.");
                return;
            }

            string hostPlayFabId = hostPlayer.PlayFabId.ToString();
            string clientPlayFabId = clientPlayer.PlayFabId.ToString();

            Debug.Log(
                $"Creating backend match. Host={hostPlayFabId}, Client={clientPlayFabId}"
            );

            MatchCreatedDto result = await BackendApiClient.Instance.CreateMatchAsync(
                hostPlayFabId,
                clientPlayFabId
            );

            MatchId.Value = result.matchId;

            Debug.Log($"Backend match created. MatchId={MatchId.Value}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to create backend match: {ex}");
        }
    }

    public void ReportHitOnServer(ulong attackerClientId, ulong victimClientId)
    {
        if (!IsServer)
        {
            return;
        }

        _ = ReportHitAsync(attackerClientId, victimClientId);
    }

    private async Task ReportHitAsync(ulong attackerClientId, ulong victimClientId)
    {
        try
        {
            if (_createMatchTask != null && !_createMatchTask.IsCompleted)
            {
                await _createMatchTask;
            }

            if (MatchId.Value <= 0)
            {
                Debug.LogWarning("Cannot report hit: backend MatchId is not created yet.");
                return;
            }

            if (BackendApiClient.Instance == null)
            {
                Debug.LogError("Cannot report hit: BackendApiClient.Instance is null.");
                return;
            }

            NetworkPlayerRegistry registry = NetworkPlayerRegistry.Instance;

            if (registry == null)
            {
                Debug.LogError("Cannot report hit: NetworkPlayerRegistry.Instance is null.");
                return;
            }

            if (!registry.TryGetPlayerByClientId(attackerClientId, out NetworkPlayerData attacker))
            {
                Debug.LogError($"Cannot report hit: attacker client {attackerClientId} not found.");
                return;
            }

            if (!registry.TryGetPlayerByClientId(victimClientId, out NetworkPlayerData victim))
            {
                Debug.LogError($"Cannot report hit: victim client {victimClientId} not found.");
                return;
            }

            string attackerPlayFabId = attacker.PlayFabId.ToString();
            string victimPlayFabId = victim.PlayFabId.ToString();

            Debug.Log(
                $"Reporting backend hit. MatchId={MatchId.Value}, " +
                $"Attacker={attackerPlayFabId}, Victim={victimPlayFabId}"
            );

            await BackendApiClient.Instance.ReportPlayerHitAsync(
                MatchId.Value,
                attackerPlayFabId,
                victimPlayFabId
            );

            Debug.Log("Backend hit reported.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to report backend hit: {ex}");
        }
    }
}
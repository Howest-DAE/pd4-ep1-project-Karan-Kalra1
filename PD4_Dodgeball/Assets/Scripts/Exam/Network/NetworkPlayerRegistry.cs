using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerRegistry : NetworkBehaviour
{
    public static NetworkPlayerRegistry Instance { get; private set; }

    public event Action PlayersChanged;

    private NetworkList<NetworkPlayerData> _players;

    public int PlayerCount => _players == null ? 0 : _players.Count;

    public bool HasTwoPlayers => PlayerCount >= 2;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate NetworkPlayerRegistry found.");
        }

        Instance = this;

        _players = new NetworkList<NetworkPlayerData>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _players.OnListChanged += OnPlayerListChanged;

        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        if (IsClient)
        {
            RegisterLocalPlayer();
        }

        Debug.Log($"NetworkPlayerRegistry spawned. IsServer={IsServer}, IsClient={IsClient}");
    }

    public override void OnNetworkDespawn()
    {
        if (_players != null)
        {
            _players.OnListChanged -= OnPlayerListChanged;
        }

        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        base.OnNetworkDespawn();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (_players != null)
        {
            _players.Dispose();
            _players = null;
        }
    }

    private void RegisterLocalPlayer()
    {
        if (string.IsNullOrWhiteSpace(SessionData.PlayFabId))
        {
            Debug.LogWarning("Cannot register local player: SessionData.PlayFabId is empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(SessionData.DisplayName))
        {
            Debug.LogWarning("Cannot register local player: SessionData.DisplayName is empty.");
            return;
        }

        Debug.Log(
            $"Registering local player with server. " +
            $"PlayFabId={SessionData.PlayFabId}, DisplayName={SessionData.DisplayName}"
        );

        RegisterLocalPlayerRpc(SessionData.PlayFabId, SessionData.DisplayName);
    }

    [Rpc(SendTo.Server)]
    private void RegisterLocalPlayerRpc(
        string playFabId,
        string displayName,
        RpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        Debug.Log(
            $"Server received player registration. " +
            $"ClientId={senderClientId}, PlayFabId={playFabId}, DisplayName={displayName}"
        );

        UpsertPlayer(senderClientId, playFabId, displayName);
    }

    private void UpsertPlayer(ulong clientId, string playFabId, string displayName)
    {
        if (!IsServer)
        {
            return;
        }

        int existingIndex = IndexOfClient(clientId);

        NetworkPlayerData data = new NetworkPlayerData
        {
            ClientId = clientId,
            PlayFabId = new FixedString64Bytes(playFabId),
            DisplayName = new FixedString64Bytes(displayName),
            IsHost = clientId == NetworkManager.ServerClientId,
            TeamIndex = -1,
            HasSelectedTeam = false,
            IsReady = false
        };

        if (existingIndex >= 0)
        {
            NetworkPlayerData oldData = _players[existingIndex];

            data.TeamIndex = oldData.TeamIndex;
            data.HasSelectedTeam = oldData.HasSelectedTeam;
            data.IsReady = oldData.IsReady;

            _players[existingIndex] = data;
        }
        else
        {
            _players.Add(data);
        }

        LogPlayers("After UpsertPlayer");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer)
        {
            return;
        }

        int index = IndexOfClient(clientId);

        if (index >= 0)
        {
            Debug.Log($"Removing disconnected player: {clientId}");
            _players.RemoveAt(index);
        }
    }

    public void RequestSelectTeam(int teamIndex)
    {
        if (!IsClient)
        {
            return;
        }

        RequestSelectTeamRpc(teamIndex);
    }

    public void RequestReady(bool isReady)
    {
        if (!IsClient)
        {
            return;
        }

        RequestReadyRpc(isReady);
    }

    [Rpc(SendTo.Server)]
    private void RequestSelectTeamRpc(
        int teamIndex,
        RpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        if (teamIndex < 0 || teamIndex > 1)
        {
            Debug.LogWarning($"Invalid team index requested: {teamIndex}");
            return;
        }

        int playerIndex = IndexOfClient(senderClientId);

        if (playerIndex < 0)
        {
            Debug.LogWarning($"Client {senderClientId} tried to select a team before registering.");
            return;
        }

        if (IsTeamAlreadyTakenByOtherClient(teamIndex, senderClientId))
        {
            Debug.LogWarning($"Team {teamIndex} is already taken.");
            return;
        }

        NetworkPlayerData data = _players[playerIndex];
        data.TeamIndex = teamIndex;
        data.HasSelectedTeam = true;

        // If player changes team, force ready off.
        data.IsReady = false;

        _players[playerIndex] = data;

        Debug.Log($"Client {senderClientId} selected team {teamIndex}.");
    }

    [Rpc(SendTo.Server)]
    private void RequestReadyRpc(
        bool isReady,
        RpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        int playerIndex = IndexOfClient(senderClientId);

        if (playerIndex < 0)
        {
            Debug.LogWarning($"Client {senderClientId} tried to ready before registering.");
            return;
        }

        NetworkPlayerData data = _players[playerIndex];

        if (isReady && !data.HasSelectedTeam)
        {
            Debug.LogWarning($"Client {senderClientId} tried to ready without selecting a team.");
            return;
        }

        data.IsReady = isReady;

        _players[playerIndex] = data;

        Debug.Log($"Client {senderClientId} ready state changed to {isReady}.");
    }

    public bool AreBothPlayersReady()
    {
        if (_players == null || _players.Count < 2)
        {
            return false;
        }

        for (int i = 0; i < _players.Count; i++)
        {
            NetworkPlayerData player = _players[i];

            if (!player.HasSelectedTeam || !player.IsReady)
            {
                return false;
            }
        }

        return true;
    }

    public bool TryGetLocalPlayer(out NetworkPlayerData player)
    {
        player = default;

        if (NetworkManager.Singleton == null)
        {
            return false;
        }

        return TryGetPlayerByClientId(NetworkManager.Singleton.LocalClientId, out player);
    }

    public bool TryGetPlayerByClientId(ulong clientId, out NetworkPlayerData player)
    {
        player = default;

        if (_players == null)
        {
            return false;
        }

        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].ClientId == clientId)
            {
                player = _players[i];
                return true;
            }
        }

        return false;
    }

    public bool TryGetHostPlayer(out NetworkPlayerData player)
    {
        player = default;

        if (_players == null)
        {
            return false;
        }

        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].IsHost)
            {
                player = _players[i];
                return true;
            }
        }

        return false;
    }

    public bool TryGetOtherPlayer(ulong localClientId, out NetworkPlayerData player)
    {
        player = default;

        if (_players == null)
        {
            return false;
        }

        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].ClientId != localClientId)
            {
                player = _players[i];
                return true;
            }
        }

        return false;
    }

    public NetworkPlayerData GetPlayerAt(int index)
    {
        return _players[index];
    }

    private int IndexOfClient(ulong clientId)
    {
        if (_players == null)
        {
            return -1;
        }

        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].ClientId == clientId)
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsTeamAlreadyTakenByOtherClient(int teamIndex, ulong clientId)
    {
        for (int i = 0; i < _players.Count; i++)
        {
            NetworkPlayerData player = _players[i];

            if (player.ClientId == clientId)
            {
                continue;
            }

            if (player.HasSelectedTeam && player.TeamIndex == teamIndex)
            {
                return true;
            }
        }

        return false;
    }

    private void OnPlayerListChanged(NetworkListEvent<NetworkPlayerData> changeEvent)
    {
        LogPlayers($"Player list changed: {changeEvent.Type}");

        PlayersChanged?.Invoke();
    }

    private void LogPlayers(string prefix)
    {
        if (_players == null)
        {
            Debug.Log($"{prefix}: player list is null");
            return;
        }

        Debug.Log($"{prefix}: PlayerCount={_players.Count}");

        for (int i = 0; i < _players.Count; i++)
        {
            Debug.Log($"  [{i}] {_players[i]}");
        }
    }
}
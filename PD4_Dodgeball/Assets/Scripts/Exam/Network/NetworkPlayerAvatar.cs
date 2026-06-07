using System.Collections;
using Dodgeball.Model;
using Dodgeball.Presenter;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayerAvatar : NetworkBehaviour
{
    public NetworkVariable<ulong> PlayerClientId = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> TeamIndex = new(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private PlayerPresenter _playerPresenter;

    private void Awake()
    {
        _playerPresenter = GetComponent<PlayerPresenter>();
    }

    public void SetPlayerDataOnServer(ulong clientId, int teamIndex)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("Only server can set network player data.");
            return;
        }

        PlayerClientId.Value = clientId;
        TeamIndex.Value = teamIndex;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        PlayerClientId.OnValueChanged += OnPlayerDataChanged;
        TeamIndex.OnValueChanged += OnTeamChanged;

        StartCoroutine(InitializeWhenModelExists());
    }

    public override void OnNetworkDespawn()
    {
        PlayerClientId.OnValueChanged -= OnPlayerDataChanged;
        TeamIndex.OnValueChanged -= OnTeamChanged;

        base.OnNetworkDespawn();
    }

    private void OnPlayerDataChanged(ulong previous, ulong current)
    {
        StartCoroutine(InitializeWhenModelExists());
    }

    private void OnTeamChanged(int previous, int current)
    {
        StartCoroutine(InitializeWhenModelExists());
    }

    private IEnumerator InitializeWhenModelExists()
    {
        if (_playerPresenter == null)
        {
            yield break;
        }

        while (PlayerClientId.Value == 0 && !IsHostPlayer())
        {
            yield return null;
        }

        ArenaPresenter arena = null;
        PlayerModel model = null;

        while (model == null)
        {
            arena = FindAnyObjectByType<ArenaPresenter>();

            if (arena != null)
            {
                model = arena.Model.GetPlayer(PlayerClientId.Value);
            }

            yield return null;
        }

        bool isLocalOwner = IsOwner;

        _playerPresenter.InitializeNetworkPlayer(PlayerClientId.Value, isLocalOwner);

        NetworkPlayerMovement movement = GetComponent<NetworkPlayerMovement>();
        if (movement != null)
        {
            movement.enabled = true;
        }

        NetworkPlayerThrow thrower = GetComponent<NetworkPlayerThrow>();
        if (thrower != null)
        {
            thrower.enabled = true;
        }
    }

    private bool IsHostPlayer()
    {
        return PlayerClientId.Value == NetworkManager.ServerClientId;
    }
}
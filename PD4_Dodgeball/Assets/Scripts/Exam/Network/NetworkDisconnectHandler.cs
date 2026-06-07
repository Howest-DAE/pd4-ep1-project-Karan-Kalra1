using Dodgeball.Presenter;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class NetworkDisconnectHandler : NetworkBehaviour
{
    [SerializeField]
    private GamePresenter _gamePresenter;

    [Header("Optional UI")]
    [SerializeField]
    private GameObject _disconnectPanel;

    [SerializeField]
    private TextMeshProUGUI _disconnectMessageText;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        if (_disconnectPanel != null)
        {
            _disconnectPanel.SetActive(false);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        base.OnNetworkDespawn();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"NetworkDisconnectHandler detected disconnect: ClientId={clientId}");

        if (_gamePresenter == null)
        {
            _gamePresenter = FindAnyObjectByType<GamePresenter>();
        }

        // Case 1: We are the server/host and the remote client disconnected.
        if (IsServer && clientId != NetworkManager.ServerClientId)
        {
            Debug.Log($"Remote client {clientId} disconnected. Ending match.");

            if (NetworkMatchTimer.Instance != null)
            {
                NetworkMatchTimer.Instance.StopTimerOnServer();
            }

            NotifyOpponentDisconnectedRpc(clientId);
            return;
        }

        // Case 2: We are a client and the host/server disconnected.
        if (!IsServer && clientId == NetworkManager.ServerClientId)
        {
            Debug.LogWarning("Host disconnected. Leaving match.");

            ShowDisconnectMessage("Host disconnected.");

            if (_gamePresenter != null)
            {
                NetworkGameOverBridge.TriggerGameOver(_gamePresenter);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void NotifyOpponentDisconnectedRpc(ulong disconnectedClientId)
    {
        ShowDisconnectMessage($"Player {disconnectedClientId} disconnected.");

        if (_gamePresenter == null)
        {
            _gamePresenter = FindAnyObjectByType<GamePresenter>();
        }

        if (_gamePresenter != null)
        {
            NetworkGameOverBridge.TriggerGameOver(_gamePresenter);
        }
    }

    private void ShowDisconnectMessage(string message)
    {
        Debug.Log(message);

        if (_disconnectPanel != null)
        {
            _disconnectPanel.SetActive(true);
        }

        if (_disconnectMessageText != null)
        {
            _disconnectMessageText.text = message;
        }
    }
}
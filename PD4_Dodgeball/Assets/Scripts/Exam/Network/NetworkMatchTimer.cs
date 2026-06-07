using System;
using Dodgeball.Presenter;
using Unity.Netcode;
using UnityEngine;

public class NetworkMatchTimer : NetworkBehaviour
{
    public static NetworkMatchTimer Instance { get; private set; }

    [SerializeField]
    private float _matchDurationSeconds = 60f;

    [SerializeField]
    private GamePresenter _gamePresenter;

    public NetworkVariable<float> RemainingSeconds = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> IsRunning = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public event Action<float> RemainingTimeChanged;

    private bool _gameOverTriggered;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        RemainingSeconds.OnValueChanged += OnRemainingSecondsChanged;

        if (_gamePresenter == null)
        {
            _gamePresenter = FindAnyObjectByType<GamePresenter>();
        }

        Debug.Log($"NetworkMatchTimer spawned. IsServer={IsServer}, IsClient={IsClient}");
    }

    public override void OnNetworkDespawn()
    {
        RemainingSeconds.OnValueChanged -= OnRemainingSecondsChanged;
        base.OnNetworkDespawn();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!IsServer)
        {
            return;
        }

        if (!IsRunning.Value)
        {
            return;
        }

        if (_gameOverTriggered)
        {
            return;
        }

        RemainingSeconds.Value = Mathf.Max(
            0f,
            RemainingSeconds.Value - Time.deltaTime
        );

        if (RemainingSeconds.Value <= 0f)
        {
            _gameOverTriggered = true;
            IsRunning.Value = false;

            Debug.Log("Network match timer reached zero. Triggering GameOver.");

            TriggerGameOverRpc();
        }
    }

    public void StartTimerOnServer()
    {
        if (!IsServer)
        {
            return;
        }

        _gameOverTriggered = false;
        RemainingSeconds.Value = _matchDurationSeconds;
        IsRunning.Value = true;

        Debug.Log($"Network match timer started: {_matchDurationSeconds} seconds");
    }

    public void StopTimerOnServer()
    {
        if (!IsServer)
        {
            return;
        }

        IsRunning.Value = false;
    }

    private void OnRemainingSecondsChanged(float previousValue, float newValue)
    {
        RemainingTimeChanged?.Invoke(newValue);
    }

    [Rpc(SendTo.Everyone)]
    private void TriggerGameOverRpc()
    {
        if (_gamePresenter == null)
        {
            _gamePresenter = FindAnyObjectByType<GamePresenter>();
        }

        if (_gamePresenter == null)
        {
            Debug.LogError("Cannot trigger GameOver: GamePresenter not found.");
            return;
        }

        Debug.Log("Received network GameOver RPC.");

        NetworkGameOverBridge.TriggerGameOver(_gamePresenter);
    }
}
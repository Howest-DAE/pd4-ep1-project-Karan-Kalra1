using Dodgeball.Model;
using Dodgeball.Presenter;
using Unity.Netcode;
using UnityEngine;

public class NetworkMatchState : NetworkBehaviour
{
    public static NetworkMatchState Instance { get; private set; }

    [SerializeField]
    private GamePresenter _gamePresenter;

    private readonly NetworkVariable<int> _scoreRed = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<int> _scoreBlue = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private GameModel _gameModel;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _scoreRed.OnValueChanged += OnScoreRedChanged;
        _scoreBlue.OnValueChanged += OnScoreBlueChanged;

        if (_gamePresenter == null)
        {
            _gamePresenter = FindAnyObjectByType<GamePresenter>();
        }

        if (_gamePresenter != null)
        {
            _gameModel = _gamePresenter.Model;
        }

        ApplyScoresToLocalModel();

        Debug.Log($"NetworkMatchState spawned. IsServer={IsServer}, IsClient={IsClient}");
    }

    public override void OnNetworkDespawn()
    {
        _scoreRed.OnValueChanged -= OnScoreRedChanged;
        _scoreBlue.OnValueChanged -= OnScoreBlueChanged;

        base.OnNetworkDespawn();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ResetScoresOnServer()
    {
        if (!IsServer)
        {
            return;
        }

        _scoreRed.Value = 0;
        _scoreBlue.Value = 0;

        ApplyScoresToLocalModel();

        Debug.Log("Network scores reset.");
    }

    public void RegisterHitOnServer(
    ulong attackerClientId,
    ulong victimClientId,
    int attackerTeamIndex,
    Vector3 hitPoint)
    {
        if (!IsServer)
        {
            return;
        }

        if (attackerClientId == victimClientId)
        {
            Debug.Log("Ignoring self-hit.");
            return;
        }

        PlayerColor attackerColor = (PlayerColor)attackerTeamIndex;

        if (attackerColor == PlayerColor.Red)
        {
            _scoreRed.Value++;
        }
        else if (attackerColor == PlayerColor.Blue)
        {
            _scoreBlue.Value++;
        }

        AwardGoldRpc(
                        1,
                         RpcTarget.Single(attackerClientId, RpcTargetUse.Temp)
                     );

        PlayHitEffectRpc(
                            victimClientId,
                            attackerTeamIndex,
                            hitPoint
                        );

        if (NetworkMatchReporter.Instance != null)
        {
            NetworkMatchReporter.Instance.ReportHitOnServer(
                attackerClientId,
                victimClientId
            );
        }
        else
        {
            Debug.LogWarning("Cannot report hit: NetworkMatchReporter.Instance is null.");
        }

        Debug.Log(
            $"Registered hit. Attacker={attackerClientId}, Victim={victimClientId}, " +
            $"Team={attackerColor}, Red={_scoreRed.Value}, Blue={_scoreBlue.Value}"
        );

        ApplyScoresToLocalModel();
    }

    private void OnScoreRedChanged(int previousValue, int newValue)
    {
        ApplyScoresToLocalModel();
    }

    private void OnScoreBlueChanged(int previousValue, int newValue)
    {
        ApplyScoresToLocalModel();
    }

    private void ApplyScoresToLocalModel()
    {
        if (_gameModel == null)
        {
            if (_gamePresenter == null)
            {
                _gamePresenter = FindAnyObjectByType<GamePresenter>();
            }

            if (_gamePresenter != null)
            {
                _gameModel = _gamePresenter.Model;
            }
        }

        if (_gameModel == null || _gameModel.CurrentMatch == null)
        {
            return;
        }

        _gameModel.CurrentMatch.ScoreRed = _scoreRed.Value;
        _gameModel.CurrentMatch.ScoreBlue = _scoreBlue.Value;
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void AwardGoldRpc(int amount, RpcParams rpcParams = default)
    {
        _ = AwardGoldAsync(amount);
    }

    private async System.Threading.Tasks.Task AwardGoldAsync(int amount)
    {
        if (PlayFabEconomyService.Instance == null)
        {
            Debug.LogWarning("Cannot award Gold: PlayFabEconomyService.Instance is null.");
            return;
        }

        try
        {
            int newBalance = await PlayFabEconomyService.Instance.AddGoldAsync(amount);

            Debug.Log($"Awarded {amount} Gold to local player. New balance: {newBalance}");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Failed to award Gold: {ex.Message}");
        }
    }


    [Rpc(SendTo.Everyone)]
    private void PlayHitEffectRpc(
    ulong victimClientId,
    int attackerTeamIndex,
    Vector3 hitPoint)
    {
        PlayerPresenter[] presenters = FindObjectsByType<PlayerPresenter>(
            FindObjectsSortMode.None
        );

        foreach (PlayerPresenter presenter in presenters)
        {
            if (presenter.PlayerId == victimClientId)
            {
                presenter.PlayNetworkHitEffect(
                    (PlayerColor)attackerTeamIndex,
                    hitPoint
                );

                return;
            }
        }

        Debug.LogWarning($"Could not find PlayerPresenter for hit victim {victimClientId}.");
    }
}
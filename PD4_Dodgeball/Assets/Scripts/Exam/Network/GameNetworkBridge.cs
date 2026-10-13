using Dodgeball.Model;
using Dodgeball.Presenter;
using Unity.Netcode;
using UnityEngine;

public class GameNetworkBridge : MonoBehaviour
{
    [SerializeField]
    private GamePresenter _gamePresenter;

    private GameModel _gameModel;
    private NetworkPlayerRegistry _registry;
    private bool _isSubscribed;

    private void Start()
    {
        _gameModel = _gamePresenter.Model;

        // SelectTeamModel with real network client IDs.
        _gameModel.TeamSelectionFactory = CreateNetworkTeamSelectionModel;

        TrySubscribeRegistry();
    }

    private void Update()
    {
        // In case the registry spawns slightly after this bridge.
        if (!_isSubscribed)
        {
            TrySubscribeRegistry();
        }
    }

    private void OnDestroy()
    {
        if (_registry != null)
        {
            _registry.PlayersChanged -= OnRegistryPlayersChanged;
        }
    }

    private void TrySubscribeRegistry()
    {
        _registry = NetworkPlayerRegistry.Instance;

        if (_registry == null)
        {
            return;
        }

        _registry.PlayersChanged -= OnRegistryPlayersChanged;
        _registry.PlayersChanged += OnRegistryPlayersChanged;
        _isSubscribed = true;

        OnRegistryPlayersChanged();
    }

    private void OnRegistryPlayersChanged()
    {
        if (_gameModel == null || _registry == null)
        {
            return;
        }

        if (_registry.HasTwoPlayers)
        {
            _gameModel.NotifyEnoughPlayersConnected();
        }

        SyncTeamSelectionModelFromRegistry();
    }

    private SelectTeamModel CreateNetworkTeamSelectionModel()
    {
        if (_registry == null || _registry.PlayerCount < 2)
        {
            Debug.LogWarning("Creating fallback SelectTeamModel because registry does not have two players yet.");
            return new SelectTeamModel();
        }

        ulong player1Id = _registry.GetPlayerAt(0).ClientId;
        ulong player2Id = _registry.GetPlayerAt(1).ClientId;
        ulong localClientId = NetworkManager.Singleton.LocalClientId;

        Debug.Log(
            $"Creating network SelectTeamModel. " +
            $"Player1={player1Id}, Player2={player2Id}, Local={localClientId}"
        );

        return new SelectTeamModel(player1Id, player2Id, localClientId);
    }

    private void SyncTeamSelectionModelFromRegistry()
    {
        if (_gameModel == null ||
            _gameModel.TeamSelection == null ||
            _registry == null)
        {
            return;
        }

        for (int i = 0; i < _registry.PlayerCount; i++)
        {
            NetworkPlayerData player = _registry.GetPlayerAt(i);

            if (player.HasSelectedTeam)
            {
                PlayerColor color = (PlayerColor)player.TeamIndex;
                _gameModel.TeamSelection.SetSelection(player.ClientId, color);
            }

            _gameModel.TeamSelection.SetReady(player.ClientId, player.IsReady);
        }
    }
}
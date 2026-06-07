using TMPro;
using Unity.Netcode;
using UnityEngine;

public class GameplayPlayerNamesPresenter : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _redPlayerNameText;

    [SerializeField]
    private TextMeshProUGUI _bluePlayerNameText;

    [SerializeField]
    private TextMeshProUGUI _localPlayerNameText;

    [SerializeField]
    private TextMeshProUGUI _opponentPlayerNameText;

    private NetworkPlayerRegistry _registry;

    private void OnEnable()
    {
        TrySubscribe();
        Refresh();
    }

    private void Update()
    {
        if (_registry == null)
        {
            TrySubscribe();
        }
    }

    private void OnDisable()
    {
        if (_registry != null)
        {
            _registry.PlayersChanged -= Refresh;
        }
    }

    private void TrySubscribe()
    {
        _registry = NetworkPlayerRegistry.Instance;

        if (_registry == null)
        {
            return;
        }

        _registry.PlayersChanged -= Refresh;
        _registry.PlayersChanged += Refresh;

        Refresh();
    }

    private void Refresh()
    {
        if (_registry == null)
        {
            SetText(_redPlayerNameText, "Red: -");
            SetText(_bluePlayerNameText, "Blue: -");
            SetText(_localPlayerNameText, "You: -");
            SetText(_opponentPlayerNameText, "Opponent: -");
            return;
        }

        string redName = "-";
        string blueName = "-";

        for (int i = 0; i < _registry.PlayerCount; i++)
        {
            NetworkPlayerData player = _registry.GetPlayerAt(i);

            string displayName = player.DisplayName.ToString();

            if (player.HasSelectedTeam)
            {
                if (player.TeamIndex == 1)
                {
                    redName = displayName;
                }
                else if (player.TeamIndex == 0)
                {
                    blueName = displayName;
                }
            }
        }

        SetText(_redPlayerNameText, $"Red: {redName}");
        SetText(_bluePlayerNameText, $"Blue: {blueName}");

        if (NetworkManager.Singleton != null &&
            _registry.TryGetLocalPlayer(out NetworkPlayerData localPlayer))
        {
            SetText(_localPlayerNameText, $"You: {localPlayer.DisplayName}");
        }

        if (NetworkManager.Singleton != null &&
            _registry.TryGetOtherPlayer(NetworkManager.Singleton.LocalClientId, out NetworkPlayerData opponent))
        {
            SetText(_opponentPlayerNameText, $"Opponent: {opponent.DisplayName}");
        }
    }

    private void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}
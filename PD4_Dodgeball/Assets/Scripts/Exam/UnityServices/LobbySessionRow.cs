using System;
using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class LobbySessionRow : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _sessionNameText;

    [SerializeField]
    private TextMeshProUGUI _playerCountText;

    [SerializeField]
    private Button _joinButton;

    private string _sessionId;
    private Action<string> _joinRequested;

    public void Setup(ISessionInfo sessionInfo, Action<string> joinRequested)
    {
        _sessionId = sessionInfo.Id;
        _joinRequested = joinRequested;

        if (_sessionNameText != null)
        {
            _sessionNameText.text = sessionInfo.Name;
        }

        int playerCount = sessionInfo.MaxPlayers - sessionInfo.AvailableSlots;

        if (_playerCountText != null)
        {
            _playerCountText.text =
                $"{playerCount}/{sessionInfo.MaxPlayers}";
        }

        _joinButton.onClick.RemoveAllListeners();
        _joinButton.onClick.AddListener(OnJoinClicked);
    }

    private void OnJoinClicked()
    {
        _joinRequested?.Invoke(_sessionId);
    }
}
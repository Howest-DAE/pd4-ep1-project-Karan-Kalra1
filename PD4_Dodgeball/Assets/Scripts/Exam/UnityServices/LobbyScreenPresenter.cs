using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class LobbyScreenPresenter : MonoBehaviour
{
    [Header("Services")]
    [SerializeField]
    private MultiplayerSessionService _sessionService;

    [Header("Create")]
    [SerializeField]
    private TMP_InputField _sessionNameInput;

    [SerializeField]
    private Button _createSessionButton;

    [Header("Join By Code")]
    [SerializeField]
    private TMP_InputField _joinCodeInput;

    [SerializeField]
    private Button _joinByCodeButton;

    [Header("Browse")]
    [SerializeField]
    private Button _refreshSessionsButton;

    [SerializeField]
    private Transform _sessionListParent;

    [SerializeField]
    private LobbySessionRow _sessionRowPrefab;

    [Header("Game Start")]
    [SerializeField]
    private Button _loadGameButton;

    [Header("Status")]
    [SerializeField]
    private TextMeshProUGUI _statusText;

    private void Awake()
    {
        _createSessionButton.onClick.AddListener(OnCreateSessionClicked);
        _joinByCodeButton.onClick.AddListener(OnJoinByCodeClicked);
        _refreshSessionsButton.onClick.AddListener(OnRefreshSessionsClicked);

        if (_loadGameButton != null)
        {
            _loadGameButton.onClick.AddListener(OnLoadGameClicked);
            _loadGameButton.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        _createSessionButton.onClick.RemoveListener(OnCreateSessionClicked);
        _joinByCodeButton.onClick.RemoveListener(OnJoinByCodeClicked);
        _refreshSessionsButton.onClick.RemoveListener(OnRefreshSessionsClicked);

        if (_loadGameButton != null)
        {
            _loadGameButton.onClick.RemoveListener(OnLoadGameClicked);
        }
    }

    private async void OnCreateSessionClicked()
    {
        await RunLobbyActionAsync(async () =>
        {
            SetStatus("Creating session...");

            string joinCode = await _sessionService.CreateSessionAsync(
                _sessionNameInput.text.Trim()
            );

            SetStatus($"Session created. Join code: {joinCode}");

            if (_loadGameButton != null)
            {
                _loadGameButton.gameObject.SetActive(true);
            }
        });
    }

    private async void OnJoinByCodeClicked()
    {
        await RunLobbyActionAsync(async () =>
        {
            SetStatus("Joining session...");

            await _sessionService.JoinSessionByCodeAsync(_joinCodeInput.text);

            SetStatus("Joined session. Waiting for host to load game...");
        });
    }

    private async void OnRefreshSessionsClicked()
    {
        await RunLobbyActionAsync(async () =>
        {
            SetStatus("Searching for sessions...");

            IList<ISessionInfo> sessions = await _sessionService.QuerySessionsAsync();

            ClearSessionList();

            foreach (ISessionInfo session in sessions)
            {
                LobbySessionRow row = Instantiate(
                    _sessionRowPrefab,
                    _sessionListParent
                );

                row.Setup(session, OnJoinSessionFromListClicked);
            }

            SetStatus($"Found {sessions.Count} sessions.");
        });
    }

    private async void OnJoinSessionFromListClicked(string sessionId)
    {
        await RunLobbyActionAsync(async () =>
        {
            SetStatus("Joining selected session...");

            await _sessionService.JoinSessionByIdAsync(sessionId);

            SetStatus("Joined session. Waiting for host to load game...");
        });
    }

    private async void OnLoadGameClicked()
    {
        await RunLobbyActionAsync(async () =>
        {
            SetStatus("Loading game scene...");

            await _sessionService.LoadGameSceneAsHostAsync();
        });
    }

    private async Task RunLobbyActionAsync(Func<Task> action)
    {
        SetInteractable(false);

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
            Debug.LogWarning($"Lobby action failed: {ex}");
        }
        finally
        {
            SetInteractable(true);
        }
    }

    private void ClearSessionList()
    {
        if (_sessionListParent == null)
        {
            return;
        }

        for (int i = _sessionListParent.childCount - 1; i >= 0; i--)
        {
            Destroy(_sessionListParent.GetChild(i).gameObject);
        }
    }

    private void SetInteractable(bool interactable)
    {
        _createSessionButton.interactable = interactable;
        _joinByCodeButton.interactable = interactable;
        _refreshSessionsButton.interactable = interactable;

        if (_loadGameButton != null)
        {
            _loadGameButton.interactable = interactable;
        }
    }

    private void SetStatus(string message)
    {
        if (_statusText != null)
        {
            _statusText.text = message;
        }

        Debug.Log(message);
    }
}
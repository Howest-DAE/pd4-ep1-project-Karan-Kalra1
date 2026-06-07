using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerSessionService : MonoBehaviour
{
    [SerializeField]
    private UnityServicesBootstrapper _unityServicesBootstrapper;

    [SerializeField]
    private string _gameSceneName = "GameScene";

    public ISession CurrentSession { get; private set; }

    public bool HasSession => CurrentSession != null;

    public bool IsSessionHost => CurrentSession != null && CurrentSession.IsHost;

    public string CurrentJoinCode =>
        CurrentSession != null ? CurrentSession.Code : string.Empty;

    public async Task<string> CreateSessionAsync(string sessionName)
    {
        await _unityServicesBootstrapper.EnsureInitializedAndSignedInAsync();

        if (string.IsNullOrWhiteSpace(sessionName))
        {
            sessionName = $"{SessionData.DisplayName}'s Session";
        }

        SessionOptions options = new SessionOptions
        {
            Name = sessionName,
            MaxPlayers = 2,
            IsPrivate = false,
            IsLocked = false
        }.WithRelayNetwork();

        CurrentSession = await MultiplayerService.Instance.CreateSessionAsync(options);

        RegisterSessionEvents(CurrentSession);

        Debug.Log(
            $"Session created. Id: {CurrentSession.Id}, Code: {CurrentSession.Code}"
        );

        return CurrentSession.Code;
    }

    public async Task JoinSessionByCodeAsync(string joinCode)
    {
        await _unityServicesBootstrapper.EnsureInitializedAndSignedInAsync();

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            throw new ArgumentException("Join code is required.");
        }

        try
        {
            CurrentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(
                joinCode.Trim().ToUpperInvariant()
            );

            RegisterSessionEvents(CurrentSession);

            Debug.Log(
                $"Joined session. Id: {CurrentSession.Id}, Code: {CurrentSession.Code}"
            );

            Debug.Log(
                $"After JoinSession: IsListening={NetworkManager.Singleton.IsListening}, " +
                $"IsHost={NetworkManager.Singleton.IsHost}, " +
                $"IsClient={NetworkManager.Singleton.IsClient}, " +
                $"IsServer={NetworkManager.Singleton.IsServer}"
            );
        }
        catch
        {
            if (CurrentSession != null)
            {
                try
                {
                    await CurrentSession.LeaveAsync();
                }
                catch
                {
                    // Ignore cleanup errors.
                }

                CurrentSession = null;
            }

            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            throw;
        }
    }

    public async Task<IList<ISessionInfo>> QuerySessionsAsync()
    {
        await _unityServicesBootstrapper.EnsureInitializedAndSignedInAsync();

        QuerySessionsOptions options = new QuerySessionsOptions
        {
            Count = 10
        };

        QuerySessionsResults results =
            await MultiplayerService.Instance.QuerySessionsAsync(options);

        Debug.Log($"Found {results.Sessions.Count} sessions.");

        return results.Sessions;
    }

    public async Task JoinSessionByIdAsync(string sessionId)
    {
        await _unityServicesBootstrapper.EnsureInitializedAndSignedInAsync();

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session id is required.");
        }

        CurrentSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);

        RegisterSessionEvents(CurrentSession);

        Debug.Log($"Joined session by id. Id: {CurrentSession.Id}");
    }

    public async Task LeaveSessionAsync()
    {
        if (CurrentSession == null)
        {
            return;
        }

        try
        {
            await CurrentSession.LeaveAsync();
        }
        finally
        {
            CurrentSession = null;

            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }
    }

    public async Task LoadGameSceneAsHostAsync()
    {
        if (NetworkManager.Singleton == null)
        {
            throw new InvalidOperationException("NetworkManager.Singleton is missing.");
        }

        while (!NetworkManager.Singleton.IsListening)
        {
            await Task.Yield();
        }

        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("Only the host/server can load the network game scene.");
            return;
        }

        Debug.Log("Loading GameScene through Netcode SceneManager...");

        NetworkManager.Singleton.SceneManager.LoadScene(
            _gameSceneName,
            LoadSceneMode.Single
        );
    }

    private void RegisterSessionEvents(ISession session)
    {
        session.PlayerJoined += OnSessionPlayerJoined;
        session.PlayerLeaving += OnSessionPlayerLeft;
    }

    private void OnSessionPlayerJoined(string playerId)
    {
        Debug.Log($"Session player joined: {playerId}");

        if (CurrentSession != null)
        {
            Debug.Log(
                $"Session player count: {GetCurrentPlayerCount()}/{CurrentSession.MaxPlayers}"
            );
        }
    }

    private void OnSessionPlayerLeft(string playerId)
    {
        Debug.Log($"Session player left: {playerId}");

        if (CurrentSession != null)
        {
            Debug.Log(
                $"Session player count: {GetCurrentPlayerCount()}/{CurrentSession.MaxPlayers}"
            );
        }
    }

    private async void OnDestroy()
    {
        if (CurrentSession != null)
        {
            try
            {
                await CurrentSession.LeaveAsync();
            }
            catch
            {
                // Ignore cleanup exceptions during shutdown.
            }
        }
    }

    private int GetCurrentPlayerCount()
    {
        if (CurrentSession == null)
        {
            return 0;
        }

        return CurrentSession.MaxPlayers - CurrentSession.AvailableSlots;
    }
}
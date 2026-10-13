using System;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class GameOverBackendStatsPresenter : MonoBehaviour
{
    [Header("UI")]
    //[SerializeField]
    //private TextMeshProUGUI _titleText;

    [SerializeField]
    private TextMeshProUGUI _statsText;

    [SerializeField]
    private TextMeshProUGUI _statusText;

    [Header("Settings")]
    [SerializeField]
    private float _waitForMatchIdTimeoutSeconds = 5f;

    private bool _isLoading;

    private void OnEnable()
    {
        _ = LoadStatsAsync();
    }

    private async Task LoadStatsAsync()
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;

        //SetTitle("Game Over");
        SetStatus("Loading match stats...");
        SetStats("");

        try
        {
            int matchId = await WaitForMatchIdAsync();

            if (matchId <= 0)
            {
                SetStatus("No backend match ID found.");
                return;
            }

            if (BackendApiClient.Instance == null)
            {
                SetStatus("Backend API client is missing.");
                return;
            }

            MatchSummaryDto summary =
                await BackendApiClient.Instance.GetMatchAsync(matchId);

            DisplaySummary(summary);
            SetStatus("");
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to load match stats: {ex.Message}");
            Debug.LogWarning($"Failed to load backend match stats: {ex}");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task<int> WaitForMatchIdAsync()
    {
        float startTime = Time.time;

        while (Time.time - startTime < _waitForMatchIdTimeoutSeconds)
        {
            if (NetworkMatchReporter.Instance != null &&
                NetworkMatchReporter.Instance.MatchId.Value > 0)
            {
                return NetworkMatchReporter.Instance.MatchId.Value;
            }

            await Task.Yield();
        }

        return 0;
    }

    private void DisplaySummary(MatchSummaryDto summary)
    {
        if (summary == null)
        {
            SetStats("No match summary received.");
            return;
        }

        StringBuilder builder = new StringBuilder();

        builder.AppendLine($"Match #{summary.matchId}");
        builder.AppendLine();

        if (summary.players == null || summary.players.Count == 0)
        {
            builder.AppendLine("No player stats found.");
            SetStats(builder.ToString());
            return;
        }

        foreach (MatchPlayerInfoDto player in summary.players)
        {
            bool isLocalPlayer = player.playFabId == SessionData.PlayFabId;

            string localMarker = isLocalPlayer ? " (You)" : "";
            string role = player.isHost ? "Host" : "Client";

            builder.AppendLine($"{player.displayName}{localMarker}");
            builder.AppendLine($"Role: {role}");
            builder.AppendLine($"Points scored: {player.pointsScored}");
            builder.AppendLine($"Times hit: {player.timesHit}");
            builder.AppendLine($"Crowns remaining: {player.crownsRemaining}");
            builder.AppendLine();
        }

        SetStats(builder.ToString());
    }

   /* private void SetTitle(string text)
    {
        if (_titleText != null)
        {
            _titleText.text = text;
        }
    }*/

    private void SetStats(string text)
    {
        if (_statsText != null)
        {
            _statsText.text = text;
        }
    }

    private void SetStatus(string text)
    {
        if (_statusText != null)
        {
            _statusText.text = text;
        }
    }
}
using UnityEngine;

public static class SessionData
{
    public static string PlayFabId { get; private set; }
    public static string DisplayName { get; private set; }
    public static string Email { get; private set; }

    public static bool IsLoggedIn => !string.IsNullOrWhiteSpace(PlayFabId);

    public static void SetLogin(string playFabId, string displayName, string email)
    {
        PlayFabId = playFabId;
        DisplayName = displayName;
        Email = email;

        Debug.Log($"Session started. PlayFabId: {PlayFabId}, DisplayName: {DisplayName}");
    }

    public static void Clear()
    {
        PlayFabId = null;
        DisplayName = null;
        Email = null;
    }
}
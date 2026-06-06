public readonly struct AuthSession
{
    public readonly string PlayFabId;
    public readonly string DisplayName;
    public readonly string Email;

    public AuthSession(string playFabId, string displayName, string email)
    {
        PlayFabId = playFabId;
        DisplayName = displayName;
        Email = email;
    }
}
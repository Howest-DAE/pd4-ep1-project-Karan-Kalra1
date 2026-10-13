namespace PD4Dodgeball.Api.DTOs;

public class MatchPlayerInfoDto
{
    public string PlayFabId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsHost { get; set; }

    public int PointsScored { get; set; }

    public int TimesHit { get; set; }

    public int CrownsRemaining { get; set; } = 1;

}
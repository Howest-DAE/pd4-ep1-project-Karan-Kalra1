namespace PD4Dodgeball.Api.Models;

public class MatchPlayer
{
    public int Id { get; set; }

    public int MatchId { get; set; }
    public Match Match { get; set; } = null!;

    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public bool IsHost { get; set; }

    public int PointsScored { get; set; }

    public int CrownsRemaining { get; set; } = 5;
}
namespace PD4Dodgeball.Api.Models;

public class Match
{
    public int Id { get; set; }

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? EndedAtUtc { get; set; }

    public List<MatchPlayer> MatchPlayers { get; set; } = new();

    public List<PlayerHit> PlayerHits { get; set; } = new();
}
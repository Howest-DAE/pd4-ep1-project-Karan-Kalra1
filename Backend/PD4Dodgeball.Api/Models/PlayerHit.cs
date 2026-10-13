namespace PD4Dodgeball.Api.Models;

public class PlayerHit
{
    public int Id { get; set; }

    public int MatchId { get; set; }
    public Match Match { get; set; } = null!;

    public int AttackerPlayerId { get; set; }
    public Player AttackerPlayer { get; set; } = null!;

    public int VictimPlayerId { get; set; }
    public Player VictimPlayer { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
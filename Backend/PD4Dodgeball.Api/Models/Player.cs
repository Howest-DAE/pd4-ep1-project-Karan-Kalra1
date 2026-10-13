using System.ComponentModel.DataAnnotations;

namespace PD4Dodgeball.Api.Models;

public class Player
{
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string PlayFabId { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string DisplayName { get; set; } = string.Empty;

    public List<MatchPlayer> MatchPlayers { get; set; } = new();
}
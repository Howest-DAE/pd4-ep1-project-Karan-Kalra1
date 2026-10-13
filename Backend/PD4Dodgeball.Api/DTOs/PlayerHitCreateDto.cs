using System.ComponentModel.DataAnnotations;

namespace PD4Dodgeball.Api.DTOs;

public class PlayerHitCreateDto
{
    [Required]
    public string AttackerPlayFabId { get; set; } = string.Empty;

    [Required]
    public string VictimPlayFabId { get; set; } = string.Empty;
}
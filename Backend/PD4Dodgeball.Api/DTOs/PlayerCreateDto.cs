using System.ComponentModel.DataAnnotations;

namespace PD4Dodgeball.Api.DTOs;

public class PlayerCreateDto
{
    [Required]
    public string PlayFabId { get; set; } = string.Empty;

    [Required]
    public string DisplayName { get; set; } = string.Empty;
}
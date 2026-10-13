using System.ComponentModel.DataAnnotations;

namespace PD4Dodgeball.Api.DTOs;

public class CreateMatchDto
{
    [Required]
    public string HostPlayFabId { get; set; } = string.Empty;

    [Required]
    public string ClientPlayFabId { get; set; } = string.Empty;
}
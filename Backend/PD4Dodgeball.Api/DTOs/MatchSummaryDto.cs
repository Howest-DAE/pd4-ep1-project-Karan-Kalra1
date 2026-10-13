namespace PD4Dodgeball.Api.DTOs;

public class MatchSummaryDto
{
    public int MatchId { get; set; }

    public List<MatchPlayerInfoDto> Players { get; set; } = new();
}
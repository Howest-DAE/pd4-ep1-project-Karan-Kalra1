using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PD4Dodgeball.Api.Data;
using PD4Dodgeball.Api.DTOs;
using PD4Dodgeball.Api.Models;

namespace PD4Dodgeball.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class PlayersController : ControllerBase
{
    private readonly DodgeballDbContext _db;

    public PlayersController(DodgeballDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<ActionResult<PlayerDto>> CreateOrUpdatePlayer(PlayerCreateDto dto)
    {
        Player? player = await _db.Players
            .FirstOrDefaultAsync(p => p.PlayFabId == dto.PlayFabId);

        if (player == null)
        {
            player = new Player
            {
                PlayFabId = dto.PlayFabId,
                DisplayName = dto.DisplayName
            };

            _db.Players.Add(player);
        }
        else
        {
            player.DisplayName = dto.DisplayName;
        }

        await _db.SaveChangesAsync();

        return Ok(new PlayerDto
        {
            PlayFabId = player.PlayFabId,
            DisplayName = player.DisplayName
        });
    }

    [HttpGet("{playFabId}")]
    public async Task<ActionResult<PlayerDto>> GetPlayer(string playFabId)
    {
        Player? player = await _db.Players
            .FirstOrDefaultAsync(p => p.PlayFabId == playFabId);

        if (player == null)
        {
            return NotFound();
        }

        return Ok(new PlayerDto
        {
            PlayFabId = player.PlayFabId,
            DisplayName = player.DisplayName
        });
    }

    [HttpGet("{playFabId}/Matches")]
    public async Task<ActionResult<List<MatchSummaryDto>>> GetMatchesForPlayer(string playFabId)
    {
        Player? player = await _db.Players
            .FirstOrDefaultAsync(p => p.PlayFabId == playFabId);

        if (player == null)
        {
            return NotFound();
        }

        List<int> matchIds = await _db.MatchPlayers
            .Where(mp => mp.PlayerId == player.Id)
            .Select(mp => mp.MatchId)
            .ToListAsync();

        List<MatchSummaryDto> summaries = new();

        foreach (int matchId in matchIds)
        {
            MatchSummaryDto? summary = await BuildMatchSummary(matchId);

            if (summary != null)
            {
                summaries.Add(summary);
            }
        }

        return Ok(summaries);
    }

    private async Task<MatchSummaryDto?> BuildMatchSummary(int matchId)
    {
        Match? match = await _db.Matches
            .Include(m => m.MatchPlayers)
                .ThenInclude(mp => mp.Player)
            .Include(m => m.PlayerHits)
            .FirstOrDefaultAsync(m => m.Id == matchId);

        if (match == null)
        {
            return null;
        }

        MatchSummaryDto dto = new MatchSummaryDto
        {
            MatchId = match.Id
        };

        foreach (MatchPlayer matchPlayer in match.MatchPlayers)
        {
            int timesHit = match.PlayerHits
                .Count(h => h.VictimPlayerId == matchPlayer.PlayerId);

            dto.Players.Add(new MatchPlayerInfoDto
            {
                PlayFabId = matchPlayer.Player.PlayFabId,
                DisplayName = matchPlayer.Player.DisplayName,
                IsHost = matchPlayer.IsHost,
                PointsScored = matchPlayer.PointsScored,
                TimesHit = timesHit,
                CrownsRemaining = matchPlayer.CrownsRemaining
            });
        }

        return dto;
    }
}
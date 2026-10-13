using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PD4Dodgeball.Api.Data;
using PD4Dodgeball.Api.DTOs;
using PD4Dodgeball.Api.Models;

namespace PD4Dodgeball.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class MatchesController : ControllerBase
{
    private readonly DodgeballDbContext _db;

    public MatchesController(DodgeballDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<ActionResult<MatchCreatedDto>> CreateMatch(CreateMatchDto dto)
    {
        Player? host = await _db.Players
            .FirstOrDefaultAsync(p => p.PlayFabId == dto.HostPlayFabId);

        Player? client = await _db.Players
            .FirstOrDefaultAsync(p => p.PlayFabId == dto.ClientPlayFabId);

        if (host == null || client == null)
        {
            return BadRequest("Both players must exist before creating a match.");
        }

        Match match = new Match
        {
            StartedAtUtc = DateTime.UtcNow
        };

        match.MatchPlayers.Add(new MatchPlayer
        {
            Player = host,
            IsHost = true,
            PointsScored = 0,
            CrownsRemaining = 1
        });

        match.MatchPlayers.Add(new MatchPlayer
        {
            Player = client,
            IsHost = false,
            PointsScored = 0,
            CrownsRemaining = 1
        });

        _db.Matches.Add(match);

        await _db.SaveChangesAsync();

        return Ok(new MatchCreatedDto
        {
            MatchId = match.Id
        });
    }

    [HttpGet("{matchId:int}")]
    public async Task<ActionResult<MatchSummaryDto>> GetMatch(int matchId)
    {
        MatchSummaryDto? summary = await BuildMatchSummary(matchId);

        if (summary == null)
        {
            return NotFound();
        }

        return Ok(summary);
    }

    [HttpPost("{matchId:int}/PlayerHit")]
    public async Task<IActionResult> AddPlayerHit(int matchId, PlayerHitCreateDto dto)
    {
        Match? match = await _db.Matches
            .Include(m => m.MatchPlayers)
            .FirstOrDefaultAsync(m => m.Id == matchId);

        if (match == null)
        {
            return NotFound("Match not found.");
        }

        Player? attacker = await _db.Players
            .FirstOrDefaultAsync(p => p.PlayFabId == dto.AttackerPlayFabId);

        Player? victim = await _db.Players
            .FirstOrDefaultAsync(p => p.PlayFabId == dto.VictimPlayFabId);

        if (attacker == null || victim == null)
        {
            return BadRequest("Attacker and victim must both exist.");
        }

        bool attackerInMatch = match.MatchPlayers.Any(mp => mp.PlayerId == attacker.Id);
        bool victimInMatch = match.MatchPlayers.Any(mp => mp.PlayerId == victim.Id);

        if (!attackerInMatch || !victimInMatch)
        {
            return BadRequest("Both players must belong to this match.");
        }

        PlayerHit hit = new PlayerHit
        {
            MatchId = match.Id,
            AttackerPlayerId = attacker.Id,
            VictimPlayerId = victim.Id,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.PlayerHits.Add(hit);

        MatchPlayer? attackerMatchPlayer = match.MatchPlayers
            .FirstOrDefault(mp => mp.PlayerId == attacker.Id);

        MatchPlayer? victimMatchPlayer = match.MatchPlayers
           .FirstOrDefault(mp => mp.PlayerId == victim.Id);

        if (attackerMatchPlayer != null)
        {
            attackerMatchPlayer.PointsScored += 1;
        }


        if (victimMatchPlayer != null)
        {
            victimMatchPlayer.CrownsRemaining =
       Math.Max(0, victimMatchPlayer.CrownsRemaining - 1);

        }

        await _db.SaveChangesAsync();

        return Ok();
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
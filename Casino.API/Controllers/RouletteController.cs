using System.Security.Claims;
using System.Text.Json;
using Casino.Application.DTOs;
using Casino.Application.Interfaces;
using Casino.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Casino.API.Controllers;

[ApiController]
[Route("api/roulette")]
[Authorize]
public sealed class RouletteController(IRouletteService rouletteService, ApplicationDbContext db) : ControllerBase
{
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var configuration = await db.RouletteConfigurations.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == 1, cancellationToken);
        var game = await db.Games.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == RouletteService.GameId && item.isActive, cancellationToken);
        if (configuration is null || game is null)
            return NotFound(new { Error = "La configuración de ruleta todavía no está disponible." });

        decimal[] chips;
        try { chips = JsonSerializer.Deserialize<decimal[]>(configuration.ChipValuesJson) ?? [10m, 25m, 50m, 100m]; }
        catch (JsonException) { chips = [10m, 25m, 50m, 100m]; }
        return Ok(new
        {
            configuration.BettingDurationSeconds,
            MinimumBet = game.ApuestaMinima,
            MaximumBet = game.ApuestaMaxima,
            ChipValues = chips
        });
    }

    [HttpGet("state")]
    public async Task<IActionResult> GetState(CancellationToken cancellationToken)
    {
        var state = await rouletteService.GetStateAsync(cancellationToken);
        return state is null ? NotFound() : Ok(state);
    }

    [HttpPost("bets")]
    public async Task<IActionResult> PlaceBet([FromBody] RouletteBetRequest request, CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized(new { Error = "Token inválido: no se pudo identificar al usuario." });

        try
        {
            return Ok(await rouletteService.PlaceBetAsync(userId, request, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { Error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { Error = exception.Message });
        }
    }
}

using System.Security.Claims;
using System.Text.Json;
using Casino.Application.DTOs;
using Casino.Application.Interfaces;
using Casino.Domain.Entities;
using Casino.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Casino.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminController(
    ApplicationDbContext db,
    IRenaperService ageCheck,
    IRouletteEventStream events,
    ILogger<AdminController> logger) : ControllerBase
{
    private static readonly Guid RouletteGameId = RouletteService.GameId;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken)
    {
        var utcToday = DateTime.UtcNow.Date;
        var registeredUsers = await db.Users.CountAsync(cancellationToken);
        var admins = await db.Users.CountAsync(user => user.Rol != Role.user, cancellationToken);
        var rounds = await db.RouletteRounds.CountAsync(cancellationToken);
        var bets = await db.Bets.CountAsync(bet => bet.RouletteRoundId != null, cancellationToken);
        var roundsToday = await db.RouletteRounds.CountAsync(round => round.OpenedAtUtc >= utcToday, cancellationToken);
        var betsToday = await db.Bets.CountAsync(bet => bet.RouletteRoundId != null && bet.Fecha >= utcToday, cancellationToken);
        var totalWagered = await db.Bets.Where(bet => bet.RouletteRoundId != null)
            .SumAsync(bet => (decimal?)bet.MontoApostado, cancellationToken) ?? 0m;
        var totalPaid = await db.Bets.Where(bet => bet.RouletteRoundId != null)
            .SumAsync(bet => (decimal?)bet.MontoGanado, cancellationToken) ?? 0m;
        var todayWagered = await db.Bets.Where(bet => bet.RouletteRoundId != null && bet.Fecha >= utcToday)
            .SumAsync(bet => (decimal?)bet.MontoApostado, cancellationToken) ?? 0m;
        var todayPaid = await db.Bets.Where(bet => bet.RouletteRoundId != null && bet.Fecha >= utcToday)
            .SumAsync(bet => (decimal?)bet.MontoGanado, cancellationToken) ?? 0m;
        var recentRounds = await db.RouletteRounds.AsNoTracking()
            .OrderByDescending(round => round.OpenedAtUtc)
            .Take(10)
            .Select(round => new AdminRoundSummary(
                round.Id,
                round.OpenedAtUtc,
                round.SettledAtUtc,
                round.WinningNumber,
                round.Bets.Count(),
                round.Bets.Sum(bet => (decimal?)bet.MontoApostado) ?? 0m,
                round.Bets.Sum(bet => (decimal?)bet.MontoGanado) ?? 0m))
            .ToArrayAsync(cancellationToken);

        return Ok(new AdminStatisticsResponse(
            registeredUsers, admins, rounds, bets, roundsToday, betsToday,
            totalWagered, totalPaid, totalWagered - totalPaid,
            todayWagered, todayPaid, recentRounds));
    }

    [HttpGet("roulette-settings")]
    public async Task<IActionResult> GetRouletteSettings(CancellationToken cancellationToken)
    {
        var settings = await db.RouletteConfigurations.AsNoTracking()
            .SingleOrDefaultAsync(configuration => configuration.Id == 1, cancellationToken);
        var game = await db.Games.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == RouletteGameId, cancellationToken);
        if (settings is null || game is null) return NotFound(new { Error = "La configuración de ruleta todavía no está disponible." });

        var updatedByEmail = settings.UpdatedByUserId is Guid updatedBy
            ? await db.Users.AsNoTracking().Where(user => user.Id == updatedBy).Select(user => user.Email).SingleOrDefaultAsync(cancellationToken)
            : null;
        return Ok(ToSettingsResponse(settings, game, updatedByEmail));
    }

    [HttpPut("roulette-settings")]
    public async Task<IActionResult> UpdateRouletteSettings(
        [FromBody] UpdateRouletteSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var invalid = ValidateSettings(request);
        if (invalid is not null) return BadRequest(new { Error = invalid });
        if (!TryGetActorId(out var actorId)) return Unauthorized();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var settings = await db.RouletteConfigurations
            .FromSqlRaw("SELECT * FROM \"RouletteConfigurations\" WHERE \"Id\" = 1 FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        var game = await db.Games.SingleOrDefaultAsync(item => item.Id == RouletteGameId, cancellationToken);
        if (settings is null || game is null)
            return Conflict(new { Error = "No se encontró la configuración inicial de la ruleta." });

        var before = new
        {
            bettingDurationSeconds = settings.BettingDurationSeconds,
            minimumBet = game.ApuestaMinima,
            maximumBet = game.ApuestaMaxima,
            chipValues = DeserializeChipValues(settings.ChipValuesJson)
        };
        settings.BettingDurationSeconds = request.BettingDurationSeconds;
        settings.ChipValuesJson = JsonSerializer.Serialize(request.ChipValues, JsonOptions);
        settings.UpdatedAtUtc = DateTime.UtcNow;
        settings.UpdatedByUserId = actorId;
        game.ApuestaMinima = request.MinimumBet;
        game.ApuestaMaxima = request.MaximumBet;

        var audit = new AdminAuditLog
        {
            Id = Guid.NewGuid(), ActorUserId = actorId, Action = "roulette.settings.updated",
            DetailsJson = JsonSerializer.Serialize(new { before, after = request }, JsonOptions),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.AdminAuditLogs.Add(audit);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var actorEmail = await db.Users.AsNoTracking().Where(user => user.Id == actorId)
            .Select(user => user.Email).SingleOrDefaultAsync(cancellationToken);
        var response = ToSettingsResponse(settings, game, actorEmail);
        try
        {
            await events.PublishAsync(new RouletteEvent("roulette-settings-updated", response), CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Se guardó la configuración, pero no se pudo notificar a la mesa en tiempo real.");
        }
        return Ok(response);
    }

    [HttpGet("admins")]
    public async Task<IActionResult> GetAdmins(CancellationToken cancellationToken)
    {
        var users = await db.Users.AsNoTracking()
            .Where(user => user.Rol != Role.user)
            .OrderBy(user => user.Nombre)
            .Select(user => new { user.Id, user.Nombre, user.Apellido, user.Email, user.Rol, user.Saldo })
            .ToArrayAsync(cancellationToken);
        return Ok(users.Select(user => new AdminUserSummary(
            user.Id, $"{user.Nombre} {user.Apellido}", user.Email, user.Rol.ToString(), user.Saldo)));
    }

    [HttpPost("admins")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorId)) return Unauthorized();
        var email = request.Email.Trim().ToLowerInvariant();
        var dni = request.Dni.Trim();
        if (await db.Users.AnyAsync(user => user.Email.ToLower() == email || user.Dni == dni, cancellationToken))
            return Conflict(new { Error = "El correo o DNI ya pertenece a una cuenta." });

        if (DateTime.UtcNow.Date < request.FechaNacimiento.Date.AddYears(18))
            return BadRequest(new { Error = "El administrador debe ser mayor de edad." });
        if (!await ageCheck.EsMayorDeEdadAsync(dni))
            return BadRequest(new { Error = "No se pudo validar la mayoría de edad para ese DNI." });

        var admin = new User
        {
            Id = Guid.NewGuid(), Dni = dni, Nombre = request.Nombre.Trim(), Apellido = request.Apellido.Trim(),
            FechaNacimiento = DateTime.SpecifyKind(request.FechaNacimiento.Date, DateTimeKind.Utc),
            Email = email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Saldo = 0m, Rol = Role.admin, BilleteraID = ""
        };

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Users.Add(admin);
        db.AdminAuditLogs.Add(new AdminAuditLog
        {
            Id = Guid.NewGuid(), ActorUserId = actorId, TargetUserId = admin.Id,
            Action = "admin.created", DetailsJson = JsonSerializer.Serialize(new { adminId = admin.Id }, JsonOptions),
            CreatedAtUtc = DateTime.UtcNow
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Se rechazó la creación concurrente de una cuenta de administrador.");
            return Conflict(new { Error = "El correo o DNI ya pertenece a una cuenta." });
        }

        return StatusCode(StatusCodes.Status201Created,
            new AdminUserSummary(admin.Id, $"{admin.Nombre} {admin.Apellido}", admin.Email, admin.Rol.ToString(), admin.Saldo));
    }

    [HttpGet("audit-log")]
    public async Task<IActionResult> GetAuditLog(CancellationToken cancellationToken)
    {
        var entries = await (from log in db.AdminAuditLogs.AsNoTracking()
                             join actor in db.Users.AsNoTracking() on log.ActorUserId equals actor.Id
                             orderby log.CreatedAtUtc descending
                             select new AdminAuditSummary(
                                 log.Id, log.Action, log.DetailsJson, log.CreatedAtUtc, log.TargetUserId, actor.Email))
            .Take(30)
            .ToArrayAsync(cancellationToken);
        return Ok(entries);
    }

    private bool TryGetActorId(out Guid actorId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out actorId);
    }

    private static string? ValidateSettings(UpdateRouletteSettingsRequest request)
    {
        if (request.BettingDurationSeconds is < 5 or > 300)
            return "El intervalo entre tiradas debe estar entre 5 y 300 segundos.";
        if (request.MinimumBet <= 0m || request.MaximumBet < request.MinimumBet || request.MaximumBet > 1_000_000m)
            return "Definí montos válidos: mínimo mayor a cero y máximo igual o superior al mínimo.";
        if (decimal.Round(request.MinimumBet, 2) != request.MinimumBet || decimal.Round(request.MaximumBet, 2) != request.MaximumBet)
            return "Los montos admiten hasta dos decimales.";
        if (request.ChipValues is null || request.ChipValues.Length is < 1 or > 12)
            return "Configurá entre 1 y 12 valores de ficha.";
        if (request.ChipValues.Any(value => value < request.MinimumBet || value > request.MaximumBet || decimal.Round(value, 2) != value)
            || request.ChipValues.Distinct().Count() != request.ChipValues.Length)
            return "Cada ficha debe ser única, tener hasta dos decimales y estar entre el mínimo y el máximo de apuesta.";
        return null;
    }

    private static RouletteSettingsResponse ToSettingsResponse(RouletteConfiguration settings, Game game, string? updatedByEmail) =>
        new(settings.BettingDurationSeconds, game.ApuestaMinima, game.ApuestaMaxima,
            DeserializeChipValues(settings.ChipValuesJson), settings.UpdatedAtUtc, updatedByEmail);

    private static decimal[] DeserializeChipValues(string json)
    {
        try { return JsonSerializer.Deserialize<decimal[]>(json, JsonOptions) ?? [10m, 25m, 50m, 100m]; }
        catch (JsonException) { return [10m, 25m, 50m, 100m]; }
    }
}

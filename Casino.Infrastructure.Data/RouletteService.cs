using System.Security.Cryptography;
using System.Text.Json;
using Casino.Application.DTOs;
using Casino.Application.Interfaces;
using Casino.Application.Services;
using Casino.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Casino.Infrastructure.Data;

public sealed class RouletteService : IRouletteService
{
    public static readonly Guid GameId = Guid.Parse("a941f3ab-6ca8-4b9c-9af5-b5f6033d1d39");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _db;
    private readonly IRouletteEventStream _events;

    public RouletteService(ApplicationDbContext db, IRouletteEventStream events)
    {
        _db = db;
        _events = events;
    }

    public async Task<RouletteStateResponse?> GetStateAsync(CancellationToken cancellationToken)
    {
        var round = await _db.RouletteRounds.AsNoTracking()
            .OrderByDescending(r => r.OpenedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return round is null ? null : ToResponse(round);
    }

    public async Task<RouletteBetResponse> PlaceBetAsync(Guid userId, RouletteBetRequest request, CancellationToken cancellationToken)
    {
        RouletteRules.ValidateBet(request);

        var priorBet = await _db.Bets.AsNoTracking()
            .SingleOrDefaultAsync(b => b.UserId == userId && b.IdempotencyKey == request.RequestId, cancellationToken);
        if (priorBet is not null)
        {
            var originalDetails = JsonSerializer.Deserialize<RouletteBetDetails>(priorBet.DetallesJson, JsonOptions);
            if (originalDetails is null || priorBet.RouletteRoundId != request.RoundId || priorBet.MontoApostado != request.Amount ||
                originalDetails.Type != request.Type || originalDetails.Selection != RouletteRules.NormalizeSelection(request.Type, request.Selection))
                throw new InvalidOperationException("El requestId ya fue utilizado con otra apuesta.");

            var currentBalance = await _db.Users.AsNoTracking().Where(u => u.Id == userId)
                .Select(u => u.Saldo).SingleAsync(cancellationToken);
            return new RouletteBetResponse(priorBet.Id, priorBet.RouletteRoundId!.Value, currentBalance);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var round = await _db.RouletteRounds
            .FromSqlInterpolated($"SELECT * FROM \"RouletteRounds\" WHERE \"Id\" = {request.RoundId} FOR SHARE")
            .SingleOrDefaultAsync(cancellationToken);

        if (round is null || round.State != RouletteRoundState.BettingOpen || round.BettingClosesAtUtc <= DateTime.UtcNow)
            throw new InvalidOperationException("La ronda no existe o ya cerró las apuestas.");

        // Comparte el bloqueo con el panel admin: una apuesta concurrente ve
        // íntegramente los límites anteriores o los nuevos.
        var game = await _db.Games
            .FromSqlInterpolated($"SELECT * FROM \"Games\" WHERE \"Id\" = {GameId} FOR SHARE")
            .SingleOrDefaultAsync(cancellationToken);
        if (game is null || !game.isActive)
            throw new InvalidOperationException("La ruleta no está disponible.");
        if (request.Amount < game.ApuestaMinima || request.Amount > game.ApuestaMaxima)
            throw new InvalidOperationException($"La apuesta debe estar entre {game.ApuestaMinima} y {game.ApuestaMaxima}.");

        var debited = await _db.Users
            .Where(u => u.Id == userId && u.Saldo >= request.Amount)
            .ExecuteUpdateAsync(update => update.SetProperty(u => u.Saldo, u => u.Saldo - request.Amount), cancellationToken);
        if (debited != 1)
            throw new InvalidOperationException("Usuario inexistente o saldo insuficiente.");

        var betTransaction = new Transaction
        {
            Id = Guid.NewGuid(), UserId = userId, Tipo = TransactionType.Apuesta,
            Monto = request.Amount, Fecha = DateTime.UtcNow, ReferenciaExterna = $"roulette:{round.Id}"
        };
        var bet = new Bet
        {
            Id = Guid.NewGuid(), UserId = userId, GameId = GameId, RouletteRoundId = round.Id,
            IdempotencyKey = request.RequestId,
            TransactionId = betTransaction.Id, MontoApostado = request.Amount, Estado = BetStatus.Pendiente,
            Fecha = DateTime.UtcNow,
            DetallesJson = JsonSerializer.Serialize(new RouletteBetDetails(request.Type, RouletteRules.NormalizeSelection(request.Type, request.Selection)), JsonOptions)
        };
        _db.Transactions.Add(betTransaction);
        _db.Bets.Add(bet);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var balance = await _db.Users.AsNoTracking().Where(u => u.Id == userId)
            .Select(u => u.Saldo).SingleAsync(cancellationToken);
        await _events.PublishAsync(new RouletteEvent("bet-accepted", new { BetId = bet.Id, RoundId = round.Id }), cancellationToken);
        return new RouletteBetResponse(bet.Id, round.Id, balance);
    }

    public async Task<RouletteRound> EnsureOpenRoundAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var openRound = await _db.RouletteRounds
            .FromSqlRaw("SELECT * FROM \"RouletteRounds\" WHERE \"State\" = 1 ORDER BY \"OpenedAtUtc\" DESC LIMIT 1 FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (openRound is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return openRound;
        }

        await EnsureGameAsync(cancellationToken);
        // El bloqueo comparte el mismo valor de configuración entre instancias
        // y evita iniciar una ronda con una actualización a medio guardar.
        var configuration = await _db.RouletteConfigurations
            .FromSqlRaw("SELECT * FROM \"RouletteConfigurations\" WHERE \"Id\" = 1 FOR SHARE")
            .SingleOrDefaultAsync(cancellationToken);
        var duration = Math.Clamp(configuration?.BettingDurationSeconds ?? 30, 5, 300);
        var now = DateTime.UtcNow;
        var round = new RouletteRound
        {
            Id = Guid.NewGuid(), State = RouletteRoundState.BettingOpen,
            OpenedAtUtc = now, BettingClosesAtUtc = now.AddSeconds(duration)
        };
        _db.RouletteRounds.Add(round);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await _events.PublishAsync(new RouletteEvent("round-opened", ToResponse(round)), cancellationToken);
        return round;
    }

    public async Task SettleRoundAsync(Guid roundId, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var round = await _db.RouletteRounds
            .FromSqlInterpolated($"SELECT * FROM \"RouletteRounds\" WHERE \"Id\" = {roundId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (round is null || round.State != RouletteRoundState.BettingOpen)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }
        if (round.BettingClosesAtUtc > DateTime.UtcNow)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var winningNumber = RandomNumberGenerator.GetInt32(0, 37);
        var bets = await _db.Bets.Where(b => b.RouletteRoundId == roundId && b.Estado == BetStatus.Pendiente)
            .ToListAsync(cancellationToken);
        foreach (var bet in bets)
        {
            var details = JsonSerializer.Deserialize<RouletteBetDetails>(bet.DetallesJson, JsonOptions)!;
            var multiplier = RouletteRules.IsWinner(details, winningNumber) ? RouletteRules.PayoutMultiplier(details.Type) : 0;
            bet.Estado = multiplier > 0 ? BetStatus.Ganada : BetStatus.Perdida;
            bet.MontoGanado = bet.MontoApostado * multiplier;
            if (bet.MontoGanado > 0)
            {
                await _db.Users.Where(u => u.Id == bet.UserId)
                    .ExecuteUpdateAsync(update => update.SetProperty(u => u.Saldo, u => u.Saldo + bet.MontoGanado), cancellationToken);
                _db.Transactions.Add(new Transaction
                {
                    Id = Guid.NewGuid(), UserId = bet.UserId, Tipo = TransactionType.Premio,
                    Monto = bet.MontoGanado, Fecha = DateTime.UtcNow, ReferenciaExterna = $"roulette:{roundId}:{bet.Id}"
                });
            }
        }

        round.State = RouletteRoundState.Settled;
        round.WinningNumber = winningNumber;
        round.SettledAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await _events.PublishAsync(new RouletteEvent("round-settled", ToResponse(round)), cancellationToken);
    }

    private async Task EnsureGameAsync(CancellationToken cancellationToken)
    {
        if (!await _db.Games.AnyAsync(g => g.Id == GameId, cancellationToken))
        {
            _db.Games.Add(new Game
            {
                Id = GameId, Nombre = "Ruleta europea", ApuestaMinima = 1m,
                ApuestaMaxima = 10000m, isActive = true
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private static RouletteStateResponse ToResponse(RouletteRound round) => new(
        round.Id, round.State.ToString(), round.OpenedAtUtc, round.BettingClosesAtUtc,
        round.SettledAtUtc, round.WinningNumber,
        round.WinningNumber is null ? null : round.WinningNumber == 0 ? "green" : RouletteRules.IsRed(round.WinningNumber.Value) ? "red" : "black");
}

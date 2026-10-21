using Casino.Domain.Entities;

namespace Casino.Application.DTOs;

public sealed record RouletteBetRequest(
    Guid RequestId,
    Guid RoundId,
    decimal Amount,
    RouletteBetType Type,
    string Selection);

public sealed record RouletteBetResponse(Guid BetId, Guid RoundId, decimal Balance);

public sealed record RouletteStateResponse(
    Guid Id,
    string State,
    DateTime OpenedAtUtc,
    DateTime BettingClosesAtUtc,
    DateTime? SettledAtUtc,
    int? WinningNumber,
    string? WinningColor);

public sealed record RouletteEvent(string Name, object Data);

public sealed record RouletteBetDetails(RouletteBetType Type, string Selection);

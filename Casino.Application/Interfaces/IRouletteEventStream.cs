using Casino.Application.DTOs;
using Casino.Domain.Entities;

namespace Casino.Application.Interfaces;

public interface IRouletteEventStream
{
    ValueTask PublishAsync(RouletteEvent rouletteEvent, CancellationToken cancellationToken = default);
    IAsyncEnumerable<RouletteEvent> ReadAllAsync(CancellationToken cancellationToken = default);
}

public interface IRouletteService
{
    Task<RouletteStateResponse?> GetStateAsync(CancellationToken cancellationToken);
    Task<RouletteBetResponse> PlaceBetAsync(Guid userId, RouletteBetRequest request, CancellationToken cancellationToken);
    Task<RouletteRound> EnsureOpenRoundAsync(CancellationToken cancellationToken);
    Task SettleRoundAsync(Guid roundId, CancellationToken cancellationToken);
}

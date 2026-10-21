using System.Threading.Channels;
using Casino.Application.DTOs;
using Casino.Application.Interfaces;

namespace Casino.API.Realtime;

public sealed class RouletteEventStream : IRouletteEventStream
{
    private readonly Channel<RouletteEvent> _channel = Channel.CreateUnbounded<RouletteEvent>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ValueTask PublishAsync(RouletteEvent rouletteEvent, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(rouletteEvent, cancellationToken);

    public IAsyncEnumerable<RouletteEvent> ReadAllAsync(CancellationToken cancellationToken = default) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

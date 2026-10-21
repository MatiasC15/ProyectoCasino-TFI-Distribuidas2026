using Casino.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Casino.API.Realtime;

public sealed class RouletteRealtimeWorker(
    IRouletteEventStream eventStream,
    IHubContext<RouletteHub> hubContext,
    ILogger<RouletteRealtimeWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var rouletteEvent in eventStream.ReadAllAsync(stoppingToken))
        {
            try
            {
                await hubContext.Clients.All.SendAsync("roulette-event", rouletteEvent, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "No se pudo difundir el evento de ruleta {EventName}.", rouletteEvent.Name);
            }
        }
    }
}

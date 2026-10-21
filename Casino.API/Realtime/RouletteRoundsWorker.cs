using Casino.Application.Interfaces;

namespace Casino.API.Realtime;

public sealed class RouletteRoundsWorker(IServiceScopeFactory scopeFactory, ILogger<RouletteRoundsWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var rouletteService = scope.ServiceProvider.GetRequiredService<IRouletteService>();
                var round = await rouletteService.EnsureOpenRoundAsync(stoppingToken);
                var delay = round.BettingClosesAtUtc - DateTime.UtcNow;
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, stoppingToken);

                await rouletteService.SettleRoundAsync(round.Id, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error en el ciclo de rondas de ruleta.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }
}

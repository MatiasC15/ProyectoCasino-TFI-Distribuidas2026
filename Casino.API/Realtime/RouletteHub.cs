using Casino.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Casino.API.Realtime;

[Authorize]
public sealed class RouletteHub(IRouletteService rouletteService) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var state = await rouletteService.GetStateAsync(Context.ConnectionAborted);
        if (state is not null)
            await Clients.Caller.SendAsync("roulette-event", new { name = "round-state", data = state }, Context.ConnectionAborted);

        await base.OnConnectedAsync();
    }
}

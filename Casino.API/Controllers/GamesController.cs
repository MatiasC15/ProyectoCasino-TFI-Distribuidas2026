using Casino.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Casino.API.Controllers;

[ApiController]
[Route("api/games")]
public sealed class GamesController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetGames(CancellationToken cancellationToken)
    {
        var games = await db.Games.AsNoTracking()
            .Where(game => game.isActive)
            .OrderBy(game => game.Nombre)
            .Select(game => new
            {
                game.Id,
                game.Nombre,
                game.ApuestaMinima,
                game.ApuestaMaxima,
                game.isActive
            })
            .ToListAsync(cancellationToken);

        return Ok(games);
    }
}

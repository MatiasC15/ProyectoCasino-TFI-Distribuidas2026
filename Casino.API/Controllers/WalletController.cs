using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Casino.Application.DTOs;
using Casino.Application.Services;
using Casino.Application.Interfaces;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Casino.Infrastructure.Data;

namespace Casino.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] 
public class WalletController : ControllerBase

{
    private readonly DepositService _depositService;
    private readonly WithdrawService _withdrawService;
    private readonly IUserRepository _users;
    private readonly ApplicationDbContext _db;

    public WalletController(DepositService depositService, WithdrawService withdrawService, IUserRepository users, ApplicationDbContext db)
    {
        _depositService = depositService;
        _withdrawService = withdrawService;
        _users = users;
        _db = db;
    }

    [HttpGet("balance")]
    public async Task<IActionResult> Balance()
    {
        var balance = await _users.GetBalanceAsync(GetUserIdFromToken());
        return balance is null ? NotFound() : Ok(new { Saldo = balance.Value });
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> Transactions(CancellationToken cancellationToken)
    {
        var userId = GetUserIdFromToken();
        var transactions = await _db.Transactions.AsNoTracking()
            .Where(transaction => transaction.UserId == userId)
            .OrderByDescending(transaction => transaction.Fecha)
            .ThenByDescending(transaction => transaction.Id)
            .Select(transaction => new
            {
                transaction.Id,
                Tipo = transaction.Tipo.ToString(),
                transaction.Monto,
                transaction.Fecha,
                transaction.ReferenciaExterna
            })
            .ToListAsync(cancellationToken);
        return Ok(transactions);
    }

    private Guid GetUserIdFromToken()
    {
        
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                          ?? User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
            throw new Exception("Token inválido: No se pudo identificar al usuario.");

        return Guid.Parse(userIdClaim);
    }

    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit([FromBody] TransactionRequest request)
    {
        try
        {
            var userId = GetUserIdFromToken();
            var newBalance = await _depositService.ExecuteAsync(userId, request.Amount);
            return Ok(new { Message = "Depósito exitoso", NuevoSaldo = newBalance });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw([FromBody] TransactionRequest request)
    {
        try
        {
            var userId = GetUserIdFromToken();
            var newBalance = await _withdrawService.ExecuteAsync(userId, request.Amount);
            return Ok(new { Message = "Retiro exitoso", NuevoSaldo = newBalance });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}

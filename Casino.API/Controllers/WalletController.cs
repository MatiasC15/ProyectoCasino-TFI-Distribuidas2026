using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Casino.Application.DTOs;
using Casino.Application.Services;
using System.Security.Claims;

namespace Casino.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] 
public class WalletController : ControllerBase

{
    private readonly DepositService _depositService;
    private readonly WithdrawService _withdrawService;

    public WalletController(DepositService depositService, WithdrawService withdrawService)
    {
        _depositService = depositService;
        _withdrawService = withdrawService;
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

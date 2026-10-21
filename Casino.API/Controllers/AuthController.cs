using Casino.Application.DTOs;
using Casino.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Casino.API.Controllers;

    [ApiController]
    [Route("api/[controller]")]

    public class AuthController : ControllerBase
    {
    private readonly RegisterUserService _registerUserService;
    private readonly LoginUserService _loginUserService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(RegisterUserService registerUserService, LoginUserService loginUserService, ILogger<AuthController> logger)
    {
        _registerUserService = registerUserService;
        _loginUserService = loginUserService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
    {
        try
        {
            var response = await _registerUserService.ExecuteAsync(request);
            return Ok(response);
        }
        catch (DbUpdateException ex)
        {
            // Conservamos el detalle para el log del servidor, sin exponer SQL ni
            // datos de la cuenta en la respuesta que recibe el navegador.
            _logger.LogError(ex, "No se pudo persistir el usuario durante el registro.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Error = "No se pudo guardar la cuenta. Revisá que el DNI y el correo sean únicos e intentá nuevamente." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _loginUserService.ExecuteAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return Unauthorized(new { Error = ex.Message });
        }
    }

}


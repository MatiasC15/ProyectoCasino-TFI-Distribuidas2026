using Casino.Application.DTOs;
using Casino.Application.Services;
using Microsoft.AspNetCore.Mvc;


namespace Casino.API.Controllers;

    [ApiController]
    [Route("api/[controller]")]

    public class AuthController : ControllerBase
    {
    private readonly RegisterUserService _registerUserService;
    private readonly LoginUserService _loginUserService;

    public AuthController(RegisterUserService registerUserService, LoginUserService loginUserService)
    {
        _registerUserService = registerUserService;
        _loginUserService = loginUserService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
    {
        try
        {
            var response = await _registerUserService.ExecuteAsync(request);
            return Ok(response);
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


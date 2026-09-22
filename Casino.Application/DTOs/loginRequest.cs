using System;
using System.Collections.Generic;
using System.Text;

namespace Casino.Application.DTOs
{
    public record LoginRequest(string Email, string Password);
    public record AuthResponse(string Token, Guid UserId);
}

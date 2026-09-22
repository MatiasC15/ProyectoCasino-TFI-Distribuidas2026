using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Casino.Application.Application;
using Casino.Application.Interfaces;
using Casino.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;


namespace Casino.Infrastructure.External
{
    public class JwtProvider : IJwtProvider
    {
        private readonly IConfiguration _configuration;

        public JwtProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string Generate(User user)
        {
            var secretKey = _configuration["JwtSettings:Secret"];
            var securiryKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
            var credentials = new SigningCredentials(securiryKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("rol", user.Rol.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["jwtSettings:Issuer"],
                audience: _configuration["jwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(int.Parse(_configuration["JwtSettings:ExpirationInMinutes"]!)),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

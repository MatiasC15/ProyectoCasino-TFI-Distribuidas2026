using Casino.Application.Application;
using Casino.Application.Interfaces;
using Casino.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Casino.Application.Services
{
    public class LoginUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtProvider _jwtProvider;

        public LoginUserService(IUserRepository userRepository, IJwtProvider jwtProvider)
        {
            _userRepository = userRepository;
            _jwtProvider = jwtProvider;
        }

        public async Task <AuthResponse> ExecuteAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                throw new Exception("Credenciales inválidas.");
            }
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                throw new Exception("Credenciales inválidas.");
            }
            var token = _jwtProvider.Generate(user);

            return new AuthResponse(token, user.Id);
        }
    }
}

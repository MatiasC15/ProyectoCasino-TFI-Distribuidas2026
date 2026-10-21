using Casino.Application.Application;
using Casino.Application.DTOs;
using Casino.Application.Interfaces;
using Casino.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;


namespace Casino.Application.Services
{
    public class RegisterUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRenaperService _renaperService;
        private readonly IJwtProvider _jwtProvider;

        public RegisterUserService(IUserRepository userRepository, IRenaperService renaperService, IJwtProvider jwtProvider)
        {
            _userRepository = userRepository;
            _renaperService = renaperService;
            _jwtProvider = jwtProvider;
        }

        public async Task <Guid> ExecuteAsync(RegisterUserRequest request)
        {
            var existingDni = await _userRepository.GetByDniAsync(request.Dni);
            if (existingDni != null)
            {
                throw new Exception("El DNI ya está registrado.");
            }
            var existingEmail = await _userRepository.GetByEmailAsync(request.Email);
            if (existingEmail != null)
                throw new Exception("El Email ya se encuentra registrado.");

            bool esMayorDeEdad = await _renaperService.EsMayorDeEdadAsync(request.Dni);
            if(!esMayorDeEdad)
            {
                throw new Exception("El Renaper indica que no eres mayor de edad o el DNI es inválido.");
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Dni = request.Dni,
                Nombre = request.Nombre,
                Apellido = request.Apellido,
                // El formulario envía una fecha civil sin zona horaria. La columna
                // PostgreSQL es timestamptz y Npgsql solo acepta DateTime UTC.
                // Guardar medianoche UTC conserva exactamente el día elegido.
                FechaNacimiento = DateTime.SpecifyKind(request.FechaNacimiento.Date, DateTimeKind.Utc),
                Email = request.Email,
                PasswordHash = hashedPassword,
                Saldo = 0, 
                Rol = Role.user,
                BilleteraID = "" //Se vincula luego con la billetera del usuario
            };

            await _userRepository.AddAsync(newUser);

            var token = _jwtProvider.Generate(newUser);

            return newUser.Id;
        }
    }
}

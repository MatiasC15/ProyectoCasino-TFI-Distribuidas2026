using System.Globalization;
using Casino.Domain.Entities;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Casino.API.Security;

public static class InitialSuperAdminBootstrapper
{
    public static async Task EnsureCreatedAsync(
        ApplicationDbContext db,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var email = configuration["AdminBootstrap:Email"]?.Trim().ToLowerInvariant();
        var dni = configuration["AdminBootstrap:Dni"]?.Trim();
        var password = configuration["AdminBootstrap:Password"];
        var name = configuration["AdminBootstrap:Nombre"]?.Trim();
        var surname = configuration["AdminBootstrap:Apellido"]?.Trim();
        var birthDateText = configuration["AdminBootstrap:FechaNacimiento"];

        if (new[] { email, dni, password, name, surname, birthDateText }.All(string.IsNullOrWhiteSpace))
        {
            logger.LogWarning("No se configuró AdminBootstrap; no existe una cuenta inicial de superadministrador.");
            return;
        }

        if (new[] { email, dni, password, name, surname, birthDateText }.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("AdminBootstrap debe incluir Email, Dni, Nombre, Apellido, FechaNacimiento y Password.");
        if (password!.Length < 12)
            throw new InvalidOperationException("La contraseña de AdminBootstrap debe tener al menos 12 caracteres.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(dni!, @"^\d{7,8}$"))
            throw new InvalidOperationException("AdminBootstrap:Dni debe tener 7 u 8 dígitos.");
        if (!DateTime.TryParse(birthDateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var birthDate))
            throw new InvalidOperationException("AdminBootstrap:FechaNacimiento debe usar el formato YYYY-MM-DD.");
        if (DateTime.UtcNow.Date < birthDate.Date.AddYears(18))
            throw new InvalidOperationException("El superadministrador inicial debe ser mayor de edad.");

        if (await db.Users.AnyAsync(user => user.Rol == Role.superadmin, cancellationToken)) return;
        if (await db.Users.AnyAsync(user => user.Email.ToLower() == email || user.Dni == dni, cancellationToken))
            throw new InvalidOperationException("El correo o DNI configurado para AdminBootstrap ya pertenece a otra cuenta; no se elevó su rol.");

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(), Email = email!, Dni = dni!, Nombre = name!, Apellido = surname!,
            FechaNacimiento = DateTime.SpecifyKind(birthDate.Date, DateTimeKind.Utc),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Saldo = 0m, Rol = Role.superadmin, BilleteraID = ""
        });
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Se creó el superadministrador inicial configurado en AdminBootstrap.");
    }
}

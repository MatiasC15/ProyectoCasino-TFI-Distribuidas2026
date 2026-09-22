using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Casino.Application.DTOs
{
    public record RegisterUserRequest(
       [Required(ErrorMessage = "El DNI es obligatorio.")]
    [RegularExpression(@"^\d{7,8}$", ErrorMessage = "El DNI debe tener 7 u 8 números.")]
    string Dni,

       [Required(ErrorMessage = "El nombre es obligatorio.")]
    string Nombre,

       [Required]
    string Apellido,

       [Required]
    DateTime FechaNacimiento,

       [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido.")] // Regex interna de Microsoft
    string Email,

       [Required]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    string Password
   );
}

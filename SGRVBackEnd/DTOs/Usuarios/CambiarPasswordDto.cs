using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Usuarios
{
    public class CambiarPasswordDto
    {
        [Required]
        public string PasswordActual { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string PasswordNueva { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(PasswordNueva))]
        public string ConfirmarPassword { get; set; } = string.Empty;
    }
}

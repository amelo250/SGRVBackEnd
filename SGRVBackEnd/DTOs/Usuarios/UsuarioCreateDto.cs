using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Usuarios
{
    public class UsuarioCreateDto
    {

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Apellido { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Correo { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Rol { get; set; } = "Empleado";
    }
}

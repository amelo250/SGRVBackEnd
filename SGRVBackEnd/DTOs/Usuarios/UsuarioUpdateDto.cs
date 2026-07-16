using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Usuarios
{
    public class UsuarioUpdateDto
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
        [MaxLength(30)]
        public string Rol { get; set; } = string.Empty;

        public bool Activo { get; set; }
    }
}

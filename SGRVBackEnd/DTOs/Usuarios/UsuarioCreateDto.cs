using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Usuarios
{
    public class UsuarioCreateDto
    {

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string Telefono { get; set; } = string.Empty;

        [Required]
        public int IdEmpresa { get; set; }

        [Required]
        public int IdRol { get; set; }


        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string PasswordHash { get; set; } = string.Empty;
        [Required]
        public bool Activo { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;




    }
}

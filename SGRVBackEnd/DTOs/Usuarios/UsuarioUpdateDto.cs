using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Usuarios
{
    public class UsuarioUpdateDto
    {
        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;
        public int IdEmpresa { get; set; } 
        


        [Required]
        [MaxLength(100)]
        public string Apellido { get; set; } = string.Empty;

      

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public int IdRol { get; set; } = 0;

        public string? Telefono { get; set; }

        public bool Activo { get; set; }
    }
}

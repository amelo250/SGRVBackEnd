using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Clientes
{
    public class ClienteCreateDto
    {

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Apellido { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Documento { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Telefono { get; set; }

        [EmailAddress]
        [MaxLength(150)]
        public string? Correo { get; set; }

        [MaxLength(250)]
        public string? Direccion { get; set; }
    }
}

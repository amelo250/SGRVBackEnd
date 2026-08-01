using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Empresas
{
    public class EmpresaCreateDto
    {

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; } = string.Empty;
        [Required]
        public int IdPlan { get; set; }
        [Required]
        public string NombreComercial { get; set; } = string.Empty;
       

        [Required]
        [MaxLength(20)]
        public string Rnc { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Telefono { get; set; }

        [EmailAddress]
        [MaxLength(150)]
        
        public string? Correo { get; set; }
        public string? LogoUrl { get; set; }


        [MaxLength(250)]
        public string? Direccion { get; set; }

    }
}

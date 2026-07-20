using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.Models.Empresa
{
    public class Empresa
    {
        [Key]
        public int IdEmpresa { get; set; }
        public int IdPlan { get; set; } 
        public string Nombre { get; set; } = string.Empty;
        public string NombreComercial { get; set; } = string.Empty;
        public string RNC { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public DateTime? FechaActualizacion { get; set; }
        public bool Activo { get; set; } = false;


    }
}

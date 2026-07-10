using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.Models.Cliente
{
    public class Cliente
    {
        [Key]
        public int IdCliente { get; set; }
        public int IdEmpresa { get; set; } 
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string CedulaPasaporte { get; set; } = string.Empty;
        public DateTime FechaNacimiento { get; set; } 
        public string email { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Nacionalidad { get; set; } = string.Empty;
        public string LicenciaConducir { get; set; } = string.Empty;
        public DateTime FechaExpLicencia { get; set; } 
        public DateTime FechaVencLicencia { get; set; }
        public bool Activo { get; set; } = false;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}

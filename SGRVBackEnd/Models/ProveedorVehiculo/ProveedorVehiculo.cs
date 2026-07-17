using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.Models.ProveedoresVehiculos
{
    public class ProveedorVehiculo
    {
        [Key]
        public int IdProveedorVehiculo { get; set; }
        [Required]
        public int IdEmpresa { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string RncCedula { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Contacto { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
        public bool Activo { get; set; } = false;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaActualizacion { get; set; }



    }
}

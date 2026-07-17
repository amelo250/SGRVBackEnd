using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.Models.Mantenimientos
{
    public class Mantenimientos
    {
        [Key]
         public int IdMantenimiento { get; set; }
        public int IdVehiculo { get; set; }
        public int IdTipoMantenimiento { get; set; }
        public DateTime Fecha { get; set; }
        public string? Taller { get; set; }
        public int Kilometraje { get; set; }
        public decimal Costo { get; set; }
        public string Observacion { get; set; } = string.Empty;
    }
}

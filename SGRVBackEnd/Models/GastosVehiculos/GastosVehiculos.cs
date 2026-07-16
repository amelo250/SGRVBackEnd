using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.Models.GastosVehiculos
{
    public class GastosVehiculos
    {
        [Key]
        public int IdGasto { get; set; } 
        public int IdVehiculo { get; set; }
        public int IdTipoGasto { get; set; }
        public DateTime Fecha { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string Observacion { get; set; } = string.Empty;

    }
}

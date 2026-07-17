using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.Models.CxPProveedores
{
    public class CuentaPorPagarProveedor
    {
        [Key]
        public int IdCuentaPorPagar { get; set; }
        public int IdEmpresa { get; set; }
        public int IdProveedorVehiculo { get; set; }
        public int IdRenta { get; set; }
        public int IdMoneda { get; set; }
        public decimal MontoOriginal { get; set; }
        public decimal MontoPagado { get; set; }
        public decimal BalancePendiente { get; set; }
        public DateTime FechaGeneracion { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;



    }
}

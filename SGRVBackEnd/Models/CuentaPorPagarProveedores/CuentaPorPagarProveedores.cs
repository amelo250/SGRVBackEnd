using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.Models.CxPProveedores
{
    public class CuentaPorPagarProveedores
    {
        [Key]
        public int IdCuentaPorPagar { get; set; }
        public int IdEmpresa { get; set; }
        public int IdProveedor { get; set; }
        public int IdRenta { get; set; }
        public int IdMoneda { get; set; }
        public decimal MontoOriginal { get; set; }
        public decimal MontoPagado { get; set; }
        public decimal BalancePendiente { get; set; }
        public DateTime


    }
}

using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.Models.PagosProveedores
{
    public class PagosProveedores
    {
        [Key]
        public int IdPagoProveedor { get; set; }
        public int IdEmpresa { get; set; }
        public int IdCuentaPorPagar { get; set; }
      
        public int IdMoneda { get; set; }
        public decimal MontoPagado { get; set; }
        public decimal TasaCambioAplicada { get; set; }
        public decimal MontoEquivalenteCuenta { get; set; }
        public DateTime FechaPago { get; set; }
        public int IdMetodoPago { get; set; }
        public string? Referencia { get; set; }
        public bool Anulado { get; set; } 
        public DateTime? FechaAnulacion { get; set; }
        public string? MotivoAnulacion { get; set; }
       public DateTime FechaCreacion { get; set; } = DateTime.Now;


    }
}

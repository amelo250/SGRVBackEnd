using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.Models.Renta
{
    public class Renta
    {
        [Key]
        public int IdRenta { get; set; }
        public int IdEmpresa { get; set; }
        public int IdVehiculo { get; set; }
        public int IdCliente { get; set; }
        public int IdEstado { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public DateTime? FechaEntregaReal { get; set; }
        public decimal PrecioPorDia { get; set; }
        public int CantidadDias { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuestos { get; set; }
        public decimal Descuentos { get; set; }
        public decimal Total { get; set; }
        public decimal Deposito { get; set; }
        public string? Observaciones { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public int IdUsuarioCreacion { get; set; }
        public int? IdProveedorVehiculo { get; set; }
        public int? IdAcuerdoVehiculo { get; set; }
        public int IdMoneda { get; set; }
        public decimal TasaCambioAplicada { get; set; }
        public decimal TotalMonedaLocal { get; set; }



    }
}

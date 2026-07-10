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
        public DateTime FechaEntregaReal { get; set; }
        public double PrecioPorDia { get; set; }
        public int CantidadDias { get; set; }
        public double Subtotal { get; set; }
        public double Impuestos { get; set; }
        public double Descuentos { get; set; }
        public double Total { get; set; }
        public double Deposito { get; set; }
        public string Observaciones { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}

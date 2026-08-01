using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Pagos
{
    public class PagoCreateDto
    {
        public int IdEmpresa { get; set; }
        public int IdRenta { get; set; }
        public int IdMetodoPago { get; set; }
        public int IdEstadoPago { get; set; }
        public int IdMoneda { get; set; }
        public decimal Monto { get; set; }
        public decimal TasaCambioAplicada { get; set; } = 1m;
        public DateTime FechaPago { get; set; }
        public string? Referencia { get; set; }
        public string? Observaciones { get; set; }
    }

}

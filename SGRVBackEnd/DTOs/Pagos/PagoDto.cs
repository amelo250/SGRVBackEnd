namespace SGRVBackEnd.DTOs.Pagos
{

    public class PagoDto
    {
        public int IdPago { get; set; }
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
        public DateTime FechaRegistro { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public bool Activo { get; set; }
    }

}

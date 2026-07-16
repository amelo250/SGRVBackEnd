namespace SGRVBackEnd.DTOs.Pagos
{
    public class PagoResponseDto
    {
        public int Id { get; set; }

        public int RentaId { get; set; }

        public DateTime FechaPago { get; set; }

        public decimal Monto { get; set; }

        public string MetodoPago { get; set; } = string.Empty;

        public string? Referencia { get; set; }

        public string Estado { get; set; } = string.Empty;

        public string? Observaciones { get; set; }
    }
}

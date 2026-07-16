using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Pagos
{
    public class PagoCreateDto
    {
        [Required]
        public int RentaId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Monto { get; set; }

        [Required]
        [MaxLength(50)]
        public string MetodoPago { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Referencia { get; set; }

        [MaxLength(250)]
        public string? Observaciones { get; set; }
    }
}

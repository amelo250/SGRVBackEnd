using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Pagos
{
    public class PagoUpdateDto
    {
        [Range(0.01, double.MaxValue)]
        public decimal Monto { get; set; }

        [Required]
        [MaxLength(50)]
        public string MetodoPago { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Referencia { get; set; }

        [Required]
        [MaxLength(30)]
        public string Estado { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Observaciones { get; set; }
    }
}

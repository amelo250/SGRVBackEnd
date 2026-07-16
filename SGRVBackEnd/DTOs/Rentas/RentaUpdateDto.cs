using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Rentas
{
    public class RentaUpdateDto
    {
        [Required]
        public int ClienteId { get; set; }

        [Required]
        public int VehiculoId { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public DateTime FechaFin { get; set; }

        public DateTime? FechaEntrega { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Deposito { get; set; }

        [Required]
        [MaxLength(30)]
        public string Estado { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Observaciones { get; set; }
    }
}

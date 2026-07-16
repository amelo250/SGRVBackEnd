using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Rentas
{
    public class RentaCreateDto
    {
        [Required]
        public int ClienteId { get; set; }

        [Required]
        public int VehiculoId { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public DateTime FechaFin { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Deposito { get; set; }

        [MaxLength(500)]
        public string? Observaciones { get; set; }
    }
}

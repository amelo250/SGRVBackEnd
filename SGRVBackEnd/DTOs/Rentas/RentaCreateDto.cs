using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Rentas
{
    public class RentaCreateDto
    {
        [Required]
        public int IdCliente { get; set; }

        [Required]
        public int IdVehiculo { get; set; }

        [Required]
        public int IdUsuario { get; set; }

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

using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Vehiculos
{
    public class VehiculoUpdateDto
    {
        [Required]
        public int IdEmpresa { get; set; }
        [Required]
        public int IdEstado { get; set; }

        [Required]
        [MaxLength(50)]
        public string Marca { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Modelo { get; set; } = string.Empty;

        [Range(1900, 2100)]
        public int Anio { get; set; }

        [Required]
        [MaxLength(20)]
        public string Placa { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Color { get; set; }

        [MaxLength(100)]
        public string? Chasis { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal PrecioPorDia { get; set; }

        [Required]
        [MaxLength(30)]
        public string Estado { get; set; } = string.Empty;

        public bool Activo { get; set; }
    }
}

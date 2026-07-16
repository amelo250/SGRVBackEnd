using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Vehiculos
{
    public class VehiculoCreateDto
    {
        [Required]
        public int IdEmpresa { get; set; }
        [Required]
        public int IdEstado { get; set; }
        [Required]
        public int IdCombustible { get; set; }
        [Required]
        public int IdTransmision { get; set; }

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
        public string? VIN { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal PrecioPorDia { get; set; }

        public int Kilometraje { get; set; }

        public string Descripcion { get; set; } = string.Empty;
        public decimal DepositoCombustible { get; set; }
    }
}

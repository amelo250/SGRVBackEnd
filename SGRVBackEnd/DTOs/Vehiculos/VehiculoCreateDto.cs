using System.ComponentModel.DataAnnotations;
using SGRVBackEnd.Enums;
namespace SGRVBackEnd.DTOs.Vehiculos;

public class VehiculoCreateDto
{
    [Range(1, int.MaxValue)] public int IdCombustible { get; set; }
    [Range(1, int.MaxValue)] public int IdTransmision { get; set; }
    [Range(1, int.MaxValue)] public int IdTipo { get; set; }
    [Required] public TipoPropiedadVehiculo TipoPropiedad { get; set; }
    public int? IdProveedorVehiculo { get; set; }
    [Range(1, int.MaxValue)] public int IdMonedaTarifa { get; set; }
    [Required, MaxLength(50)] public string Marca { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string Modelo { get; set; } = string.Empty;
    [Range(1900, 2100)] public int Anio { get; set; }
    [Required, MaxLength(20)] public string Placa { get; set; } = string.Empty;
    [MaxLength(30)] public string? Color { get; set; }
    [MaxLength(100)] public string? VIN { get; set; }
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")] public decimal PrecioPorDia { get; set; }
    [Range(0, int.MaxValue)] public int Kilometraje { get; set; }
    [MaxLength(500)] public string? Descripcion { get; set; }
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] public decimal DepositoCombustible { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.AcuerdosVehiculosProveedor;

public class AcuerdoVehiculoProveedorCreateDto
{
    [Required(ErrorMessage = "El proveedor es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El proveedor seleccionado no es válido.")]
    public int ProveedorId { get; set; }

    [Required(ErrorMessage = "El vehículo es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El vehículo seleccionado no es válido.")]
    public int VehiculoId { get; set; }

    [Required(ErrorMessage = "El costo acordado es obligatorio.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El costo acordado debe ser mayor que cero.")]
    public decimal CostoAcordado { get; set; }

    [Required(ErrorMessage = "La moneda es obligatoria.")]
    [StringLength(3, MinimumLength = 3)]
    public string Moneda { get; set; } = "USD";

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    [StringLength(500)]
    public string? Observaciones { get; set; }

    public bool Activo { get; set; } = true;
}
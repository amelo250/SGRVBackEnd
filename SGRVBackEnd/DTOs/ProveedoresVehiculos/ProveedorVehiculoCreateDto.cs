using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.ProveedoresVehiculos;

public class ProveedorVehiculoCreateDto
{
    [Required, MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string RncCedula { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Telefono { get; set; }

    [MaxLength(250)]
    public string? Direccion { get; set; }

    [MaxLength(150)]
    public string? Contacto { get; set; }

    [MaxLength(500)]
    public string? Observacion { get; set; }
}

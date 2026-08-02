namespace SGRVBackEnd.DTOs.ProveedoresVehiculos;

public sealed class ProveedorVehiculoResponseDto
{
    public int IdProveedorVehiculo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string RncCedula { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Contacto { get; set; }
    public string? Observacion { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}

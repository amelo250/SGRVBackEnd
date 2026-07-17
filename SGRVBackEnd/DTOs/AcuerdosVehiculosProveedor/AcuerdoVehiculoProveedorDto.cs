namespace SGRVBackEnd.DTOs.AcuerdosVehiculosProveedor;

public class AcuerdoVehiculoProveedorDto
{
    public int Id { get; set; }

    public int ProveedorId { get; set; }

    public string? NombreProveedor { get; set; }

    public int VehiculoId { get; set; }

    public string? DescripcionVehiculo { get; set; }

    public decimal CostoAcordado { get; set; }

    public string Moneda { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public string? Observaciones { get; set; }

    public bool Activo { get; set; }
}
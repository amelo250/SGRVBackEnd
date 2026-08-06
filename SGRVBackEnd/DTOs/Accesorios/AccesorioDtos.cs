namespace SGRVBackEnd.DTOs.Accesorios;

public class AccesorioCreateDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Icono { get; set; }
}

public sealed class AccesorioUpdateDto : AccesorioCreateDto
{
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class AccesorioResponseDto
{
    public int IdAccesorio { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Icono { get; set; }
    public bool EsGlobal { get; set; }
    public bool Activo { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class VehiculoAccesorioCreateDto
{
    public int IdAccesorio { get; set; }
    public string? Observaciones { get; set; }
}

public sealed class VehiculoAccesorioResponseDto
{
    public int IdVehiculoAccesorio { get; set; }
    public int IdVehiculo { get; set; }
    public int IdAccesorio { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Icono { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; }
}

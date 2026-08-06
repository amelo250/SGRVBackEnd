namespace SGRVBackEnd.DTOs.Dashboard;

public sealed class DashboardTareaResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public int IdEntidad { get; set; }
    public int IdVehiculo { get; set; }
    public string Vehiculo { get; set; } = string.Empty;
    public int? IdCliente { get; set; }
    public string? Cliente { get; set; }
    public DateTime Fecha { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public string EstadoCodigo { get; set; } = string.Empty;
    public bool EsHoy { get; set; }
}

public sealed class DashboardTareasResponseDto
{
    public int TotalHoy { get; set; }
    public int TotalProximas { get; set; }
    public IReadOnlyList<DashboardTareaResponseDto> Hoy { get; set; } =
        Array.Empty<DashboardTareaResponseDto>();
    public IReadOnlyList<DashboardTareaResponseDto> Proximas { get; set; } =
        Array.Empty<DashboardTareaResponseDto>();
}

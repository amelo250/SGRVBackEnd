namespace SGRVBackEnd.DTOs.Calendario;

public sealed class CalendarioSearchDto
{
    public DateTimeOffset FechaDesde { get; set; }
    public DateTimeOffset FechaHasta { get; set; }
    public string? Tipo { get; set; }
    public int? IdVehiculo { get; set; }
}

public sealed class CalendarioEventoResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public int IdEntidad { get; set; }
    public int IdVehiculo { get; set; }
    public string Vehiculo { get; set; } = string.Empty;
    public int IdCliente { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string EstadoCodigo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string? Observacion { get; set; }
}

public sealed class CalendarioResumenResponseDto
{
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
    public int TotalEventos { get; set; }
    public int TotalReservaciones { get; set; }
    public int TotalRentas { get; set; }
    public IReadOnlyList<CalendarioEventoResponseDto> Eventos { get; set; } =
        Array.Empty<CalendarioEventoResponseDto>();
}

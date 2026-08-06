namespace SGRVBackEnd.DTOs.Mantenimientos;

public class MantenimientoCreateDto
{
    public int IdVehiculo { get; set; }
    public int IdTipoMantenimiento { get; set; }
    public DateTime Fecha { get; set; }
    public string? Taller { get; set; }
    public int? Kilometraje { get; set; }
    public decimal Costo { get; set; }
    public string? Observacion { get; set; }
}

public sealed class MantenimientoUpdateDto : MantenimientoCreateDto;

public sealed class MantenimientoSearchDto
{
    public int? IdVehiculo { get; set; }
    public int? IdTipoMantenimiento { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public string? Search { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class MantenimientoResponseDto
{
    public int IdMantenimiento { get; set; }
    public int IdVehiculo { get; set; }
    public string Vehiculo { get; set; } = string.Empty;
    public int IdTipoMantenimiento { get; set; }
    public string TipoCodigo { get; set; } = string.Empty;
    public string TipoNombre { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string? Taller { get; set; }
    public int? Kilometraje { get; set; }
    public decimal Costo { get; set; }
    public string? Observacion { get; set; }
}

public sealed class MantenimientoAlertaDto
{
    public int IdVehiculo { get; set; }
    public string Vehiculo { get; set; } = string.Empty;
    public int KilometrajeActual { get; set; }
    public DateTime? UltimaFecha { get; set; }
    public int? UltimoKilometraje { get; set; }
    public DateTime? ProximaFechaEstimada { get; set; }
    public int? ProximoKilometrajeEstimado { get; set; }
    public string Nivel { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}

public sealed class MantenimientoResumenDto
{
    public int TotalRegistros { get; set; }
    public decimal CostoTotal { get; set; }
    public int VehiculosSinHistorial { get; set; }
    public int AlertasProximas { get; set; }
    public int AlertasVencidas { get; set; }
    public IReadOnlyList<MantenimientoAlertaDto> Alertas { get; set; } =
        Array.Empty<MantenimientoAlertaDto>();
}

public sealed class MantenimientoCatalogoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}

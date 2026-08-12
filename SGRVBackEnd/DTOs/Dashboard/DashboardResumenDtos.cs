namespace SGRVBackEnd.DTOs.Dashboard;

public sealed class DashboardResumenResponseDto
{
    public int ReservasHoy { get; set; }
    public int VehiculosAlquilados { get; set; }
    public decimal IngresosHoy { get; set; }
    public int ClientesActivos { get; set; }
    public decimal IngresosMes { get; set; }
    public IReadOnlyList<DashboardIngresoDiarioDto> IngresosDiarios { get; set; } = [];
    public IReadOnlyList<DashboardReservaRecienteDto> ReservasRecientes { get; set; } = [];
    public IReadOnlyList<DashboardCategoriaVehiculoDto> VehiculosPorCategoria { get; set; } = [];
    public IReadOnlyList<DashboardActividadDto> ActividadReciente { get; set; } = [];
}

public sealed class DashboardIngresoDiarioDto
{
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
}

public sealed class DashboardReservaRecienteDto
{
    public int IdReservacion { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string Vehiculo { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public string EstadoCodigo { get; set; } = string.Empty;
    public string EstadoNombre { get; set; } = string.Empty;
}

public sealed class DashboardCategoriaVehiculoDto
{
    public string Categoria { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal Porcentaje { get; set; }
}

public sealed class DashboardActividadDto
{
    public string Tipo { get; set; } = string.Empty;
    public int IdEntidad { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}

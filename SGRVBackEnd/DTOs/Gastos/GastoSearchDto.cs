using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Gastos;

public sealed class GastoSearchDto
{
    public int? IdTipoGasto { get; set; }
    public int? IdVehiculo { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public string? Search { get; set; }
    public bool IncluirInactivos { get; set; }

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Rentas;

public sealed class RentaSearchDto
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [MaxLength(150)]
    public string? Search { get; set; }

    [Range(1, int.MaxValue)]
    public int? IdEstado { get; set; }

    [Range(1, int.MaxValue)]
    public int? IdCliente { get; set; }

    [Range(1, int.MaxValue)]
    public int? IdVehiculo { get; set; }

    public DateTimeOffset? FechaDesde { get; set; }
    public DateTimeOffset? FechaHasta { get; set; }
}

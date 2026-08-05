using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Pagos;

public sealed class PagoSearchDto
{
    public int? IdRenta { get; set; }
    public int? IdMetodoPago { get; set; }
    public int? IdEstado { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public string? Search { get; set; }
    public bool IncluirInactivos { get; set; }

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

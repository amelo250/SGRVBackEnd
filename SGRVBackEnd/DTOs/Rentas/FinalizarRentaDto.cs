using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Rentas;

public sealed class FinalizarRentaDto
{
    public DateTimeOffset FechaEntregaReal { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

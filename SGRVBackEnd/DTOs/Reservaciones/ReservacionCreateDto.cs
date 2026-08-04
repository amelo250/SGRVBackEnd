using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Reservaciones;

public class ReservacionCreateDto
{
    [Range(1, int.MaxValue)]
    public int IdVehiculo { get; set; }

    [Range(1, int.MaxValue)]
    public int IdCliente { get; set; }

    public DateTimeOffset FechaInicio { get; set; }

    public DateTimeOffset FechaFin { get; set; }

    [MaxLength(500)]
    public string? Observacion { get; set; }
}

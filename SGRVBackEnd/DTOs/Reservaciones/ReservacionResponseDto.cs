namespace SGRVBackEnd.DTOs.Reservaciones;

public class ReservacionResponseDto
{
    public int IdReservacion { get; set; }
    public int IdVehiculo { get; set; }
    public string VehiculoDescripcion { get; set; } = string.Empty;
    public int IdCliente { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public int IdEstado { get; set; }
    public string EstadoCodigo { get; set; } = string.Empty;
    public string EstadoNombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Observacion { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}

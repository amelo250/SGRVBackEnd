
namespace SGRVBackEnd.DTOs.Pagos;

public sealed class PagoResponseDto
{
    public int IdPago { get; set; }
    public int IdRenta { get; set; }
    public int IdMetodoPago { get; set; }
    public string MetodoPagoNombre { get; set; } = string.Empty;
    public int IdEstado { get; set; }
    public string EstadoCodigo { get; set; } = string.Empty;
    public string EstadoNombre { get; set; } = string.Empty;
    public int IdMoneda { get; set; }
    public string CodigoMoneda { get; set; } = string.Empty;
    public string SimboloMoneda { get; set; } = string.Empty;
    public string ClienteNombre { get; set; } = string.Empty;
    public string VehiculoDescripcion { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public decimal TasaCambioAplicada { get; set; }
    public decimal MontoMonedaLocal { get; set; }
    public DateTime FechaPago { get; set; }
    public string? Referencia { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaRegistro { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}

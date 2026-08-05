namespace SGRVBackEnd.DTOs.Rentas;

public sealed class RentaResponseDto
{
    public int IdRenta { get; set; }
    public int? IdReservacion { get; set; }
    public int IdCliente { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public int IdVehiculo { get; set; }
    public string VehiculoDescripcion { get; set; } = string.Empty;
    public int IdEstado { get; set; }
    public string EstadoCodigo { get; set; } = string.Empty;
    public string EstadoNombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public DateTime? FechaEntregaReal { get; set; }
    public decimal PrecioPorDia { get; set; }
    public int CantidadDias { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Impuestos { get; set; }
    public decimal Descuentos { get; set; }
    public decimal Total { get; set; }
    public decimal Deposito { get; set; }
    public int IdMoneda { get; set; }
    public string MonedaCodigo { get; set; } = string.Empty;
    public string MonedaSimbolo { get; set; } = string.Empty;
    public decimal TasaCambioAplicada { get; set; }
    public decimal TotalMonedaLocal { get; set; }
    public int? IdProveedorVehiculo { get; set; }
    public int? IdAcuerdoVehiculo { get; set; }
    public string? Observaciones { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public int IdUsuarioCreacion { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

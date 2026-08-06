namespace SGRVBackEnd.DTOs.Gastos;

public sealed class GastoResponseDto
{
    public int IdGasto { get; set; }
    public int IdTipoGasto { get; set; }
    public string TipoCodigo { get; set; } = string.Empty;
    public string TipoNombre { get; set; } = string.Empty;
    public int IdMoneda { get; set; }
    public string MonedaCodigo { get; set; } = string.Empty;
    public string MonedaSimbolo { get; set; } = string.Empty;
    public int? IdVehiculo { get; set; }
    public string? VehiculoDescripcion { get; set; }
    public DateTime Fecha { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public string? NumeroComprobante { get; set; }
    public string? Proveedor { get; set; }
    public decimal Monto { get; set; }
    public decimal TasaCambioAplicada { get; set; }
    public decimal MontoMonedaLocal { get; set; }
    public int? Kilometraje { get; set; }
    public string? Taller { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

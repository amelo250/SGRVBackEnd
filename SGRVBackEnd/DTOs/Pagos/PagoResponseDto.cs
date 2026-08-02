public sealed class PagoResponseDto
{
    public int IdPago { get; set; }
    public int IdRenta { get; set; }
    public int IdMetodoPago { get; set; }
    public int IdEstado { get; set; }
    public int IdMoneda { get; set; }
    public string CodigoMoneda { get; set; } = string.Empty;
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
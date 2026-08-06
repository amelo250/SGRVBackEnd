namespace SGRVBackEnd.DTOs.Gastos;

public class GastoCreateDto
{
    public int IdTipoGasto { get; set; }
    public int IdMoneda { get; set; }
    public int? IdVehiculo { get; set; }
    public DateTime Fecha { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public string? NumeroComprobante { get; set; }
    public string? Proveedor { get; set; }
    public decimal Monto { get; set; }
    public decimal TasaCambioAplicada { get; set; }
    public int? Kilometraje { get; set; }
    public string? Taller { get; set; }
    public string? Observaciones { get; set; }
}

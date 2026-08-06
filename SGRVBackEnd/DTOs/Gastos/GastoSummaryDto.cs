namespace SGRVBackEnd.DTOs.Gastos;

public sealed class GastoSummaryDto
{
    public decimal TotalMonedaLocal { get; set; }
    public decimal TotalMantenimiento { get; set; }
    public decimal TotalOperativo { get; set; }
    public int Cantidad { get; set; }
}

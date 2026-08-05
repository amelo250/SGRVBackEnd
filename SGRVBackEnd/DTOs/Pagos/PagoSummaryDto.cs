namespace SGRVBackEnd.DTOs.Pagos;

public sealed class PagoSummaryDto
{
    public int IdRenta { get; set; }
    public decimal TotalRentaMonedaLocal { get; set; }
    public decimal TotalPagadoMonedaLocal { get; set; }
    public decimal BalancePendienteMonedaLocal { get; set; }
    public bool TieneSobrepago { get; set; }
    public decimal MontoSobrepagoMonedaLocal { get; set; }
}

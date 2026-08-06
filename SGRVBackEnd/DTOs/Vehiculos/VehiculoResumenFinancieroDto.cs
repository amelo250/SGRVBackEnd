namespace SGRVBackEnd.DTOs.Vehiculos;

public sealed class VehiculoResumenFinancieroDto
{
    public int IdVehiculo { get; set; }
    public decimal IngresosMonedaLocal { get; set; }
    public decimal GastosMonedaLocal { get; set; }
    public decimal ResultadoNeto { get; set; }
    public int CantidadRentas { get; set; }
    public decimal IngresoPromedioPorRenta { get; set; }
    public DateTime? UltimaRenta { get; set; }
    public DateTime? UltimoGasto { get; set; }
}

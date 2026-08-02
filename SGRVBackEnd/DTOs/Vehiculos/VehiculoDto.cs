using SGRVBackEnd.Enums;
namespace SGRVBackEnd.DTOs.Vehiculos;

public class VehiculoDto
{
    public int IdVehiculo { get; set; }
    public int IdEstado { get; set; }
    public int IdCombustible { get; set; }
    public int IdTransmision { get; set; }
    public int IdTipo { get; set; }
    public TipoPropiedadVehiculo TipoPropiedad { get; set; }
    public int? IdProveedorVehiculo { get; set; }
    public int IdMonedaTarifa { get; set; }
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int Anio { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string? VIN { get; set; }
    public string? Color { get; set; }
    public int Kilometraje { get; set; }
    public decimal PrecioPorDia { get; set; }
    public decimal DepositoCombustible { get; set; }
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}
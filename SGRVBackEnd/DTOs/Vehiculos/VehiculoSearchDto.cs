namespace SGRVBackEnd.DTOs.Vehiculos;

public sealed class VehiculoSearchDto
{
    public int? IdTipo { get; set; }
    public int? IdCombustible { get; set; }
    public string? Marca { get; set; }
    public bool IncluirInactivos { get; set; }
}

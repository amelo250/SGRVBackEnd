using SGRVBackEnd.DTOs.Vehiculos;

namespace SGRVBackEnd.Services.Vehiculos;

public interface IVehiculoListadoService
{
    Task<IReadOnlyList<VehiculoDto>> GetAllAsync(
        int idEmpresa,
        VehiculoSearchDto search,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetMarcasAsync(
        int idEmpresa,
        CancellationToken cancellationToken);
}

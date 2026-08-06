using SGRVBackEnd.DTOs.Vehiculos;

namespace SGRVBackEnd.Services.Vehiculos;

public interface IVehiculoResumenService
{
    Task<VehiculoResumenFinancieroDto?> GetAsync(
        int idVehiculo,
        int idEmpresa,
        CancellationToken cancellationToken);
}

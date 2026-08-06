using SGRVBackEnd.DTOs.Accesorios;

namespace SGRVBackEnd.Services.Accesorios;

public interface IAccesorioService
{
    Task<IReadOnlyList<AccesorioResponseDto>> GetCatalogAsync(int idEmpresa, bool incluirInactivos, CancellationToken ct);
    Task<AccesorioResponseDto> CreateAsync(int idEmpresa, AccesorioCreateDto dto, CancellationToken ct);
    Task<AccesorioResponseDto?> UpdateAsync(int id, int idEmpresa, AccesorioUpdateDto dto, CancellationToken ct);
    Task<bool> SetCatalogActiveAsync(int id, int idEmpresa, bool active, CancellationToken ct);
    Task<IReadOnlyList<VehiculoAccesorioResponseDto>> GetVehicleAsync(int idVehiculo, int idEmpresa, CancellationToken ct);
    Task<VehiculoAccesorioResponseDto> AssignAsync(int idVehiculo, int idEmpresa, int idUsuario, VehiculoAccesorioCreateDto dto, CancellationToken ct);
    Task<bool> RemoveAsync(int idVehiculo, int idAccesorio, int idEmpresa, int idUsuario, CancellationToken ct);
}

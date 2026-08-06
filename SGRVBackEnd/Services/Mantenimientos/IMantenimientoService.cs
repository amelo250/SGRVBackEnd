using SGRVBackEnd.DTOs.Mantenimientos;

namespace SGRVBackEnd.Services.Mantenimientos;

public interface IMantenimientoService
{
    Task<(IReadOnlyList<MantenimientoResponseDto> Items, int Total)> GetAllAsync(
        int idEmpresa, MantenimientoSearchDto search, CancellationToken cancellationToken);
    Task<MantenimientoResponseDto?> GetByIdAsync(
        int id, int idEmpresa, CancellationToken cancellationToken);
    Task<MantenimientoResponseDto> CreateAsync(
        int idEmpresa, MantenimientoCreateDto request, CancellationToken cancellationToken);
    Task<MantenimientoResponseDto?> UpdateAsync(
        int id, int idEmpresa, MantenimientoUpdateDto request,
        CancellationToken cancellationToken);
    Task<MantenimientoResumenDto> GetSummaryAsync(
        int idEmpresa, int intervaloDias, int intervaloKilometros,
        int diasAlerta, int kilometrosAlerta, CancellationToken cancellationToken);
    Task<IReadOnlyList<MantenimientoCatalogoDto>> GetTypesAsync(
        CancellationToken cancellationToken);
    Task<IReadOnlyList<MantenimientoCatalogoDto>> GetVehiclesAsync(
        int idEmpresa, CancellationToken cancellationToken);
}

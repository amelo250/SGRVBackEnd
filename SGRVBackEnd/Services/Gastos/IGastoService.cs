using SGRVBackEnd.DTOs.Gastos;

namespace SGRVBackEnd.Services.Gastos;

public interface IGastoService
{
    Task<(IReadOnlyList<GastoResponseDto> Items, int Total)> GetAllAsync(
        int idEmpresa, GastoSearchDto search, CancellationToken cancellationToken);
    Task<GastoResponseDto?> GetByIdAsync(int id, int idEmpresa, CancellationToken cancellationToken);
    Task<GastoResponseDto> CreateAsync(
        int idEmpresa, int idUsuario, GastoCreateDto request, CancellationToken cancellationToken);
    Task<GastoResponseDto?> UpdateAsync(
        int id, int idEmpresa, int idUsuario, GastoUpdateDto request, CancellationToken cancellationToken);
    Task<bool> SetActiveAsync(
        int id, int idEmpresa, int idUsuario, bool active, CancellationToken cancellationToken);
    Task<GastoSummaryDto> GetSummaryAsync(
        int idEmpresa, DateTime? desde, DateTime? hasta, CancellationToken cancellationToken);
}

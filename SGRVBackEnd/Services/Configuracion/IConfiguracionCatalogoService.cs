using SGRVBackEnd.DTOs.Configuracion;

namespace SGRVBackEnd.Services.Configuracion;

public interface IConfiguracionCatalogoService
{
    IReadOnlyList<CatalogoConfiguracionDefinitionDto> GetDefinitions();
    bool TryGetDefinition(string key, out CatalogoConfiguracionDefinitionDto definition);
    Task<IReadOnlyList<CatalogoConfiguracionResponseDto>> GetAllAsync(
        string key, bool incluirInactivos, CancellationToken cancellationToken);
    Task<CatalogoConfiguracionResponseDto?> GetByIdAsync(
        string key, int id, CancellationToken cancellationToken);
    Task<CatalogoConfiguracionResponseDto> CreateAsync(
        string key, CatalogoConfiguracionCreateDto dto, CancellationToken cancellationToken);
    Task<CatalogoConfiguracionResponseDto?> UpdateAsync(
        string key, int id, CatalogoConfiguracionUpdateDto dto,
        CancellationToken cancellationToken);
    Task<bool> SetActiveAsync(
        string key, int id, bool activo, CancellationToken cancellationToken);
}

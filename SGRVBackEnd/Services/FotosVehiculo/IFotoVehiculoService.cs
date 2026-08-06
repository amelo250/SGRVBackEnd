using SGRVBackEnd.DTOs.FotosVehiculo;

namespace SGRVBackEnd.Services.FotosVehiculo;

public interface IFotoVehiculoService
{
    Task<IReadOnlyList<FotoVehiculoResponseDto>> GetAsync(int idVehiculo, int idEmpresa, CancellationToken ct);
    Task<FotoVehiculoResponseDto> UploadAsync(int idVehiculo, int idEmpresa, int idUsuario, FotoVehiculoUploadDto dto, CancellationToken ct);
    Task<FotoVehiculoResponseDto?> UpdateAsync(int idVehiculo, int idFoto, int idEmpresa, int idUsuario, FotoVehiculoUpdateDto dto, CancellationToken ct);
    Task<FotoVehiculoResponseDto?> SetPrincipalAsync(int idVehiculo, int idFoto, int idEmpresa, int idUsuario, CancellationToken ct);
    Task<bool> DeleteAsync(int idVehiculo, int idFoto, int idEmpresa, int idUsuario, CancellationToken ct);
    Task<(Stream Stream, string ContentType, string Name)?> OpenContentAsync(int idVehiculo, int idFoto, int idEmpresa, CancellationToken ct);
}

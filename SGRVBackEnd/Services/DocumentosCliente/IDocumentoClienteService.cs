using SGRVBackEnd.DTOs.DocumentosCliente;

namespace SGRVBackEnd.Services.DocumentosCliente;

public interface IDocumentoClienteService
{
    Task<IReadOnlyList<DocumentoClienteResponseDto>> GetAllAsync(
        int idCliente, int idEmpresa, CancellationToken cancellationToken);
    Task<IReadOnlyList<TipoDocumentoClienteDto>> GetTypesAsync(
        int idCliente, int idEmpresa, CancellationToken cancellationToken);
    Task<DocumentoClienteResponseDto> UploadAsync(
        int idCliente, int idEmpresa, DocumentoClienteUploadDto request,
        CancellationToken cancellationToken);
    Task<(Stream Stream, string ContentType)?> OpenAsync(
        int idCliente, int idDocumento, int idEmpresa,
        CancellationToken cancellationToken);
    Task<bool> DeleteAsync(
        int idCliente, int idDocumento, int idEmpresa,
        CancellationToken cancellationToken);
}

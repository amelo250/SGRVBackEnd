using Microsoft.AspNetCore.Http;

namespace SGRVBackEnd.DTOs.DocumentosCliente;

public sealed class DocumentoClienteUploadDto
{
    public int IdTipoDocumento { get; set; }
    public IFormFile? Archivo { get; set; }
}

public sealed class DocumentoClienteResponseDto
{
    public int IdDocumentoCliente { get; set; }
    public int IdCliente { get; set; }
    public int IdTipoDocumento { get; set; }
    public string TipoDocumentoNombre { get; set; } = string.Empty;
    public string UrlDocumento { get; set; } = string.Empty;
    public string? TipoContenido { get; set; }
    public DateTime FechaSubida { get; set; }
}

public sealed class TipoDocumentoClienteDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}

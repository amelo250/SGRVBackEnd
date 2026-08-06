using SGRVBackEnd.DTOs.DocumentosCliente;

namespace SGRVBackEnd.Validators;

public static class DocumentoClienteValidator
{
    private static readonly HashSet<string> AllowedTypes = new(
        ["image/jpeg", "image/png", "image/webp", "application/pdf"],
        StringComparer.OrdinalIgnoreCase);

    public static string? Validate(DocumentoClienteUploadDto dto)
    {
        if (dto.IdTipoDocumento <= 0) return "Debe indicar el tipo de documento.";
        if (dto.Archivo is null || dto.Archivo.Length == 0)
            return "Debe seleccionar un archivo.";
        if (dto.Archivo.Length > 10 * 1024 * 1024)
            return "El archivo no puede exceder 10 MB.";
        return AllowedTypes.Contains(dto.Archivo.ContentType)
            ? null
            : "Solo se permiten imágenes JPEG, PNG, WebP o archivos PDF.";
    }
}

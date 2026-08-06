using SGRVBackEnd.DTOs.Accesorios;
using SGRVBackEnd.DTOs.FotosVehiculo;

namespace SGRVBackEnd.Validators;

public static class VehiculoMediaValidator
{
    public static string? Validate(AccesorioCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Codigo) || dto.Codigo.Trim().Length > 50)
            return "El código es obligatorio y admite hasta 50 caracteres.";
        if (string.IsNullOrWhiteSpace(dto.Nombre) || dto.Nombre.Trim().Length > 100)
            return "El nombre es obligatorio y admite hasta 100 caracteres.";
        if (dto.Descripcion?.Trim().Length > 250 || dto.Icono?.Trim().Length > 100)
            return "La descripción o el icono exceden la longitud permitida.";
        return null;
    }

    public static string? Validate(FotoVehiculoUploadDto dto)
    {
        if (dto.Archivo is null || dto.Archivo.Length == 0) return "Debe seleccionar una imagen.";
        if (dto.Archivo.Length > 10 * 1024 * 1024) return "La imagen no puede exceder 10 MB.";
        if (dto.Orden < 0) return "El orden no puede ser negativo.";
        if (dto.Titulo?.Trim().Length > 150 || dto.TextoAlternativo?.Trim().Length > 250)
            return "El título o texto alternativo exceden la longitud permitida.";
        string[] allowed = ["image/jpeg", "image/png", "image/webp"];
        return allowed.Contains(dto.Archivo.ContentType, StringComparer.OrdinalIgnoreCase)
            ? null : "Solo se permiten imágenes JPEG, PNG o WebP.";
    }

    public static bool IsRowVersion(string value)
    {
        try { return Convert.FromBase64String(value).Length == 8; }
        catch { return false; }
    }
}

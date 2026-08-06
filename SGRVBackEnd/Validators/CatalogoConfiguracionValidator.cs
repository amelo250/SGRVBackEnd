using SGRVBackEnd.DTOs.Configuracion;

namespace SGRVBackEnd.Validators;

public static class CatalogoConfiguracionValidator
{
    public static string? Validate(
        CatalogoConfiguracionCreateDto dto,
        bool usaCategoria,
        bool usaSimbolo)
    {
        if (string.IsNullOrWhiteSpace(dto.Codigo) || dto.Codigo.Trim().Length > 50)
            return "El código es obligatorio y admite hasta 50 caracteres.";

        if (string.IsNullOrWhiteSpace(dto.Nombre) || dto.Nombre.Trim().Length > 100)
            return "El nombre es obligatorio y admite hasta 100 caracteres.";

        if (usaCategoria &&
            (string.IsNullOrWhiteSpace(dto.Categoria) || dto.Categoria.Trim().Length > 50))
            return "La categoría es obligatoria y admite hasta 50 caracteres.";

        if (usaSimbolo &&
            (string.IsNullOrWhiteSpace(dto.Simbolo) || dto.Simbolo.Trim().Length > 10))
            return "El símbolo es obligatorio y admite hasta 10 caracteres.";

        return null;
    }
}

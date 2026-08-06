namespace SGRVBackEnd.DTOs.Configuracion;

public class CatalogoConfiguracionCreateDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public string? Simbolo { get; set; }
}

public sealed class CatalogoConfiguracionUpdateDto : CatalogoConfiguracionCreateDto;

public sealed class CatalogoConfiguracionResponseDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public string? Simbolo { get; set; }
    public bool Activo { get; set; }
}

public sealed class CatalogoConfiguracionDefinitionDto
{
    public string Key { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool UsaCategoria { get; set; }
    public bool UsaSimbolo { get; set; }
    public bool EsGlobal { get; set; } = true;
}

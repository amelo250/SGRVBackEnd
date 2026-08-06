using Microsoft.AspNetCore.Http;

namespace SGRVBackEnd.DTOs.FotosVehiculo;

public sealed class FotoVehiculoUploadDto
{
    public IFormFile? Archivo { get; set; }
    public string? Titulo { get; set; }
    public string? TextoAlternativo { get; set; }
    public bool EsPrincipal { get; set; }
    public int Orden { get; set; }
}

public sealed class FotoVehiculoUpdateDto
{
    public string? Titulo { get; set; }
    public string? TextoAlternativo { get; set; }
    public int Orden { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class FotoVehiculoResponseDto
{
    public int IdFoto { get; set; }
    public int IdVehiculo { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? NombreArchivo { get; set; }
    public string? TipoContenido { get; set; }
    public long? TamanioBytes { get; set; }
    public string? Titulo { get; set; }
    public string? TextoAlternativo { get; set; }
    public bool EsPrincipal { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaSubida { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

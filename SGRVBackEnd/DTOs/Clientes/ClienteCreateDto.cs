using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Clientes;

public class ClienteCreateDto
{
    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Apellido { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string CedulaPasaporte { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Telefono { get; set; }

    public DateTime FechaNacimiento { get; set; }

    [MaxLength(100)]
    public string? Nacionalidad { get; set; }

    [EmailAddress]
    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(250)]
    public string? Direccion { get; set; }

    [Required]
    [MaxLength(50)]
    public string LicenciaConducir { get; set; } = string.Empty;

    [Required]
    public DateTime? FechaExpLicencia { get; set; }

    [Required]
    public DateTime? FechaVencLicencia { get; set; }
}

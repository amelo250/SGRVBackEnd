using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Pagos;

public  class PagoCreateDto
{
    [Range(1, int.MaxValue)]
    public int IdRenta { get; set; }

    [Range(1, int.MaxValue)]
    public int IdMetodoPago { get; set; }

    [Range(1, int.MaxValue)]
    public int IdMoneda { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal Monto { get; set; }

    [Range(typeof(decimal), "0.000001", "999999999999")]
    public decimal TasaCambioAplicada { get; set; } = 1m;

    public DateTime? FechaPago { get; set; }

    [MaxLength(100)]
    public string? Referencia { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }
}
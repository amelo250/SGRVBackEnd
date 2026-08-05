using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Rentas;

public sealed class ConvertirReservacionRentaDto
{
    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal Impuestos { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal Descuentos { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal Deposito { get; set; }

    [Range(typeof(decimal), "0", "999999999999.999999")]
    public decimal TasaCambioAplicada { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }
}

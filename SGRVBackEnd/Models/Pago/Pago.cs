using System.ComponentModel.DataAnnotations;
using SGRVBackEnd.Models.Catalogo;
using SGRVBackEnd.Models.EmpresasScopedEntity;

namespace SGRVBackEnd.Models.Pago;

public class Pago : EmpresaScopedEntity
{
    [Key]
    public int IdPago { get; set; }

    public int IdRenta { get; set; }
    public int IdMetodoPago { get; set; }
    public int IdEstado { get; set; }
    public int IdMoneda { get; set; }

    public decimal Monto { get; set; }
    public decimal TasaCambioAplicada { get; set; } = 1m;
    public decimal MontoMonedaLocal { get; set; }

    public DateTime FechaPago { get; set; }

    [MaxLength(100)]
    public string? Referencia { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    public Renta.Renta Renta { get; set; } = null!;
    public MetodoPago MetodoPago { get; set; } = null!;
    public Estado Estado { get; set; } = null!;
    public Moneda Moneda { get; set; } = null!;
}
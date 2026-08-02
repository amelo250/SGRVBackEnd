
using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Pagos;

public sealed class PagoUpdateDto : PagoCreateDto
{
    [Range(1, int.MaxValue)]
    public int IdEstado { get; set; }

    public bool Activo { get; set; }
}
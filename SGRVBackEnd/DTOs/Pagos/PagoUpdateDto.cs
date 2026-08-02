using SGRVBackEnd.DTOs.Pagos;
using System.ComponentModel.DataAnnotations;

public sealed class PagoUpdateDto : PagoCreateDto
{
    [Range(1, int.MaxValue)]
    public int IdEstado { get; set; }

    public bool Activo { get; set; }
}
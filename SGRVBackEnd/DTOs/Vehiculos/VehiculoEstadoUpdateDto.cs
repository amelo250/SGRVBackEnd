using System.ComponentModel.DataAnnotations;
namespace SGRVBackEnd.DTOs.Vehiculos;

public sealed class VehiculoEstadoUpdateDto
{
    [Range(1, int.MaxValue)] public int IdEstado { get; set; }
}

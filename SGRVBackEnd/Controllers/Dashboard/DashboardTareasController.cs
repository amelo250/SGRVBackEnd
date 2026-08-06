using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGRVBackEnd.DTOs.Dashboard;
using SGRVBackEnd.Services.Dashboard;
using SGRVBackEnd.Shared;

namespace SGRVBackEnd.Controllers.Dashboard;

[ApiController]
[Authorize]
[Route("api/dashboard/tareas")]
public sealed class DashboardTareasController : BaseApiController
{
    private readonly IDashboardTareaService _service;
    public DashboardTareasController(IDashboardTareaService service) => _service = service;

    /// <summary>Obtiene las tareas de hoy y las próximas operaciones.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<DashboardTareasResponseDto>>> Get(
        [FromQuery] DateTime? fecha = null,
        [FromQuery] int dias = 7,
        [FromQuery] int limite = 8,
        CancellationToken cancellationToken = default)
    {
        if (dias is < 1 or > 31)
            return BadRequest(Failure("dias debe estar entre 1 y 31."));
        if (limite is < 1 or > 50)
            return BadRequest(Failure("limite debe estar entre 1 y 50."));

        var result = await _service.GetAsync(
            GetEmpresaId(), fecha ?? DateTime.Now, dias, limite, cancellationToken);
        return Ok(new ApiResponse<DashboardTareasResponseDto>
        {
            Success = true,
            Message = "Tareas del Dashboard obtenidas correctamente.",
            Data = result
        });
    }

    private static ApiResponse<DashboardTareasResponseDto> Failure(string message) =>
        new() { Success = false, Message = message };
}

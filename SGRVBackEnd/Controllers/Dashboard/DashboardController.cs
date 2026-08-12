using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGRVBackEnd.DTOs.Dashboard;
using SGRVBackEnd.Services.Dashboard;
using SGRVBackEnd.Shared;

namespace SGRVBackEnd.Controllers.Dashboard;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController : BaseApiController
{
    private readonly IDashboardTareaService _service;

    public DashboardController(IDashboardTareaService service) => _service = service;

    /// <summary>Obtiene indicadores y actividad operativa de la empresa autenticada.</summary>
    [HttpGet("resumen")]
    [ProducesResponseType(typeof(ApiResponse<DashboardResumenResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DashboardResumenResponseDto>>> GetResumen(
        [FromQuery] DateTime? fecha = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetResumenAsync(
            GetEmpresaId(), fecha ?? DateTime.Now, cancellationToken);

        return Ok(new ApiResponse<DashboardResumenResponseDto>
        {
            Success = true,
            Message = "Resumen del Dashboard obtenido correctamente.",
            Data = result
        });
    }
}

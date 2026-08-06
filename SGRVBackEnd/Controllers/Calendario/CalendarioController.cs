using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGRVBackEnd.DTOs.Calendario;
using SGRVBackEnd.Services.Calendario;
using SGRVBackEnd.Shared;

namespace SGRVBackEnd.Controllers.Calendario;

[ApiController]
[Authorize]
[Route("api/calendario")]
public sealed class CalendarioController : BaseApiController
{
    private const int MaximoDiasConsulta = 366;
    private readonly ICalendarioService _service;

    public CalendarioController(ICalendarioService service)
    {
        _service = service;
    }

    /// <summary>
    /// Obtiene reservaciones y rentas que se cruzan con el periodo solicitado.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<CalendarioResumenResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CalendarioResumenResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CalendarioResumenResponseDto>>> Get(
        [FromQuery] CalendarioSearchDto search,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(search);
        if (validation is not null)
        {
            return BadRequest(new ApiResponse<CalendarioResumenResponseDto>
            {
                Success = false,
                Message = validation
            });
        }

        var result = await _service.ObtenerAsync(
            GetEmpresaId(), search, cancellationToken);

        return Ok(new ApiResponse<CalendarioResumenResponseDto>
        {
            Success = true,
            Message = "Calendario obtenido correctamente.",
            Data = result
        });
    }

    private static string? Validate(CalendarioSearchDto search)
    {
        if (search.FechaDesde == default || search.FechaHasta == default)
            return "Debe indicar fechaDesde y fechaHasta.";
        if (search.FechaHasta <= search.FechaDesde)
            return "fechaHasta debe ser posterior a fechaDesde.";
        if ((search.FechaHasta - search.FechaDesde).TotalDays > MaximoDiasConsulta)
            return $"El periodo máximo de consulta es de {MaximoDiasConsulta} días.";
        if (search.IdVehiculo.HasValue && search.IdVehiculo <= 0)
            return "idVehiculo debe ser mayor que cero.";

        var tipo = search.Tipo?.Trim().ToUpperInvariant();
        return tipo is null or "" or "RESERVACION" or "RENTA"
            ? null
            : "tipo sólo admite RESERVACION o RENTA.";
    }
}

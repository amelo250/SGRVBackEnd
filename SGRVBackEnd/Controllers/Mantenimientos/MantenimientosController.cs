using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGRVBackEnd.DTOs.Mantenimientos;
using SGRVBackEnd.Helpers;
using SGRVBackEnd.Services.Mantenimientos;
using SGRVBackEnd.Shared;
using SGRVBackEnd.Validators;

namespace SGRVBackEnd.Controllers.Mantenimientos;

[ApiController]
[Authorize]
[Route("api/mantenimientos")]
public sealed class MantenimientosController : BaseApiController
{
    private readonly IMantenimientoService _service;
    public MantenimientosController(IMantenimientoService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<MantenimientoResponseDto>>>> GetAll(
        [FromQuery] MantenimientoSearchDto search, CancellationToken cancellationToken)
    {
        var error = MantenimientoValidator.ValidateSearch(search);
        if (error is not null) return BadRequest(Failure<IEnumerable<MantenimientoResponseDto>>(error));
        var result = await _service.GetAllAsync(GetEmpresaId(), search, cancellationToken);
        Response.Headers.Append("X-Total-Count", result.Total.ToString());
        Response.Headers.Append("X-Page-Number", search.PageNumber.ToString());
        Response.Headers.Append("X-Page-Size", search.PageSize.ToString());
        return Ok(Success<IEnumerable<MantenimientoResponseDto>>(result.Items, "Mantenimientos obtenidos correctamente."));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<MantenimientoResponseDto>>> GetById(
        int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, GetEmpresaId(), cancellationToken);
        return item is null ? NotFound(Failure<MantenimientoResponseDto>("No se encontró el mantenimiento."))
            : Ok(Success(item, "Mantenimiento obtenido correctamente."));
    }

    [HttpPost]
    [Authorize(Roles = ApplicationRoles.ReservationManagers)]
    public async Task<ActionResult<ApiResponse<MantenimientoResponseDto>>> Create(
        [FromBody] MantenimientoCreateDto request, CancellationToken cancellationToken)
    {
        var error = MantenimientoValidator.Validate(request);
        if (error is not null) return BadRequest(Failure<MantenimientoResponseDto>(error));
        try
        {
            var item = await _service.CreateAsync(GetEmpresaId(), request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = item.IdMantenimiento },
                Success(item, "Mantenimiento registrado correctamente."));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(Failure<MantenimientoResponseDto>(exception.Message));
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = ApplicationRoles.ReservationManagers)]
    public async Task<ActionResult<ApiResponse<MantenimientoResponseDto>>> Update(
        int id, [FromBody] MantenimientoUpdateDto request, CancellationToken cancellationToken)
    {
        var error = MantenimientoValidator.Validate(request);
        if (error is not null) return BadRequest(Failure<MantenimientoResponseDto>(error));
        try
        {
            var item = await _service.UpdateAsync(id, GetEmpresaId(), request, cancellationToken);
            return item is null ? NotFound(Failure<MantenimientoResponseDto>("No se encontró el mantenimiento."))
                : Ok(Success(item, "Mantenimiento actualizado correctamente."));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(Failure<MantenimientoResponseDto>(exception.Message));
        }
    }

    [HttpGet("resumen")]
    public async Task<ActionResult<ApiResponse<MantenimientoResumenDto>>> Summary(
        [FromQuery] int intervaloDias = 180,
        [FromQuery] int intervaloKilometros = 5000,
        [FromQuery] int diasAlerta = 30,
        [FromQuery] int kilometrosAlerta = 1000,
        CancellationToken cancellationToken = default)
    {
        if (intervaloDias is < 1 or > 3650 || intervaloKilometros is < 1 or > 500000 ||
            diasAlerta is < 0 or > 3650 || kilometrosAlerta is < 0 or > 500000)
            return BadRequest(Failure<MantenimientoResumenDto>("Los umbrales de alerta no son válidos."));
        var result = await _service.GetSummaryAsync(GetEmpresaId(), intervaloDias,
            intervaloKilometros, diasAlerta, kilometrosAlerta, cancellationToken);
        return Ok(Success(result, "Resumen de mantenimiento obtenido correctamente."));
    }

    [HttpGet("tipos")]
    public async Task<ActionResult<ApiResponse<IEnumerable<MantenimientoCatalogoDto>>>> Types(
        CancellationToken cancellationToken) =>
        Ok(Success<IEnumerable<MantenimientoCatalogoDto>>(
            await _service.GetTypesAsync(cancellationToken), "Tipos obtenidos correctamente."));

    [HttpGet("vehiculos")]
    public async Task<ActionResult<ApiResponse<IEnumerable<MantenimientoCatalogoDto>>>> Vehicles(
        CancellationToken cancellationToken) =>
        Ok(Success<IEnumerable<MantenimientoCatalogoDto>>(
            await _service.GetVehiclesAsync(GetEmpresaId(), cancellationToken), "Vehículos obtenidos correctamente."));

    private static ApiResponse<T> Success<T>(T data, string message) =>
        new() { Success = true, Message = message, Data = data };
    private static ApiResponse<T> Failure<T>(string message) =>
        new() { Success = false, Message = message };
}

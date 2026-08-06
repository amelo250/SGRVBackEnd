using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGRVBackEnd.DTOs.Gastos;
using SGRVBackEnd.Services.Gastos;
using SGRVBackEnd.Shared;
using SGRVBackEnd.Validators;

namespace SGRVBackEnd.Controllers.Gastos;

[ApiController]
[Authorize]
[Route("api/gastos")]
public sealed class GastosController : BaseApiController
{
    private const string ManagerRoles = "ADMIN,SUPADMIN,Admin,SuperUsuario";
    private readonly IGastoService _service;

    public GastosController(IGastoService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<GastoResponseDto>>>> GetAll(
        [FromQuery] GastoSearchDto search, CancellationToken cancellationToken)
    {
        var result = await _service.GetAllAsync(GetEmpresaId(), search, cancellationToken);
        Response.Headers.Append("X-Total-Count", result.Total.ToString());
        Response.Headers.Append("X-Page-Number", search.PageNumber.ToString());
        Response.Headers.Append("X-Page-Size", search.PageSize.ToString());
        return Ok(Success<IEnumerable<GastoResponseDto>>(result.Items, "Gastos obtenidos correctamente."));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<GastoResponseDto>>> GetById(
        int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, GetEmpresaId(), cancellationToken);
        return item is null
            ? NotFound(Failure<GastoResponseDto>("No se encontró el gasto solicitado."))
            : Ok(Success(item, "Gasto obtenido correctamente."));
    }

    [HttpGet("resumen")]
    public async Task<ActionResult<ApiResponse<GastoSummaryDto>>> GetSummary(
        [FromQuery] DateTime? fechaDesde, [FromQuery] DateTime? fechaHasta,
        CancellationToken cancellationToken)
    {
        if (fechaDesde.HasValue && fechaHasta.HasValue && fechaDesde > fechaHasta)
            return BadRequest(Failure<GastoSummaryDto>("El rango de fechas no es válido."));
        var summary = await _service.GetSummaryAsync(
            GetEmpresaId(), fechaDesde, fechaHasta, cancellationToken);
        return Ok(Success(summary, "Resumen de gastos obtenido correctamente."));
    }

    [HttpPost]
    [Authorize(Roles = ManagerRoles)]
    public async Task<ActionResult<ApiResponse<GastoResponseDto>>> Create(
        [FromBody] GastoCreateDto request, CancellationToken cancellationToken)
    {
        var error = GastoValidator.Validate(request);
        if (error is not null) return BadRequest(Failure<GastoResponseDto>(error));
        try
        {
            var item = await _service.CreateAsync(
                GetEmpresaId(), GetUsuarioId(), request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = item.IdGasto },
                Success(item, "Gasto creado correctamente."));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(Failure<GastoResponseDto>(exception.Message));
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = ManagerRoles)]
    public async Task<ActionResult<ApiResponse<GastoResponseDto>>> Update(
        int id, [FromBody] GastoUpdateDto request, CancellationToken cancellationToken)
    {
        var error = GastoValidator.Validate(request);
        if (error is not null) return BadRequest(Failure<GastoResponseDto>(error));
        if (!GastoValidator.TryDecodeRowVersion(request.RowVersion, out _))
            return BadRequest(Failure<GastoResponseDto>("RowVersion no es válido."));
        try
        {
            var item = await _service.UpdateAsync(
                id, GetEmpresaId(), GetUsuarioId(), request, cancellationToken);
            return item is null
                ? NotFound(Failure<GastoResponseDto>("No se encontró el gasto solicitado."))
                : Ok(Success(item, "Gasto actualizado correctamente."));
        }
        catch (DBConcurrencyException exception)
        {
            return Conflict(Failure<GastoResponseDto>(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(Failure<GastoResponseDto>(exception.Message));
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = ManagerRoles)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int id, CancellationToken cancellationToken)
    {
        var changed = await _service.SetActiveAsync(
            id, GetEmpresaId(), GetUsuarioId(), false, cancellationToken);
        return changed
            ? Ok(Success<object>(new { IdGasto = id, Activo = false }, "Gasto eliminado correctamente."))
            : NotFound(Failure<object>("No se encontró el gasto solicitado."));
    }

    [HttpPatch("{id:int}/restaurar")]
    [Authorize(Roles = ManagerRoles)]
    public async Task<ActionResult<ApiResponse<GastoResponseDto>>> Restore(
        int id, CancellationToken cancellationToken)
    {
        var idEmpresa = GetEmpresaId();
        var changed = await _service.SetActiveAsync(
            id, idEmpresa, GetUsuarioId(), true, cancellationToken);
        if (!changed) return NotFound(Failure<GastoResponseDto>("No se encontró el gasto solicitado."));
        var item = await _service.GetByIdAsync(id, idEmpresa, cancellationToken);
        return Ok(Success(item!, "Gasto restaurado correctamente."));
    }

    private static ApiResponse<T> Success<T>(T data, string message) =>
        new() { Success = true, Message = message, Data = data };

    private static ApiResponse<T> Failure<T>(string message) =>
        new() { Success = false, Message = message };
}

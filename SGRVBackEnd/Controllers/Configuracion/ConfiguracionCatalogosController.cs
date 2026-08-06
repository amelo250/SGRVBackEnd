using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGRVBackEnd.DTOs.Configuracion;
using SGRVBackEnd.Services.Configuracion;
using SGRVBackEnd.Shared;
using SGRVBackEnd.Validators;

namespace SGRVBackEnd.Controllers.Configuracion;

[ApiController]
[Authorize]
[Route("api/configuracion/catalogos")]
public sealed class ConfiguracionCatalogosController : ControllerBase
{
    private const string PlatformAdminRoles = "SUPADMIN,SuperUsuario";
    private readonly IConfiguracionCatalogoService _service;

    public ConfiguracionCatalogosController(IConfiguracionCatalogoService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<ApiResponse<IReadOnlyList<CatalogoConfiguracionDefinitionDto>>> GetDefinitions()
    {
        return Ok(Success(_service.GetDefinitions(), "Catálogos configurables obtenidos correctamente."));
    }

    [HttpGet("{catalogo}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CatalogoConfiguracionResponseDto>>>> GetAll(
        string catalogo,
        [FromQuery] bool incluirInactivos,
        CancellationToken cancellationToken)
    {
        if (!_service.TryGetDefinition(catalogo, out _))
            return NotFound(Failure<IReadOnlyList<CatalogoConfiguracionResponseDto>>(
                "El catálogo solicitado no está habilitado para configuración."));

        var data = await _service.GetAllAsync(catalogo, incluirInactivos, cancellationToken);
        return Ok(Success(data, "Registros obtenidos correctamente."));
    }

    [HttpGet("{catalogo}/{id:int}")]
    public async Task<ActionResult<ApiResponse<CatalogoConfiguracionResponseDto>>> GetById(
        string catalogo,
        int id,
        CancellationToken cancellationToken)
    {
        if (!_service.TryGetDefinition(catalogo, out _))
            return NotFound(Failure<CatalogoConfiguracionResponseDto>(
                "El catálogo solicitado no está habilitado para configuración."));

        var item = await _service.GetByIdAsync(catalogo, id, cancellationToken);
        return item is null
            ? NotFound(Failure<CatalogoConfiguracionResponseDto>("No se encontró el registro solicitado."))
            : Ok(Success(item, "Registro obtenido correctamente."));
    }

    [HttpPost("{catalogo}")]
    [Authorize(Roles = PlatformAdminRoles)]
    public async Task<ActionResult<ApiResponse<CatalogoConfiguracionResponseDto>>> Create(
        string catalogo,
        [FromBody] CatalogoConfiguracionCreateDto request,
        CancellationToken cancellationToken)
    {
        if (!_service.TryGetDefinition(catalogo, out var definition))
            return NotFound(Failure<CatalogoConfiguracionResponseDto>(
                "El catálogo solicitado no está habilitado para configuración."));

        var error = CatalogoConfiguracionValidator.Validate(
            request, definition.UsaCategoria, definition.UsaSimbolo);
        if (error is not null) return BadRequest(Failure<CatalogoConfiguracionResponseDto>(error));

        try
        {
            var item = await _service.CreateAsync(catalogo, request, cancellationToken);
            return CreatedAtAction(
                nameof(GetById),
                new { catalogo, id = item.Id },
                Success(item, "Registro creado correctamente."));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(Failure<CatalogoConfiguracionResponseDto>(exception.Message));
        }
    }

    [HttpPut("{catalogo}/{id:int}")]
    [Authorize(Roles = PlatformAdminRoles)]
    public async Task<ActionResult<ApiResponse<CatalogoConfiguracionResponseDto>>> Update(
        string catalogo,
        int id,
        [FromBody] CatalogoConfiguracionUpdateDto request,
        CancellationToken cancellationToken)
    {
        if (!_service.TryGetDefinition(catalogo, out var definition))
            return NotFound(Failure<CatalogoConfiguracionResponseDto>(
                "El catálogo solicitado no está habilitado para configuración."));

        var error = CatalogoConfiguracionValidator.Validate(
            request, definition.UsaCategoria, definition.UsaSimbolo);
        if (error is not null) return BadRequest(Failure<CatalogoConfiguracionResponseDto>(error));

        try
        {
            var item = await _service.UpdateAsync(catalogo, id, request, cancellationToken);
            return item is null
                ? NotFound(Failure<CatalogoConfiguracionResponseDto>("No se encontró el registro solicitado."))
                : Ok(Success(item, "Registro actualizado correctamente."));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(Failure<CatalogoConfiguracionResponseDto>(exception.Message));
        }
    }

    [HttpDelete("{catalogo}/{id:int}")]
    [Authorize(Roles = PlatformAdminRoles)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        string catalogo,
        int id,
        CancellationToken cancellationToken)
    {
        if (!_service.TryGetDefinition(catalogo, out _))
            return NotFound(Failure<object>("El catálogo solicitado no está habilitado para configuración."));

        var changed = await _service.SetActiveAsync(catalogo, id, false, cancellationToken);
        return changed
            ? Ok(Success<object>(new { Id = id, Activo = false }, "Registro desactivado correctamente."))
            : NotFound(Failure<object>("No se encontró el registro solicitado."));
    }

    [HttpPatch("{catalogo}/{id:int}/restaurar")]
    [Authorize(Roles = PlatformAdminRoles)]
    public async Task<ActionResult<ApiResponse<CatalogoConfiguracionResponseDto>>> Restore(
        string catalogo,
        int id,
        CancellationToken cancellationToken)
    {
        if (!_service.TryGetDefinition(catalogo, out _))
            return NotFound(Failure<CatalogoConfiguracionResponseDto>(
                "El catálogo solicitado no está habilitado para configuración."));

        if (!await _service.SetActiveAsync(catalogo, id, true, cancellationToken))
            return NotFound(Failure<CatalogoConfiguracionResponseDto>("No se encontró el registro solicitado."));

        var item = await _service.GetByIdAsync(catalogo, id, cancellationToken);
        return Ok(Success(item!, "Registro restaurado correctamente."));
    }

    private static ApiResponse<T> Success<T>(T data, string message) =>
        new() { Success = true, Message = message, Data = data };

    private static ApiResponse<T> Failure<T>(string message) =>
        new() { Success = false, Message = message };
}

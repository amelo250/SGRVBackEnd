using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGRVBackEnd.Data;
using SGRVBackEnd.DTOs.ProveedoresVehiculos;
using SGRVBackEnd.Models.ProveedoresVehiculos;
using SGRVBackEnd.Shared;

namespace SGRVBackEnd.Controllers;

[ApiController]
[Authorize]
[Route("api/proveedoresvehiculos")]
public sealed class ProveedoresVehiculosController : BaseApiController
{
    private readonly AppDbContext _context;

    public ProveedoresVehiculosController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProveedorVehiculoResponseDto>>>> GetAll(
        [FromQuery] ProveedorVehiculoSearchDto search,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        var query = _context.Set<ProveedorVehiculo>()
            .AsNoTracking()
            .Where(x => x.IdEmpresa == idEmpresa);

        if (!search.IncluirInactivos)
            query = query.Where(x => x.Activo);

        if (!string.IsNullOrWhiteSpace(search.Search))
        {
            var term = search.Search.Trim();
            query = query.Where(x =>
                x.Nombre.Contains(term) || x.RncCedula.Contains(term));
        }

        var data = await query
            .OrderBy(x => x.Nombre)
            .Skip((search.PageNumber - 1) * search.PageSize)
            .Take(search.PageSize)
            .Select(x => Map(x))
            .ToListAsync(cancellationToken);

        return Ok(Success<IEnumerable<ProveedorVehiculoResponseDto>>(
            data,
            "Proveedores obtenidos correctamente."));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ProveedorVehiculoResponseDto>>> GetById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        var proveedor = await _context.Set<ProveedorVehiculo>()
            .AsNoTracking()
            .Where(x => x.IdProveedorVehiculo == id && x.IdEmpresa == idEmpresa)
            .Select(x => Map(x))
            .FirstOrDefaultAsync(cancellationToken);

        return proveedor is null
            ? NotFound(Failure<ProveedorVehiculoResponseDto>(
                "No se encontró el proveedor solicitado."))
            : Ok(Success(proveedor, "Proveedor obtenido correctamente."));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ADMIN,SUPADMIN,SuperUsuario")]
    public async Task<ActionResult<ApiResponse<ProveedorVehiculoResponseDto>>> Create(
        [FromBody] ProveedorVehiculoCreateDto request,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        var nombre = request.Nombre.Trim();
        var documento = request.RncCedula.Trim().ToUpperInvariant();

        var duplicate = await _context.Set<ProveedorVehiculo>()
            .AsNoTracking()
            .AnyAsync(x =>
                x.IdEmpresa == idEmpresa &&
                x.Activo &&
                (x.Nombre == nombre || x.RncCedula == documento),
                cancellationToken);

        if (duplicate)
            return Conflict(Failure<ProveedorVehiculoResponseDto>(
                "Ya existe un proveedor activo con el mismo nombre o RNC/cédula."));

        var proveedor = new ProveedorVehiculo
        {
            IdEmpresa = idEmpresa,
            Nombre = nombre,
            RncCedula = documento,
            Telefono = Normalize(request.Telefono),
            Direccion = Normalize(request.Direccion),
            Contacto = Normalize(request.Contacto),
            Observacion = Normalize(request.Observacion),
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Set<ProveedorVehiculo>().Add(proveedor);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = proveedor.IdProveedorVehiculo },
            Success(Map(proveedor), "Proveedor creado correctamente."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,ADMIN,SUPADMIN,SuperUsuario")]
    public async Task<ActionResult<ApiResponse<ProveedorVehiculoResponseDto>>> Update(
        int id,
        [FromBody] ProveedorVehiculoUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        var proveedor = await _context.Set<ProveedorVehiculo>()
            .FirstOrDefaultAsync(x =>
                x.IdProveedorVehiculo == id && x.IdEmpresa == idEmpresa,
                cancellationToken);

        if (proveedor is null)
            return NotFound(Failure<ProveedorVehiculoResponseDto>(
                "No se encontró el proveedor solicitado."));

        var nombre = request.Nombre.Trim();
        var documento = request.RncCedula.Trim().ToUpperInvariant();
        var duplicate = await _context.Set<ProveedorVehiculo>()
            .AsNoTracking()
            .AnyAsync(x =>
                x.IdEmpresa == idEmpresa &&
                x.IdProveedorVehiculo != id &&
                x.Activo &&
                (x.Nombre == nombre || x.RncCedula == documento),
                cancellationToken);

        if (duplicate)
            return Conflict(Failure<ProveedorVehiculoResponseDto>(
                "Otro proveedor activo utiliza el mismo nombre o RNC/cédula."));

        proveedor.Nombre = nombre;
        proveedor.RncCedula = documento;
        proveedor.Telefono = Normalize(request.Telefono);
        proveedor.Direccion = Normalize(request.Direccion);
        proveedor.Contacto = Normalize(request.Contacto);
        proveedor.Observacion = Normalize(request.Observacion);
        proveedor.Activo = request.Activo;
        proveedor.FechaActualizacion = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(Success(Map(proveedor), "Proveedor actualizado correctamente."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,ADMIN,SUPADMIN,SuperUsuario")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int id,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        var proveedor = await _context.Set<ProveedorVehiculo>()
            .FirstOrDefaultAsync(x =>
                x.IdProveedorVehiculo == id && x.IdEmpresa == idEmpresa,
                cancellationToken);

        if (proveedor is null)
            return NotFound(Failure<object>("No se encontró el proveedor solicitado."));

        var hasActiveVehicles = await _context.Vehiculos
            .AsNoTracking()
            .AnyAsync(x =>
                x.IdEmpresa == idEmpresa &&
                x.IdProveedorVehiculo == id &&
                x.Activo,
                cancellationToken);

        if (hasActiveVehicles)
            return Conflict(Failure<object>(
                "No se puede desactivar un proveedor con vehículos activos asociados."));

        proveedor.Activo = false;
        proveedor.FechaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(Success<object>(
            new { proveedor.IdProveedorVehiculo, proveedor.Activo },
            "Proveedor desactivado correctamente."));
    }

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;

    private static ApiResponse<T> Success<T>(T data, string message) =>
        new() { Success = true, Message = message, Data = data };

    private static ApiResponse<T> Failure<T>(string message) =>
        new() { Success = false, Message = message };

    private static ProveedorVehiculoResponseDto Map(ProveedorVehiculo entity) => new()
    {
        IdProveedorVehiculo = entity.IdProveedorVehiculo,
        Nombre = entity.Nombre,
        RncCedula = entity.RncCedula,
        Telefono = entity.Telefono,
        Direccion = entity.Direccion,
        Contacto = entity.Contacto,
        Observacion = entity.Observacion,
        Activo = entity.Activo,
        FechaCreacion = entity.FechaCreacion,
        FechaActualizacion = entity.FechaActualizacion
    };
}

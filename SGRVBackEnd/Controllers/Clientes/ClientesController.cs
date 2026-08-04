using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGRVBackEnd.Data;
using SGRVBackEnd.DTOs.Clientes;
using SGRVBackEnd.Models.Cliente;
using SGRVBackEnd.Shared;

namespace SGRVBackEnd.Controllers;

[ApiController]
[Authorize]
[Route("api/clientes")]
public sealed class ClientesController : BaseApiController
{
    private readonly AppDbContext _context;

    public ClientesController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<ClienteDto>>>> GetAll(
        [FromQuery] ClienteSearchDto search,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        var query = _context.Clientes.AsNoTracking()
            .Where(x => x.IdEmpresa == idEmpresa);

        if (!search.IncluirInactivos)
            query = query.Where(x => x.Activo);

        var term = search.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(x =>
                x.Nombre.Contains(term) ||
                x.Apellido.Contains(term) ||
                x.CedulaPasaporte.Contains(term) ||
                x.Email.Contains(term) ||
                x.Telefono.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var data = await query
            .OrderBy(x => x.Nombre)
            .ThenBy(x => x.Apellido)
            .Skip((search.PageNumber - 1) * search.PageSize)
            .Take(search.PageSize)
            .Select(x => Map(x))
            .ToListAsync(cancellationToken);

        Response.Headers.Append("X-Total-Count", total.ToString());
        Response.Headers.Append("X-Page-Number", search.PageNumber.ToString());
        Response.Headers.Append("X-Page-Size", search.PageSize.ToString());

        return Ok(Success<IEnumerable<ClienteDto>>(
            data, "Clientes obtenidos correctamente."));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ClienteDto>>> GetById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        var cliente = await _context.Clientes.AsNoTracking()
            .Where(x => x.IdCliente == id && x.IdEmpresa == idEmpresa)
            .Select(x => Map(x))
            .FirstOrDefaultAsync(cancellationToken);

        return cliente is null
            ? NotFound(Failure<ClienteDto>("No se encontró el cliente solicitado."))
            : Ok(Success(cliente, "Cliente obtenido correctamente."));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ADMIN,SUPADMIN,SuperUsuario")]
    public async Task<ActionResult<ApiResponse<ClienteDto>>> Create(
        [FromBody] ClienteCreateDto request,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        var validationError = await ValidateRequest(
            request.CedulaPasaporte,
            request.Email,
            request.FechaNacimiento,
            idEmpresa,
            null,
            cancellationToken);

        if (validationError is not null)
            return Conflict(Failure<ClienteDto>(validationError));

        var cliente = new Cliente
        {
            IdEmpresa = idEmpresa,
            Nombre = request.Nombre.Trim(),
            Apellido = request.Apellido.Trim(),
            CedulaPasaporte = request.CedulaPasaporte.Trim().ToUpperInvariant(),
            FechaNacimiento = request.FechaNacimiento,
            Telefono = request.Telefono?.Trim() ?? string.Empty,
            Email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty,
            Direccion = request.Direccion?.Trim() ?? string.Empty,
            Nacionalidad = request.Nacionalidad?.Trim() ?? string.Empty,
            LicenciaConducir = string.Empty,
            FechaExpLicencia = default,
            FechaVencLicencia = default,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = cliente.IdCliente },
            Success(Map(cliente), "Cliente creado correctamente."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,ADMIN,SUPADMIN,SuperUsuario")]
    public async Task<ActionResult<ApiResponse<ClienteDto>>> Update(
        int id,
        [FromBody] ClienteUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        var cliente = await _context.Clientes.FirstOrDefaultAsync(
            x => x.IdCliente == id && x.IdEmpresa == idEmpresa,
            cancellationToken);

        if (cliente is null)
            return NotFound(Failure<ClienteDto>("No se encontró el cliente solicitado."));

        var validationError = await ValidateRequest(
            request.CedulaPasaporte,
            request.Email,
            request.FechaNacimiento,
            idEmpresa,
            id,
            cancellationToken);

        if (validationError is not null)
            return Conflict(Failure<ClienteDto>(validationError));

        cliente.Nombre = request.Nombre.Trim();
        cliente.Apellido = request.Apellido.Trim();
        cliente.CedulaPasaporte = request.CedulaPasaporte.Trim().ToUpperInvariant();
        cliente.FechaNacimiento = request.FechaNacimiento;
        cliente.Telefono = request.Telefono?.Trim() ?? string.Empty;
        cliente.Email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        cliente.Direccion = request.Direccion?.Trim() ?? string.Empty;
        cliente.Nacionalidad = request.Nacionalidad?.Trim() ?? string.Empty;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(Success(Map(cliente), "Cliente actualizado correctamente."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,ADMIN,SUPADMIN,SuperUsuario")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int id,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        var cliente = await _context.Clientes.FirstOrDefaultAsync(
            x => x.IdCliente == id && x.IdEmpresa == idEmpresa,
            cancellationToken);

        if (cliente is null)
            return NotFound(Failure<object>("No se encontró el cliente solicitado."));

        if (!cliente.Activo)
            return Ok(Success<object>(
                new { cliente.IdCliente, cliente.Activo },
                "El cliente ya se encontraba inactivo."));

        cliente.Activo = false;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(Success<object>(
            new { cliente.IdCliente, cliente.Activo },
            "Cliente desactivado correctamente."));
    }

    private async Task<string?> ValidateRequest(
        string cedulaPasaporte,
        string? email,
        DateTime fechaNacimiento,
        int idEmpresa,
        int? idCliente,
        CancellationToken cancellationToken)
    {
        if (fechaNacimiento.Date > DateTime.UtcNow.Date)
            return "La fecha de nacimiento no puede estar en el futuro.";

        var document = cedulaPasaporte.Trim().ToUpperInvariant();
        var duplicateDocument = await _context.Clientes.AsNoTracking().AnyAsync(x =>
            x.IdEmpresa == idEmpresa &&
            x.CedulaPasaporte == document &&
            x.Activo &&
            (!idCliente.HasValue || x.IdCliente != idCliente.Value),
            cancellationToken);

        if (duplicateDocument)
            return "Ya existe un cliente activo con la misma cédula o pasaporte.";

        var normalizedEmail = email?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            var duplicateEmail = await _context.Clientes.AsNoTracking().AnyAsync(x =>
                x.IdEmpresa == idEmpresa &&
                x.Email == normalizedEmail &&
                x.Activo &&
                (!idCliente.HasValue || x.IdCliente != idCliente.Value),
                cancellationToken);

            if (duplicateEmail)
                return "Ya existe un cliente activo con el mismo correo electrónico.";
        }

        return null;
    }

    private static ApiResponse<T> Success<T>(T data, string message) =>
        new() { Success = true, Message = message, Data = data };

    private static ApiResponse<T> Failure<T>(string message) =>
        new() { Success = false, Message = message };

    private static ClienteDto Map(Cliente x) => new()
    {
        IdCliente = x.IdCliente,
        IdEmpresa = x.IdEmpresa,
        Nombre = x.Nombre,
        Apellido = x.Apellido,
        CedulaPasaporte = x.CedulaPasaporte,
        FechaNacimiento = x.FechaNacimiento,
        Telefono = x.Telefono,
        Email = x.Email,
        Direccion = x.Direccion,
        Nacionalidad = x.Nacionalidad,
        LicenciaConducir = x.LicenciaConducir,
        FechaExpLicencia = x.FechaExpLicencia,
        FechaVencLicencia = x.FechaVencLicencia,
        Activo = x.Activo,
        FechaCreacion = x.FechaCreacion
    };
}

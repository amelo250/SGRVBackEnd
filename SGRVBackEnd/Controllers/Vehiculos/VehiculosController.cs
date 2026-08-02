using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGRVBackEnd.Data;
using SGRVBackEnd.DTOs.Vehiculos;
using SGRVBackEnd.Enums;
using SGRVBackEnd.Models.Vehiculo;
using SGRVBackEnd.Shared;

namespace SGRVBackEnd.Controllers;

[ApiController]
[Authorize]
[Route("api/vehiculos")]
public sealed class VehiculosController : BaseApiController
{
    private const int IdEstadoRentaActivaLegacy = 6;
    private readonly AppDbContext _context;

    public VehiculosController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<VehiculoDto>>>> GetAll(
        [FromQuery] bool incluirInactivos = false,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        var query = _context.Vehiculos.AsNoTracking().Where(x => x.IdEmpresa == idEmpresa);
        if (!incluirInactivos) query = query.Where(x => x.Activo);

        var data = await query.OrderBy(x => x.Marca).ThenBy(x => x.Modelo)
            .Select(x => Map(x)).ToListAsync(cancellationToken);

        return Ok(Success<IEnumerable<VehiculoDto>>(data, "Vehículos obtenidos correctamente."));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<VehiculoDto>>> GetById(
        int id, CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        var vehiculo = await _context.Vehiculos.AsNoTracking()
            .Where(x => x.IdVehiculo == id && x.IdEmpresa == idEmpresa)
            .Select(x => Map(x)).FirstOrDefaultAsync(cancellationToken);

        return vehiculo is null
            ? NotFound(Failure<VehiculoDto>("No se encontró el vehículo solicitado."))
            : Ok(Success(vehiculo, "Vehículo obtenido correctamente."));
    }

    [HttpGet("disponibles")]
    public async Task<ActionResult<ApiResponse<IEnumerable<VehiculoDto>>>> GetDisponibles(
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        var idEstadoDisponible = await GetEstadoDisponibleId(cancellationToken);
        if (idEstadoDisponible is null)
            return BadRequest(Failure<IEnumerable<VehiculoDto>>("No existe el estado VEHICULO/DISPONIBLE."));

        var data = await _context.Vehiculos.AsNoTracking()
            .Where(x => x.IdEmpresa == idEmpresa && x.Activo && x.IdEstado == idEstadoDisponible)
            .OrderBy(x => x.Marca).ThenBy(x => x.Modelo)
            .Select(x => Map(x)).ToListAsync(cancellationToken);

        return Ok(Success<IEnumerable<VehiculoDto>>(data, "Vehículos disponibles obtenidos correctamente."));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ADMIN,SUPADMIN,SuperUsuario")]
    public async Task<ActionResult<ApiResponse<VehiculoDto>>> Create(
        [FromBody] VehiculoCreateDto request,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        var validationError = await ValidateRequest(request, idEmpresa, null, cancellationToken);
        if (validationError is not null) return BadRequest(Failure<VehiculoDto>(validationError));

        var maxVehiculos = await (
            from empresa in _context.Empresas.AsNoTracking()
            join plan in _context.Planes.AsNoTracking() on empresa.IdPlan equals plan.IdPlan
            where empresa.IdEmpresa == idEmpresa && empresa.Activo && plan.Activo
            select (int?)plan.MaxVehiculos).FirstOrDefaultAsync(cancellationToken);

        if (maxVehiculos is null or <= 0)
            return BadRequest(Failure<VehiculoDto>("La empresa no tiene un plan activo válido."));

        var cantidadActual = await _context.Vehiculos
            .CountAsync(x => x.IdEmpresa == idEmpresa && x.Activo, cancellationToken);
        if (cantidadActual >= maxVehiculos)
            return Conflict(Failure<VehiculoDto>("Se alcanzó el límite de vehículos del plan."));

        var idEstadoDisponible = await GetEstadoDisponibleId(cancellationToken);
        if (idEstadoDisponible is null)
            return BadRequest(Failure<VehiculoDto>("No existe el estado VEHICULO/DISPONIBLE."));

        var vehiculo = new Vehiculo
        {
            IdEmpresa = idEmpresa,
            IdEstado = idEstadoDisponible.Value,
            IdCombustible = request.IdCombustible,
            IdTransmision = request.IdTransmision,
            IdTipo = request.IdTipo,
            TipoPropiedad = request.TipoPropiedad,
            IdProveedorVehiculo = request.IdProveedorVehiculo,
            IdMonedaTarifa = request.IdMonedaTarifa,
            Marca = request.Marca.Trim(),
            Modelo = request.Modelo.Trim(),
            Anio = request.Anio,
            Placa = request.Placa.Trim().ToUpperInvariant(),
            VIN = request.VIN?.Trim().ToUpperInvariant() ?? string.Empty,
            Color = request.Color?.Trim() ?? string.Empty,
            Kilometraje = request.Kilometraje,
            PrecioPorDia = request.PrecioPorDia,
            DepositoCombustible = request.DepositoCombustible,
            Descripcion = request.Descripcion?.Trim() ?? string.Empty,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Vehiculos.Add(vehiculo);
        await _context.SaveChangesAsync(cancellationToken);
        var result = Map(vehiculo);

        return CreatedAtAction(nameof(GetById), new { id = vehiculo.IdVehiculo },
            Success(result, "Vehículo creado correctamente."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,ADMIN,SUPADMIN,SuperUsuario")]
    public async Task<ActionResult<ApiResponse<VehiculoDto>>> Update(
        int id, [FromBody] VehiculoUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        var vehiculo = await _context.Vehiculos
            .FirstOrDefaultAsync(x => x.IdVehiculo == id && x.IdEmpresa == idEmpresa, cancellationToken);
        if (vehiculo is null) return NotFound(Failure<VehiculoDto>("No se encontró el vehículo solicitado."));

        var validationError = await ValidateRequest(request, idEmpresa, id, cancellationToken);
        if (validationError is not null) return BadRequest(Failure<VehiculoDto>(validationError));

        vehiculo.IdCombustible = request.IdCombustible;
        vehiculo.IdTransmision = request.IdTransmision;
        vehiculo.IdTipo = request.IdTipo;
        vehiculo.TipoPropiedad = request.TipoPropiedad;
        vehiculo.IdProveedorVehiculo = request.IdProveedorVehiculo;
        vehiculo.IdMonedaTarifa = request.IdMonedaTarifa;
        vehiculo.Marca = request.Marca.Trim();
        vehiculo.Modelo = request.Modelo.Trim();
        vehiculo.Anio = request.Anio;
        vehiculo.Placa = request.Placa.Trim().ToUpperInvariant();
        vehiculo.VIN = request.VIN?.Trim().ToUpperInvariant() ?? string.Empty;
        vehiculo.Color = request.Color?.Trim() ?? string.Empty;
        vehiculo.Kilometraje = request.Kilometraje;
        vehiculo.PrecioPorDia = request.PrecioPorDia;
        vehiculo.DepositoCombustible = request.DepositoCombustible;
        vehiculo.Descripcion = request.Descripcion?.Trim() ?? string.Empty;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(Success(Map(vehiculo), "Vehículo actualizado correctamente."));
    }

    [HttpPut("{id:int}/estado")]
    [Authorize(Roles = "Admin,ADMIN,SUPADMIN,SuperUsuario")]
    public async Task<ActionResult<ApiResponse<VehiculoDto>>> CambiarEstado(
        int id, [FromBody] VehiculoEstadoUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        var vehiculo = await _context.Vehiculos
            .FirstOrDefaultAsync(x => x.IdVehiculo == id && x.IdEmpresa == idEmpresa, cancellationToken);
        if (vehiculo is null) return NotFound(Failure<VehiculoDto>("No se encontró el vehículo solicitado."));

        var estadoValido = await _context.Estados.AsNoTracking().AnyAsync(x =>
            x.IdEstado == request.IdEstado && x.Categoria == "VEHICULO" && x.Activo, cancellationToken);
        if (!estadoValido) return BadRequest(Failure<VehiculoDto>("El estado no corresponde a vehículos."));

        vehiculo.IdEstado = request.IdEstado;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(Success(Map(vehiculo), "Estado actualizado correctamente."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,ADMIN,SUPADMIN,SuperUsuario")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int id, CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        var vehiculo = await _context.Vehiculos
            .FirstOrDefaultAsync(x => x.IdVehiculo == id && x.IdEmpresa == idEmpresa, cancellationToken);
        if (vehiculo is null) return NotFound(Failure<object>("No se encontró el vehículo solicitado."));

        var tieneRentaActiva = await _context.Rentas.AsNoTracking().AnyAsync(x =>
            x.IdEmpresa == idEmpresa && x.IdVehiculo == id && x.IdEstado == IdEstadoRentaActivaLegacy,
            cancellationToken);
        if (tieneRentaActiva)
            return Conflict(Failure<object>("No se puede desactivar un vehículo con una renta activa."));

        vehiculo.Activo = false;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(Success<object>(new { vehiculo.IdVehiculo, vehiculo.Activo },
            "Vehículo desactivado correctamente."));
    }

    private async Task<string?> ValidateRequest(
        VehiculoCreateDto request, int idEmpresa, int? idVehiculo,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.TipoPropiedad))
            return "El tipo de propiedad del vehículo no es válido.";
        if (request.TipoPropiedad == TipoPropiedadVehiculo.Propio && request.IdProveedorVehiculo.HasValue)
            return "Un vehículo propio no debe indicar proveedor.";
        if (request.TipoPropiedad == TipoPropiedadVehiculo.Tercero && !request.IdProveedorVehiculo.HasValue)
            return "Un vehículo de tercero debe indicar proveedor.";

        var placa = request.Placa.Trim().ToUpperInvariant();
        if (await _context.Vehiculos.AsNoTracking().AnyAsync(x =>
            x.IdEmpresa == idEmpresa && x.Placa == placa && x.Activo &&
            (!idVehiculo.HasValue || x.IdVehiculo != idVehiculo.Value), cancellationToken))
            return "Ya existe un vehículo activo con esa placa.";

        if (!await _context.Combustibles.AsNoTracking().AnyAsync(x => x.IdCombustible == request.IdCombustible && x.Activo, cancellationToken))
            return "El combustible indicado no existe o está inactivo.";
        if (!await _context.Transmisiones.AsNoTracking().AnyAsync(x => x.IdTransmision == request.IdTransmision && x.Activo, cancellationToken))
            return "La transmisión indicada no existe o está inactiva.";
        if (!await _context.Tipos.AsNoTracking().AnyAsync(x => x.IdTipo == request.IdTipo && x.Categoria == "VEHICULO" && x.Activo, cancellationToken))
            return "El tipo indicado no corresponde a vehículos.";
        if (!await _context.Monedas.AsNoTracking().AnyAsync(x => x.IdMoneda == request.IdMonedaTarifa && x.Activo, cancellationToken))
            return "La moneda de tarifa indicada no existe o está inactiva.";

        return null;
    }

    private Task<int?> GetEstadoDisponibleId(CancellationToken cancellationToken) =>
        _context.Estados.AsNoTracking()
            .Where(x => x.Categoria == "VEHICULO" && x.Codigo == "DISPONIBLE" && x.Activo)
            .Select(x => (int?)x.IdEstado).FirstOrDefaultAsync(cancellationToken);

    private static ApiResponse<T> Success<T>(T data, string message) =>
        new() { Success = true, Message = message, Data = data };

    private static ApiResponse<T> Failure<T>(string message) =>
        new() { Success = false, Message = message };

    private static VehiculoDto Map(Vehiculo x) => new()
    {
        IdVehiculo = x.IdVehiculo,
        IdEstado = x.IdEstado,
        IdCombustible = x.IdCombustible,
        IdTransmision = x.IdTransmision,
        IdTipo = x.IdTipo,
        TipoPropiedad = x.TipoPropiedad,
        IdProveedorVehiculo = x.IdProveedorVehiculo,
        IdMonedaTarifa = x.IdMonedaTarifa,
        Marca = x.Marca,
        Modelo = x.Modelo,
        Anio = x.Anio,
        Placa = x.Placa,
        VIN = x.VIN,
        Color = x.Color,
        Kilometraje = x.Kilometraje,
        PrecioPorDia = x.PrecioPorDia,
        DepositoCombustible = x.DepositoCombustible,
        Descripcion = x.Descripcion,
        Activo = x.Activo,
        FechaCreacion = x.FechaCreacion
    };
}

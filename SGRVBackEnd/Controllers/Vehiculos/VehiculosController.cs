using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGRVBackEnd.Data;
using SGRVBackEnd.DTOs.Vehiculos;
using SGRVBackEnd.Helpers;
using SGRVBackEnd.Models;
using SGRVBackEnd.Models.Vehiculo;

namespace SGRVBackEnd.Controllers;

[Authorize]
[Route("api/vehiculos")]
public sealed class VehiculosController : BaseApiController
{
    private readonly AppDbContext _context;

    public VehiculosController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VehiculoDto>>> GetAll(
        [FromQuery] bool incluirInactivos = false)
    {
        var idEmpresa = GetEmpresaId();
        var query = _context.Vehiculos.AsNoTracking()
            .Where(x => x.IdEmpresa == idEmpresa);

        if (!incluirInactivos)
            query = query.Where(x => x.Activo);

        var data = await query
            .OrderBy(x => x.Marca)
            .ThenBy(x => x.Modelo)
            .Select(x => Map(x))
            .ToListAsync();

        return Ok(ApiResponseHelper<IEnumerable<VehiculoDto>>.Correcto(data));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VehiculoDto>> GetById(int id)
    {
        var idEmpresa = ClaimsHelper.ObtenerIdEmpresa(User);

        var vehiculo = await _context.Vehiculos
            .AsNoTracking()
            .Where(x => x.IdVehiculo == id && x.IdEmpresa == idEmpresa)
            .Select(x => Map(x))
            .FirstOrDefaultAsync();

        return vehiculo is null ? NotFound() : Ok(ApiResponseHelper<VehiculoDto>.Correcto(vehiculo));
    }

    [HttpGet("disponibles")]
    public async Task<ActionResult<IEnumerable<VehiculoDto>>> GetDisponibles()
    {
        var idEmpresa = ClaimsHelper.ObtenerIdEmpresa(User);

        var data = await _context.Vehiculos
            .AsNoTracking()
            .Where(x =>
                x.IdEmpresa == idEmpresa &&
                x.Activo &&
                              
                x.IdEstado==1)
            .Select(x => Map(x))
            .ToListAsync();

        return Ok(ApiResponseHelper<IEnumerable<VehiculoDto>>.Correcto(data));
    }

    [HttpPost]
    public async Task<ActionResult> Create(VehiculoCreateDto request)
    {
        var idEmpresa = GetEmpresaId();

        var maxVehiculos = await _context.Empresas
            .Where(x => x.IdEmpresa == idEmpresa)
            .Select(x => x.IdPlan != null ? x.IdPlan:1)
            .FirstOrDefaultAsync();

        var cantidadActual = await _context.Vehiculos
            .CountAsync(x => x.IdEmpresa == idEmpresa && x.Activo);

        if (maxVehiculos <= 0)
            return BadRequest(new { message = "La empresa no tiene un plan válido." });

        if (cantidadActual >= maxVehiculos)
            return Conflict(new { message = "Se alcanzó el límite de vehículos del plan." });

        if (!string.IsNullOrWhiteSpace(request.Placa))
        {
            var placa = request.Placa.Trim().ToUpperInvariant();
            if (await _context.Vehiculos.AnyAsync(x =>
                x.IdEmpresa == idEmpresa && x.Placa == placa && x.Activo))
            {
                return Conflict(new { message = "Ya existe un vehículo activo con esa placa." });
            }
        }

        var idEstadoDisponible = await _context.Estados
            .Where(x => x.Categoria == "VEHICULO" && x.Codigo == "DISPONIBLE" && x.Activo)
            .Select(x => (int?)x.IdEstado)
            .FirstOrDefaultAsync();

        if (idEstadoDisponible is null)
            return BadRequest(new { message = "No existe el estado VEHICULO/DISPONIBLE." });

        var vehiculo = new Vehiculo
        {
            IdEmpresa = idEmpresa,
            IdEstado = idEstadoDisponible.Value,
            IdCombustible = request.IdCombustible,
            IdTransmision = request.IdTransmision,
            Marca = request.Marca.Trim(),
            Modelo = request.Modelo.Trim(),
            Anio = request.Anio,
            Placa = request.Placa?.Trim().ToUpperInvariant(),
            VIN = request.VIN?.Trim().ToUpperInvariant(),
            Color = request.Color?.Trim(),
            Kilometraje = request.Kilometraje,
            PrecioPorDia = request.PrecioPorDia,
            DepositoCombustible = request.DepositoCombustible,
            Descripcion = request.Descripcion?.Trim(),
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Vehiculos.Add(vehiculo);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = vehiculo.IdVehiculo }, new { vehiculo.IdVehiculo });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, VehiculoCreateDto request)
    {
        var idEmpresa = GetEmpresaId();
        var vehiculo = await _context.Vehiculos
            .FirstOrDefaultAsync(x => x.IdVehiculo == id && x.IdEmpresa == idEmpresa);

        if (vehiculo is null)
            return NotFound();

        vehiculo.IdCombustible = request.IdCombustible;
        vehiculo.IdTransmision = request.IdTransmision;
        vehiculo.Marca = request.Marca.Trim();
        vehiculo.Modelo = request.Modelo.Trim();
        vehiculo.Anio = request.Anio;
        vehiculo.Placa = request.Placa?.Trim().ToUpperInvariant();
        vehiculo.VIN = request.VIN?.Trim().ToUpperInvariant();
        vehiculo.Color = request.Color?.Trim();
        vehiculo.Kilometraje = request.Kilometraje;
        vehiculo.PrecioPorDia = request.PrecioPorDia;
        vehiculo.DepositoCombustible = request.DepositoCombustible;
        vehiculo.Descripcion = request.Descripcion?.Trim();

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, VehiculoUpdateDto request)
    {
        var idEmpresa = GetEmpresaId();
        var vehiculo = await _context.Vehiculos
            .FirstOrDefaultAsync(x => x.IdVehiculo == id && x.IdEmpresa == idEmpresa);

        if (vehiculo is null)
            return NotFound();

        var estadoValido = await _context.Estados.AnyAsync(x =>
            x.IdEstado == request.IdEstado &&
            x.Categoria == "VEHICULO" &&
            x.Activo);

        if (!estadoValido)
            return BadRequest(new { message = "El estado indicado no corresponde a vehículos." });

        vehiculo.IdEstado = request.IdEstado;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var idEmpresa = GetEmpresaId();
        var vehiculo = await _context.Vehiculos
            .FirstOrDefaultAsync(x => x.IdVehiculo == id && x.IdEmpresa == idEmpresa);

        if (vehiculo is null)
            return NotFound();

        var tieneRentaActiva = await _context.Rentas.AnyAsync(x =>
            x.IdEmpresa == idEmpresa &&
            x.IdVehiculo == id &&
            x.IdEstado == 6
            );

        if (tieneRentaActiva)
            return Conflict(new { message = "No se puede desactivar un vehículo con una renta activa." });

        vehiculo.Activo = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static VehiculoDto Map(Vehiculo x) => new()
    {
        IdVehiculo = x.IdVehiculo,
        IdEmpresa = x.IdEmpresa,
        IdEstado = x.IdEstado,
        IdCombustible = x.IdCombustible,
        IdTransmision = x.IdTransmision,
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

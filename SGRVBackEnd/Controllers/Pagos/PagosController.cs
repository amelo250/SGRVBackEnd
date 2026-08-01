using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGRVBackEnd.Data;
using SGRVBackEnd.DTOs.Pagos;
using SGRVBackEnd.Helpers;
using SGRVBackEnd.Models;
using SGRVBackEnd.Models.Pago;
using SGRVBackEnd.Shared;

namespace SGRVBackEnd.Controllers.Pagos;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PagosController : ControllerBase
{
    private readonly AppDbContext _context;

    public PagosController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Pagos
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<PagoDto>>>> GetPagos(
        [FromQuery] int? idEmpresa,
        [FromQuery] int? idRenta,
        [FromQuery] bool incluirInactivos = false)
    {
        IQueryable<Pago> query = _context.Pagos
            .AsNoTracking();

        if (idEmpresa.HasValue)
        {
            query = query.Where(p => p.IdEmpresa == idEmpresa.Value);
        }

        if (idRenta.HasValue)
        {
            query = query.Where(p => p.IdRenta == idRenta.Value);
        }

        if (!incluirInactivos)
        {
            query = query.Where(p => p.Activo);
        }

        var pagos = await query
            .OrderByDescending(p => p.FechaPago)
            .Select(p => new PagoDto
            {
                IdPago = p.IdPago,
                IdEmpresa = p.IdEmpresa,
                IdRenta = p.IdRenta,
                IdMetodoPago = p.IdMetodoPago,
                Monto = p.Monto,
                Moneda = p.Moneda,
                TasaCambio = p.TasaCambio,
                MontoAplicado = p.MontoAplicado,
                Referencia = p.Referencia,
                FechaPago = p.FechaPago,
                Observacion = p.Observacion,
                Activo = p.Activo,
                FechaRegistro = p.FechaRegistro,
                FechaActualizacion = p.FechaActualizacion
            })
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<PagoDto>>.Ok(
            pagos,
            "Pagos obtenidos correctamente."
        ));
    }

    // GET: api/Pagos/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<PagoDto>>> GetPago(int id)
    {
        var pago = await _context.Pagos
            .AsNoTracking()
            .Where(p => p.IdPago == id)
            .Select(p => new PagoDto
            {
                IdPago = p.IdPago,
                IdEmpresa = p.IdEmpresa,
                IdRenta = p.IdRenta,
                IdMetodoPago = p.IdMetodoPago,
                Monto = p.Monto,
                Moneda = p.Moneda,
                TasaCambio = p.TasaCambio,
                MontoAplicado = p.MontoAplicado,
                Referencia = p.Referencia,
                FechaPago = p.FechaPago,
                Observacion = p.Observacion,
                Activo = p.Activo,
                FechaRegistro = p.FechaRegistro,
                FechaActualizacion = p.FechaActualizacion
            })
            .FirstOrDefaultAsync();

        if (pago is null)
        {
            return NotFound(ApiResponse<PagoDto>.Fail(
                "No se encontró el pago solicitado."
            ));
        }

        return Ok(ApiResponse<PagoDto>.Ok(
            pago,
            "Pago obtenido correctamente."
        ));
    }

    // GET: api/Pagos/renta/5
    [HttpGet("renta/{idRenta:int}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PagoDto>>>>
        GetPagosPorRenta(int idRenta)
    {
        var rentaExiste = await _context.Rentas
            .AsNoTracking()
            .AnyAsync(r => r.IdRenta == idRenta);

        if (!rentaExiste)
        {
            return NotFound(ApiResponse<IEnumerable<PagoDto>>.Fail(
                "No se encontró la renta indicada."
            ));
        }

        var pagos = await _context.Pagos
            .AsNoTracking()
            .Where(p => p.IdRenta == idRenta && p.Activo)
            .OrderByDescending(p => p.FechaPago)
            .Select(p => new PagoDto
            {
                IdPago = p.IdPago,
                IdEmpresa = p.IdEmpresa,
                IdRenta = p.IdRenta,
                IdMetodoPago = p.IdMetodoPago,
                Monto = p.Monto,
                Moneda = p.Moneda,
                TasaCambio = p.TasaCambio,
                MontoAplicado = p.MontoAplicado,
                Referencia = p.Referencia,
                FechaPago = p.FechaPago,
                Observacion = p.Observacion,
                Activo = p.Activo,
                FechaRegistro = p.FechaRegistro,
                FechaActualizacion = p.FechaActualizacion
            })
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<PagoDto>>.Ok(
            pagos,
            "Pagos de la renta obtenidos correctamente."
        ));
    }

    // POST: api/Pagos
    [HttpPost]
    public async Task<ActionResult<ApiResponse<PagoDto>>> CreatePago(
        PagoCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<PagoDto>.Fail(
                "Los datos proporcionados no son válidos."
            ));
        }

        if (dto.Monto <= 0)
        {
            return BadRequest(ApiResponse<PagoDto>.Fail(
                "El monto del pago debe ser mayor que cero."
            ));
        }

        var empresaExiste = await _context.Empresas
            .AsNoTracking()
            .AnyAsync(e =>
                e.IdEmpresa == dto.IdEmpresa &&
                e.Activo
            );

        if (!empresaExiste)
        {
            return BadRequest(ApiResponse<PagoDto>.Fail(
                "La empresa indicada no existe o está inactiva."
            ));
        }

        var renta = await _context.Rentas
            .FirstOrDefaultAsync(r =>
                r.IdRenta == dto.IdRenta &&
                r.IdEmpresa == dto.IdEmpresa
            );

        if (renta is null)
        {
            return BadRequest(ApiResponse<PagoDto>.Fail(
                "La renta indicada no existe o no pertenece a la empresa."
            ));
        }

        var metodoPagoExiste = await _context.MetodosPago
            .AsNoTracking()
            .AnyAsync(m =>
                m.IdMetodoPago == dto.IdMetodoPago &&
                m.Activo
            );

        if (!metodoPagoExiste)
        {
            return BadRequest(ApiResponse<PagoDto>.Fail(
                "El método de pago indicado no existe o está inactivo."
            ));
        }

        var moneda = dto.Moneda.Trim().ToUpperInvariant();

        if (moneda is not "DOP" and not "USD")
        {
            return BadRequest(ApiResponse<PagoDto>.Fail(
                "La moneda debe ser DOP o USD."
            ));
        }

        if (moneda == "USD" &&
            (!dto.TasaCambio.HasValue || dto.TasaCambio <= 0))
        {
            return BadRequest(ApiResponse<PagoDto>.Fail(
                "Debe indicar una tasa de cambio válida para pagos en USD."
            ));
        }

        decimal montoAplicado = moneda == "USD"
            ? dto.Monto * dto.TasaCambio!.Value
            : dto.Monto;

        var pago = new Pago
        {
            IdEmpresa = dto.IdEmpresa,
            IdRenta = dto.IdRenta,
            IdMetodoPago = dto.IdMetodoPago,
            Monto = dto.Monto,
            Moneda = moneda,
            TasaCambio = moneda == "USD"
                ? dto.TasaCambio
                : null,
            MontoAplicado = montoAplicado,
            Referencia = dto.Referencia?.Trim(),
            FechaPago = dto.FechaPago ?? DateTime.UtcNow,
            Observacion = dto.Observacion?.Trim(),
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };

        _context.Pagos.Add(pago);
        await _context.SaveChangesAsync();

        var resultado = MapToDto(pago);

        return CreatedAtAction(
            nameof(GetPago),
            new { id = pago.IdPago },
            ApiResponse<PagoDto>.Ok(
                resultado,
                "Pago registrado correctamente."
            )
        );
    }

    // PUT: api/Pagos/5
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<PagoDto>>> UpdatePago(
        int id,
        PagoUpdateDto dto)
    {
        if (id != dto.IdPago)
        {
            return BadRequest(ApiResponse<PagoDto>.Fail(
                "El ID de la URL no coincide con el ID del pago."
            ));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<PagoDto>.Fail(
                "Los datos proporcionados no son válidos."
            ));
        }

        var pago = await _context.Pagos
            .FirstOrDefaultAsync(p => p.IdPago == id);

        if (pago is null)
        {
            return NotFound(ApiResponse<PagoDto>.Fail(
                "No se encontró el pago solicitado."
            ));
        }

        if (dto.Monto <= 0)
        {
            return BadRequest(ApiResponse<PagoDto>.Fail(
                "El monto del pago debe ser mayor que cero."
            ));
        }

        var rentaExiste = await _context.Rentas
            .AsNoTracking()
            .AnyAsync(r =>
                r.IdRenta == dto.IdRenta &&
                r.IdEmpresa == pago.IdEmpresa
            );

        if (!rentaExiste)
        {
            return BadRequest(ApiResponse<PagoDto>.Fail(
                "La renta no existe o no pertenece a la empresa del pago."
            ));
        }

        var metodoPagoExiste = await _context.MetodosPago
            .AsNoTracking()
            .AnyAsync(m =>
                m.IdMetodoPago == dto.IdMetodoPago &&
                m.Activo
            );

        if (!metodoPagoExiste)
        {
            return BadRequest(ApiResponse<PagoDto>.Fail(
                "El método de pago indicado no existe o está inactivo."
            ));
        }

        var moneda = dto.Moneda.Trim().ToUpperInvariant();

        if (moneda is not "DOP" and not "USD")
        {
            return BadRequest(ApiResponse<PagoDto>.Fail(
                "La moneda debe ser DOP o USD."
            ));
        }

        if (moneda == "USD" &&
            (!dto.TasaCambio.HasValue || dto.TasaCambio <= 0))
        {
            return BadRequest(ApiResponse<PagoDto>.Fail(
                "Debe indicar una tasa de cambio válida para pagos en USD."
            ));
        }

        pago.IdRenta = dto.IdRenta;
        pago.IdMetodoPago = dto.IdMetodoPago;
        pago.Monto = dto.Monto;
        pago.Moneda = moneda;
        pago.TasaCambio = moneda == "USD"
            ? dto.TasaCambio
            : null;
        pago.MontoAplicado = moneda == "USD"
            ? dto.Monto * dto.TasaCambio!.Value
            : dto.Monto;
        pago.Referencia = dto.Referencia?.Trim();
        pago.FechaPago = dto.FechaPago;
        pago.Observacion = dto.Observacion?.Trim();
        pago.Activo = dto.Activo;
        pago.FechaActualizacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<PagoDto>.Ok(
            MapToDto(pago),
            "Pago actualizado correctamente."
        ));
    }

    // DELETE: api/Pagos/5
    // Eliminación lógica
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeletePago(int id)
    {
        var pago = await _context.Pagos
            .FirstOrDefaultAsync(p => p.IdPago == id);

        if (pago is null)
        {
            return NotFound(ApiResponse<object>.Fail(
                "No se encontró el pago solicitado."
            ));
        }

        if (!pago.Activo)
        {
            return BadRequest(ApiResponse<object>.Fail(
                "El pago ya se encuentra anulado."
            ));
        }

        pago.Activo = false;
        pago.FechaActualizacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(
            new
            {
                pago.IdPago,
                pago.Activo
            },
            "Pago anulado correctamente."
        ));
    }

    // PATCH: api/Pagos/5/restaurar
    [HttpPatch("{id:int}/restaurar")]
    public async Task<ActionResult<ApiResponse<PagoDto>>> RestaurarPago(int id)
    {
        var pago = await _context.Pagos
            .FirstOrDefaultAsync(p => p.IdPago == id);

        if (pago is null)
        {
            return NotFound(ApiResponse<PagoDto>.Fail(
                "No se encontró el pago solicitado."
            ));
        }

        if (pago.Activo)
        {
            return BadRequest(ApiResponse<PagoDto>.Fail(
                "El pago ya se encuentra activo."
            ));
        }

        pago.Activo = true;
        pago.FechaActualizacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<PagoDto>.Ok(
            MapToDto(pago),
            "Pago restaurado correctamente."
        ));
    }

    // GET: api/Pagos/renta/5/resumen
    [HttpGet("renta/{idRenta:int}/resumen")]
    public async Task<ActionResult<ApiResponse<object>>> GetResumenPagos(
        int idRenta)
    {
        var renta = await _context.Rentas
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.IdRenta == idRenta);

        if (renta is null)
        {
            return NotFound(ApiResponse<object>.Fail(
                "No se encontró la renta indicada."
            ));
        }

        var totalPagado = await _context.Pagos
            .AsNoTracking()
            .Where(p =>
                p.IdRenta == idRenta &&
                p.Activo
            )
            .SumAsync(p => (decimal?)p.MontoAplicado) ?? 0;

        /*
         * Cambia renta.Total por el nombre real de la propiedad
         * que almacena el total de la renta.
         */
        var totalRenta = renta.Total;
        var balancePendiente = totalRenta - totalPagado;

        var resumen = new
        {
            IdRenta = idRenta,
            TotalRenta = totalRenta,
            TotalPagado = totalPagado,
            BalancePendiente = balancePendiente < 0
                ? 0
                : balancePendiente,
            TieneSobrepago = totalPagado > totalRenta,
            MontoSobrepago = totalPagado > totalRenta
                ? totalPagado - totalRenta
                : 0
        };

        return Ok(ApiResponse<object>.Ok(
            resumen,
            "Resumen de pagos obtenido correctamente."
        ));
    }

    private static PagoDto MapToDto(Pago pago)
    {
        return new PagoDto
        {
            IdPago = pago.IdPago,
            IdEmpresa = pago.IdEmpresa,
            IdRenta = pago.IdRenta,
            IdMetodoPago = pago.IdMetodoPago,
            Monto = pago.Monto,
            Moneda = pago.Moneda,
            TasaCambio = pago.TasaCambio,
            MontoAplicado = pago.MontoAplicado,
            Referencia = pago.Referencia,
            FechaPago = pago.FechaPago,
            Observacion = pago.Observacion,
            Activo = pago.Activo,
            FechaRegistro = pago.FechaRegistro,
            FechaActualizacion = pago.FechaActualizacion
        };
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGRVBackEnd.Data;
using Microsoft.EntityFrameworkCore;
using SGRVBackEnd.DTOs.Catalogos;
using SGRVBackEnd.Shared;

namespace SGRVBackEnd.Controllers.CatalogosController
{
    namespace SGRVBackEnd.Controllers.Catalogos
    {
        [Authorize]
        [Route("api/catalogos")]
        public class CatalogosController : ControllerBase
        {
            private readonly AppDbContext _context;

            public CatalogosController(AppDbContext context)
            {
                _context = context;
            }

            [HttpGet("estados")]
            public async Task<IActionResult> GetEstados([FromQuery] string? categoria)
            {
                var query = _context.Estados.Where(x => x.Activo);

                if (!string.IsNullOrWhiteSpace(categoria))
                    query = query.Where(x => x.Categoria == categoria);

                return Ok(await query.ToListAsync());
            }

            [HttpGet("tipos")]
            public async Task<ActionResult<ApiResponse<IEnumerable<CatalogOptionDto>>>> GetTipos(
                [FromQuery] string? categoria,
                CancellationToken cancellationToken = default)
            {
                var query = _context.Tipos.AsNoTracking().Where(x => x.Activo);

                if (!string.IsNullOrWhiteSpace(categoria))
                    query = query.Where(x => x.Categoria == categoria);

                var data = await query.OrderBy(x => x.nombre)
                    .Select(x => new CatalogOptionDto
                    {
                        Id = x.IdTipo,
                        Code = x.Codigo,
                        Name = x.nombre
                    })
                    .ToListAsync(cancellationToken);

                return Ok(Success<IEnumerable<CatalogOptionDto>>(
                    data, "Tipos obtenidos correctamente."));
            }

            [HttpGet("roles")]
            public async Task<IActionResult> GetRoles()
            {
                return Ok(await _context.Roles.Where(x => x.Activo).ToListAsync());
            }

            [HttpGet("planes")]
            public async Task<IActionResult> GetPlanes()
            {
                return Ok(await _context.Planes.Where(x => x.Activo).ToListAsync());
            }

            [HttpGet("metodos-pago")]
            public async Task<IActionResult> GetMetodosPago()
            {
                return Ok(await _context.MetodosPago.Where(x => x.Activo).ToListAsync());
            }

            [HttpGet("combustibles")]
            public async Task<ActionResult<ApiResponse<IEnumerable<CatalogOptionDto>>>> GetCombustibles(
                CancellationToken cancellationToken = default)
            {
                var data = await _context.Combustibles.AsNoTracking()
                    .Where(x => x.Activo)
                    .OrderBy(x => x.nombre)
                    .Select(x => new CatalogOptionDto
                    {
                        Id = x.IdCombustible,
                        Code = x.Codigo,
                        Name = x.nombre
                    })
                    .ToListAsync(cancellationToken);

                return Ok(Success<IEnumerable<CatalogOptionDto>>(
                    data, "Combustibles obtenidos correctamente."));
            }

            [HttpGet("transmisiones")]
            public async Task<ActionResult<ApiResponse<IEnumerable<CatalogOptionDto>>>> GetTransmisiones(
                CancellationToken cancellationToken = default)
            {
                var data = await _context.Transmisiones.AsNoTracking()
                    .Where(x => x.Activo)
                    .OrderBy(x => x.nombre)
                    .Select(x => new CatalogOptionDto
                    {
                        Id = x.IdTransmision,
                        Code = x.Codigo,
                        Name = x.nombre
                    })
                    .ToListAsync(cancellationToken);

                return Ok(Success<IEnumerable<CatalogOptionDto>>(
                    data, "Transmisiones obtenidas correctamente."));
            }
            
            [HttpGet("monedas")]
            public async Task<ActionResult<ApiResponse<IEnumerable<CurrencyOptionDto>>>> GetMonedas(
                CancellationToken cancellationToken = default)
            {
                var data = await _context.Monedas.AsNoTracking()
                    .Where(x => x.Activo)
                    .OrderBy(x => x.Codigo)
                    .Select(x => new CurrencyOptionDto
                    {
                        Id = x.IdMoneda,
                        Code = x.Codigo,
                        Name = x.Nombre,
                        Symbol = x.Simbolo
                    })
                    .ToListAsync(cancellationToken);

                return Ok(Success<IEnumerable<CurrencyOptionDto>>(
                    data, "Monedas obtenidas correctamente."));
            }

            private static ApiResponse<T> Success<T>(T data, string message) =>
                new() { Success = true, Message = message, Data = data };
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGRVBackEnd.Data;
using Microsoft.EntityFrameworkCore;

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
            public async Task<IActionResult> GetTipos([FromQuery] string? categoria)
            {
                var query = _context.Tipos.Where(x => x.Activo);

                if (!string.IsNullOrWhiteSpace(categoria))
                    query = query.Where(x => x.Categoria == categoria);

                return Ok(await query.ToListAsync());
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
            public async Task<IActionResult> GetCombustibles()
            {
                return Ok(await _context.Combustibles.Where(x => x.Activo).ToListAsync());
            }

            [HttpGet("transmisiones")]
            public async Task<IActionResult> GetTransmisiones()
            {
                return Ok(await _context.Transmisiones.Where(x => x.Activo).ToListAsync());
            }
        }
    }
}

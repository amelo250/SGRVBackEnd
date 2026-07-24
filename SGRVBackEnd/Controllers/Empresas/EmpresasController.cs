using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop.Infrastructure;
using SGRVBackEnd.Data;
using SGRVBackEnd.DTOs.Empresas;
using SGRVBackEnd.Models;
using SGRVBackEnd.Models.Empresa;
using SGRVBackEnd.Helpers;

namespace SGRVBackEnd.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmpresasController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<EmpresasController> _logger;

    public EmpresasController(
        AppDbContext context,
        ILogger<EmpresasController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/empresas
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EmpresaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EmpresaDto>>> GetEmpresas(
        CancellationToken cancellationToken)
    {

  
        var empresas = await _context.Empresas
            .AsNoTracking()
            .OrderBy(e => e.Nombre)
            .Select(e => new EmpresaDto
            {
                IdEmpresa = e.IdEmpresa,
                IdPlan=e.IdPlan,
                NombreComercial=e.NombreComercial,
            
                Nombre = e.Nombre,
                RNC = e.RNC,
                Telefono = e.Telefono,
                Correo = e.Email,
                Direccion = e.Direccion,
                Activo = e.Activo,
                LogoUrl=e.LogoUrl,
                FechaRegistro = e.FechaRegistro,
                FechaActualizacion = e.FechaActualizacion
              
            })
            .ToListAsync(cancellationToken);

        return Ok( ApiResponse<IEnumerable<EmpresaDto>>.Correcto(empresas));
    }

    // GET: api/empresas/5
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EmpresaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmpresaDto>> GetEmpresa(
        int id,
        CancellationToken cancellationToken)
    {
        var empresa = await _context.Empresas
            .AsNoTracking()
            .Where(e => e.IdEmpresa == id)
            .Select(e => new EmpresaDto
            {
                IdEmpresa = e.IdEmpresa,
                Nombre = e.Nombre,
                RNC = e.RNC,
                Telefono = e.Telefono,
                Correo = e.Email,
                Direccion = e.Direccion,
                Activo = e.Activo,
                FechaRegistro = e.FechaRegistro,
                FechaActualizacion = e.FechaActualizacion
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (empresa is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Empresa no encontrada",
                Detail = $"No existe una empresa con el identificador {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok( ApiResponse<EmpresaDto>.Correcto(empresa));
    }

    // POST: api/empresas
    [HttpPost]
    [ProducesResponseType(typeof(EmpresaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmpresaDto>> CreateEmpresa(
        [FromBody] EmpresaCreateDto dto,
        CancellationToken cancellationToken)
    {
        var nombreNormalizado = dto.Nombre.Trim();
        var rncNormalizado = dto.Rnc?.Trim();

        var nombreExiste = await _context.Empresas
            .AnyAsync(
                e => e.Nombre.ToLower() == nombreNormalizado.ToLower(),
                cancellationToken);

        if (nombreExiste)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Empresa duplicada",
                Detail = "Ya existe una empresa registrada con ese nombre.",
                Status = StatusCodes.Status409Conflict
            });
        }

        if (!string.IsNullOrWhiteSpace(rncNormalizado))
        {
            var rncExiste = await _context.Empresas
                .AnyAsync(
                    e => e.RNC == rncNormalizado,
                    cancellationToken);

            if (rncExiste)      
            {
                return Conflict(new ProblemDetails
                {
                    Title = "RNC duplicado",
                    Detail = "Ya existe una empresa registrada con ese RNC.",
                    Status = StatusCodes.Status409Conflict
                });
            }
        }

        var empresa = new Empresa
        {
            Nombre = nombreNormalizado,
            IdPlan=dto.IdPlan,
            NombreComercial=dto.NombreComercial,
            LogoUrl=dto.LogoUrl,
            RNC = rncNormalizado,
            Telefono = dto.Telefono?.Trim(),
            Email = dto.Correo?.Trim(),
            Direccion = dto.Direccion?.Trim(),
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };

        _context.Empresas.Add(empresa);
        await _context.SaveChangesAsync(cancellationToken);

        var resultado = MapToDto(empresa);

        return CreatedAtAction(
            nameof(GetEmpresa),
            new { id = empresa.IdEmpresa },
            resultado);
    }

    // PUT: api/empresas/5
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(EmpresaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmpresaDto>> UpdateEmpresa(
        int id,
        [FromBody] EmpresaUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var empresa = await _context.Empresas
            .FirstOrDefaultAsync(e => e.IdEmpresa == id, cancellationToken);

        if (empresa is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Empresa no encontrada",
                Detail = $"No existe una empresa con el identificador {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var nombreNormalizado = dto.Nombre.Trim();
        var rncNormalizado = dto.Rnc?.Trim();

        var nombreExiste = await _context.Empresas
            .AnyAsync(
                e => e.IdEmpresa != id &&
                     e.Nombre.ToLower() == nombreNormalizado.ToLower(),
                cancellationToken);

        if (nombreExiste)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Empresa duplicada",
                Detail = "Otra empresa ya utiliza ese nombre.",
                Status = StatusCodes.Status409Conflict
            });
        }

        if (!string.IsNullOrWhiteSpace(rncNormalizado))
        {
            var rncExiste = await _context.Empresas
                .AnyAsync(
                    e => e.IdEmpresa != id &&
                         e.RNC == rncNormalizado,
                    cancellationToken);

            if (rncExiste)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "RNC duplicado",
                    Detail = "Otra empresa ya utiliza ese RNC.",
                    Status = StatusCodes.Status409Conflict
                });
            }
        }

        empresa.Nombre = nombreNormalizado;
        empresa.NombreComercial = dto.NombtreComercial;
        empresa.RNC = rncNormalizado;
        empresa.Telefono = dto.Telefono?.Trim();
        empresa.Email = dto.Correo?.Trim();
        empresa.Direccion = dto.Direccion?.Trim();
        empresa.LogoUrl = dto.LogoUrl?.Trim();
        empresa.Activo = dto.Activo;
        empresa.FechaActualizacion = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(empresa));
    }

    // DELETE: api/empresas/5
    // Realiza eliminación lógica.
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEmpresa(
        int id,
        CancellationToken cancellationToken)
    {
        var empresa = await _context.Empresas
            .FirstOrDefaultAsync(e => e.IdEmpresa == id, cancellationToken);

        if (empresa is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Empresa no encontrada",
                Detail = $"No existe una empresa con el identificador {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        empresa.Activo = false;
        empresa.FechaActualizacion = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static EmpresaDto MapToDto(Empresa empresa)
    {
        return new EmpresaDto
        {
            IdEmpresa = empresa.IdEmpresa,
            Nombre = empresa.Nombre,
            RNC = empresa.RNC,
            Telefono = empresa.Telefono,
            Correo = empresa.Email,
            Direccion = empresa.Direccion,
            Activo = empresa.Activo,
            FechaRegistro = empresa.FechaRegistro,
            FechaActualizacion = empresa.FechaActualizacion
        };
    }
}
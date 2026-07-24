using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGRVBackEnd.Data;
using SGRVBackEnd.DTOs.Usuarios;
using SGRVBackEnd.Helpers;
using SGRVBackEnd.Models;
using SGRVBackEnd.Models.Usuarios;

namespace SGRVBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<Usuario> _passwordHasher;

        public UsuariosController(
            AppDbContext context,
            IPasswordHasher<Usuario> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // GET: api/usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioDto>>> GetUsuarios(
            [FromQuery] int? idEmpresa,
            [FromQuery] bool? Activo,
            CancellationToken cancellationToken)
        {
            var query = _context.Usuarios
                .AsNoTracking()
                .AsQueryable();

            if (idEmpresa.HasValue)
            {
                query = query.Where(u => u.IdEmpresa == idEmpresa.Value);
            }

            if (Activo.HasValue)
            {
                query = query.Where(u => u.Activo == Activo.Value);
            }

            var usuarios = await query
                .OrderBy(u => u.Nombre)
                .Select(u => new UsuarioDto
                {
                    IdUsuario = u.IdUsuario,
                    IdEmpresa = u.IdEmpresa,
                    Nombre = u.Nombre,
                    Email = u.Email,
                    IdRol = u.IdRol,
                    Activo = u.Activo,
                    Telefono = u.Telefono,
                    UltimoAcceso = u.UltimoAcceso,
                    FechaCreacion = u.FechaCreacion
                })
                .ToListAsync(cancellationToken);

            return Ok(ApiResponse<IEnumerable<UsuarioDto>>.Correcto(usuarios));
        }

        // GET: api/usuarios/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UsuarioDto>> GetUsuario(
            int id,
            CancellationToken cancellationToken)
        {
            var usuario = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.IdUsuario == id)
                .Select(u => new UsuarioDto
                {
                    IdUsuario = u.IdUsuario,
                    IdEmpresa = u.IdEmpresa,
                    Nombre = u.Nombre,
                    Email = u.Email,
                    IdRol = u.IdRol,
                    Activo = u.Activo,
                    Telefono = u.Telefono,
                    UltimoAcceso = u.UltimoAcceso,
                    FechaCreacion = u.FechaCreacion
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (usuario is null)
            {
                return NotFound(new
                {
                    mensaje = $"No se encontró el usuario con ID {id}."
                });
            }

            return Ok(ApiResponse<UsuarioDto>.Correcto(usuario));
        }

        // POST: api/usuarios
        [HttpPost]
        public async Task<ActionResult<UsuarioDto>> CrearUsuario(
            UsuarioCreateDto dto,
            CancellationToken cancellationToken)
        {
            var emailNormalizado = dto.Email.Trim().ToLowerInvariant();

            var emailExiste = await _context.Usuarios
                .AnyAsync(
                    u => u.Email.ToLower() == emailNormalizado,
                    cancellationToken);

            if (emailExiste)
            {
                return Conflict(new
                {
                    mensaje = "Ya existe un usuario registrado con este correo electrónico."
                });
            }

            var empresaExiste = await _context.Empresas
                .AnyAsync(
                    e => e.IdEmpresa == dto.IdEmpresa,
                    cancellationToken);

            if (!empresaExiste)
            {
                return BadRequest(new
                {
                    mensaje = $"La empresa con ID {dto.IdEmpresa} no existe."
                });
            }

            var rolExiste = await _context.Roles
                .AnyAsync(
                    r => r.IdRol == dto.IdRol,
                    cancellationToken);

            if (!rolExiste)
            {
                return BadRequest(new
                {
                    mensaje = $"El rol con ID {dto.IdRol} no existe."
                });
            }

            var usuario = new Usuario
            {
                IdEmpresa = dto.IdEmpresa,
                Nombre = dto.Nombre.Trim(),
                Email = emailNormalizado,
                IdRol = dto.IdRol,
                Activo = dto.Activo,
                Telefono = string.IsNullOrWhiteSpace(dto.Telefono)
                    ? null
                    : dto.Telefono.Trim(),
                FechaCreacion = DateTime.UtcNow
            };

            usuario.PasswordHash = _passwordHasher.HashPassword(
                usuario,
                dto.PasswordHash);

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync(cancellationToken);

            var usuarioDto = MapearUsuarioDto(usuario);

            return CreatedAtAction(
                nameof(GetUsuario),
                new { id = usuario.IdUsuario },
                usuarioDto);
        }

        // PUT: api/usuarios/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> ActualizarUsuario(
            int id,
            UsuarioUpdateDto dto,
            CancellationToken cancellationToken)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(
                    u => u.IdUsuario == id,
                    cancellationToken);

            if (usuario is null)
            {
                return NotFound(new
                {
                    mensaje = $"No se encontró el usuario con ID {id}."
                });
            }

            var emailNormalizado = dto.Email.Trim().ToLowerInvariant();

            var emailExiste = await _context.Usuarios
                .AnyAsync(
                    u => u.Email.ToLower() == emailNormalizado &&
                         u.IdUsuario != id,
                    cancellationToken);

            if (emailExiste)
            {
                return Conflict(new
                {
                    mensaje = "Otro usuario ya utiliza este correo electrónico."
                });
            }

            var empresaExiste = await _context.Empresas
                .AnyAsync(
                    e => e.IdEmpresa == dto.IdEmpresa,
                    cancellationToken);

            if (!empresaExiste)
            {
                return BadRequest(new
                {
                    mensaje = $"La empresa con ID {dto.IdEmpresa} no existe."
                });
            }

            var rolExiste = await _context.Roles
                .AnyAsync(
                    r => r.IdRol == dto.IdRol,
                    cancellationToken);

            if (!rolExiste)
            {
                return BadRequest(new
                {
                    mensaje = $"El rol con ID {dto.IdRol} no existe."
                });
            }

            usuario.IdEmpresa = dto.IdEmpresa;
            usuario.Nombre = dto.Nombre.Trim();
            usuario.Email = emailNormalizado;
            usuario.IdRol = dto.IdRol;
            usuario.Activo = dto.Activo;
            usuario.Telefono = string.IsNullOrWhiteSpace(dto.Telefono)
                ? null
                : dto.Telefono.Trim();

            await _context.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PATCH: api/usuarios/5/cambiar-password
        [HttpPatch("{id:int}/cambiar-password")]
        public async Task<IActionResult> CambiarPassword(
            int id,
            CambiarPasswordDto dto,
            CancellationToken cancellationToken)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(
                    u => u.IdUsuario == id,
                    cancellationToken);

            if (usuario is null)
            {
                return NotFound(new
                {
                    mensaje = $"No se encontró el usuario con ID {id}."
                });
            }

            usuario.PasswordHash = _passwordHasher.HashPassword(
                usuario,
                dto.PasswordNueva);

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                mensaje = "La contraseña fue actualizada correctamente."
            });
        }

        // PATCH: api/usuarios/5/estado
        [HttpPatch("{id:int}/estado")]
        public async Task<IActionResult> CambiarEstado(
            int id,
            [FromQuery] bool activo,
            CancellationToken cancellationToken)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(
                    u => u.IdUsuario == id,
                    cancellationToken);

            if (usuario is null)
            {
                return NotFound(new
                {
                    mensaje = $"No se encontró el usuario con ID {id}."
                });
            }

            usuario.Activo = activo;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                mensaje = activo
                    ? "El usuario fue activado correctamente."
                    : "El usuario fue desactivado correctamente."
            });
        }

        // DELETE: api/usuarios/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> EliminarUsuario(
            int id,
            CancellationToken cancellationToken)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(
                    u => u.IdUsuario == id,
                    cancellationToken);

            if (usuario is null)
            {
                return NotFound(new
                {
                    mensaje = $"No se encontró el usuario con ID {id}."
                });
            }

            // Borrado lógico recomendado.
            usuario.Activo = false;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                mensaje = "El usuario fue desactivado correctamente."
            });
        }

        private static UsuarioDto MapearUsuarioDto(Usuario usuario)
        {
            return new UsuarioDto
            {
                IdUsuario = usuario.IdUsuario,
                IdEmpresa = usuario.IdEmpresa,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                IdRol = usuario.IdRol,
                Activo = usuario.Activo,
                Telefono = usuario.Telefono,
                UltimoAcceso = usuario.UltimoAcceso,
                FechaCreacion = usuario.FechaCreacion
            };
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SGRVBackEnd.Data;
using SGRVBackEnd.Helpers;
using SGRVBackEnd.Models;
using SGRVBackEnd.Models.Auth;
using SGRVBackEnd.Models.Usuarios;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace SGRVBackEnd.Controllers.Auth;
    [ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    private readonly IPasswordHasher<Usuario> _passwordHasher;


    public AuthController(IConfiguration configuration,AppDbContext context, IPasswordHasher<Usuario> passwordHasher)
    {
        _configuration = configuration;
        _context = context;
        _passwordHasher = passwordHasher;   
    }

    [HttpPost("login")]
    public  async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        // Usuario temporal para prueba
        //if (request.email != "admin@rentcar.com" || request.password != "123456")
        //{
        //    return Unauthorized(new
        //    {
        //        message = "Credenciales incorrectas"
        //    });
        //}

        var email = request.email.Trim().ToLowerInvariant();

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(
                x => x.Email.ToLower() == email &&
                     x.Activo,
                cancellationToken);

        if (usuario == null)
        {
            return Unauthorized(new
            {
                message = "Credenciales incorrectas"



            });
        }
            

        var result = _passwordHasher.VerifyHashedPassword(usuario,usuario.PasswordHash,request.password);

        if (result == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new
                {
                    message = "Credenciales incorrectas"
                });
            }



        var rol = await _context.Roles
    .AsNoTracking()
    .FirstOrDefaultAsync(
        x => x.IdRol == usuario.IdRol && x.Activo,cancellationToken);

        if (rol is null)
        {
            return Unauthorized(new
            {
                message = "El usuario no tiene un rol activo válido."
            });
        }

        var token = GenerateJwtToken(usuario, rol.Codigo);

        return Ok(new LoginResponse
        {
            Token = token,
            Expiration = DateTime.UtcNow.AddMinutes(
                Convert.ToDouble(_configuration["Jwt:ExpiresInMinutes"])
            )
        });
    }

    [Authorize(Roles = "SUPADMIN")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request,CancellationToken cancellationToken)
    { 
        var existeusuario = await _context.Usuarios.AnyAsync(u => u.Email == request.Email, cancellationToken);

        if (existeusuario) {


            return BadRequest(new
            {
                message = "Ya existe el usuario"

            });
                
                }


        var empresaValida = await _context.Empresas
    .AsNoTracking()
    .AnyAsync(
        empresa =>
            empresa.IdEmpresa == request.IdEmpresa &&
            empresa.Activo,
        cancellationToken);

        if (!empresaValida)
        {
            return BadRequest(new
            {
                message = "La empresa no existe o está inactiva."
            });
        }


        var rolValido = await _context.Roles
    .AsNoTracking()
    .AnyAsync(
        rol =>
            rol.IdRol == request.IdRol &&
            rol.Activo,
        cancellationToken);

        if (!rolValido)
        {
            return BadRequest(new
            {
                message = "El rol no existe o está inactivo."
            });
        }
        var usuario = new Models.Usuarios.Usuario
        {
            
            Nombre = request.Nombre,
            Email = request.Email,
            PasswordHash = request.Password,
            IdEmpresa = request.IdEmpresa,
            IdRol = request.IdRol,
            Activo = request.Activo
        };

        var result = _passwordHasher.VerifyHashedPassword(usuario,usuario.PasswordHash,request.Password);

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Usuario registrado exitosamente",
            usuario.IdUsuario,
            usuario.Nombre,
            usuario.Email
        });









    }

    private string GenerateJwtToken(
     Models.Usuarios.Usuario usuario,
     string codigoRol)
    {
        var jwtSettings = _configuration.GetSection("Jwt");

        var claims = new List<Claim>
    {
        new(
            ClaimTypes.NameIdentifier,
            usuario.IdUsuario.ToString()),

        new(
            CustomClaimTypes.UsuarioId,
            usuario.IdUsuario.ToString()),

        new(
            CustomClaimTypes.EmpresaId,
            usuario.IdEmpresa.ToString()),

        new(
            ClaimTypes.Email,
            usuario.Email),

        new(
            ClaimTypes.Name,
            usuario.Nombre),

        new(
            ClaimTypes.Role,
            codigoRol)
    };

        var keyValue = jwtSettings["Key"]
            ?? throw new InvalidOperationException(
                "No se encontró la configuración Jwt:Key.");

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(keyValue));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var expiration = DateTime.UtcNow.AddMinutes(
            Convert.ToDouble(jwtSettings["ExpiresInMinutes"]));

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiration,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

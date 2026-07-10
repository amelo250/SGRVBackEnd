using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SGRVBackEnd.Models.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Identity;
using Microsoft.EntityFrameworkCore;
using SGRVBackEnd.Data;
using SGRVBackEnd.Models;


namespace SGRVBackEnd.Controllers.Auth;
    [ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;

    public AuthController(IConfiguration configuration,AppDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Usuario temporal para prueba
        //if (request.email != "admin@rentcar.com" || request.password != "123456")
        //{
        //    return Unauthorized(new
        //    {
        //        message = "Credenciales incorrectas"
        //    });
        //}

        var usuario = _context.Usuarios.FirstOrDefault(u => u.Email == request.email && u.Activo==true);

        if (usuario == null)
        {
            return Unauthorized(new
            {
                message = "Credenciales incorrectas"



            });
        }
            var passwordHasher = new PasswordHasher<Models.Usuarios.Usuario>();

            var result = passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.password);

            if (result == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new
                {
                    message = "Credenciales incorrectas"
                });
            }



            var token = GenerateJwtToken(request.email);

        return Ok(new LoginResponse
        {
            Token = token,
            Expiration = DateTime.UtcNow.AddMinutes(
                Convert.ToDouble(_configuration["Jwt:ExpiresInMinutes"])
            )
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    { 
        var existeusuario = await _context.Usuarios.AnyAsync(u => u.Email == request.Email);

        if (existeusuario) {


            return BadRequest(new
            {
                message = "Ya existe el usuario"

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

        var passwordHash = new PasswordHasher<Models.Usuarios.Usuario>();

        usuario.PasswordHash = passwordHash.HashPassword(usuario, request.Password);

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Usuario registrado exitosamente",
            usuario.IdUsuario,
            usuario.Nombre,
            usuario.Email
        });









    }

    private string GenerateJwtToken(string email)
    {
        var jwtSettings = _configuration.GetSection("Jwt");

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("EmpresaId", "1")
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Key"]!)
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var expiration = DateTime.UtcNow.AddMinutes(
            Convert.ToDouble(jwtSettings["ExpiresInMinutes"])
        );

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiration,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGRVBackEnd.Data;
using SGRVBackEnd.DTOs.Clientes;
using SGRVBackEnd.Models;
using SGRVBackEnd.Models.Cliente;

namespace SGRVBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ClientesController : BaseApiController
    {
        private readonly AppDbContext _context;

        public ClientesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/clientes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClienteDto>>> GetClientes()
        {
            var clientes = await _context.Clientes
                .OrderBy(c => c.Nombre)
                .Select(c => new ClienteDto
                {
                    IdCliente = c.IdCliente,
                    Nombre = c.Nombre,
                    Apellido = c.Apellido,
                    CedulaPasaporte = c.CedulaPasaporte,
                    
                    Telefono = c.Telefono,
                     Email = c.Email,
                    Direccion = c.Direccion,
                    FechaNacimiento = c.FechaNacimiento,
                    IdEmpresa = c.IdEmpresa,
                    Activo = c.Activo
                })
                .ToListAsync();

            return Ok(clientes);
        }

        // GET: api/clientes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ClienteDto>> GetCliente(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
                return NotFound();

            return Ok(new ClienteDto
            {
                IdCliente = cliente.IdCliente,
                Nombre = cliente.Nombre,
                Apellido = cliente.Apellido,
                CedulaPasaporte = cliente.CedulaPasaporte,
                Telefono = cliente.Telefono,
                Email = cliente.Email,
                Direccion = cliente.Direccion,
                FechaNacimiento = cliente.FechaNacimiento,
                IdEmpresa = cliente.IdEmpresa,
                Activo = cliente.Activo
            });
        }

        // GET: api/clientes/empresa/1
        [HttpGet("empresa/{empresaId}")]
        public async Task<ActionResult<IEnumerable<ClienteDto>>> GetClientesPorEmpresa(int empresaId)
        {
            var clientes = await _context.Clientes
                .Where(c => c.IdEmpresa == empresaId)
                .OrderBy(c => c.Nombre)
                .Select(c => new ClienteDto
                {
                    IdCliente = c.IdCliente,
                    Nombre = c.Nombre,
                    Apellido = c.Apellido,
                    CedulaPasaporte = c.CedulaPasaporte,
                    Telefono = c.Telefono,
                    Email = c.Email,
                   
                    Direccion = c.Direccion,
                    FechaNacimiento = c.FechaNacimiento,
                    IdEmpresa = c.IdEmpresa,
                    Activo = c.Activo
                })
                .ToListAsync();

            return Ok(clientes);
        }

        // POST: api/clientes
        [HttpPost]
        public async Task<ActionResult<ClienteDto>> PostCliente(ClienteCreateDto dto)
        {
            var cliente = new Cliente
            {
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                CedulaPasaporte = dto.CedulaPasaporte,
               
                Telefono = dto.Telefono,
               
                Email = dto.Email,
                Direccion = dto.Direccion,
                FechaNacimiento = dto.FechaNacimiento,
                IdEmpresa = dto.IdEmpresa,
                Activo = true
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCliente), new { IdCliente = cliente.IdCliente }, new ClienteDto
            {
                IdCliente = cliente.IdCliente,
                Nombre = cliente.Nombre,
                Apellido = cliente.Apellido,
                CedulaPasaporte = cliente.CedulaPasaporte,
                Telefono = cliente.Telefono,
                Email = cliente.Email,
                Direccion = cliente.Direccion,
                FechaNacimiento = cliente.FechaNacimiento,
                IdEmpresa = cliente.IdEmpresa,
                Activo = cliente.Activo
            });
        }

        // PUT: api/clientes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCliente(int id, ClienteUpdateDto dto)
        {
            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
                return NotFound();

            cliente.Nombre = dto.Nombre;
            cliente.Apellido = dto.Apellido;
            cliente.CedulaPasaporte = dto.CedulaPasaporte;
            cliente.Telefono = dto.Telefono;
            cliente.Email = dto.Email;
            cliente.Direccion = dto.Direccion;
            cliente.FechaNacimiento = dto.FechaNacimiento;
            cliente.IdEmpresa = dto.IdEmpresa   ;
            cliente.Activo = dto.Activo;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/clientes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCliente(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
                return NotFound();

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SGRVBackEnd.Models.Usuarios
{
    public class Usuario
    {
        [Key]
        public int IdUsuario { get; set; }

        public int IdEmpresa { get; set; }
        public int IdRol { get; set; }
        public string Nombre { get; set; }=string.Empty;
        public string Email { get; set; }= string.Empty;
       
        public string PasswordHash { get; set; }
        [Required]
      
        public bool Activo { get; set; }
        public string? Telefono { get; set; }
        public DateTime UltimoAcceso { get; set; }
        public DateTime FechaCreacion { get; set; }



    }
}



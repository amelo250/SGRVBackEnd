namespace SGRVBackEnd.DTOs.Usuarios
{
    public class UsuarioDto
    {

        public int IdUsuario { get; set; }
        public int IdEmpresa { get; set; }
        public int IdRol { get; set; }

        public string Nombre { get; set; } = string.Empty;  

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public DateTime UltimoAcceso { get; set; }
        public DateTime FechaCreacion { get; set; }= DateTime.UtcNow;



        public bool Activo { get; set; }
    }
}

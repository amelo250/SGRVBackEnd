namespace SGRVBackEnd.Models.Auth
{
    public class RegisterRequest
    {
    public string Nombre { get; set; } = string.Empty;
    public int IdEmpresa { get; set; } 
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int IdRol { get; set; } = 1; // Asignar un rol predeterminado (por ejemplo, 1 para usuario regular)
    public Boolean Activo { get; set; } = true; // Asignar un estado predeterminado (por ejemplo, 1 para activo)
    public DateTime FechaCreacion { get; set; } = DateTime.Now; // Asignar la fecha de creación actual
    public DateTime UltimoAcceso { get; set; } 






    }
}

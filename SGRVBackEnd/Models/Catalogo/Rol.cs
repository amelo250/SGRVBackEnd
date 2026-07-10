using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.Models.Catalogo
{
    public class Rol
    {
        [Key]
        public int IdRol { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; } = false;



    }
}

using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.Models.Catalogo
{
    public class Combustibles
    {
        [Key]
        public int IdCombustible { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string nombre { get; set; } = string.Empty;
        public bool Activo { get; set; } = false;

    }
}

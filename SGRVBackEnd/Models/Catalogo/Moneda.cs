using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.Models.Catalogo
{
    public class Moneda
    {
        [Key]
        public int idMoneda { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Simbolo { get; set; } = string.Empty;
        public bool Activo { get; set; } = false;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;



    }
}

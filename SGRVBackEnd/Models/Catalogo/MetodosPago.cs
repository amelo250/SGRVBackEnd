using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.Models.Catalogo
{
    public class MetodosPago
    {

        [Key]
        public int IdMetodoPago { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;        
        public bool Activo { get; set; } = false;



    }
}

using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.Models.Catalogo
{
    public class Estado
    {
        [Key]
        public int IdEstado { get; set; }
      public string Categoria { get; set; } = string.Empty;
      public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
      public bool Activo { get; set; } 
      




    }
}

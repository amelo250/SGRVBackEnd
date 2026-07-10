using Microsoft.Data.SqlClient.DataClassification;
using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.Models.Catalogo
{
    public class Tipo
    {
        [Key]
        public  int IdTipo { get; set; }
        public  string Categoria { get; set; }=string.Empty;
        public  string Codigo {  get; set; }=string.Empty;
        public  string nombre {  get; set; }=string.Empty;
        public  bool Activo { get; set; }=false;

    }

}


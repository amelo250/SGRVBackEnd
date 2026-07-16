using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.Models.DocumentosCliente
{
    public class DocumentosCliente
    {
        [Key]
        public int IdDocumentoCliente { get; set; }
        public int IdCliente { get; set; }
        public int IdTipoDocumento { get; set; }
        public string Urldocumento { get; set; } = string.Empty;
        public DateTime FechaSubida { get; set; } = DateTime.Now;




    }
}

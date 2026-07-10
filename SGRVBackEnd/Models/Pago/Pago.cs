using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.Models.Pago
{
    public class Pago
    {
        [Key]
        public int IdPago { get; set; }
        public int IdRenta { get; set; } 
        public int IdEstado { get; set; } 
        public DateTime FechaPago { get; set; } 
        public double Monto { get; set; }
        public string Referencia { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;


    }
}

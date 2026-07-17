using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.Models.Reservaciones
{
    public class Reservacion
    {
        [Key]
        public int IdReservacion { get; set; }
        public int IdEmpresa { get; set; }
        public int IdVehiculo { get; set; }
        public int IdCliente { get; set; }
        public int IdEstado { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Observacion { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;




    }
}

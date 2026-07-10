using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.Models.Vehiculo
{
    public class Vehiculo
    {
        [Key]
        public int IdVehiculo { get; set; }
        public int IdEmpresa { get; set; }
        public int IdEstado { get; set; }
        public int IdCombustible { get; set; }
        public int IdTransmision { get; set; }
        public string Marca { get; set; } = string.Empty;

        public string Modelo { get; set; } = string.Empty;
        public int Anio { get; set; } 
        public string Placa { get; set; } = string.Empty;
        public string VIN { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int Kilometraje { get; set; }
        public double PrecioPorDia { get; set; }
        public double DepositoCombustible { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; } = false;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

    }
}

using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.Models.Catalogo
{
    public class Plan
    {
        [Key]
        public int IdPlan { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public double PrecioMensual { get; set; }
        public int MaxVehiculos { get; set; }
        public bool PermiteMantenimiento { get; set; }
        public bool PermiteReservas { get; set; }
        public bool PermiteReportes { get; set; }
        public bool PermiteCuentasPorCobrar { get; set; }

        public bool Activo { get; set; } = false;

    }
}

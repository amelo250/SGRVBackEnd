namespace SGRVBackEnd.Models
{
    public abstract class BaseEntity
    {
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        public DateTime? FechaActualizacion { get; set; }
        public bool Activo { get; set; } = true;
    }
}

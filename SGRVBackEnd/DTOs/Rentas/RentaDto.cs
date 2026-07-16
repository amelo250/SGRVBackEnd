namespace SGRVBackEnd.DTOs.Rentas
{
    public class RentaDto
    {
        public int Id { get; set; }

        public int ClienteId { get; set; }

        public string ClienteNombre { get; set; } = string.Empty;

        public int VehiculoId { get; set; }

        public string VehiculoDescripcion { get; set; } = string.Empty;

        public int UsuarioId { get; set; }

        public string UsuarioNombre { get; set; } = string.Empty;

        public DateTime FechaInicio { get; set; }

        public DateTime FechaFin { get; set; }

        public DateTime? FechaEntrega { get; set; }

        public decimal PrecioPorDia { get; set; }

        public decimal Deposito { get; set; }

        public decimal Total { get; set; }

        public string Estado { get; set; } = string.Empty;

        public string? Observaciones { get; set; }
    }
}

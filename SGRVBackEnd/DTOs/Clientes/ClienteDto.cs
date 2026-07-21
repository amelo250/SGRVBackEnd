namespace SGRVBackEnd.DTOs.Clientes
{
    public class ClienteDto
    {
        public int IdCliente { get; set; }
        public int IdEmpresa { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Apellido { get; set; } = string.Empty;

        public string CedulaPasaporte { get; set; } = string.Empty;
        public DateTime FechaNacimiento { get; set; }


        public string? Telefono { get; set; }

        public string? Email { get; set; }

        public string? Direccion { get; set; }
        public string? Nacionalidad { get; set; }
        public string? LicenciaConducir { get; set; }
        public DateTime FechaExpLicencia { get; set; }
        public DateTime FechaVencLicencia { get; set; }


        public bool Activo { get; set; }

        public DateTime FechaCreacion { get; set; }

    }
}

namespace SGRVBackEnd.DTOs.Empresas
{
    public class EmpresaDto
    {
        public int IdEmpresa { get; set; }
        public int IdPlan { get; set; }


        public string Nombre { get; set; } = string.Empty;
        public string NombreComercial { get; set; } = string.Empty;


        public string RNC { get; set; } = string.Empty;

        public string? Telefono { get; set; }

        public string? Correo { get; set; }

        public string? Direccion { get; set; }
        public string? LogoUrl { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime? FechaActualizacion { get; set; } 

        public bool Activo { get; set; }
    }
}

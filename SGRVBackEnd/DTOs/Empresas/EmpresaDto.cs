namespace SGRVBackEnd.DTOs.Empresas
{
    public class EmpresaDto
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Rnc { get; set; } = string.Empty;

        public string? Telefono { get; set; }

        public string? Correo { get; set; }

        public string? Direccion { get; set; }

        public bool Activo { get; set; }
    }
}

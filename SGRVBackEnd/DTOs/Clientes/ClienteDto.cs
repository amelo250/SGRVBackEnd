namespace SGRVBackEnd.DTOs.Clientes
{
    public class ClienteDto
    {
        public int Idcliente { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Apellido { get; set; } = string.Empty;

        public string Documento { get; set; } = string.Empty;

        public string? Telefono { get; set; }

        public string? Correo { get; set; }

        public string? Direccion { get; set; }

        public bool Activo { get; set; }

    }
}

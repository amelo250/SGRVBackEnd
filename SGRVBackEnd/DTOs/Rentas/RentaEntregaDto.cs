namespace SGRVBackEnd.DTOs.Rentas;

public sealed class RentaEntregaDto
{
    public int IdRenta { get; set; }
    public int IdVehiculo { get; set; }
    public string NumeroContrato { get; set; } = string.Empty;
    public RentaEntregaEmpresaDto Empresa { get; set; } = new();
    public RentaEntregaClienteDto Cliente { get; set; } = new();
    public RentaEntregaVehiculoDto Vehiculo { get; set; } = new();
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public decimal PrecioPorDiaPactado { get; set; }
    public int CantidadDias { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Impuestos { get; set; }
    public decimal Descuentos { get; set; }
    public decimal Deposito { get; set; }
    public decimal Total { get; set; }
    public string MonedaCodigo { get; set; } = string.Empty;
    public string MonedaSimbolo { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public IReadOnlyList<RentaEntregaPagoDto> Pagos { get; set; } = [];
    public IReadOnlyList<RentaEntregaAccesorioDto> Accesorios { get; set; } = [];
}

public sealed class RentaEntregaEmpresaDto
{
    public string NombreComercial { get; set; } = string.Empty;
    public string Rnc { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
}

public sealed class RentaEntregaClienteDto
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Nacionalidad { get; set; } = string.Empty;
    public string CedulaPasaporte { get; set; } = string.Empty;
    public string LicenciaConducir { get; set; } = string.Empty;
    public DateTime? FechaVencimientoLicencia { get; set; }
}

public sealed class RentaEntregaVehiculoDto
{
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int Anio { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string Vin { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public int Kilometraje { get; set; }
}

public sealed class RentaEntregaPagoDto
{
    public DateTime FechaPago { get; set; }
    public string Metodo { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string MonedaCodigo { get; set; } = string.Empty;
    public decimal MontoMonedaLocal { get; set; }
}

public sealed class RentaEntregaAccesorioDto
{
    public int IdAccesorio { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
}

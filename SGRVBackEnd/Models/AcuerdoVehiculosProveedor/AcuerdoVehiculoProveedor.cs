namespace SGRVBackEnd.Models.AcuerdoVehiculoProveedor
{
    public class AcuerdoVehiculoProveedor
    {
       
        
            public int IdAcuerdoVehiculo { get; set; }
            public int IdEmpresa { get; set; }
            public int IdVehiculo { get; set; }
            public int IdProveedorVehiculo { get; set; }
            public int IdMoneda { get; set; }

            public decimal CostoPorDia { get; set; }
            public decimal? CostoFijo { get; set; }

            public DateTime FechaInicioVigencia { get; set; }
            public DateTime? FechaFinVigencia { get; set; }

            public bool Activo { get; set; }
            public string? Observaciones { get; set; }
            public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGRVBackEnd.Models.AcuerdoVehiculoProveedor;

namespace SGRVBackEnd.Configurations;

public sealed class AcuerdoVehiculoProveedorConfiguration
    : IEntityTypeConfiguration<AcuerdoVehiculoProveedor>
{
    public void Configure(EntityTypeBuilder<AcuerdoVehiculoProveedor> entity)
    {
        entity.ToTable("AcuerdosVehiculosProveedor");
        entity.HasKey(x => x.IdAcuerdoVehiculo);
        entity.Property(x => x.CostoPorDia).HasPrecision(18, 2);
        entity.Property(x => x.CostoFijo).HasPrecision(18, 2);
        entity.Property(x => x.Observaciones).HasMaxLength(500);
        entity.HasIndex(x => new
        {
            x.IdEmpresa,
            x.IdVehiculo,
            x.IdProveedorVehiculo,
            x.Activo
        });
    }
}

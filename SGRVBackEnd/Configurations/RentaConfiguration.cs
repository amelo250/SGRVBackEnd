using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGRVBackEnd.Models.AcuerdoVehiculoProveedor;
using SGRVBackEnd.Models.Catalogo;
using SGRVBackEnd.Models.Cliente;
using SGRVBackEnd.Models.Empresa;
using SGRVBackEnd.Models.ProveedoresVehiculos;
using SGRVBackEnd.Models.Reservaciones;
using SGRVBackEnd.Models.Renta;
using SGRVBackEnd.Models.Usuarios;
using SGRVBackEnd.Models.Vehiculo;

namespace SGRVBackEnd.Configurations;

public sealed class RentaConfiguration : IEntityTypeConfiguration<Renta>
{
    public void Configure(EntityTypeBuilder<Renta> entity)
    {
        entity.ToTable("Rentas");
        entity.HasKey(x => x.IdRenta);

        entity.Property(x => x.Observaciones).HasMaxLength(500);
        entity.Property(x => x.PrecioPorDia).HasPrecision(18, 2);
        entity.Property(x => x.Subtotal).HasPrecision(18, 2);
        entity.Property(x => x.Impuestos).HasPrecision(18, 2);
        entity.Property(x => x.Descuentos).HasPrecision(18, 2);
        entity.Property(x => x.Total).HasPrecision(18, 2);
        entity.Property(x => x.Deposito).HasPrecision(18, 2);
        entity.Property(x => x.TasaCambioAplicada).HasPrecision(18, 6);
        entity.Property(x => x.TotalMonedaLocal).HasPrecision(18, 2);
        entity.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        entity.HasIndex(x => new { x.IdEmpresa, x.IdEstado, x.FechaInicio });
        entity.HasIndex(x => new { x.IdEmpresa, x.IdVehiculo, x.FechaInicio, x.FechaFin });
        entity.HasIndex(x => new { x.IdEmpresa, x.IdCliente, x.FechaInicio });
        entity.HasIndex(x => new { x.IdEmpresa, x.IdReservacion })
            .IsUnique()
            .HasFilter("[IdReservacion] IS NOT NULL");

        entity.HasOne<Empresa>().WithMany().HasForeignKey(x => x.IdEmpresa)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Reservacion>().WithMany().HasForeignKey(x => x.IdReservacion)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Vehiculo>().WithMany().HasForeignKey(x => x.IdVehiculo)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Cliente>().WithMany().HasForeignKey(x => x.IdCliente)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Estado>().WithMany().HasForeignKey(x => x.IdEstado)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Usuario>().WithMany().HasForeignKey(x => x.IdUsuarioCreacion)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Moneda>().WithMany().HasForeignKey(x => x.IdMoneda)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ProveedorVehiculo>().WithMany().HasForeignKey(x => x.IdProveedorVehiculo)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<AcuerdoVehiculoProveedor>().WithMany().HasForeignKey(x => x.IdAcuerdoVehiculo)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

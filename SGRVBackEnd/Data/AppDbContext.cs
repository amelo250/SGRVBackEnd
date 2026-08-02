using Microsoft.EntityFrameworkCore;
using SGRVBackEnd.Models;
using SGRVBackEnd.Models.Catalogo;
using SGRVBackEnd.Models.Pago;

namespace SGRVBackEnd.Data
{
    public class AppDbContext:DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Models.Usuarios.Usuario> Usuarios=> Set<Models.Usuarios.Usuario>();
        public DbSet<Models.Empresa.Empresa> Empresas => Set<Models.Empresa.Empresa>();
        public DbSet<Rol> Roles => Set<Rol>();
        public DbSet<Plan> Planes => Set<Plan>();
        public DbSet<Estado> Estados => Set<Estado>();
        public DbSet<Tipo> Tipos => Set<Tipo>();

        public DbSet<Models.Vehiculo.Vehiculo> Vehiculos => Set<Models.Vehiculo.Vehiculo>();
        public DbSet<Models.Cliente.Cliente> Clientes => Set < Models.Cliente.Cliente>();
        public DbSet<Models.Renta.Renta> Rentas => Set<Models.Renta.Renta>();

        public DbSet<Models.Catalogo.MetodoPago> MetodosPago => Set<Models.Catalogo.MetodoPago>();
        public DbSet<Models.Catalogo.Combustibles> Combustibles => Set<Models.Catalogo.Combustibles>();
        public DbSet<Models.Catalogo.Transmisiones> Transmisiones => Set<Models.Catalogo.Transmisiones>();
        public DbSet<Models.Catalogo.Moneda> Monedas => Set<Models.Catalogo.Moneda>();
        public DbSet<Models.Pago.Pago> Pagos => Set<Models.Pago.Pago>();

        public DbSet<Models.ProveedoresVehiculos.ProveedorVehiculo> ProveedoresVehiculos => Set<Models.ProveedoresVehiculos.ProveedorVehiculo>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Models.Usuarios.Usuario>(entity =>
             {
             entity.ToTable("Usuarios");
             entity.HasKey(e => e.IdUsuario);
                 entity.Property(e => e.IdUsuario).HasColumnName("Idusuario");
                 entity.Property(e => e.IdEmpresa).IsRequired();
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.IdRol).IsRequired();
                entity.Property(e => e.Activo).IsRequired();
            });
            modelBuilder.Entity<Moneda>(entity =>
            {
                entity.ToTable("Monedas");

                entity.HasKey(x => x.IdMoneda);

                entity.Property(x => x.IdMoneda)
                    .HasColumnName("idMoneda");

                entity.Property(x => x.Codigo)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(x => x.Nombre)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.Simbolo)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(x => x.Activo)
                    .IsRequired();
            });

            modelBuilder.Entity<Pago>(entity =>
            {
                entity.ToTable("Pagos");

                entity.HasKey(x => x.IdPago);

                entity.Property(x => x.Monto)
                    .HasPrecision(18, 2);

                entity.Property(x => x.TasaCambioAplicada)
                    .HasPrecision(18, 6);

                entity.Property(x => x.MontoMonedaLocal)
                    .HasPrecision(18, 2);

                entity.HasOne(x => x.Moneda)
                    .WithMany()
                    .HasForeignKey(x => x.IdMoneda)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Estado)
                    .WithMany()
                    .HasForeignKey(x => x.IdEstado)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.MetodoPago)
                    .WithMany()
                    .HasForeignKey(x => x.IdMetodoPago)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Renta)
                    .WithMany()
                    .HasForeignKey(x => x.IdRenta)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }





    }


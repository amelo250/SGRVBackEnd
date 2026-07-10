using Microsoft.EntityFrameworkCore;
using SGRVBackEnd.Models;
using SGRVBackEnd.Models.Catalogo;

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

        public DbSet<Models.Catalogo.MetodosPago> MetodosPago => Set<Models.Catalogo.MetodosPago>();
        public DbSet<Models.Catalogo.Combustibles> Combustibles => Set<Models.Catalogo.Combustibles>();
        public DbSet<Models.Catalogo.Transmisiones> Transmisiones => Set<Models.Catalogo.Transmisiones>();
        public DbSet<Models.Pago.Pago> Pagos => Set<Models.Pago.Pago>();
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
        }





    }
}

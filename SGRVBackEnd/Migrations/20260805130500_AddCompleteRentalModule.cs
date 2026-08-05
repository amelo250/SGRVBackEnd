using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SGRVBackEnd.Data;

#nullable disable

namespace SGRVBackEnd.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260805130500_AddCompleteRentalModule")]
public partial class AddCompleteRentalModule : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM Estados WHERE Categoria = 'RENTA' AND Codigo = 'ACTIVA')
                INSERT INTO Estados (Categoria, Codigo, Nombre, Activo)
                VALUES ('RENTA', 'ACTIVA', 'Activa', 1);

            IF NOT EXISTS (SELECT 1 FROM Estados WHERE Categoria = 'RENTA' AND Codigo = 'FINALIZADA')
                INSERT INTO Estados (Categoria, Codigo, Nombre, Activo)
                VALUES ('RENTA', 'FINALIZADA', 'Finalizada', 1);

            IF NOT EXISTS (SELECT 1 FROM Estados WHERE Categoria = 'RENTA' AND Codigo = 'CANCELADA')
                INSERT INTO Estados (Categoria, Codigo, Nombre, Activo)
                VALUES ('RENTA', 'CANCELADA', 'Cancelada', 1);
            """);

        migrationBuilder.AddColumn<int>(
            name: "IdReservacion",
            table: "Rentas",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "FechaActualizacion",
            table: "Rentas",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<byte[]>(
            name: "RowVersion",
            table: "Rentas",
            type: "rowversion",
            rowVersion: true,
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_Rentas_IdEmpresa_IdCliente_FechaInicio",
            table: "Rentas",
            columns: new[] { "IdEmpresa", "IdCliente", "FechaInicio" });

        migrationBuilder.CreateIndex(
            name: "IX_Rentas_IdEmpresa_IdEstado_FechaInicio",
            table: "Rentas",
            columns: new[] { "IdEmpresa", "IdEstado", "FechaInicio" });

        migrationBuilder.CreateIndex(
            name: "IX_Rentas_IdEmpresa_IdVehiculo_FechaInicio_FechaFin",
            table: "Rentas",
            columns: new[] { "IdEmpresa", "IdVehiculo", "FechaInicio", "FechaFin" });

        migrationBuilder.CreateIndex(
            name: "IX_Rentas_IdEmpresa_IdReservacion",
            table: "Rentas",
            columns: new[] { "IdEmpresa", "IdReservacion" },
            unique: true,
            filter: "[IdReservacion] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Rentas_IdAcuerdoVehiculo",
            table: "Rentas",
            column: "IdAcuerdoVehiculo");

        migrationBuilder.CreateIndex(
            name: "IX_Rentas_IdCliente",
            table: "Rentas",
            column: "IdCliente");

        migrationBuilder.CreateIndex(
            name: "IX_Rentas_IdEmpresa",
            table: "Rentas",
            column: "IdEmpresa");

        migrationBuilder.CreateIndex(
            name: "IX_Rentas_IdEstado",
            table: "Rentas",
            column: "IdEstado");

        migrationBuilder.CreateIndex(
            name: "IX_Rentas_IdMoneda",
            table: "Rentas",
            column: "IdMoneda");

        migrationBuilder.CreateIndex(
            name: "IX_Rentas_IdProveedorVehiculo",
            table: "Rentas",
            column: "IdProveedorVehiculo");

        migrationBuilder.CreateIndex(
            name: "IX_Rentas_IdReservacion",
            table: "Rentas",
            column: "IdReservacion");

        migrationBuilder.CreateIndex(
            name: "IX_Rentas_IdUsuarioCreacion",
            table: "Rentas",
            column: "IdUsuarioCreacion");

        migrationBuilder.CreateIndex(
            name: "IX_Rentas_IdVehiculo",
            table: "Rentas",
            column: "IdVehiculo");

        migrationBuilder.AddForeignKey(
            name: "FK_Rentas_AcuerdosVehiculosProveedor_IdAcuerdoVehiculo",
            table: "Rentas",
            column: "IdAcuerdoVehiculo",
            principalTable: "AcuerdosVehiculosProveedor",
            principalColumn: "IdAcuerdoVehiculo",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Rentas_Clientes_IdCliente",
            table: "Rentas",
            column: "IdCliente",
            principalTable: "Clientes",
            principalColumn: "IdCliente",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Rentas_Empresas_IdEmpresa",
            table: "Rentas",
            column: "IdEmpresa",
            principalTable: "Empresas",
            principalColumn: "IdEmpresa",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Rentas_Estados_IdEstado",
            table: "Rentas",
            column: "IdEstado",
            principalTable: "Estados",
            principalColumn: "IdEstado",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Rentas_Monedas_IdMoneda",
            table: "Rentas",
            column: "IdMoneda",
            principalTable: "Monedas",
            principalColumn: "IdMoneda",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Rentas_ProveedoresVehiculos_IdProveedorVehiculo",
            table: "Rentas",
            column: "IdProveedorVehiculo",
            principalTable: "ProveedoresVehiculos",
            principalColumn: "IdProveedorVehiculo",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Rentas_Reservaciones_IdReservacion",
            table: "Rentas",
            column: "IdReservacion",
            principalTable: "Reservaciones",
            principalColumn: "IdReservacion",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Rentas_Usuarios_IdUsuarioCreacion",
            table: "Rentas",
            column: "IdUsuarioCreacion",
            principalTable: "Usuarios",
            principalColumn: "IdUsuario",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Rentas_Vehiculos_IdVehiculo",
            table: "Rentas",
            column: "IdVehiculo",
            principalTable: "Vehiculos",
            principalColumn: "IdVehiculo",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_Rentas_AcuerdosVehiculosProveedor_IdAcuerdoVehiculo", "Rentas");
        migrationBuilder.DropForeignKey("FK_Rentas_Clientes_IdCliente", "Rentas");
        migrationBuilder.DropForeignKey("FK_Rentas_Empresas_IdEmpresa", "Rentas");
        migrationBuilder.DropForeignKey("FK_Rentas_Estados_IdEstado", "Rentas");
        migrationBuilder.DropForeignKey("FK_Rentas_Monedas_IdMoneda", "Rentas");
        migrationBuilder.DropForeignKey("FK_Rentas_ProveedoresVehiculos_IdProveedorVehiculo", "Rentas");
        migrationBuilder.DropForeignKey("FK_Rentas_Reservaciones_IdReservacion", "Rentas");
        migrationBuilder.DropForeignKey("FK_Rentas_Usuarios_IdUsuarioCreacion", "Rentas");
        migrationBuilder.DropForeignKey("FK_Rentas_Vehiculos_IdVehiculo", "Rentas");

        migrationBuilder.DropIndex("IX_Rentas_IdEmpresa_IdCliente_FechaInicio", "Rentas");
        migrationBuilder.DropIndex("IX_Rentas_IdEmpresa_IdEstado_FechaInicio", "Rentas");
        migrationBuilder.DropIndex("IX_Rentas_IdEmpresa_IdVehiculo_FechaInicio_FechaFin", "Rentas");
        migrationBuilder.DropIndex("IX_Rentas_IdEmpresa_IdReservacion", "Rentas");
        migrationBuilder.DropIndex("IX_Rentas_IdAcuerdoVehiculo", "Rentas");
        migrationBuilder.DropIndex("IX_Rentas_IdCliente", "Rentas");
        migrationBuilder.DropIndex("IX_Rentas_IdEmpresa", "Rentas");
        migrationBuilder.DropIndex("IX_Rentas_IdEstado", "Rentas");
        migrationBuilder.DropIndex("IX_Rentas_IdMoneda", "Rentas");
        migrationBuilder.DropIndex("IX_Rentas_IdProveedorVehiculo", "Rentas");
        migrationBuilder.DropIndex("IX_Rentas_IdReservacion", "Rentas");
        migrationBuilder.DropIndex("IX_Rentas_IdUsuarioCreacion", "Rentas");
        migrationBuilder.DropIndex("IX_Rentas_IdVehiculo", "Rentas");

        migrationBuilder.DropColumn("IdReservacion", "Rentas");
        migrationBuilder.DropColumn("FechaActualizacion", "Rentas");
        migrationBuilder.DropColumn("RowVersion", "Rentas");
    }
}

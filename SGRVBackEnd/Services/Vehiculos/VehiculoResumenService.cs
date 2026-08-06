using System.Data;
using Microsoft.Data.SqlClient;
using SGRVBackEnd.DTOs.Vehiculos;

namespace SGRVBackEnd.Services.Vehiculos;

public sealed class VehiculoResumenService : IVehiculoResumenService
{
    private readonly string _connectionString;

    public VehiculoResumenService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "No se encontró la conexión DefaultConnection.");
    }

    public async Task<VehiculoResumenFinancieroDto?> GetAsync(
        int idVehiculo,
        int idEmpresa,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.Vehiculos
                WHERE IdVehiculo = @IdVehiculo AND IdEmpresa = @IdEmpresa
            )
            BEGIN
                SELECT CAST(0 AS bit) AS Existe,
                       CAST(0 AS decimal(18,2)) AS Ingresos,
                       CAST(0 AS decimal(18,2)) AS Gastos,
                       0 AS CantidadRentas,
                       CAST(NULL AS datetime2) AS UltimaRenta,
                       CAST(NULL AS date) AS UltimoGasto;
                RETURN;
            END;

            SELECT CAST(1 AS bit) AS Existe,
                   COALESCE
                   (
                       (
                           SELECT SUM(p.MontoMonedaLocal)
                           FROM dbo.Pagos AS p
                           INNER JOIN dbo.Rentas AS r
                             ON r.IdRenta = p.IdRenta
                            AND r.IdEmpresa = p.IdEmpresa
                           WHERE p.IdEmpresa = @IdEmpresa
                             AND r.IdVehiculo = @IdVehiculo
                             AND p.Activo = 1
                       ), 0
                   ) AS Ingresos,
                   COALESCE
                   (
                       (
                           SELECT SUM(g.MontoMonedaLocal)
                           FROM dbo.Gastos AS g
                           WHERE g.IdEmpresa = @IdEmpresa
                             AND g.IdVehiculo = @IdVehiculo
                             AND g.Activo = 1
                       ), 0
                   ) AS Gastos,
                   (
                       SELECT COUNT(1)
                       FROM dbo.Rentas AS r
                       WHERE r.IdEmpresa = @IdEmpresa
                         AND r.IdVehiculo = @IdVehiculo
                   ) AS CantidadRentas,
                   (
                       SELECT MAX(r.FechaInicio)
                       FROM dbo.Rentas AS r
                       WHERE r.IdEmpresa = @IdEmpresa
                         AND r.IdVehiculo = @IdVehiculo
                   ) AS UltimaRenta,
                   (
                       SELECT MAX(g.Fecha)
                       FROM dbo.Gastos AS g
                       WHERE g.IdEmpresa = @IdEmpresa
                         AND g.IdVehiculo = @IdVehiculo
                         AND g.Activo = 1
                   ) AS UltimoGasto;
            """;
        command.Parameters.Add("@IdVehiculo", SqlDbType.Int).Value = idVehiculo;
        command.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken) || !reader.GetBoolean(0))
            return null;

        var ingresos = reader.GetDecimal(reader.GetOrdinal("Ingresos"));
        var gastos = reader.GetDecimal(reader.GetOrdinal("Gastos"));
        var cantidad = reader.GetInt32(reader.GetOrdinal("CantidadRentas"));

        return new VehiculoResumenFinancieroDto
        {
            IdVehiculo = idVehiculo,
            IngresosMonedaLocal = ingresos,
            GastosMonedaLocal = gastos,
            ResultadoNeto = ingresos - gastos,
            CantidadRentas = cantidad,
            IngresoPromedioPorRenta = cantidad == 0
                ? 0
                : Math.Round(ingresos / cantidad, 2),
            UltimaRenta = GetNullableDateTime(reader, "UltimaRenta"),
            UltimoGasto = GetNullableDateTime(reader, "UltimoGasto")
        };
    }

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}

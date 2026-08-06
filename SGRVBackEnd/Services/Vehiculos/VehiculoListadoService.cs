using System.Data;
using Microsoft.Data.SqlClient;
using SGRVBackEnd.DTOs.Vehiculos;
using SGRVBackEnd.Enums;

namespace SGRVBackEnd.Services.Vehiculos;

public sealed class VehiculoListadoService : IVehiculoListadoService
{
    private readonly string _connectionString;

    public VehiculoListadoService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No se encontró DefaultConnection.");
    }

    public async Task<IReadOnlyList<VehiculoDto>> GetAllAsync(
        int idEmpresa,
        VehiculoSearchDto search,
        CancellationToken cancellationToken)
    {
        var conditions = new List<string> { "v.IdEmpresa = @IdEmpresa" };
        if (!search.IncluirInactivos) conditions.Add("v.Activo = 1");
        if (search.IdTipo.HasValue) conditions.Add("v.IdTipo = @IdTipo");
        if (search.IdCombustible.HasValue)
            conditions.Add("v.IdCombustible = @IdCombustible");
        if (!string.IsNullOrWhiteSpace(search.Marca))
            conditions.Add("UPPER(v.Marca) = @Marca");

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT v.IdVehiculo,v.IdEstado,v.IdCombustible,v.IdTransmision,v.IdTipo,
                   v.TipoPropiedad,v.IdProveedorVehiculo,v.IdMonedaTarifa,
                   v.Marca,v.Modelo,v.Anio,v.Placa,v.VIN,v.Color,v.Kilometraje,
                   v.PrecioPorDia,v.DepositoCombustible,v.Descripcion,v.Activo,
                   v.FechaCreacion,t.nombre AS TipoNombre,
                   c.nombre AS CombustibleNombre,portada.Url AS FotoPortadaUrl
              FROM dbo.Vehiculos AS v
              LEFT JOIN dbo.Tipos AS t ON t.IdTipo = v.IdTipo
              LEFT JOIN dbo.Combustibles AS c ON c.IdCombustible = v.IdCombustible
              OUTER APPLY
              (
                  SELECT TOP (1) f.Url
                    FROM dbo.FotosVehiculo AS f
                   WHERE f.IdEmpresa = v.IdEmpresa
                     AND f.IdVehiculo = v.IdVehiculo
                     AND f.Activo = 1
                   ORDER BY f.EsPrincipal DESC,f.Orden,f.IdFoto
              ) AS portada
             WHERE {string.Join(" AND ", conditions)}
             ORDER BY v.Marca,v.Modelo,v.Anio DESC;
            """;
        AddParameters(command, idEmpresa, search);

        var result = new List<VehiculoDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(Map(reader));
        return result;
    }

    public async Task<IReadOnlyList<string>> GetMarcasAsync(
        int idEmpresa,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT DISTINCT LTRIM(RTRIM(Marca)) AS Marca
              FROM dbo.Vehiculos
             WHERE IdEmpresa=@IdEmpresa AND Activo=1
               AND NULLIF(LTRIM(RTRIM(Marca)),'') IS NOT NULL
             ORDER BY Marca;
            """;
        command.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;

        var result = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(reader.GetString(0));
        return result;
    }

    private static void AddParameters(
        SqlCommand command,
        int idEmpresa,
        VehiculoSearchDto search)
    {
        command.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
        if (search.IdTipo.HasValue)
            command.Parameters.Add("@IdTipo", SqlDbType.Int).Value = search.IdTipo.Value;
        if (search.IdCombustible.HasValue)
            command.Parameters.Add("@IdCombustible", SqlDbType.Int).Value =
                search.IdCombustible.Value;
        if (!string.IsNullOrWhiteSpace(search.Marca))
            command.Parameters.Add("@Marca", SqlDbType.NVarChar, 80).Value =
                search.Marca.Trim().ToUpperInvariant();
    }

    private static VehiculoDto Map(SqlDataReader reader) => new()
    {
        IdVehiculo = reader.GetInt32(reader.GetOrdinal("IdVehiculo")),
        IdEstado = reader.GetInt32(reader.GetOrdinal("IdEstado")),
        IdCombustible = reader.GetInt32(reader.GetOrdinal("IdCombustible")),
        IdTransmision = reader.GetInt32(reader.GetOrdinal("IdTransmision")),
        IdTipo = reader.GetInt32(reader.GetOrdinal("IdTipo")),
        TipoPropiedad = (TipoPropiedadVehiculo)Convert.ToInt32(
            reader.GetValue(reader.GetOrdinal("TipoPropiedad"))),
        IdProveedorVehiculo = reader.IsDBNull(reader.GetOrdinal("IdProveedorVehiculo"))
            ? null : reader.GetInt32(reader.GetOrdinal("IdProveedorVehiculo")),
        IdMonedaTarifa = reader.GetInt32(reader.GetOrdinal("IdMonedaTarifa")),
        Marca = reader.GetString(reader.GetOrdinal("Marca")),
        Modelo = reader.GetString(reader.GetOrdinal("Modelo")),
        Anio = reader.GetInt32(reader.GetOrdinal("Anio")),
        Placa = reader.GetString(reader.GetOrdinal("Placa")),
        VIN = reader.IsDBNull(reader.GetOrdinal("VIN"))
            ? null : reader.GetString(reader.GetOrdinal("VIN")),
        Color = reader.IsDBNull(reader.GetOrdinal("Color"))
            ? null : reader.GetString(reader.GetOrdinal("Color")),
        Kilometraje = reader.GetInt32(reader.GetOrdinal("Kilometraje")),
        PrecioPorDia = reader.GetDecimal(reader.GetOrdinal("PrecioPorDia")),
        DepositoCombustible = reader.GetDecimal(
            reader.GetOrdinal("DepositoCombustible")),
        Descripcion = reader.IsDBNull(reader.GetOrdinal("Descripcion"))
            ? null : reader.GetString(reader.GetOrdinal("Descripcion")),
        Activo = reader.GetBoolean(reader.GetOrdinal("Activo")),
        FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
        TipoNombre = reader.IsDBNull(reader.GetOrdinal("TipoNombre"))
            ? null : reader.GetString(reader.GetOrdinal("TipoNombre")),
        CombustibleNombre = reader.IsDBNull(reader.GetOrdinal("CombustibleNombre"))
            ? null : reader.GetString(reader.GetOrdinal("CombustibleNombre")),
        FotoPortadaUrl = reader.IsDBNull(reader.GetOrdinal("FotoPortadaUrl"))
            ? null : reader.GetString(reader.GetOrdinal("FotoPortadaUrl"))
    };
}

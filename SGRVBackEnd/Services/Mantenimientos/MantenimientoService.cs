using System.Data;
using Microsoft.Data.SqlClient;
using SGRVBackEnd.DTOs.Mantenimientos;

namespace SGRVBackEnd.Services.Mantenimientos;

public sealed class MantenimientoService : IMantenimientoService
{
    private const string SelectSql = """
        SELECT m.IdMantenimiento, m.IdVehiculo,
               v.Marca + ' ' + v.Modelo + ' (' + v.Placa + ')' AS Vehiculo,
               m.IdTipoMantenimiento, t.Codigo AS TipoCodigo,
               t.nombre AS TipoNombre, m.Fecha, m.Taller, m.Kilometraje,
               m.Costo, m.Observacion
          FROM dbo.Mantenimientos AS m
          INNER JOIN dbo.Vehiculos AS v ON v.IdVehiculo = m.IdVehiculo
          INNER JOIN dbo.Tipos AS t ON t.IdTipo = m.IdTipoMantenimiento
        """;

    private readonly string _connectionString;

    public MantenimientoService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No existe DefaultConnection.");
    }

    public async Task<(IReadOnlyList<MantenimientoResponseDto> Items, int Total)> GetAllAsync(
        int idEmpresa, MantenimientoSearchDto search, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var where = new List<string> { "v.IdEmpresa = @IdEmpresa" };
        if (search.IdVehiculo.HasValue) where.Add("m.IdVehiculo = @IdVehiculo");
        if (search.IdTipoMantenimiento.HasValue)
            where.Add("m.IdTipoMantenimiento = @IdTipo");
        if (search.FechaDesde.HasValue) where.Add("m.Fecha >= @FechaDesde");
        if (search.FechaHasta.HasValue) where.Add("m.Fecha < DATEADD(day, 1, @FechaHasta)");
        if (!string.IsNullOrWhiteSpace(search.Search))
            where.Add("(v.Marca LIKE @Search OR v.Modelo LIKE @Search OR v.Placa LIKE @Search OR t.nombre LIKE @Search OR m.Taller LIKE @Search)");
        var predicate = string.Join(" AND ", where);

        await using var count = connection.CreateCommand();
        count.CommandText = $"SELECT COUNT(1) FROM dbo.Mantenimientos m INNER JOIN dbo.Vehiculos v ON v.IdVehiculo=m.IdVehiculo INNER JOIN dbo.Tipos t ON t.IdTipo=m.IdTipoMantenimiento WHERE {predicate};";
        AddSearchParameters(count, idEmpresa, search);
        var total = Convert.ToInt32(await count.ExecuteScalarAsync(cancellationToken));

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            {SelectSql}
            WHERE {predicate}
            ORDER BY m.Fecha DESC, m.IdMantenimiento DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;
        AddSearchParameters(command, idEmpresa, search);
        command.Parameters.Add("@Offset", SqlDbType.Int).Value =
            (search.PageNumber - 1) * search.PageSize;
        command.Parameters.Add("@PageSize", SqlDbType.Int).Value = search.PageSize;
        var items = new List<MantenimientoResponseDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) items.Add(Map(reader));
        return (items, total);
    }

    public async Task<MantenimientoResponseDto?> GetByIdAsync(
        int id, int idEmpresa, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return await LoadAsync(connection, id, idEmpresa, cancellationToken);
    }

    public async Task<MantenimientoResponseDto> CreateAsync(
    int idEmpresa,
    MantenimientoCreateDto request,
    CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await ValidateRelatedAsync(
            connection,
            idEmpresa,
            request,
            cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = """
        INSERT INTO dbo.Mantenimientos
            (
                IdEmpresa,
                IdVehiculo,
                IdTipoMantenimiento,
                Fecha,
                Taller,
                Kilometraje,
                Costo,
                Observacion
            )
        OUTPUT INSERTED.IdMantenimiento
        VALUES
            (
                @IdEmpresa,
                @IdVehiculo,
                @IdTipo,
                @Fecha,
                @Taller,
                @Kilometraje,
                @Costo,
                @Observacion
            );
        """;

        command.Parameters.Add(
            "@IdEmpresa",
            SqlDbType.Int
        ).Value = idEmpresa;

        AddWriteParameters(command, request);

        var idMantenimiento = Convert.ToInt32(
            await command.ExecuteScalarAsync(cancellationToken));

        return await LoadAsync(
            connection,
            idMantenimiento,
            idEmpresa,
            cancellationToken)
            ?? throw new InvalidOperationException(
                "No fue posible recuperar el mantenimiento creado.");
    }

    public async Task<MantenimientoResponseDto?> UpdateAsync(
        int id, int idEmpresa, MantenimientoUpdateDto request,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        if (await LoadAsync(connection, id, idEmpresa, cancellationToken) is null) return null;
        await ValidateRelatedAsync(connection, idEmpresa, request, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE m SET IdVehiculo=@IdVehiculo, IdTipoMantenimiento=@IdTipo,
                Fecha=@Fecha, Taller=@Taller, Kilometraje=@Kilometraje,
                Costo=@Costo, Observacion=@Observacion
              FROM dbo.Mantenimientos m
              INNER JOIN dbo.Vehiculos v ON v.IdVehiculo=m.IdVehiculo
             WHERE m.IdMantenimiento=@Id AND v.IdEmpresa=@IdEmpresa;
            """;
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        command.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
        AddWriteParameters(command, request);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return await LoadAsync(connection, id, idEmpresa, cancellationToken);
    }

    public async Task<MantenimientoResumenDto> GetSummaryAsync(
        int idEmpresa, int intervaloDias, int intervaloKilometros,
        int diasAlerta, int kilometrosAlerta, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            WITH Ultimo AS (
                SELECT m.IdVehiculo, m.Fecha, m.Kilometraje,
                       ROW_NUMBER() OVER(PARTITION BY m.IdVehiculo ORDER BY m.Fecha DESC, m.IdMantenimiento DESC) rn
                  FROM dbo.Mantenimientos m
                  INNER JOIN dbo.Vehiculos v ON v.IdVehiculo=m.IdVehiculo
                 WHERE v.IdEmpresa=@IdEmpresa
            )
            SELECT v.IdVehiculo, v.Marca + ' ' + v.Modelo + ' (' + v.Placa + ')' Vehiculo,
                   v.Kilometraje KilometrajeActual, u.Fecha UltimaFecha,
                   u.Kilometraje UltimoKilometraje
              FROM dbo.Vehiculos v
              LEFT JOIN Ultimo u ON u.IdVehiculo=v.IdVehiculo AND u.rn=1
             WHERE v.IdEmpresa=@IdEmpresa AND v.Activo=1
             ORDER BY v.Marca, v.Modelo;
            """;
        command.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
        var alerts = new List<MantenimientoAlertaDto>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var currentKm = reader.GetInt32(reader.GetOrdinal("KilometrajeActual"));
                var lastDate = reader.IsDBNull(reader.GetOrdinal("UltimaFecha"))
                    ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("UltimaFecha"));
                var lastKm = reader.IsDBNull(reader.GetOrdinal("UltimoKilometraje"))
                    ? (int?)null : reader.GetInt32(reader.GetOrdinal("UltimoKilometraje"));
                var nextDate = lastDate?.AddDays(intervaloDias);
                var nextKm = lastKm + intervaloKilometros;
                var level = "AL_DIA";
                var message = "Mantenimiento dentro de los umbrales configurados.";
                if (!lastDate.HasValue)
                {
                    level = "SIN_HISTORIAL";
                    message = "El vehículo no tiene mantenimientos registrados.";
                }
                else if ((nextDate.HasValue && nextDate.Value.Date < DateTime.UtcNow.Date) ||
                         (nextKm.HasValue && currentKm >= nextKm.Value))
                {
                    level = "VENCIDO";
                    message = "El mantenimiento estimado se encuentra vencido.";
                }
                else if ((nextDate.HasValue && nextDate.Value <= DateTime.UtcNow.AddDays(diasAlerta)) ||
                         (nextKm.HasValue && nextKm.Value - currentKm <= kilometrosAlerta))
                {
                    level = "PROXIMO";
                    message = "El mantenimiento se aproxima por fecha o kilometraje.";
                }
                alerts.Add(new MantenimientoAlertaDto
                {
                    IdVehiculo = reader.GetInt32(reader.GetOrdinal("IdVehiculo")),
                    Vehiculo = reader.GetString(reader.GetOrdinal("Vehiculo")),
                    KilometrajeActual = currentKm,
                    UltimaFecha = lastDate,
                    UltimoKilometraje = lastKm,
                    ProximaFechaEstimada = nextDate,
                    ProximoKilometrajeEstimado = nextKm,
                    Nivel = level,
                    Mensaje = message
                });
            }
        }

        await using var totals = connection.CreateCommand();
        totals.CommandText = """
            SELECT COUNT(1), COALESCE(SUM(m.Costo),0)
              FROM dbo.Mantenimientos m
              INNER JOIN dbo.Vehiculos v ON v.IdVehiculo=m.IdVehiculo
             WHERE v.IdEmpresa=@IdEmpresa;
            """;
        totals.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
        await using var totalsReader = await totals.ExecuteReaderAsync(cancellationToken);
        await totalsReader.ReadAsync(cancellationToken);
        return new MantenimientoResumenDto
        {
            TotalRegistros = totalsReader.GetInt32(0),
            CostoTotal = totalsReader.GetDecimal(1),
            VehiculosSinHistorial = alerts.Count(x => x.Nivel == "SIN_HISTORIAL"),
            AlertasProximas = alerts.Count(x => x.Nivel == "PROXIMO"),
            AlertasVencidas = alerts.Count(x => x.Nivel == "VENCIDO"),
            Alertas = alerts.Where(x => x.Nivel != "AL_DIA").ToList()
        };
    }

    public Task<IReadOnlyList<MantenimientoCatalogoDto>> GetTypesAsync(
        CancellationToken cancellationToken) => CatalogAsync(
            "SELECT IdTipo, Codigo, nombre FROM dbo.Tipos WHERE Activo=1 AND UPPER(Categoria) LIKE '%MANTEN%' ORDER BY nombre;",
            null, cancellationToken);

    public Task<IReadOnlyList<MantenimientoCatalogoDto>> GetVehiclesAsync(
        int idEmpresa, CancellationToken cancellationToken) => CatalogAsync(
            "SELECT IdVehiculo, Placa, Marca + ' ' + Modelo + ' (' + Placa + ')' FROM dbo.Vehiculos WHERE IdEmpresa=@IdEmpresa AND Activo=1 ORDER BY Marca, Modelo;",
            idEmpresa, cancellationToken);

    private async Task<IReadOnlyList<MantenimientoCatalogoDto>> CatalogAsync(
        string sql, int? idEmpresa, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (idEmpresa.HasValue)
            command.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa.Value;
        var result = new List<MantenimientoCatalogoDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new MantenimientoCatalogoDto
            {
                Id = reader.GetInt32(0),
                Codigo = reader.GetString(1),
                Nombre = reader.GetString(2)
            });
        return result;
    }

    private static async Task<MantenimientoResponseDto?> LoadAsync(
        SqlConnection connection, int id, int idEmpresa, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"{SelectSql} WHERE m.IdMantenimiento=@Id AND v.IdEmpresa=@IdEmpresa;";
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        command.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    private static async Task ValidateRelatedAsync(
        SqlConnection connection, int idEmpresa, MantenimientoCreateDto request,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CASE WHEN EXISTS(SELECT 1 FROM dbo.Vehiculos WHERE IdVehiculo=@IdVehiculo AND IdEmpresa=@IdEmpresa AND Activo=1) THEN 1 ELSE 0 END,
                   CASE WHEN EXISTS(SELECT 1 FROM dbo.Tipos WHERE IdTipo=@IdTipo AND Activo=1 AND UPPER(Categoria) LIKE '%MANTEN%') THEN 1 ELSE 0 END;
            """;
        command.Parameters.Add("@IdVehiculo", SqlDbType.Int).Value = request.IdVehiculo;
        command.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
        command.Parameters.Add("@IdTipo", SqlDbType.Int).Value = request.IdTipoMantenimiento;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        if (reader.GetInt32(0) != 1) throw new InvalidOperationException("El vehículo no existe en la empresa o está inactivo.");
        if (reader.GetInt32(1) != 1) throw new InvalidOperationException("El tipo de mantenimiento no es válido o está inactivo.");
    }

    private static void AddSearchParameters(
        SqlCommand command, int idEmpresa, MantenimientoSearchDto search)
    {
        command.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
        if (search.IdVehiculo.HasValue)
            command.Parameters.Add("@IdVehiculo", SqlDbType.Int).Value = search.IdVehiculo.Value;
        if (search.IdTipoMantenimiento.HasValue)
            command.Parameters.Add("@IdTipo", SqlDbType.Int).Value = search.IdTipoMantenimiento.Value;
        if (search.FechaDesde.HasValue)
            command.Parameters.Add("@FechaDesde", SqlDbType.DateTime2).Value = search.FechaDesde.Value;
        if (search.FechaHasta.HasValue)
            command.Parameters.Add("@FechaHasta", SqlDbType.DateTime2).Value = search.FechaHasta.Value;
        if (!string.IsNullOrWhiteSpace(search.Search))
            command.Parameters.Add("@Search", SqlDbType.NVarChar, 200).Value = $"%{search.Search.Trim()}%";
    }

    private static void AddWriteParameters(SqlCommand command, MantenimientoCreateDto request)
    {
        command.Parameters.Add("@IdVehiculo", SqlDbType.Int).Value = request.IdVehiculo;
        command.Parameters.Add("@IdTipo", SqlDbType.Int).Value = request.IdTipoMantenimiento;
        command.Parameters.Add("@Fecha", SqlDbType.DateTime2).Value = request.Fecha;
        command.Parameters.Add("@Taller", SqlDbType.NVarChar, 150).Value = (object?)request.Taller?.Trim() ?? DBNull.Value;
        command.Parameters.Add("@Kilometraje", SqlDbType.Int).Value = (object?)request.Kilometraje ?? DBNull.Value;
        var costo = command.Parameters.Add("@Costo", SqlDbType.Decimal);
        costo.Precision = 18;
        costo.Scale = 2;
        costo.Value = request.Costo;
        command.Parameters.Add("@Observacion", SqlDbType.NVarChar, -1).Value = (object?)request.Observacion?.Trim() ?? DBNull.Value;
    }

    private static MantenimientoResponseDto Map(SqlDataReader reader) => new()
    {
        IdMantenimiento = reader.GetInt32(reader.GetOrdinal("IdMantenimiento")),
        IdVehiculo = reader.GetInt32(reader.GetOrdinal("IdVehiculo")),
        Vehiculo = reader.GetString(reader.GetOrdinal("Vehiculo")),
        IdTipoMantenimiento = reader.GetInt32(reader.GetOrdinal("IdTipoMantenimiento")),
        TipoCodigo = reader.GetString(reader.GetOrdinal("TipoCodigo")),
        TipoNombre = reader.GetString(reader.GetOrdinal("TipoNombre")),
        Fecha = reader.GetDateTime(reader.GetOrdinal("Fecha")),
        Taller = reader.IsDBNull(reader.GetOrdinal("Taller")) ? null : reader.GetString(reader.GetOrdinal("Taller")),
        Kilometraje = reader.IsDBNull(reader.GetOrdinal("Kilometraje")) ? null : reader.GetInt32(reader.GetOrdinal("Kilometraje")),
        Costo = reader.GetDecimal(reader.GetOrdinal("Costo")),
        Observacion = reader.IsDBNull(reader.GetOrdinal("Observacion")) ? null : reader.GetString(reader.GetOrdinal("Observacion"))
    };
}

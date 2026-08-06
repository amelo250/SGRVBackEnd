using System.Data;
using Microsoft.Data.SqlClient;
using SGRVBackEnd.DTOs.Gastos;

namespace SGRVBackEnd.Services.Gastos;

public sealed class GastoService : IGastoService
{
    private const string ResponseSelect = """
        SELECT g.IdGasto, g.IdTipoGasto, t.Codigo AS TipoCodigo, t.nombre AS TipoNombre,
               g.IdMoneda, m.Codigo AS MonedaCodigo, m.Simbolo AS MonedaSimbolo,
               g.IdVehiculo,
               CASE WHEN v.IdVehiculo IS NULL THEN NULL
                    ELSE CONCAT(v.Marca, ' ', v.Modelo, ' · ', v.Placa) END AS VehiculoDescripcion,
               g.Fecha, g.Concepto, g.NumeroComprobante, g.Proveedor,
               g.Monto, g.TasaCambioAplicada, g.MontoMonedaLocal,
               g.Kilometraje, g.Taller, g.Observaciones, g.Activo,
               g.FechaCreacion, g.FechaActualizacion, g.RowVersion
        FROM dbo.Gastos AS g
        INNER JOIN dbo.Tipos AS t ON t.IdTipo = g.IdTipoGasto
        INNER JOIN dbo.Monedas AS m ON m.idMoneda = g.IdMoneda
        LEFT JOIN dbo.Vehiculos AS v
          ON v.IdEmpresa = g.IdEmpresa AND v.IdVehiculo = g.IdVehiculo
        """;

    private readonly string _connectionString;

    public GastoService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No se encontró la conexión DefaultConnection.");
    }

    public async Task<(IReadOnlyList<GastoResponseDto> Items, int Total)> GetAllAsync(
        int idEmpresa, GastoSearchDto search, CancellationToken cancellationToken)
    {
        var where = new List<string> { "g.IdEmpresa = @IdEmpresa" };
        if (!search.IncluirInactivos) where.Add("g.Activo = 1");
        if (search.IdTipoGasto.HasValue) where.Add("g.IdTipoGasto = @IdTipoGasto");
        if (search.IdVehiculo.HasValue) where.Add("g.IdVehiculo = @IdVehiculo");
        if (search.FechaDesde.HasValue) where.Add("g.Fecha >= @FechaDesde");
        if (search.FechaHasta.HasValue) where.Add("g.Fecha <= @FechaHasta");
        if (!string.IsNullOrWhiteSpace(search.Search))
        {
            where.Add("(g.Concepto LIKE @Search OR g.NumeroComprobante LIKE @Search " +
                      "OR g.Proveedor LIKE @Search OR g.Observaciones LIKE @Search)");
        }

        var predicate = string.Join(" AND ", where);
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var countCommand = connection.CreateCommand();
        countCommand.CommandText = $"SELECT COUNT(1) FROM dbo.Gastos AS g WHERE {predicate};";
        AddSearchParameters(countCommand, idEmpresa, search);
        var total = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            {ResponseSelect}
            WHERE {predicate}
            ORDER BY g.Fecha DESC, g.IdGasto DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;
        AddSearchParameters(command, idEmpresa, search);
        command.Parameters.Add("@Offset", SqlDbType.Int).Value =
            (search.PageNumber - 1) * search.PageSize;
        command.Parameters.Add("@PageSize", SqlDbType.Int).Value = search.PageSize;

        var items = new List<GastoResponseDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) items.Add(Map(reader));
        return (items, total);
    }

    public async Task<GastoResponseDto?> GetByIdAsync(
        int id, int idEmpresa, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return await LoadAsync(connection, id, idEmpresa, cancellationToken);
    }

    public async Task<GastoResponseDto> CreateAsync(
        int idEmpresa, int idUsuario, GastoCreateDto request, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await ValidateReferencesAsync(connection, idEmpresa, idUsuario, request, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.Gastos
            (IdEmpresa, IdTipoGasto, IdMoneda, IdVehiculo, IdUsuarioRegistro,
             Fecha, Concepto, NumeroComprobante, Proveedor, Monto,
             TasaCambioAplicada, MontoMonedaLocal, Kilometraje, Taller, Observaciones)
            OUTPUT INSERTED.IdGasto
            VALUES
            (@IdEmpresa, @IdTipoGasto, @IdMoneda, @IdVehiculo, @IdUsuario,
             @Fecha, @Concepto, @NumeroComprobante, @Proveedor, @Monto,
             @Tasa, @MontoLocal, @Kilometraje, @Taller, @Observaciones);
            """;
        AddWriteParameters(command, idEmpresa, idUsuario, request);
        var id = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        return await LoadAsync(connection, id, idEmpresa, cancellationToken)
            ?? throw new InvalidOperationException("No fue posible recuperar el gasto creado.");
    }

    public async Task<GastoResponseDto?> UpdateAsync(
        int id, int idEmpresa, int idUsuario, GastoUpdateDto request,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        if (await LoadAsync(connection, id, idEmpresa, cancellationToken) is null) return null;
        await ValidateReferencesAsync(connection, idEmpresa, idUsuario, request, cancellationToken);

        var expectedVersion = Convert.FromBase64String(request.RowVersion);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.Gastos
               SET IdTipoGasto = @IdTipoGasto, IdMoneda = @IdMoneda,
                   IdVehiculo = @IdVehiculo, IdUsuarioActualizacion = @IdUsuario,
                   Fecha = @Fecha, Concepto = @Concepto,
                   NumeroComprobante = @NumeroComprobante, Proveedor = @Proveedor,
                   Monto = @Monto, TasaCambioAplicada = @Tasa,
                   MontoMonedaLocal = @MontoLocal, Kilometraje = @Kilometraje,
                   Taller = @Taller, Observaciones = @Observaciones,
                   FechaActualizacion = SYSUTCDATETIME()
             WHERE IdGasto = @IdGasto AND IdEmpresa = @IdEmpresa
               AND RowVersion = @RowVersion;
            """;
        AddWriteParameters(command, idEmpresa, idUsuario, request);
        command.Parameters.Add("@IdGasto", SqlDbType.Int).Value = id;
        command.Parameters.Add("@RowVersion", SqlDbType.Timestamp, 8).Value = expectedVersion;
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            throw new DBConcurrencyException("El gasto fue modificado por otro usuario. Recargue e intente nuevamente.");

        return await LoadAsync(connection, id, idEmpresa, cancellationToken);
    }

    public async Task<bool> SetActiveAsync(
        int id, int idEmpresa, int idUsuario, bool active, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.Gastos
               SET Activo = @Activo, IdUsuarioActualizacion = @IdUsuario,
                   FechaActualizacion = SYSUTCDATETIME()
             WHERE IdGasto = @IdGasto AND IdEmpresa = @IdEmpresa;
            """;
        command.Parameters.Add("@Activo", SqlDbType.Bit).Value = active;
        command.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;
        command.Parameters.Add("@IdGasto", SqlDbType.Int).Value = id;
        command.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<GastoSummaryDto> GetSummaryAsync(
        int idEmpresa, DateTime? desde, DateTime? hasta, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(1) AS Cantidad,
                   COALESCE(SUM(g.MontoMonedaLocal), 0) AS Total,
                   COALESCE(SUM(CASE WHEN t.Codigo IN ('MANTENIMIENTO','REPARACION','COMBUSTIBLE')
                                     THEN g.MontoMonedaLocal ELSE 0 END), 0) AS Mantenimiento,
                   COALESCE(SUM(CASE WHEN t.Codigo NOT IN ('MANTENIMIENTO','REPARACION','COMBUSTIBLE')
                                     THEN g.MontoMonedaLocal ELSE 0 END), 0) AS Operativo
              FROM dbo.Gastos AS g
              INNER JOIN dbo.Tipos AS t ON t.IdTipo = g.IdTipoGasto
             WHERE g.IdEmpresa = @IdEmpresa AND g.Activo = 1
               AND (@Desde IS NULL OR g.Fecha >= @Desde)
               AND (@Hasta IS NULL OR g.Fecha <= @Hasta);
            """;
        command.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
        command.Parameters.Add("@Desde", SqlDbType.Date).Value = (object?)desde?.Date ?? DBNull.Value;
        command.Parameters.Add("@Hasta", SqlDbType.Date).Value = (object?)hasta?.Date ?? DBNull.Value;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return new GastoSummaryDto
        {
            Cantidad = reader.GetInt32(reader.GetOrdinal("Cantidad")),
            TotalMonedaLocal = reader.GetDecimal(reader.GetOrdinal("Total")),
            TotalMantenimiento = reader.GetDecimal(reader.GetOrdinal("Mantenimiento")),
            TotalOperativo = reader.GetDecimal(reader.GetOrdinal("Operativo"))
        };
    }

    private static async Task<GastoResponseDto?> LoadAsync(
        SqlConnection connection, int id, int idEmpresa, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"{ResponseSelect} WHERE g.IdGasto = @IdGasto AND g.IdEmpresa = @IdEmpresa;";
        command.Parameters.Add("@IdGasto", SqlDbType.Int).Value = id;
        command.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    private static async Task ValidateReferencesAsync(
        SqlConnection connection, int idEmpresa, int idUsuario, GastoCreateDto request,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
              CASE WHEN EXISTS (SELECT 1 FROM dbo.Tipos WHERE IdTipo=@IdTipo AND Categoria='GASTOS' AND Activo=1) THEN 1 ELSE 0 END,
              CASE WHEN EXISTS (SELECT 1 FROM dbo.Monedas WHERE idMoneda=@IdMoneda AND Activo=1) THEN 1 ELSE 0 END,
              CASE WHEN @IdVehiculo IS NULL OR EXISTS (SELECT 1 FROM dbo.Vehiculos WHERE IdVehiculo=@IdVehiculo AND IdEmpresa=@IdEmpresa AND Activo=1) THEN 1 ELSE 0 END,
              CASE WHEN EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Idusuario=@IdUsuario AND IdEmpresa=@IdEmpresa AND Activo=1) THEN 1 ELSE 0 END;
            """;
        command.Parameters.Add("@IdTipo", SqlDbType.Int).Value = request.IdTipoGasto;
        command.Parameters.Add("@IdMoneda", SqlDbType.Int).Value = request.IdMoneda;
        command.Parameters.Add("@IdVehiculo", SqlDbType.Int).Value = (object?)request.IdVehiculo ?? DBNull.Value;
        command.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
        command.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        if (reader.GetInt32(0) == 0) throw new InvalidOperationException("El tipo no pertenece al catálogo GASTOS o está inactivo.");
        if (reader.GetInt32(1) == 0) throw new InvalidOperationException("La moneda no existe o está inactiva.");
        if (reader.GetInt32(2) == 0) throw new InvalidOperationException("El vehículo no pertenece a la empresa o está inactivo.");
        if (reader.GetInt32(3) == 0) throw new InvalidOperationException("El usuario del token no pertenece a la empresa o está inactivo.");
    }

    private static void AddSearchParameters(SqlCommand command, int idEmpresa, GastoSearchDto search)
    {
        command.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
        if (search.IdTipoGasto.HasValue) command.Parameters.Add("@IdTipoGasto", SqlDbType.Int).Value = search.IdTipoGasto.Value;
        if (search.IdVehiculo.HasValue) command.Parameters.Add("@IdVehiculo", SqlDbType.Int).Value = search.IdVehiculo.Value;
        if (search.FechaDesde.HasValue) command.Parameters.Add("@FechaDesde", SqlDbType.Date).Value = search.FechaDesde.Value.Date;
        if (search.FechaHasta.HasValue) command.Parameters.Add("@FechaHasta", SqlDbType.Date).Value = search.FechaHasta.Value.Date;
        if (!string.IsNullOrWhiteSpace(search.Search)) command.Parameters.Add("@Search", SqlDbType.NVarChar, 220).Value = $"%{search.Search.Trim()}%";
    }

    private static void AddWriteParameters(
        SqlCommand command, int idEmpresa, int idUsuario, GastoCreateDto request)
    {
        command.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
        command.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;
        command.Parameters.Add("@IdTipoGasto", SqlDbType.Int).Value = request.IdTipoGasto;
        command.Parameters.Add("@IdMoneda", SqlDbType.Int).Value = request.IdMoneda;
        command.Parameters.Add("@IdVehiculo", SqlDbType.Int).Value = (object?)request.IdVehiculo ?? DBNull.Value;
        command.Parameters.Add("@Fecha", SqlDbType.Date).Value = request.Fecha.Date;
        command.Parameters.Add("@Concepto", SqlDbType.NVarChar, 200).Value = request.Concepto.Trim();
        command.Parameters.Add("@NumeroComprobante", SqlDbType.NVarChar, 80).Value = DbValue(request.NumeroComprobante);
        command.Parameters.Add("@Proveedor", SqlDbType.NVarChar, 150).Value = DbValue(request.Proveedor);
        command.Parameters.Add("@Monto", SqlDbType.Decimal).Value = request.Monto;
        command.Parameters["@Monto"].Precision = 18; command.Parameters["@Monto"].Scale = 2;
        command.Parameters.Add("@Tasa", SqlDbType.Decimal).Value = request.TasaCambioAplicada;
        command.Parameters["@Tasa"].Precision = 18; command.Parameters["@Tasa"].Scale = 6;
        command.Parameters.Add("@MontoLocal", SqlDbType.Decimal).Value = Math.Round(request.Monto * request.TasaCambioAplicada, 2);
        command.Parameters["@MontoLocal"].Precision = 18; command.Parameters["@MontoLocal"].Scale = 2;
        command.Parameters.Add("@Kilometraje", SqlDbType.Int).Value = (object?)request.Kilometraje ?? DBNull.Value;
        command.Parameters.Add("@Taller", SqlDbType.NVarChar, 150).Value = DbValue(request.Taller);
        command.Parameters.Add("@Observaciones", SqlDbType.NVarChar, 1000).Value = DbValue(request.Observaciones);
    }

    private static object DbValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static GastoResponseDto Map(SqlDataReader reader) => new()
    {
        IdGasto = reader.GetInt32(reader.GetOrdinal("IdGasto")),
        IdTipoGasto = reader.GetInt32(reader.GetOrdinal("IdTipoGasto")),
        TipoCodigo = reader.GetString(reader.GetOrdinal("TipoCodigo")),
        TipoNombre = reader.GetString(reader.GetOrdinal("TipoNombre")),
        IdMoneda = reader.GetInt32(reader.GetOrdinal("IdMoneda")),
        MonedaCodigo = reader.GetString(reader.GetOrdinal("MonedaCodigo")),
        MonedaSimbolo = reader.GetString(reader.GetOrdinal("MonedaSimbolo")),
        IdVehiculo = reader.IsDBNull(reader.GetOrdinal("IdVehiculo")) ? null : reader.GetInt32(reader.GetOrdinal("IdVehiculo")),
        VehiculoDescripcion = reader.IsDBNull(reader.GetOrdinal("VehiculoDescripcion")) ? null : reader.GetString(reader.GetOrdinal("VehiculoDescripcion")),
        Fecha = reader.GetDateTime(reader.GetOrdinal("Fecha")),
        Concepto = reader.GetString(reader.GetOrdinal("Concepto")),
        NumeroComprobante = reader.IsDBNull(reader.GetOrdinal("NumeroComprobante")) ? null : reader.GetString(reader.GetOrdinal("NumeroComprobante")),
        Proveedor = reader.IsDBNull(reader.GetOrdinal("Proveedor")) ? null : reader.GetString(reader.GetOrdinal("Proveedor")),
        Monto = reader.GetDecimal(reader.GetOrdinal("Monto")),
        TasaCambioAplicada = reader.GetDecimal(reader.GetOrdinal("TasaCambioAplicada")),
        MontoMonedaLocal = reader.GetDecimal(reader.GetOrdinal("MontoMonedaLocal")),
        Kilometraje = reader.IsDBNull(reader.GetOrdinal("Kilometraje")) ? null : reader.GetInt32(reader.GetOrdinal("Kilometraje")),
        Taller = reader.IsDBNull(reader.GetOrdinal("Taller")) ? null : reader.GetString(reader.GetOrdinal("Taller")),
        Observaciones = reader.IsDBNull(reader.GetOrdinal("Observaciones")) ? null : reader.GetString(reader.GetOrdinal("Observaciones")),
        Activo = reader.GetBoolean(reader.GetOrdinal("Activo")),
        FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
        FechaActualizacion = reader.IsDBNull(reader.GetOrdinal("FechaActualizacion")) ? null : reader.GetDateTime(reader.GetOrdinal("FechaActualizacion")),
        RowVersion = Convert.ToBase64String((byte[])reader["RowVersion"])
    };
}

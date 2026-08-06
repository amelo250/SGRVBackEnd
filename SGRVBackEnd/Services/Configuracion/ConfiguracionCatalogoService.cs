using System.Data;
using Microsoft.Data.SqlClient;
using SGRVBackEnd.DTOs.Configuracion;

namespace SGRVBackEnd.Services.Configuracion;

public sealed class ConfiguracionCatalogoService : IConfiguracionCatalogoService
{
    private sealed record Descriptor(
        string Key,
        string DisplayName,
        string Table,
        string IdColumn,
        string NameColumn,
        bool UsesCategory = false,
        bool UsesSymbol = false);

    private static readonly IReadOnlyDictionary<string, Descriptor> Catalogs =
        new Dictionary<string, Descriptor>(StringComparer.OrdinalIgnoreCase)
        {
            ["estados"] = new("estados", "Estados", "dbo.Estados", "IdEstado", "Nombre", true),
            ["tipos"] = new("tipos", "Tipos", "dbo.Tipos", "IdTipo", "nombre", true),
            ["metodos-pago"] = new("metodos-pago", "Métodos de pago", "dbo.MetodosPago", "IdMetodoPago", "Nombre"),
            ["combustibles"] = new("combustibles", "Combustibles", "dbo.Combustibles", "IdCombustible", "nombre"),
            ["transmisiones"] = new("transmisiones", "Transmisiones", "dbo.Transmisiones", "IdTransmision", "nombre"),
            ["monedas"] = new("monedas", "Monedas", "dbo.Monedas", "idMoneda", "Nombre", UsesSymbol: true)
        };

    private readonly string _connectionString;

    public ConfiguracionCatalogoService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No se encontró DefaultConnection.");
    }

    public IReadOnlyList<CatalogoConfiguracionDefinitionDto> GetDefinitions() =>
        Catalogs.Values.Select(ToDefinition).ToList();

    public bool TryGetDefinition(
        string key,
        out CatalogoConfiguracionDefinitionDto definition)
    {
        if (Catalogs.TryGetValue(key, out var descriptor))
        {
            definition = ToDefinition(descriptor);
            return true;
        }

        definition = new CatalogoConfiguracionDefinitionDto();
        return false;
    }

    public async Task<IReadOnlyList<CatalogoConfiguracionResponseDto>> GetAllAsync(
        string key,
        bool incluirInactivos,
        CancellationToken cancellationToken)
    {
        var descriptor = GetDescriptor(key);
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT {SelectColumns(descriptor)}
              FROM {descriptor.Table}
             WHERE (@IncluirInactivos = 1 OR Activo = 1)
             ORDER BY {descriptor.NameColumn};
            """;
        command.Parameters.Add("@IncluirInactivos", SqlDbType.Bit).Value = incluirInactivos;

        var items = new List<CatalogoConfiguracionResponseDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) items.Add(Map(reader));
        return items;
    }

    public async Task<CatalogoConfiguracionResponseDto?> GetByIdAsync(
        string key,
        int id,
        CancellationToken cancellationToken)
    {
        var descriptor = GetDescriptor(key);
        await using var connection = await OpenAsync(cancellationToken);
        return await LoadAsync(connection, descriptor, id, cancellationToken);
    }

    public async Task<CatalogoConfiguracionResponseDto> CreateAsync(
        string key,
        CatalogoConfiguracionCreateDto dto,
        CancellationToken cancellationToken)
    {
        var descriptor = GetDescriptor(key);
        await using var connection = await OpenAsync(cancellationToken);
        await EnsureUniqueAsync(connection, descriptor, null, dto, cancellationToken);

        var columns = new List<string> { "Codigo", descriptor.NameColumn, "Activo" };
        var values = new List<string> { "@Codigo", "@Nombre", "1" };
        if (descriptor.UsesCategory) { columns.Add("Categoria"); values.Add("@Categoria"); }
        if (descriptor.UsesSymbol) { columns.Add("Simbolo"); values.Add("@Simbolo"); }

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            INSERT INTO {descriptor.Table} ({string.Join(",", columns)})
            OUTPUT INSERTED.{descriptor.IdColumn}
            VALUES ({string.Join(",", values)});
            """;
        AddWriteParameters(command, descriptor, dto);
        var id = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        return await LoadAsync(connection, descriptor, id, cancellationToken)
            ?? throw new InvalidOperationException("No fue posible recuperar el registro creado.");
    }

    public async Task<CatalogoConfiguracionResponseDto?> UpdateAsync(
        string key,
        int id,
        CatalogoConfiguracionUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var descriptor = GetDescriptor(key);
        await using var connection = await OpenAsync(cancellationToken);
        if (await LoadAsync(connection, descriptor, id, cancellationToken) is null) return null;
        await EnsureUniqueAsync(connection, descriptor, id, dto, cancellationToken);

        var assignments = new List<string> { "Codigo=@Codigo", $"{descriptor.NameColumn}=@Nombre" };
        if (descriptor.UsesCategory) assignments.Add("Categoria=@Categoria");
        if (descriptor.UsesSymbol) assignments.Add("Simbolo=@Simbolo");

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            UPDATE {descriptor.Table}
               SET {string.Join(",", assignments)}
             WHERE {descriptor.IdColumn}=@Id;
            """;
        AddWriteParameters(command, descriptor, dto);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        await command.ExecuteNonQueryAsync(cancellationToken);
        return await LoadAsync(connection, descriptor, id, cancellationToken);
    }

    public async Task<bool> SetActiveAsync(
        string key,
        int id,
        bool activo,
        CancellationToken cancellationToken)
    {
        var descriptor = GetDescriptor(key);
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"UPDATE {descriptor.Table} SET Activo=@Activo WHERE {descriptor.IdColumn}=@Id;";
        command.Parameters.Add("@Activo", SqlDbType.Bit).Value = activo;
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static Descriptor GetDescriptor(string key) =>
        Catalogs.TryGetValue(key, out var descriptor)
            ? descriptor
            : throw new KeyNotFoundException("El catálogo solicitado no está habilitado para configuración.");

    private static CatalogoConfiguracionDefinitionDto ToDefinition(Descriptor descriptor) => new()
    {
        Key = descriptor.Key,
        Nombre = descriptor.DisplayName,
        UsaCategoria = descriptor.UsesCategory,
        UsaSimbolo = descriptor.UsesSymbol,
        EsGlobal = true
    };

    private static string SelectColumns(Descriptor descriptor)
    {
        var category = descriptor.UsesCategory ? "Categoria" : "CAST(NULL AS nvarchar(50))";
        var symbol = descriptor.UsesSymbol ? "Simbolo" : "CAST(NULL AS nvarchar(10))";
        return $"{descriptor.IdColumn} AS Id,Codigo,{descriptor.NameColumn} AS Nombre,{category} AS Categoria,{symbol} AS Simbolo,Activo";
    }

    private static async Task<CatalogoConfiguracionResponseDto?> LoadAsync(
        SqlConnection connection,
        Descriptor descriptor,
        int id,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {SelectColumns(descriptor)} FROM {descriptor.Table} WHERE {descriptor.IdColumn}=@Id;";
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    private static async Task EnsureUniqueAsync(
        SqlConnection connection,
        Descriptor descriptor,
        int? id,
        CatalogoConfiguracionCreateDto dto,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        var categoryPredicate = descriptor.UsesCategory ? "AND Categoria=@Categoria" : string.Empty;
        command.CommandText = $"""
            SELECT COUNT(1) FROM {descriptor.Table}
             WHERE UPPER(Codigo)=@Codigo {categoryPredicate}
               AND (@Id IS NULL OR {descriptor.IdColumn}<>@Id);
            """;
        command.Parameters.Add("@Codigo", SqlDbType.NVarChar, 50).Value = dto.Codigo.Trim().ToUpperInvariant();
        command.Parameters.Add("@Id", SqlDbType.Int).Value = (object?)id ?? DBNull.Value;
        if (descriptor.UsesCategory)
            command.Parameters.Add("@Categoria", SqlDbType.NVarChar, 50).Value = dto.Categoria!.Trim().ToUpperInvariant();
        if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0)
            throw new InvalidOperationException("Ya existe un registro con ese código en el catálogo.");
    }

    private static void AddWriteParameters(
        SqlCommand command,
        Descriptor descriptor,
        CatalogoConfiguracionCreateDto dto)
    {
        command.Parameters.Add("@Codigo", SqlDbType.NVarChar, 50).Value = dto.Codigo.Trim().ToUpperInvariant();
        command.Parameters.Add("@Nombre", SqlDbType.NVarChar, 100).Value = dto.Nombre.Trim();
        if (descriptor.UsesCategory)
            command.Parameters.Add("@Categoria", SqlDbType.NVarChar, 50).Value = dto.Categoria!.Trim().ToUpperInvariant();
        if (descriptor.UsesSymbol)
            command.Parameters.Add("@Simbolo", SqlDbType.NVarChar, 10).Value = dto.Simbolo!.Trim();
    }

    private static CatalogoConfiguracionResponseDto Map(SqlDataReader reader) => new()
    {
        Id = reader.GetInt32(reader.GetOrdinal("Id")),
        Codigo = reader.GetString(reader.GetOrdinal("Codigo")),
        Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
        Categoria = reader.IsDBNull(reader.GetOrdinal("Categoria"))
            ? null : reader.GetString(reader.GetOrdinal("Categoria")),
        Simbolo = reader.IsDBNull(reader.GetOrdinal("Simbolo"))
            ? null : reader.GetString(reader.GetOrdinal("Simbolo")),
        Activo = reader.GetBoolean(reader.GetOrdinal("Activo"))
    };
}

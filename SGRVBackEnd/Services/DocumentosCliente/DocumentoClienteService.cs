using System.Data;
using Microsoft.Data.SqlClient;
using SGRVBackEnd.DTOs.DocumentosCliente;

namespace SGRVBackEnd.Services.DocumentosCliente;

public sealed class DocumentoClienteService : IDocumentoClienteService
{
    private readonly string _connectionString;
    private readonly string _storageRoot;

    public DocumentoClienteService(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No se encontró DefaultConnection.");
        _storageRoot = Path.Combine(
            environment.ContentRootPath, "App_Data", "client-documents");
    }

    public async Task<IReadOnlyList<DocumentoClienteResponseDto>> GetAllAsync(
        int idCliente,
        int idEmpresa,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureClientAsync(connection, idCliente, idEmpresa, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            {SelectSql}
             WHERE d.IdCliente=@IdCliente
             ORDER BY d.FechaSubida DESC,d.IdDocumentoCliente DESC;
            """;
        command.Parameters.Add("@IdCliente", SqlDbType.Int).Value = idCliente;

        var result = new List<DocumentoClienteResponseDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var item = Map(reader);
            item.TipoContenido = FindContentType(
                idEmpresa, idCliente, item.IdDocumentoCliente);
            result.Add(item);
        }
        return result;
    }

    public async Task<IReadOnlyList<TipoDocumentoClienteDto>> GetTypesAsync(
        int idCliente,
        int idEmpresa,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureClientAsync(connection, idCliente, idEmpresa, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT IdTipo,Codigo,nombre
              FROM dbo.Tipos
             WHERE Activo=1 AND UPPER(Categoria) LIKE '%DOCUMENT%'
             ORDER BY nombre;
            """;
        var result = new List<TipoDocumentoClienteDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new TipoDocumentoClienteDto
            {
                Id = reader.GetInt32(0),
                Codigo = reader.GetString(1),
                Nombre = reader.GetString(2)
            });
        }
        return result;
    }

    public async Task<DocumentoClienteResponseDto> UploadAsync(
        int idCliente,
        int idEmpresa,
        DocumentoClienteUploadDto request,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureClientAsync(connection, idCliente, idEmpresa, cancellationToken);
        await EnsureDocumentTypeAsync(
            connection, request.IdTipoDocumento, cancellationToken);

        await using var transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        string? absolutePath = null;
        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO dbo.DocumentosCliente
                    (IdCliente,IdTipoDocumento,Urldocumento,FechaSubida)
                OUTPUT INSERTED.IdDocumentoCliente
                VALUES(@IdCliente,@IdTipoDocumento,'',SYSUTCDATETIME());
                """;
            command.Parameters.Add("@IdCliente", SqlDbType.Int).Value = idCliente;
            command.Parameters.Add("@IdTipoDocumento", SqlDbType.Int).Value =
                request.IdTipoDocumento;
            var idDocumento = Convert.ToInt32(
                await command.ExecuteScalarAsync(cancellationToken));

            var extension = GetExtension(request.Archivo!.ContentType);
            var folder = Path.Combine(
                _storageRoot, idEmpresa.ToString(), idCliente.ToString());
            Directory.CreateDirectory(folder);
            absolutePath = Path.Combine(folder, $"{idDocumento}{extension}");
            await using (var output = new FileStream(
                absolutePath, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 81920, useAsync: true))
            {
                await request.Archivo.CopyToAsync(output, cancellationToken);
            }

            await using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = """
                UPDATE dbo.DocumentosCliente
                   SET Urldocumento=@Url
                 WHERE IdDocumentoCliente=@IdDocumento
                   AND IdCliente=@IdCliente;
                """;
            update.Parameters.Add("@Url", SqlDbType.NVarChar, 500).Value =
                $"/api/clientes/{idCliente}/documentos/{idDocumento}/contenido";
            update.Parameters.Add("@IdDocumento", SqlDbType.Int).Value = idDocumento;
            update.Parameters.Add("@IdCliente", SqlDbType.Int).Value = idCliente;
            await update.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var result = await LoadAsync(
                connection, idCliente, idDocumento, cancellationToken)
                ?? throw new InvalidOperationException(
                    "No fue posible recuperar el documento creado.");
            result.TipoContenido = request.Archivo.ContentType;
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            if (absolutePath is not null && File.Exists(absolutePath))
                File.Delete(absolutePath);
            throw;
        }
    }

    public async Task<(Stream Stream, string ContentType)?> OpenAsync(
        int idCliente,
        int idDocumento,
        int idEmpresa,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureClientAsync(connection, idCliente, idEmpresa, cancellationToken);
        if (await LoadAsync(connection, idCliente, idDocumento, cancellationToken) is null)
            return null;

        var folder = Path.Combine(
            _storageRoot, idEmpresa.ToString(), idCliente.ToString());
        if (!Directory.Exists(folder)) return null;
        var path = Directory.EnumerateFiles(folder, $"{idDocumento}.*").FirstOrDefault();
        if (path is null) return null;
        var stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return (stream, GetContentType(Path.GetExtension(path)));
    }

    public async Task<bool> DeleteAsync(
        int idCliente,
        int idDocumento,
        int idEmpresa,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureClientAsync(connection, idCliente, idEmpresa, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM dbo.DocumentosCliente
             WHERE IdDocumentoCliente=@IdDocumento AND IdCliente=@IdCliente;
            """;
        command.Parameters.Add("@IdDocumento", SqlDbType.Int).Value = idDocumento;
        command.Parameters.Add("@IdCliente", SqlDbType.Int).Value = idCliente;
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0) return false;

        var folder = Path.Combine(
            _storageRoot, idEmpresa.ToString(), idCliente.ToString());
        if (Directory.Exists(folder))
        {
            foreach (var path in Directory.EnumerateFiles(folder, $"{idDocumento}.*"))
                File.Delete(path);
        }
        return true;
    }

    private const string SelectSql = """
        SELECT d.IdDocumentoCliente,d.IdCliente,d.IdTipoDocumento,
               t.nombre AS TipoDocumentoNombre,d.Urldocumento,d.FechaSubida
          FROM dbo.DocumentosCliente AS d
          INNER JOIN dbo.Tipos AS t ON t.IdTipo=d.IdTipoDocumento
        """;

    private async Task<SqlConnection> OpenConnectionAsync(
        CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static async Task EnsureClientAsync(
        SqlConnection connection,
        int idCliente,
        int idEmpresa,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(1) FROM dbo.Clientes
             WHERE IdCliente=@IdCliente AND IdEmpresa=@IdEmpresa;
            """;
        command.Parameters.Add("@IdCliente", SqlDbType.Int).Value = idCliente;
        command.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
        if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 0)
            throw new KeyNotFoundException("El cliente no pertenece a la empresa.");
    }

    private static async Task EnsureDocumentTypeAsync(
        SqlConnection connection,
        int idTipo,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(1) FROM dbo.Tipos
             WHERE IdTipo=@IdTipo AND Activo=1
               AND UPPER(Categoria) LIKE '%DOCUMENT%';
            """;
        command.Parameters.Add("@IdTipo", SqlDbType.Int).Value = idTipo;
        if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 0)
            throw new InvalidOperationException(
                "El tipo seleccionado no corresponde a documentos de cliente.");
    }

    private static async Task<DocumentoClienteResponseDto?> LoadAsync(
        SqlConnection connection,
        int idCliente,
        int idDocumento,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            {SelectSql}
             WHERE d.IdDocumentoCliente=@IdDocumento AND d.IdCliente=@IdCliente;
            """;
        command.Parameters.Add("@IdDocumento", SqlDbType.Int).Value = idDocumento;
        command.Parameters.Add("@IdCliente", SqlDbType.Int).Value = idCliente;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    private static DocumentoClienteResponseDto Map(SqlDataReader reader) => new()
    {
        IdDocumentoCliente = reader.GetInt32(0),
        IdCliente = reader.GetInt32(1),
        IdTipoDocumento = reader.GetInt32(2),
        TipoDocumentoNombre = reader.GetString(3),
        UrlDocumento = reader.GetString(4),
        FechaSubida = reader.GetDateTime(5)
    };

    private static string GetExtension(string contentType) =>
        contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "application/pdf" => ".pdf",
            _ => throw new InvalidOperationException("Tipo de archivo no permitido.")
        };

    private static string GetContentType(string extension) =>
        extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };

    private string? FindContentType(int idEmpresa, int idCliente, int idDocumento)
    {
        var folder = Path.Combine(
            _storageRoot, idEmpresa.ToString(), idCliente.ToString());
        if (!Directory.Exists(folder)) return null;
        var path = Directory.EnumerateFiles(folder, $"{idDocumento}.*").FirstOrDefault();
        return path is null ? null : GetContentType(Path.GetExtension(path));
    }
}

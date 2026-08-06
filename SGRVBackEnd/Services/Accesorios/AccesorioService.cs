using System.Data;
using Microsoft.Data.SqlClient;
using SGRVBackEnd.DTOs.Accesorios;

namespace SGRVBackEnd.Services.Accesorios;

public sealed class AccesorioService : IAccesorioService
{
    private readonly string _connectionString;
    public AccesorioService(IConfiguration configuration) => _connectionString =
        configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Falta DefaultConnection.");

    public async Task<IReadOnlyList<AccesorioResponseDto>> GetCatalogAsync(int idEmpresa, bool incluirInactivos, CancellationToken ct)
    {
        await using var cn = await OpenAsync(ct);
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT IdAccesorio,Codigo,Nombre,Descripcion,Icono,IdEmpresa,Activo,RowVersion
            FROM dbo.Accesorios
            WHERE (IdEmpresa IS NULL OR IdEmpresa=@IdEmpresa) AND (@Inactivos=1 OR Activo=1)
            ORDER BY Nombre;
            """;
        cmd.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
        cmd.Parameters.Add("@Inactivos", SqlDbType.Bit).Value = incluirInactivos;
        var result = new List<AccesorioResponseDto>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) result.Add(MapCatalog(reader));
        return result;
    }

    public async Task<AccesorioResponseDto> CreateAsync(int idEmpresa, AccesorioCreateDto dto, CancellationToken ct)
    {
        await using var cn = await OpenAsync(ct);
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            INSERT dbo.Accesorios(IdEmpresa,Codigo,Nombre,Descripcion,Icono)
            OUTPUT INSERTED.IdAccesorio
            VALUES(@IdEmpresa,@Codigo,@Nombre,@Descripcion,@Icono);
            """;
        AddCatalog(cmd, idEmpresa, dto);
        try
        {
            var id = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
            return (await LoadCatalogAsync(cn, id, idEmpresa, ct))!;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        { throw new InvalidOperationException("Ya existe un accesorio con ese código.", ex); }
    }

    public async Task<AccesorioResponseDto?> UpdateAsync(int id, int idEmpresa, AccesorioUpdateDto dto, CancellationToken ct)
    {
        await using var cn = await OpenAsync(ct);
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            UPDATE dbo.Accesorios SET Codigo=@Codigo,Nombre=@Nombre,Descripcion=@Descripcion,
              Icono=@Icono,FechaActualizacion=SYSUTCDATETIME()
            WHERE IdAccesorio=@Id AND IdEmpresa=@IdEmpresa AND RowVersion=@RowVersion;
            """;
        AddCatalog(cmd, idEmpresa, dto);
        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        cmd.Parameters.Add("@RowVersion", SqlDbType.Timestamp, 8).Value = Convert.FromBase64String(dto.RowVersion);
        try
        {
            if (await cmd.ExecuteNonQueryAsync(ct) == 0) return null;
            return await LoadCatalogAsync(cn, id, idEmpresa, ct);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        { throw new InvalidOperationException("Ya existe un accesorio con ese código.", ex); }
    }

    public async Task<bool> SetCatalogActiveAsync(int id, int idEmpresa, bool active, CancellationToken ct)
    {
        await using var cn = await OpenAsync(ct);
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = "UPDATE dbo.Accesorios SET Activo=@Activo,FechaActualizacion=SYSUTCDATETIME() WHERE IdAccesorio=@Id AND IdEmpresa=@IdEmpresa;";
        cmd.Parameters.Add("@Activo", SqlDbType.Bit).Value = active;
        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        cmd.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<IReadOnlyList<VehiculoAccesorioResponseDto>> GetVehicleAsync(int idVehiculo, int idEmpresa, CancellationToken ct)
    {
        await using var cn = await OpenAsync(ct);
        await EnsureVehicleAsync(cn, idVehiculo, idEmpresa, ct);
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT va.IdVehiculoAccesorio,va.IdVehiculo,va.IdAccesorio,a.Codigo,a.Nombre,a.Icono,va.Observaciones,va.Activo
            FROM dbo.VehiculosAccesorios va INNER JOIN dbo.Accesorios a ON a.IdAccesorio=va.IdAccesorio
            WHERE va.IdEmpresa=@IdEmpresa AND va.IdVehiculo=@IdVehiculo AND va.Activo=1 ORDER BY a.Nombre;
            """;
        cmd.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
        cmd.Parameters.Add("@IdVehiculo", SqlDbType.Int).Value = idVehiculo;
        var result = new List<VehiculoAccesorioResponseDto>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) result.Add(MapVehicle(reader));
        return result;
    }

    public async Task<VehiculoAccesorioResponseDto> AssignAsync(int idVehiculo, int idEmpresa, int idUsuario, VehiculoAccesorioCreateDto dto, CancellationToken ct)
    {
        await using var cn = await OpenAsync(ct);
        await EnsureVehicleAsync(cn, idVehiculo, idEmpresa, ct);
        await using (var check = cn.CreateCommand())
        {
            check.CommandText = "SELECT COUNT(1) FROM dbo.Accesorios WHERE IdAccesorio=@Id AND Activo=1 AND (IdEmpresa IS NULL OR IdEmpresa=@Empresa);";
            check.Parameters.Add("@Id", SqlDbType.Int).Value = dto.IdAccesorio;
            check.Parameters.Add("@Empresa", SqlDbType.Int).Value = idEmpresa;
            if (Convert.ToInt32(await check.ExecuteScalarAsync(ct)) == 0) throw new InvalidOperationException("El accesorio no está disponible para esta empresa.");
        }
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            MERGE dbo.VehiculosAccesorios AS target
            USING (SELECT @IdEmpresa IdEmpresa,@IdVehiculo IdVehiculo,@IdAccesorio IdAccesorio) source
            ON target.IdEmpresa=source.IdEmpresa AND target.IdVehiculo=source.IdVehiculo AND target.IdAccesorio=source.IdAccesorio
            WHEN MATCHED THEN UPDATE SET Activo=1,Observaciones=@Observaciones,IdUsuarioActualizacion=@Usuario,FechaActualizacion=SYSUTCDATETIME()
            WHEN NOT MATCHED THEN INSERT(IdEmpresa,IdVehiculo,IdAccesorio,Observaciones,IdUsuarioRegistro)
              VALUES(source.IdEmpresa,source.IdVehiculo,source.IdAccesorio,@Observaciones,@Usuario);
            """;
        AddVehicle(cmd, idVehiculo, idEmpresa, idUsuario, dto.IdAccesorio, dto.Observaciones);
        await cmd.ExecuteNonQueryAsync(ct);
        return (await GetVehicleAsync(idVehiculo, idEmpresa, ct)).Single(x => x.IdAccesorio == dto.IdAccesorio);
    }

    public async Task<bool> RemoveAsync(int idVehiculo, int idAccesorio, int idEmpresa, int idUsuario, CancellationToken ct)
    {
        await using var cn = await OpenAsync(ct);
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = "UPDATE dbo.VehiculosAccesorios SET Activo=0,IdUsuarioActualizacion=@Usuario,FechaActualizacion=SYSUTCDATETIME() WHERE IdEmpresa=@IdEmpresa AND IdVehiculo=@IdVehiculo AND IdAccesorio=@IdAccesorio AND Activo=1;";
        AddVehicle(cmd, idVehiculo, idEmpresa, idUsuario, idAccesorio, null);
        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken ct) { var cn = new SqlConnection(_connectionString); await cn.OpenAsync(ct); return cn; }
    private static async Task EnsureVehicleAsync(SqlConnection cn, int idVehiculo, int idEmpresa, CancellationToken ct)
    {
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM dbo.Vehiculos WHERE IdVehiculo=@Id AND IdEmpresa=@Empresa;";
        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = idVehiculo; cmd.Parameters.Add("@Empresa", SqlDbType.Int).Value = idEmpresa;
        if (Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)) == 0) throw new KeyNotFoundException("El vehículo no pertenece a la empresa.");
    }
    private static async Task<AccesorioResponseDto?> LoadCatalogAsync(SqlConnection cn, int id, int empresa, CancellationToken ct)
    {
        await using var cmd = cn.CreateCommand(); cmd.CommandText = "SELECT IdAccesorio,Codigo,Nombre,Descripcion,Icono,IdEmpresa,Activo,RowVersion FROM dbo.Accesorios WHERE IdAccesorio=@Id AND (IdEmpresa IS NULL OR IdEmpresa=@Empresa);";
        cmd.Parameters.Add("@Id", SqlDbType.Int).Value=id; cmd.Parameters.Add("@Empresa", SqlDbType.Int).Value=empresa;
        await using var r=await cmd.ExecuteReaderAsync(ct); return await r.ReadAsync(ct) ? MapCatalog(r) : null;
    }
    private static void AddCatalog(SqlCommand c,int e,AccesorioCreateDto d){c.Parameters.Add("@IdEmpresa",SqlDbType.Int).Value=e;c.Parameters.Add("@Codigo",SqlDbType.NVarChar,50).Value=d.Codigo.Trim().ToUpperInvariant();c.Parameters.Add("@Nombre",SqlDbType.NVarChar,100).Value=d.Nombre.Trim();c.Parameters.Add("@Descripcion",SqlDbType.NVarChar,250).Value=(object?)d.Descripcion?.Trim()??DBNull.Value;c.Parameters.Add("@Icono",SqlDbType.NVarChar,100).Value=(object?)d.Icono?.Trim()??DBNull.Value;}
    private static void AddVehicle(SqlCommand c,int v,int e,int u,int a,string? o){c.Parameters.Add("@IdEmpresa",SqlDbType.Int).Value=e;c.Parameters.Add("@IdVehiculo",SqlDbType.Int).Value=v;c.Parameters.Add("@IdAccesorio",SqlDbType.Int).Value=a;c.Parameters.Add("@Usuario",SqlDbType.Int).Value=u;c.Parameters.Add("@Observaciones",SqlDbType.NVarChar,250).Value=(object?)o?.Trim()??DBNull.Value;}
    private static AccesorioResponseDto MapCatalog(SqlDataReader r)=>new(){IdAccesorio=r.GetInt32(0),Codigo=r.GetString(1),Nombre=r.GetString(2),Descripcion=r.IsDBNull(3)?null:r.GetString(3),Icono=r.IsDBNull(4)?null:r.GetString(4),EsGlobal=r.IsDBNull(5),Activo=r.GetBoolean(6),RowVersion=Convert.ToBase64String((byte[])r[7])};
    private static VehiculoAccesorioResponseDto MapVehicle(SqlDataReader r)=>new(){IdVehiculoAccesorio=r.GetInt32(0),IdVehiculo=r.GetInt32(1),IdAccesorio=r.GetInt32(2),Codigo=r.GetString(3),Nombre=r.GetString(4),Icono=r.IsDBNull(5)?null:r.GetString(5),Observaciones=r.IsDBNull(6)?null:r.GetString(6),Activo=r.GetBoolean(7)};
}

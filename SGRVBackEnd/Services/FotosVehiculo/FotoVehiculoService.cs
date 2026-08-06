using System.Data;
using Microsoft.Data.SqlClient;
using SGRVBackEnd.DTOs.FotosVehiculo;

namespace SGRVBackEnd.Services.FotosVehiculo;

public sealed class FotoVehiculoService : IFotoVehiculoService
{
    private readonly string _connectionString;
    private readonly string _root;
    public FotoVehiculoService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Falta DefaultConnection.");
        _root = Path.Combine(environment.ContentRootPath, "App_Data", "vehicle-images");
    }

    public async Task<IReadOnlyList<FotoVehiculoResponseDto>> GetAsync(int idVehiculo, int idEmpresa, CancellationToken ct)
    {
        await using var cn = await OpenAsync(ct); await EnsureVehicleAsync(cn,idVehiculo,idEmpresa,ct);
        await using var cmd=cn.CreateCommand(); cmd.CommandText=Select+" WHERE f.IdVehiculo=@Vehiculo AND f.IdEmpresa=@Empresa AND f.Activo=1 ORDER BY f.EsPrincipal DESC,f.Orden,f.IdFoto;";
        AddIds(cmd,idVehiculo,idEmpresa); var list=new List<FotoVehiculoResponseDto>(); await using var r=await cmd.ExecuteReaderAsync(ct); while(await r.ReadAsync(ct))list.Add(Map(r)); return list;
    }

    public async Task<FotoVehiculoResponseDto> UploadAsync(int idVehiculo,int idEmpresa,int idUsuario,FotoVehiculoUploadDto dto,CancellationToken ct)
    {
        await using var cn=await OpenAsync(ct); await EnsureVehicleAsync(cn,idVehiculo,idEmpresa,ct);
        var extension=dto.Archivo!.ContentType.ToLowerInvariant() switch{"image/jpeg"=>".jpg","image/png"=>".png","image/webp"=>".webp",_=>throw new InvalidOperationException("Tipo de archivo no permitido.")};
        var relative=Path.Combine(idEmpresa.ToString(),idVehiculo.ToString(),$"{Guid.NewGuid():N}{extension}");
        var absolute=Path.Combine(_root,relative); Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
        await using(var output=new FileStream(absolute,FileMode.CreateNew,FileAccess.Write,FileShare.None,81920,true)) await dto.Archivo.CopyToAsync(output,ct);
        await using var tx=(SqlTransaction)await cn.BeginTransactionAsync(ct);
        try
        {
            if(dto.EsPrincipal){await using var clear=cn.CreateCommand();clear.Transaction=tx;clear.CommandText="UPDATE dbo.FotosVehiculo SET EsPrincipal=0,FechaActualizacion=SYSUTCDATETIME() WHERE IdEmpresa=@Empresa AND IdVehiculo=@Vehiculo AND Activo=1;";AddIds(clear,idVehiculo,idEmpresa);await clear.ExecuteNonQueryAsync(ct);}
            await using var cmd=cn.CreateCommand();cmd.Transaction=tx;cmd.CommandText="""
                INSERT dbo.FotosVehiculo(IdEmpresa,IdVehiculo,Url,ClaveAlmacenamiento,NombreArchivo,TipoContenido,TamanioBytes,Titulo,TextoAlternativo,EsPrincipal,Orden,IdUsuarioRegistro)
                OUTPUT INSERTED.IdFoto VALUES(@Empresa,@Vehiculo,'',@Clave,@Nombre,@Tipo,@Tamano,@Titulo,@Alt,@Principal,@Orden,@Usuario);
                """;
            AddIds(cmd,idVehiculo,idEmpresa);cmd.Parameters.Add("@Clave",SqlDbType.NVarChar,500).Value=relative;cmd.Parameters.Add("@Nombre",SqlDbType.NVarChar,255).Value=Path.GetFileName(dto.Archivo.FileName);cmd.Parameters.Add("@Tipo",SqlDbType.NVarChar,100).Value=dto.Archivo.ContentType;cmd.Parameters.Add("@Tamano",SqlDbType.BigInt).Value=dto.Archivo.Length;cmd.Parameters.Add("@Titulo",SqlDbType.NVarChar,150).Value=Db(dto.Titulo);cmd.Parameters.Add("@Alt",SqlDbType.NVarChar,250).Value=Db(dto.TextoAlternativo);cmd.Parameters.Add("@Principal",SqlDbType.Bit).Value=dto.EsPrincipal;cmd.Parameters.Add("@Orden",SqlDbType.Int).Value=dto.Orden;cmd.Parameters.Add("@Usuario",SqlDbType.Int).Value=idUsuario;
            var id=Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
            await using var url=cn.CreateCommand();url.Transaction=tx;url.CommandText="UPDATE dbo.FotosVehiculo SET Url=@Url WHERE IdFoto=@Foto AND IdEmpresa=@Empresa;";url.Parameters.Add("@Url",SqlDbType.NVarChar,500).Value=$"/api/vehiculos/{idVehiculo}/fotos/{id}/contenido";url.Parameters.Add("@Foto",SqlDbType.Int).Value=id;url.Parameters.Add("@Empresa",SqlDbType.Int).Value=idEmpresa;await url.ExecuteNonQueryAsync(ct);
            await tx.CommitAsync(ct); return (await LoadAsync(cn,idVehiculo,id,idEmpresa,ct))!;
        }
        catch{await tx.RollbackAsync(ct);if(File.Exists(absolute))File.Delete(absolute);throw;}
    }

    public async Task<FotoVehiculoResponseDto?> UpdateAsync(int idVehiculo,int idFoto,int idEmpresa,int idUsuario,FotoVehiculoUpdateDto dto,CancellationToken ct)
    {
        await using var cn=await OpenAsync(ct);await using var cmd=cn.CreateCommand();cmd.CommandText="UPDATE dbo.FotosVehiculo SET Titulo=@Titulo,TextoAlternativo=@Alt,Orden=@Orden,IdUsuarioActualizacion=@Usuario,FechaActualizacion=SYSUTCDATETIME() WHERE IdFoto=@Foto AND IdVehiculo=@Vehiculo AND IdEmpresa=@Empresa AND RowVersion=@Version;";AddIds(cmd,idVehiculo,idEmpresa,idFoto);cmd.Parameters.Add("@Titulo",SqlDbType.NVarChar,150).Value=Db(dto.Titulo);cmd.Parameters.Add("@Alt",SqlDbType.NVarChar,250).Value=Db(dto.TextoAlternativo);cmd.Parameters.Add("@Orden",SqlDbType.Int).Value=dto.Orden;cmd.Parameters.Add("@Usuario",SqlDbType.Int).Value=idUsuario;cmd.Parameters.Add("@Version",SqlDbType.Timestamp,8).Value=Convert.FromBase64String(dto.RowVersion);if(await cmd.ExecuteNonQueryAsync(ct)==0)return null;return await LoadAsync(cn,idVehiculo,idFoto,idEmpresa,ct);
    }

    public async Task<FotoVehiculoResponseDto?> SetPrincipalAsync(int idVehiculo,int idFoto,int idEmpresa,int idUsuario,CancellationToken ct)
    {
        await using var cn=await OpenAsync(ct);await using var tx=(SqlTransaction)await cn.BeginTransactionAsync(ct);await using var cmd=cn.CreateCommand();cmd.Transaction=tx;cmd.CommandText="""
          IF NOT EXISTS(SELECT 1 FROM dbo.FotosVehiculo WHERE IdFoto=@Foto AND IdVehiculo=@Vehiculo AND IdEmpresa=@Empresa AND Activo=1) SELECT 0;
          ELSE BEGIN UPDATE dbo.FotosVehiculo SET EsPrincipal=CASE WHEN IdFoto=@Foto THEN 1 ELSE 0 END,IdUsuarioActualizacion=@Usuario,FechaActualizacion=SYSUTCDATETIME() WHERE IdVehiculo=@Vehiculo AND IdEmpresa=@Empresa AND Activo=1; SELECT 1; END
          """;AddIds(cmd,idVehiculo,idEmpresa,idFoto);cmd.Parameters.Add("@Usuario",SqlDbType.Int).Value=idUsuario;var ok=Convert.ToInt32(await cmd.ExecuteScalarAsync(ct))==1;await tx.CommitAsync(ct);return ok?await LoadAsync(cn,idVehiculo,idFoto,idEmpresa,ct):null;
    }

    public async Task<bool> DeleteAsync(int idVehiculo,int idFoto,int idEmpresa,int idUsuario,CancellationToken ct)
    {await using var cn=await OpenAsync(ct);await using var cmd=cn.CreateCommand();cmd.CommandText="UPDATE dbo.FotosVehiculo SET Activo=0,EsPrincipal=0,IdUsuarioActualizacion=@Usuario,FechaActualizacion=SYSUTCDATETIME() WHERE IdFoto=@Foto AND IdVehiculo=@Vehiculo AND IdEmpresa=@Empresa AND Activo=1;";AddIds(cmd,idVehiculo,idEmpresa,idFoto);cmd.Parameters.Add("@Usuario",SqlDbType.Int).Value=idUsuario;return await cmd.ExecuteNonQueryAsync(ct)>0;}

    public async Task<(Stream Stream,string ContentType,string Name)?> OpenContentAsync(int idVehiculo,int idFoto,int idEmpresa,CancellationToken ct)
    {await using var cn=await OpenAsync(ct);await using var cmd=cn.CreateCommand();cmd.CommandText="SELECT ClaveAlmacenamiento,TipoContenido,NombreArchivo FROM dbo.FotosVehiculo WHERE IdFoto=@Foto AND IdVehiculo=@Vehiculo AND IdEmpresa=@Empresa AND Activo=1;";AddIds(cmd,idVehiculo,idEmpresa,idFoto);await using var r=await cmd.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct)||r.IsDBNull(0))return null;var key=r.GetString(0);var full=Path.GetFullPath(Path.Combine(_root,key));if(!full.StartsWith(Path.GetFullPath(_root),StringComparison.OrdinalIgnoreCase)||!File.Exists(full))return null;return(new FileStream(full,FileMode.Open,FileAccess.Read,FileShare.Read,81920,true),r.IsDBNull(1)?"application/octet-stream":r.GetString(1),r.IsDBNull(2)?Path.GetFileName(full):r.GetString(2));}

    private const string Select="SELECT f.IdFoto,f.IdVehiculo,f.Url,f.NombreArchivo,f.TipoContenido,f.TamanioBytes,f.Titulo,f.TextoAlternativo,f.EsPrincipal,f.Orden,f.Activo,f.FechaSubida,f.RowVersion FROM dbo.FotosVehiculo f";
    private async Task<SqlConnection> OpenAsync(CancellationToken ct){var c=new SqlConnection(_connectionString);await c.OpenAsync(ct);return c;}
    private static async Task EnsureVehicleAsync(SqlConnection c,int v,int e,CancellationToken ct){await using var x=c.CreateCommand();x.CommandText="SELECT COUNT(1) FROM dbo.Vehiculos WHERE IdVehiculo=@Vehiculo AND IdEmpresa=@Empresa;";AddIds(x,v,e);if(Convert.ToInt32(await x.ExecuteScalarAsync(ct))==0)throw new KeyNotFoundException("El vehículo no pertenece a la empresa.");}
    private static async Task<FotoVehiculoResponseDto?> LoadAsync(SqlConnection c,int v,int f,int e,CancellationToken ct){await using var x=c.CreateCommand();x.CommandText=Select+" WHERE f.IdFoto=@Foto AND f.IdVehiculo=@Vehiculo AND f.IdEmpresa=@Empresa;";AddIds(x,v,e,f);await using var r=await x.ExecuteReaderAsync(ct);return await r.ReadAsync(ct)?Map(r):null;}
    private static void AddIds(SqlCommand c,int v,int e,int? f=null){c.Parameters.Add("@Vehiculo",SqlDbType.Int).Value=v;c.Parameters.Add("@Empresa",SqlDbType.Int).Value=e;if(f.HasValue)c.Parameters.Add("@Foto",SqlDbType.Int).Value=f.Value;}
    private static object Db(string? value)=>string.IsNullOrWhiteSpace(value)?DBNull.Value:value.Trim();
    private static FotoVehiculoResponseDto Map(SqlDataReader r)=>new(){IdFoto=r.GetInt32(0),IdVehiculo=r.GetInt32(1),Url=r.GetString(2),NombreArchivo=r.IsDBNull(3)?null:r.GetString(3),TipoContenido=r.IsDBNull(4)?null:r.GetString(4),TamanioBytes=r.IsDBNull(5)?null:r.GetInt64(5),Titulo=r.IsDBNull(6)?null:r.GetString(6),TextoAlternativo=r.IsDBNull(7)?null:r.GetString(7),EsPrincipal=r.GetBoolean(8),Orden=r.GetInt32(9),Activo=r.GetBoolean(10),FechaSubida=r.GetDateTime(11),RowVersion=Convert.ToBase64String((byte[])r[12])};
}

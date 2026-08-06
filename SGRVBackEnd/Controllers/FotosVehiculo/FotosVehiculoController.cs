using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGRVBackEnd.DTOs.FotosVehiculo;
using SGRVBackEnd.Services.FotosVehiculo;
using SGRVBackEnd.Shared;
using SGRVBackEnd.Validators;

namespace SGRVBackEnd.Controllers.FotosVehiculo;

[ApiController,Authorize,Route("api/vehiculos/{idVehiculo:int}/fotos")]
public sealed class FotosVehiculoController : BaseApiController
{
    private const string Managers="ADMIN,SUPADMIN,Admin,SuperUsuario";
    private readonly IFotoVehiculoService _service;
    public FotosVehiculoController(IFotoVehiculoService service)=>_service=service;
    [HttpGet] public async Task<ActionResult<ApiResponse<IReadOnlyList<FotoVehiculoResponseDto>>>> Get(int idVehiculo,CancellationToken ct){try{return Ok(Success(await _service.GetAsync(idVehiculo,GetEmpresaId(),ct),"Galería obtenida correctamente."));}catch(KeyNotFoundException ex){return NotFound(Failure<IReadOnlyList<FotoVehiculoResponseDto>>(ex.Message));}}
    [HttpPost,Consumes("multipart/form-data"),Authorize(Roles=Managers),RequestSizeLimit(10_485_760)] public async Task<ActionResult<ApiResponse<FotoVehiculoResponseDto>>> Upload(int idVehiculo,[FromForm]FotoVehiculoUploadDto dto,CancellationToken ct){var error=VehiculoMediaValidator.Validate(dto);if(error!=null)return BadRequest(Failure<FotoVehiculoResponseDto>(error));try{return StatusCode(201,Success(await _service.UploadAsync(idVehiculo,GetEmpresaId(),GetUsuarioId(),dto,ct),"Foto cargada correctamente."));}catch(KeyNotFoundException ex){return NotFound(Failure<FotoVehiculoResponseDto>(ex.Message));}}
    [HttpPut("{idFoto:int}"),Authorize(Roles=Managers)] public async Task<ActionResult<ApiResponse<FotoVehiculoResponseDto>>> Update(int idVehiculo,int idFoto,FotoVehiculoUpdateDto dto,CancellationToken ct){if(dto.Orden<0||!VehiculoMediaValidator.IsRowVersion(dto.RowVersion))return BadRequest(Failure<FotoVehiculoResponseDto>("Orden o RowVersion no válido."));var item=await _service.UpdateAsync(idVehiculo,idFoto,GetEmpresaId(),GetUsuarioId(),dto,ct);return item is null?Conflict(Failure<FotoVehiculoResponseDto>("La foto no existe o fue modificada.")):Ok(Success(item,"Foto actualizada correctamente."));}
    [HttpPatch("{idFoto:int}/principal"),Authorize(Roles=Managers)] public async Task<ActionResult<ApiResponse<FotoVehiculoResponseDto>>> Principal(int idVehiculo,int idFoto,CancellationToken ct){var item=await _service.SetPrincipalAsync(idVehiculo,idFoto,GetEmpresaId(),GetUsuarioId(),ct);return item is null?NotFound(Failure<FotoVehiculoResponseDto>("No se encontró la foto.")):Ok(Success(item,"Portada actualizada correctamente."));}
    [HttpDelete("{idFoto:int}"),Authorize(Roles=Managers)] public async Task<ActionResult<ApiResponse<object>>> Delete(int idVehiculo,int idFoto,CancellationToken ct)=>await _service.DeleteAsync(idVehiculo,idFoto,GetEmpresaId(),GetUsuarioId(),ct)?Ok(Success<object>(new{idFoto},"Foto eliminada correctamente.")):NotFound(Failure<object>("No se encontró la foto."));
    [HttpGet("{idFoto:int}/contenido")] public async Task<IActionResult> Content(int idVehiculo,int idFoto,CancellationToken ct){var content=await _service.OpenContentAsync(idVehiculo,idFoto,GetEmpresaId(),ct);return content is null?NotFound():File(content.Value.Stream,content.Value.ContentType,enableRangeProcessing:true);}
    private static ApiResponse<T> Success<T>(T data,string message)=>new(){Success=true,Message=message,Data=data};private static ApiResponse<T> Failure<T>(string message)=>new(){Success=false,Message=message};
}

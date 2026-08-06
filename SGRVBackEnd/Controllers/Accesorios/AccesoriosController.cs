using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGRVBackEnd.DTOs.Accesorios;
using SGRVBackEnd.Services.Accesorios;
using SGRVBackEnd.Shared;
using SGRVBackEnd.Validators;

namespace SGRVBackEnd.Controllers.Accesorios;

[ApiController, Authorize, Route("api/accesorios")]
public sealed class AccesoriosController : BaseApiController
{
    private const string Managers="ADMIN,SUPADMIN,Admin,SuperUsuario";
    private readonly IAccesorioService _service;
    public AccesoriosController(IAccesorioService service)=>_service=service;

    [HttpGet] public async Task<ActionResult<ApiResponse<IReadOnlyList<AccesorioResponseDto>>>> GetAll([FromQuery]bool incluirInactivos,CancellationToken ct)=>Ok(OkResponse(await _service.GetCatalogAsync(GetEmpresaId(),incluirInactivos,ct),"Accesorios obtenidos correctamente."));
    [HttpPost,Authorize(Roles=Managers)] public async Task<ActionResult<ApiResponse<AccesorioResponseDto>>> Create(AccesorioCreateDto dto,CancellationToken ct){var error=VehiculoMediaValidator.Validate(dto);if(error!=null)return BadRequest(Fail<AccesorioResponseDto>(error));try{var item=await _service.CreateAsync(GetEmpresaId(),dto,ct);return StatusCode(201,OkResponse(item,"Accesorio creado correctamente."));}catch(InvalidOperationException ex){return Conflict(Fail<AccesorioResponseDto>(ex.Message));}}
    [HttpPut("{id:int}"),Authorize(Roles=Managers)] public async Task<ActionResult<ApiResponse<AccesorioResponseDto>>> Update(int id,AccesorioUpdateDto dto,CancellationToken ct){var error=VehiculoMediaValidator.Validate(dto);if(error!=null||!VehiculoMediaValidator.IsRowVersion(dto.RowVersion))return BadRequest(Fail<AccesorioResponseDto>(error??"RowVersion no válido."));try{var item=await _service.UpdateAsync(id,GetEmpresaId(),dto,ct);return item is null?Conflict(Fail<AccesorioResponseDto>("El accesorio no existe, es global o fue modificado.")):Ok(OkResponse(item,"Accesorio actualizado correctamente."));}catch(InvalidOperationException ex){return Conflict(Fail<AccesorioResponseDto>(ex.Message));}}
    [HttpDelete("{id:int}"),Authorize(Roles=Managers)] public async Task<ActionResult<ApiResponse<object>>> Delete(int id,CancellationToken ct)=>await _service.SetCatalogActiveAsync(id,GetEmpresaId(),false,ct)?Ok(OkResponse<object>(new{id},"Accesorio desactivado correctamente.")):NotFound(Fail<object>("No se encontró un accesorio privado de la empresa."));
    private static ApiResponse<T> OkResponse<T>(T data,string message)=>new(){Success=true,Message=message,Data=data};
    private static ApiResponse<T> Fail<T>(string message)=>new(){Success=false,Message=message};
}

[ApiController, Authorize, Route("api/vehiculos/{idVehiculo:int}/accesorios")]
public sealed class VehiculosAccesoriosController : BaseApiController
{
    private const string Managers="ADMIN,SUPADMIN,Admin,SuperUsuario";
    private readonly IAccesorioService _service;
    public VehiculosAccesoriosController(IAccesorioService service)=>_service=service;
    [HttpGet] public async Task<ActionResult<ApiResponse<IReadOnlyList<VehiculoAccesorioResponseDto>>>> Get(int idVehiculo,CancellationToken ct){try{return Ok(Success(await _service.GetVehicleAsync(idVehiculo,GetEmpresaId(),ct),"Accesorios del vehículo obtenidos correctamente."));}catch(KeyNotFoundException ex){return NotFound(Failure<IReadOnlyList<VehiculoAccesorioResponseDto>>(ex.Message));}}
    [HttpPost,Authorize(Roles=Managers)] public async Task<ActionResult<ApiResponse<VehiculoAccesorioResponseDto>>> Assign(int idVehiculo,VehiculoAccesorioCreateDto dto,CancellationToken ct){if(dto.IdAccesorio<=0)return BadRequest(Failure<VehiculoAccesorioResponseDto>("IdAccesorio es obligatorio."));try{return StatusCode(201,Success(await _service.AssignAsync(idVehiculo,GetEmpresaId(),GetUsuarioId(),dto,ct),"Accesorio vinculado correctamente."));}catch(KeyNotFoundException ex){return NotFound(Failure<VehiculoAccesorioResponseDto>(ex.Message));}catch(InvalidOperationException ex){return BadRequest(Failure<VehiculoAccesorioResponseDto>(ex.Message));}}
    [HttpDelete("{idAccesorio:int}"),Authorize(Roles=Managers)] public async Task<ActionResult<ApiResponse<object>>> Remove(int idVehiculo,int idAccesorio,CancellationToken ct)=>await _service.RemoveAsync(idVehiculo,idAccesorio,GetEmpresaId(),GetUsuarioId(),ct)?Ok(Success<object>(new{idVehiculo,idAccesorio},"Accesorio retirado correctamente.")):NotFound(Failure<object>("No se encontró la vinculación."));
    private static ApiResponse<T> Success<T>(T data,string message)=>new(){Success=true,Message=message,Data=data}; private static ApiResponse<T> Failure<T>(string message)=>new(){Success=false,Message=message};
}

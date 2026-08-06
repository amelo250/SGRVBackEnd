using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGRVBackEnd.DTOs.DocumentosCliente;
using SGRVBackEnd.Services.DocumentosCliente;
using SGRVBackEnd.Shared;
using SGRVBackEnd.Validators;

namespace SGRVBackEnd.Controllers.DocumentosCliente;

[ApiController]
[Authorize]
[Route("api/clientes/{idCliente:int}/documentos")]
public sealed class DocumentosClienteController : BaseApiController
{
    private const string ManagerRoles = "Admin,ADMIN,SUPADMIN,SuperUsuario";
    private readonly IDocumentoClienteService _service;

    public DocumentosClienteController(IDocumentoClienteService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DocumentoClienteResponseDto>>>> GetAll(
        int idCliente,
        CancellationToken cancellationToken)
    {
        try
        {
            var data = await _service.GetAllAsync(
                idCliente, GetEmpresaId(), cancellationToken);
            return Ok(Success(data, "Documentos obtenidos correctamente."));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(Failure<IReadOnlyList<DocumentoClienteResponseDto>>(
                exception.Message));
        }
    }

    [HttpGet("tipos")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TipoDocumentoClienteDto>>>> GetTypes(
        int idCliente,
        CancellationToken cancellationToken)
    {
        try
        {
            var data = await _service.GetTypesAsync(
                idCliente, GetEmpresaId(), cancellationToken);
            return Ok(Success(data, "Tipos de documento obtenidos correctamente."));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(Failure<IReadOnlyList<TipoDocumentoClienteDto>>(
                exception.Message));
        }
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_485_760)]
    [Authorize(Roles = ManagerRoles)]
    public async Task<ActionResult<ApiResponse<DocumentoClienteResponseDto>>> Upload(
        int idCliente,
        [FromForm] DocumentoClienteUploadDto request,
        CancellationToken cancellationToken)
    {
        var error = DocumentoClienteValidator.Validate(request);
        if (error is not null)
            return BadRequest(Failure<DocumentoClienteResponseDto>(error));

        try
        {
            var item = await _service.UploadAsync(
                idCliente, GetEmpresaId(), request, cancellationToken);
            return StatusCode(
                StatusCodes.Status201Created,
                Success(item, "Documento cargado correctamente."));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(Failure<DocumentoClienteResponseDto>(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(Failure<DocumentoClienteResponseDto>(exception.Message));
        }
    }

    [HttpGet("{idDocumento:int}/contenido")]
    public async Task<IActionResult> Content(
        int idCliente,
        int idDocumento,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = await _service.OpenAsync(
                idCliente, idDocumento, GetEmpresaId(), cancellationToken);
            return content is null
                ? NotFound()
                : File(
                    content.Value.Stream,
                    content.Value.ContentType,
                    enableRangeProcessing: true);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{idDocumento:int}")]
    [Authorize(Roles = ManagerRoles)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int idCliente,
        int idDocumento,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _service.DeleteAsync(
                idCliente, idDocumento, GetEmpresaId(), cancellationToken);
            return deleted
                ? Ok(Success<object>(new { idDocumento }, "Documento eliminado correctamente."))
                : NotFound(Failure<object>("No se encontró el documento solicitado."));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(Failure<object>(exception.Message));
        }
    }

    private static ApiResponse<T> Success<T>(T data, string message) =>
        new() { Success = true, Message = message, Data = data };

    private static ApiResponse<T> Failure<T>(string message) =>
        new() { Success = false, Message = message };
}

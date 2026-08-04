using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGRVBackEnd.Data;
using SGRVBackEnd.DTOs.Reservaciones;
using SGRVBackEnd.Helpers;
using SGRVBackEnd.Models.Reservaciones;
using SGRVBackEnd.Services.Reservations;
using SGRVBackEnd.Shared;
using System.Data;

namespace SGRVBackEnd.Controllers;

[ApiController]
[Authorize]
[Route("api/reservaciones")]
public sealed class ReservacionesController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly IReservationAvailabilityService _availability;

    public ReservacionesController(
        AppDbContext context,
        IReservationAvailabilityService availability)
    {
        _context = context;
        _availability = availability;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<ReservacionResponseDto>>>> GetAll(
        [FromQuery] ReservacionSearchDto search,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        var query = BuildResponseQuery(idEmpresa);

        if (search.IdEstado.HasValue)
            query = query.Where(x => x.IdEstado == search.IdEstado.Value);
        if (search.FechaDesde.HasValue)
            query = query.Where(x => x.FechaFin >= search.FechaDesde.Value.UtcDateTime);
        if (search.FechaHasta.HasValue)
            query = query.Where(x => x.FechaInicio <= search.FechaHasta.Value.UtcDateTime);

        var term = search.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(x =>
                x.ClienteNombre.Contains(term) ||
                x.VehiculoDescripcion.Contains(term) ||
                x.EstadoNombre.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var data = await query
            .OrderByDescending(x => x.FechaInicio)
            .Skip((search.PageNumber - 1) * search.PageSize)
            .Take(search.PageSize)
            .ToListAsync(cancellationToken);

        Response.Headers.Append("X-Total-Count", total.ToString());
        Response.Headers.Append("X-Page-Number", search.PageNumber.ToString());
        Response.Headers.Append("X-Page-Size", search.PageSize.ToString());

        return Ok(Success<IEnumerable<ReservacionResponseDto>>(
            data, "Reservaciones obtenidas correctamente."));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ReservacionResponseDto>>> GetById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await BuildResponseQuery(GetEmpresaId())
            .FirstOrDefaultAsync(x => x.IdReservacion == id, cancellationToken);

        return result is null
            ? NotFound(Failure<ReservacionResponseDto>(
                "No se encontró la reservación solicitada."))
            : Ok(Success(result, "Reservación obtenida correctamente."));
    }

    [HttpGet("proximas")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ReservacionResponseDto>>>> GetUpcoming(
        [FromQuery] int take = 10,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 50);
        var now = DateTime.UtcNow;
        var data = await BuildResponseQuery(GetEmpresaId())
            .Where(x =>
                x.FechaInicio >= now &&
                x.EstadoCodigo != ReservationConstants.Cancelled &&
                x.EstadoCodigo != ReservationConstants.Converted)
            .OrderBy(x => x.FechaInicio)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Ok(Success<IEnumerable<ReservacionResponseDto>>(
            data, "Próximas reservaciones obtenidas correctamente."));
    }

    [HttpGet("disponibilidad")]
    public async Task<ActionResult<ApiResponse<object>>> GetAvailability(
        [FromQuery] int idVehiculo,
        [FromQuery] DateTimeOffset fechaInicio,
        [FromQuery] DateTimeOffset fechaFin,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateDates(fechaInicio, fechaFin);
        if (validation is not null)
            return BadRequest(Failure<object>(validation));

        var idEmpresa = GetEmpresaId();
        var vehicleExists = await _context.Vehiculos.AsNoTracking().AnyAsync(x =>
            x.IdVehiculo == idVehiculo && x.IdEmpresa == idEmpresa && x.Activo,
            cancellationToken);
        if (!vehicleExists)
            return NotFound(Failure<object>(
                "No se encontró el vehículo solicitado."));

        var available = !await _availability.HasOverlapAsync(
            idEmpresa, idVehiculo, fechaInicio.UtcDateTime, fechaFin.UtcDateTime,
            null, cancellationToken);

        return Ok(Success<object>(
            new { IdVehiculo = idVehiculo, Disponible = available },
            available
                ? "El vehículo está disponible."
                : "El vehículo no está disponible para el periodo indicado."));
    }

    [HttpPost]
    [Authorize(Roles = ApplicationRoles.ReservationManagers)]
    public async Task<ActionResult<ApiResponse<ReservacionResponseDto>>> Create(
        [FromBody] ReservacionCreateDto request,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var validation = await ValidateRequest(
            request, idEmpresa, null, cancellationToken);
        if (validation is not null)
            return ValidationFailure<ReservacionResponseDto>(validation);

        var idEstado = await GetStateId(ReservationConstants.Pending, cancellationToken);
        if (!idEstado.HasValue)
            return BadRequest(Failure<ReservacionResponseDto>(
                "No existe el estado RESERVACION/PENDIENTE."));

        var entity = new Reservacion
        {
            IdEmpresa = idEmpresa,
            IdVehiculo = request.IdVehiculo,
            IdCliente = request.IdCliente,
            IdEstado = idEstado.Value,
            FechaInicio = request.FechaInicio.UtcDateTime,
            FechaFin = request.FechaFin.UtcDateTime,
            Observacion = request.Observacion?.Trim() ?? string.Empty,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Reservaciones.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        var result = await BuildResponseQuery(idEmpresa)
            .FirstAsync(x => x.IdReservacion == entity.IdReservacion, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = entity.IdReservacion },
            Success(result, "Reservación creada correctamente."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = ApplicationRoles.ReservationManagers)]
    public async Task<ActionResult<ApiResponse<ReservacionResponseDto>>> Update(
        int id,
        [FromBody] ReservacionUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        var entity = await _context.Reservaciones.FirstOrDefaultAsync(
            x => x.IdReservacion == id && x.IdEmpresa == idEmpresa,
            cancellationToken);
        if (entity is null)
            return NotFound(Failure<ReservacionResponseDto>(
                "No se encontró la reservación solicitada."));

        var currentCode = await GetStateCode(entity.IdEstado, cancellationToken);
        if (currentCode is not (ReservationConstants.Pending or ReservationConstants.Confirmed))
            return Conflict(Failure<ReservacionResponseDto>(
                "La reservación no puede modificarse en su estado actual."));

        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var validation = await ValidateRequest(
            request, idEmpresa, id, cancellationToken);
        if (validation is not null)
            return ValidationFailure<ReservacionResponseDto>(validation);

        entity.IdVehiculo = request.IdVehiculo;
        entity.IdCliente = request.IdCliente;
        entity.FechaInicio = request.FechaInicio.UtcDateTime;
        entity.FechaFin = request.FechaFin.UtcDateTime;
        entity.Observacion = request.Observacion?.Trim() ?? string.Empty;

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        var result = await BuildResponseQuery(idEmpresa)
            .FirstAsync(x => x.IdReservacion == id, cancellationToken);

        return Ok(Success(result, "Reservación actualizada correctamente."));
    }

    [HttpPatch("{id:int}/confirmar")]
    [Authorize(Roles = ApplicationRoles.ReservationManagers)]
    public Task<ActionResult<ApiResponse<ReservacionResponseDto>>> Confirm(
        int id,
        CancellationToken cancellationToken = default) =>
        ChangeState(id, ReservationConstants.Pending, ReservationConstants.Confirmed,
            "Reservación confirmada correctamente.", cancellationToken);

    [HttpPatch("{id:int}/cancelar")]
    [Authorize(Roles = ApplicationRoles.ReservationManagers)]
    public Task<ActionResult<ApiResponse<ReservacionResponseDto>>> Cancel(
        int id,
        CancellationToken cancellationToken = default) =>
        CancelInternal(id, cancellationToken);

    [HttpDelete("{id:int}")]
    [Authorize(Roles = ApplicationRoles.ReservationManagers)]
    public Task<ActionResult<ApiResponse<ReservacionResponseDto>>> Delete(
        int id,
        CancellationToken cancellationToken = default) =>
        CancelInternal(id, cancellationToken);

    private async Task<ActionResult<ApiResponse<ReservacionResponseDto>>> CancelInternal(
        int id,
        CancellationToken cancellationToken)
    {
        var idEmpresa = GetEmpresaId();
        var entity = await _context.Reservaciones.FirstOrDefaultAsync(
            x => x.IdReservacion == id && x.IdEmpresa == idEmpresa,
            cancellationToken);
        if (entity is null)
            return NotFound(Failure<ReservacionResponseDto>(
                "No se encontró la reservación solicitada."));

        var currentCode = await GetStateCode(entity.IdEstado, cancellationToken);
        if (currentCode == ReservationConstants.Cancelled)
            return Conflict(Failure<ReservacionResponseDto>(
                "La reservación ya se encuentra cancelada."));
        if (currentCode == ReservationConstants.Converted)
            return Conflict(Failure<ReservacionResponseDto>(
                "Una reservación convertida no puede cancelarse."));

        var targetId = await GetStateId(ReservationConstants.Cancelled, cancellationToken);
        if (!targetId.HasValue)
            return BadRequest(Failure<ReservacionResponseDto>(
                "No existe el estado RESERVACION/CANCELADA."));

        entity.IdEstado = targetId.Value;
        await _context.SaveChangesAsync(cancellationToken);
        var result = await BuildResponseQuery(idEmpresa)
            .FirstAsync(x => x.IdReservacion == id, cancellationToken);
        return Ok(Success(result, "Reservación cancelada correctamente."));
    }

    private async Task<ActionResult<ApiResponse<ReservacionResponseDto>>> ChangeState(
        int id,
        string requiredCode,
        string targetCode,
        string message,
        CancellationToken cancellationToken)
    {
        var idEmpresa = GetEmpresaId();
        var entity = await _context.Reservaciones.FirstOrDefaultAsync(
            x => x.IdReservacion == id && x.IdEmpresa == idEmpresa,
            cancellationToken);
        if (entity is null)
            return NotFound(Failure<ReservacionResponseDto>(
                "No se encontró la reservación solicitada."));

        var currentCode = await GetStateCode(entity.IdEstado, cancellationToken);
        if (currentCode != requiredCode)
            return Conflict(Failure<ReservacionResponseDto>(
                $"La reservación debe estar en estado {requiredCode}."));
        var validation = await ValidateRelatedEntities(
            entity.IdCliente, entity.IdVehiculo, idEmpresa, cancellationToken);
        if (validation is not null)
            return ValidationFailure<ReservacionResponseDto>(validation);

        if (await _availability.HasOverlapAsync(
                idEmpresa, entity.IdVehiculo, entity.FechaInicio, entity.FechaFin,
                id, cancellationToken))
            return Conflict(Failure<ReservacionResponseDto>(
                "El vehículo ya no está disponible para el periodo indicado."));

        var targetId = await GetStateId(targetCode, cancellationToken);
        if (!targetId.HasValue)
            return BadRequest(Failure<ReservacionResponseDto>(
                $"No existe el estado {ReservationConstants.Category}/{targetCode}."));

        entity.IdEstado = targetId.Value;
        await _context.SaveChangesAsync(cancellationToken);
        var result = await BuildResponseQuery(idEmpresa)
            .FirstAsync(x => x.IdReservacion == id, cancellationToken);
        return Ok(Success(result, message));
    }

    private async Task<RequestValidationError?> ValidateRequest(
        ReservacionCreateDto request,
        int idEmpresa,
        int? idReservacion,
        CancellationToken cancellationToken)
    {
        var dateError = ValidateDates(request.FechaInicio, request.FechaFin);
        if (dateError is not null)
            return new RequestValidationError(dateError, ValidationErrorKind.InvalidInput);

        var relatedError = await ValidateRelatedEntities(
            request.IdCliente, request.IdVehiculo, idEmpresa, cancellationToken);
        if (relatedError is not null)
            return relatedError;

        return await _availability.HasOverlapAsync(
            idEmpresa, request.IdVehiculo,
            request.FechaInicio.UtcDateTime, request.FechaFin.UtcDateTime,
            idReservacion, cancellationToken)
            ? new RequestValidationError(
                "El vehículo ya tiene una reservación o renta para el periodo indicado.",
                ValidationErrorKind.Conflict)
            : null;
    }

    private async Task<RequestValidationError?> ValidateRelatedEntities(
        int clientId,
        int vehicleId,
        int companyId,
        CancellationToken cancellationToken)
    {
        if (!await _context.Clientes.AsNoTracking().AnyAsync(x =>
                x.IdCliente == clientId && x.IdEmpresa == companyId && x.Activo,
                cancellationToken))
            return new RequestValidationError(
                "No se encontró un cliente activo con el identificador indicado.",
                ValidationErrorKind.NotFound);

        if (!await _context.Vehiculos.AsNoTracking().AnyAsync(x =>
                x.IdVehiculo == vehicleId && x.IdEmpresa == companyId && x.Activo,
                cancellationToken))
            return new RequestValidationError(
                "No se encontró un vehículo activo con el identificador indicado.",
                ValidationErrorKind.NotFound);

        return null;
    }

    private IQueryable<ReservacionResponseDto> BuildResponseQuery(int idEmpresa) =>
        from reservation in _context.Reservaciones.AsNoTracking()
        join vehicle in _context.Vehiculos.AsNoTracking()
            on reservation.IdVehiculo equals vehicle.IdVehiculo
        join client in _context.Clientes.AsNoTracking()
            on reservation.IdCliente equals client.IdCliente
        join state in _context.Estados.AsNoTracking()
            on reservation.IdEstado equals state.IdEstado
        where reservation.IdEmpresa == idEmpresa &&
              vehicle.IdEmpresa == idEmpresa &&
              client.IdEmpresa == idEmpresa &&
              state.Categoria == ReservationConstants.Category
        select new ReservacionResponseDto
        {
            IdReservacion = reservation.IdReservacion,
            IdVehiculo = reservation.IdVehiculo,
            VehiculoDescripcion = vehicle.Marca + " " + vehicle.Modelo +
                " (" + vehicle.Placa + ")",
            IdCliente = reservation.IdCliente,
            ClienteNombre = client.Nombre + " " + client.Apellido,
            IdEstado = reservation.IdEstado,
            EstadoCodigo = state.Codigo,
            EstadoNombre = state.Nombre,
            FechaInicio = reservation.FechaInicio,
            FechaFin = reservation.FechaFin,
            Observacion = reservation.Observacion,
            FechaCreacion = reservation.FechaCreacion
        };

    private Task<int?> GetStateId(string code, CancellationToken cancellationToken) =>
        _context.Estados.AsNoTracking()
            .Where(x => x.Categoria == ReservationConstants.Category &&
                        x.Codigo == code && x.Activo)
            .Select(x => (int?)x.IdEstado)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<string?> GetStateCode(
        int idEstado,
        CancellationToken cancellationToken) =>
        await _context.Estados.AsNoTracking()
            .Where(x => x.IdEstado == idEstado &&
                        x.Categoria == ReservationConstants.Category)
            .Select(x => x.Codigo)
            .FirstOrDefaultAsync(cancellationToken);

    private static string? ValidateDates(DateTimeOffset start, DateTimeOffset end) =>
        start == default || end == default
            ? "Debe indicar las fechas de inicio y fin."
            : end <= start
                ? "La fecha final debe ser posterior a la fecha inicial."
                : null;

    private static ApiResponse<T> Success<T>(T data, string message) =>
        new() { Success = true, Message = message, Data = data };

    private static ApiResponse<T> Failure<T>(string message) =>
        new() { Success = false, Message = message };

    private ActionResult<ApiResponse<T>> ValidationFailure<T>(
        RequestValidationError error) => error.Kind switch
        {
            ValidationErrorKind.NotFound => NotFound(Failure<T>(error.Message)),
            ValidationErrorKind.Conflict => Conflict(Failure<T>(error.Message)),
            _ => BadRequest(Failure<T>(error.Message))
        };

    private enum ValidationErrorKind { InvalidInput, NotFound, Conflict }

    private sealed record RequestValidationError(
        string Message,
        ValidationErrorKind Kind);
}

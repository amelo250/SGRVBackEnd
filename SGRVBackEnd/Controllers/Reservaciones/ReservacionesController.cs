using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGRVBackEnd.Data;
using SGRVBackEnd.DTOs.Reservaciones;
using SGRVBackEnd.Models.Reservaciones;
using SGRVBackEnd.Shared;

namespace SGRVBackEnd.Controllers;

[ApiController]
[Authorize]
[Route("api/reservaciones")]
public sealed class ReservacionesController : BaseApiController
{
    private const string CategoriaReservacion = "RESERVACION";
    private const string CodigoPendiente = "PENDIENTE";
    private const string CodigoConfirmada = "CONFIRMADA";
    private const string CodigoCancelada = "CANCELADA";
    private const string CodigoConvertida = "CONVERTIDA";
    private readonly AppDbContext _context;

    public ReservacionesController(AppDbContext context) => _context = context;

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
            query = query.Where(x => x.FechaFin >= search.FechaDesde.Value);
        if (search.FechaHasta.HasValue)
            query = query.Where(x => x.FechaInicio <= search.FechaHasta.Value);

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
                x.EstadoCodigo != CodigoCancelada &&
                x.EstadoCodigo != CodigoConvertida)
            .OrderBy(x => x.FechaInicio)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Ok(Success<IEnumerable<ReservacionResponseDto>>(
            data, "Próximas reservaciones obtenidas correctamente."));
    }

    [HttpGet("disponibilidad")]
    public async Task<ActionResult<ApiResponse<object>>> GetAvailability(
        [FromQuery] int idVehiculo,
        [FromQuery] DateTime fechaInicio,
        [FromQuery] DateTime fechaFin,
        [FromQuery] int? excluirIdReservacion = null,
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

        var available = !await HasOverlap(
            idEmpresa, idVehiculo, fechaInicio, fechaFin,
            excluirIdReservacion, cancellationToken);

        return Ok(Success<object>(
            new { IdVehiculo = idVehiculo, Disponible = available },
            available
                ? "El vehículo está disponible."
                : "El vehículo no está disponible para el periodo indicado."));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ADMIN,SUPADMIN,SuperUsuario")]
    public async Task<ActionResult<ApiResponse<ReservacionResponseDto>>> Create(
        [FromBody] ReservacionCreateDto request,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        var validation = await ValidateRequest(
            request, idEmpresa, null, cancellationToken);
        if (validation is not null)
            return Conflict(Failure<ReservacionResponseDto>(validation));

        var idEstado = await GetStateId(CodigoPendiente, cancellationToken);
        if (!idEstado.HasValue)
            return BadRequest(Failure<ReservacionResponseDto>(
                "No existe el estado RESERVACION/PENDIENTE."));

        var entity = new Reservacion
        {
            IdEmpresa = idEmpresa,
            IdVehiculo = request.IdVehiculo,
            IdCliente = request.IdCliente,
            IdEstado = idEstado.Value,
            FechaInicio = request.FechaInicio,
            FechaFin = request.FechaFin,
            Observacion = request.Observacion?.Trim() ?? string.Empty,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Reservaciones.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        var result = await BuildResponseQuery(idEmpresa)
            .FirstAsync(x => x.IdReservacion == entity.IdReservacion, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = entity.IdReservacion },
            Success(result, "Reservación creada correctamente."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,ADMIN,SUPADMIN,SuperUsuario")]
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
        if (currentCode is CodigoCancelada or CodigoConvertida)
            return Conflict(Failure<ReservacionResponseDto>(
                "La reservación no puede modificarse en su estado actual."));

        var validation = await ValidateRequest(
            request, idEmpresa, id, cancellationToken);
        if (validation is not null)
            return Conflict(Failure<ReservacionResponseDto>(validation));

        entity.IdVehiculo = request.IdVehiculo;
        entity.IdCliente = request.IdCliente;
        entity.FechaInicio = request.FechaInicio;
        entity.FechaFin = request.FechaFin;
        entity.Observacion = request.Observacion?.Trim() ?? string.Empty;

        await _context.SaveChangesAsync(cancellationToken);
        var result = await BuildResponseQuery(idEmpresa)
            .FirstAsync(x => x.IdReservacion == id, cancellationToken);

        return Ok(Success(result, "Reservación actualizada correctamente."));
    }

    [HttpPatch("{id:int}/confirmar")]
    [Authorize(Roles = "Admin,ADMIN,SUPADMIN,SuperUsuario")]
    public Task<ActionResult<ApiResponse<ReservacionResponseDto>>> Confirm(
        int id,
        CancellationToken cancellationToken = default) =>
        ChangeState(id, CodigoPendiente, CodigoConfirmada,
            "Reservación confirmada correctamente.", cancellationToken);

    [HttpPatch("{id:int}/cancelar")]
    [Authorize(Roles = "Admin,ADMIN,SUPADMIN,SuperUsuario")]
    public Task<ActionResult<ApiResponse<ReservacionResponseDto>>> Cancel(
        int id,
        CancellationToken cancellationToken = default) =>
        CancelInternal(id, cancellationToken);

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,ADMIN,SUPADMIN,SuperUsuario")]
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
        if (currentCode == CodigoCancelada)
            return Conflict(Failure<ReservacionResponseDto>(
                "La reservación ya se encuentra cancelada."));
        if (currentCode == CodigoConvertida)
            return Conflict(Failure<ReservacionResponseDto>(
                "Una reservación convertida no puede cancelarse."));

        var targetId = await GetStateId(CodigoCancelada, cancellationToken);
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
        if (await HasOverlap(
                idEmpresa, entity.IdVehiculo, entity.FechaInicio, entity.FechaFin,
                id, cancellationToken))
            return Conflict(Failure<ReservacionResponseDto>(
                "El vehículo ya no está disponible para el periodo indicado."));

        var targetId = await GetStateId(targetCode, cancellationToken);
        if (!targetId.HasValue)
            return BadRequest(Failure<ReservacionResponseDto>(
                $"No existe el estado {CategoriaReservacion}/{targetCode}."));

        entity.IdEstado = targetId.Value;
        await _context.SaveChangesAsync(cancellationToken);
        var result = await BuildResponseQuery(idEmpresa)
            .FirstAsync(x => x.IdReservacion == id, cancellationToken);
        return Ok(Success(result, message));
    }

    private async Task<string?> ValidateRequest(
        ReservacionCreateDto request,
        int idEmpresa,
        int? idReservacion,
        CancellationToken cancellationToken)
    {
        var dateError = ValidateDates(request.FechaInicio, request.FechaFin);
        if (dateError is not null) return dateError;

        if (!await _context.Clientes.AsNoTracking().AnyAsync(x =>
                x.IdCliente == request.IdCliente &&
                x.IdEmpresa == idEmpresa && x.Activo, cancellationToken))
            return "El cliente no existe, está inactivo o no pertenece a la empresa.";

        if (!await _context.Vehiculos.AsNoTracking().AnyAsync(x =>
                x.IdVehiculo == request.IdVehiculo &&
                x.IdEmpresa == idEmpresa && x.Activo, cancellationToken))
            return "El vehículo no existe, está inactivo o no pertenece a la empresa.";

        return await HasOverlap(
            idEmpresa, request.IdVehiculo, request.FechaInicio, request.FechaFin,
            idReservacion, cancellationToken)
            ? "El vehículo ya tiene una reservación o renta para el periodo indicado."
            : null;
    }

    private async Task<bool> HasOverlap(
        int idEmpresa,
        int idVehiculo,
        DateTime fechaInicio,
        DateTime fechaFin,
        int? excludeReservationId,
        CancellationToken cancellationToken)
    {
        var reservationOverlap = await (
            from reservation in _context.Reservaciones.AsNoTracking()
            join state in _context.Estados.AsNoTracking()
                on reservation.IdEstado equals state.IdEstado
            where reservation.IdEmpresa == idEmpresa &&
                  reservation.IdVehiculo == idVehiculo &&
                  (!excludeReservationId.HasValue ||
                   reservation.IdReservacion != excludeReservationId.Value) &&
                  state.Categoria == CategoriaReservacion &&
                  state.Codigo != CodigoCancelada &&
                  state.Codigo != CodigoConvertida &&
                  reservation.FechaInicio < fechaFin &&
                  reservation.FechaFin > fechaInicio
            select reservation.IdReservacion)
            .AnyAsync(cancellationToken);
        if (reservationOverlap) return true;

        return await (
            from rental in _context.Rentas.AsNoTracking()
            join state in _context.Estados.AsNoTracking()
                on rental.IdEstado equals state.IdEstado
            where rental.IdEmpresa == idEmpresa &&
                  rental.IdVehiculo == idVehiculo &&
                  state.Categoria == "RENTA" &&
                  state.Codigo != "CANCELADA" &&
                  state.Codigo != "FINALIZADA" &&
                  rental.FechaInicio < fechaFin &&
                  rental.FechaFin > fechaInicio
            select rental.IdRenta)
            .AnyAsync(cancellationToken);
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
              state.Categoria == CategoriaReservacion
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
            .Where(x => x.Categoria == CategoriaReservacion &&
                        x.Codigo == code && x.Activo)
            .Select(x => (int?)x.IdEstado)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<string?> GetStateCode(
        int idEstado,
        CancellationToken cancellationToken) =>
        await _context.Estados.AsNoTracking()
            .Where(x => x.IdEstado == idEstado &&
                        x.Categoria == CategoriaReservacion)
            .Select(x => x.Codigo)
            .FirstOrDefaultAsync(cancellationToken);

    private static string? ValidateDates(DateTime start, DateTime end) =>
        start == default || end == default
            ? "Debe indicar las fechas de inicio y fin."
            : end <= start
                ? "La fecha final debe ser posterior a la fecha inicial."
                : null;

    private static ApiResponse<T> Success<T>(T data, string message) =>
        new() { Success = true, Message = message, Data = data };

    private static ApiResponse<T> Failure<T>(string message) =>
        new() { Success = false, Message = message };
}

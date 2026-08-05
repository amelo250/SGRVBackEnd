using System.Data;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGRVBackEnd.Data;
using SGRVBackEnd.DTOs.Rentas;
using SGRVBackEnd.Enums;
using SGRVBackEnd.Helpers;
using SGRVBackEnd.Models.AcuerdoVehiculoProveedor;
using SGRVBackEnd.Models.Reservaciones;
using SGRVBackEnd.Models.Renta;
using SGRVBackEnd.Services.Reservations;
using SGRVBackEnd.Shared;
using SGRVBackEnd.Validators;

namespace SGRVBackEnd.Controllers.Rentas;

[ApiController]
[Authorize]
[Route("api/rentas")]
[Produces("application/json")]
public sealed class RentasController : BaseApiController
{
    private const string ManagerRoles = "ADMIN,SUPADMIN";

    private readonly AppDbContext _context;
    private readonly IReservationAvailabilityService _availability;
    private readonly IMapper _mapper;

    public RentasController(
        AppDbContext context,
        IReservationAvailabilityService availability,
        IMapper mapper)
    {
        _context = context;
        _availability = availability;
        _mapper = mapper;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<RentaResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<RentaResponseDto>>>> GetAll(
        [FromQuery] RentaSearchDto search,
        CancellationToken cancellationToken = default)
    {
        var query = BuildReadQuery(GetEmpresaId());

        if (search.IdEstado.HasValue)
            query = query.Where(x => x.Entity.IdEstado == search.IdEstado.Value);
        if (search.IdCliente.HasValue)
            query = query.Where(x => x.Entity.IdCliente == search.IdCliente.Value);
        if (search.IdVehiculo.HasValue)
            query = query.Where(x => x.Entity.IdVehiculo == search.IdVehiculo.Value);
        if (search.FechaDesde.HasValue)
            query = query.Where(x => x.Entity.FechaFin >= search.FechaDesde.Value.UtcDateTime);
        if (search.FechaHasta.HasValue)
            query = query.Where(x => x.Entity.FechaInicio <= search.FechaHasta.Value.UtcDateTime);

        var term = search.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(x =>
                x.ClientName.Contains(term) ||
                x.VehicleDescription.Contains(term) ||
                x.StateName.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(x => x.Entity.FechaInicio)
            .Skip((search.PageNumber - 1) * search.PageSize)
            .Take(search.PageSize)
            .ToListAsync(cancellationToken);

        Response.Headers.Append("X-Total-Count", total.ToString());
        Response.Headers.Append("X-Page-Number", search.PageNumber.ToString());
        Response.Headers.Append("X-Page-Size", search.PageSize.ToString());

        return Ok(Success<IEnumerable<RentaResponseDto>>(
            rows.Select(MapResponse), "Rentas obtenidas correctamente."));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<RentaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RentaResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RentaResponseDto>>> GetById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var row = await BuildReadQuery(GetEmpresaId())
            .FirstOrDefaultAsync(x => x.Entity.IdRenta == id, cancellationToken);

        return row is null
            ? NotFound(Failure<RentaResponseDto>("No se encontró la renta solicitada."))
            : Ok(Success(MapResponse(row), "Renta obtenida correctamente."));
    }

    [HttpPost]
    [Authorize(Roles = ManagerRoles)]
    [ProducesResponseType(typeof(ApiResponse<RentaResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<RentaResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<RentaResponseDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<RentaResponseDto>>> Create(
        [FromBody] RentaCreateDto request,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var preparation = await PrepareRental(
            request.IdCliente, request.IdVehiculo,
            request.FechaInicio.UtcDateTime, request.FechaFin.UtcDateTime,
            request.Impuestos, request.Descuentos, request.Deposito,
            request.TasaCambioAplicada, idEmpresa, null, null, cancellationToken);

        if (!preparation.IsValid)
            return preparation.IsConflict
                ? Conflict(Failure<RentaResponseDto>(preparation.Error!))
                : BadRequest(Failure<RentaResponseDto>(preparation.Error!));

        var entity = _mapper.Map<Renta>(request);
        ApplySnapshot(entity, preparation, idEmpresa, GetUsuarioId());

        _context.Rentas.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var result = await GetReadModel(entity.IdRenta, idEmpresa, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entity.IdRenta },
            Success(MapResponse(result!), "Renta creada correctamente."));
    }

    [HttpPost("desde-reservacion/{idReservacion:int}")]
    [Authorize(Roles = ManagerRoles)]
    [ProducesResponseType(typeof(ApiResponse<RentaResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<RentaResponseDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<RentaResponseDto>>> CreateFromReservation(
        int idReservacion,
        [FromBody] ConvertirReservacionRentaDto request,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var reservation = await _context.Reservaciones.FirstOrDefaultAsync(x =>
            x.IdReservacion == idReservacion && x.IdEmpresa == idEmpresa,
            cancellationToken);
        if (reservation is null)
            return NotFound(Failure<RentaResponseDto>("No se encontró la reservación solicitada."));

        var currentState = await GetStateCode(reservation.IdEstado, cancellationToken);
        if (currentState != ReservationConstants.Confirmed)
            return Conflict(Failure<RentaResponseDto>(
                "Solo una reservación confirmada puede convertirse en renta."));

        if (await _context.Rentas.AsNoTracking().AnyAsync(x =>
                x.IdEmpresa == idEmpresa && x.IdReservacion == idReservacion,
                cancellationToken))
            return Conflict(Failure<RentaResponseDto>(
                "La reservación ya está asociada a una renta."));

        var preparation = await PrepareRental(
            reservation.IdCliente, reservation.IdVehiculo,
            reservation.FechaInicio, reservation.FechaFin,
            request.Impuestos, request.Descuentos, request.Deposito,
            request.TasaCambioAplicada, idEmpresa, null, idReservacion, cancellationToken);

        if (!preparation.IsValid)
            return preparation.IsConflict
                ? Conflict(Failure<RentaResponseDto>(preparation.Error!))
                : BadRequest(Failure<RentaResponseDto>(preparation.Error!));

        var entity = new Renta
        {
            IdCliente = reservation.IdCliente,
            IdVehiculo = reservation.IdVehiculo,
            IdReservacion = reservation.IdReservacion,
            FechaInicio = reservation.FechaInicio,
            FechaFin = reservation.FechaFin,
            Impuestos = request.Impuestos,
            Descuentos = request.Descuentos,
            Deposito = request.Deposito,
            TasaCambioAplicada = request.TasaCambioAplicada,
            Observaciones = Normalize(request.Observaciones)
        };
        ApplySnapshot(entity, preparation, idEmpresa, GetUsuarioId());

        var convertedStateId = await GetStateId(
            ReservationConstants.Category, ReservationConstants.Converted, cancellationToken);
        if (!convertedStateId.HasValue)
            return BadRequest(Failure<RentaResponseDto>(
                "No existe el estado RESERVACION/CONVERTIDA."));

        reservation.IdEstado = convertedStateId.Value;
        _context.Rentas.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var result = await GetReadModel(entity.IdRenta, idEmpresa, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entity.IdRenta },
            Success(MapResponse(result!), "Reservación convertida en renta correctamente."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = ManagerRoles)]
    public async Task<ActionResult<ApiResponse<RentaResponseDto>>> Update(
        int id,
        [FromBody] RentaUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var entity = await _context.Rentas.FirstOrDefaultAsync(
            x => x.IdRenta == id && x.IdEmpresa == idEmpresa, cancellationToken);
        if (entity is null)
            return NotFound(Failure<RentaResponseDto>("No se encontró la renta solicitada."));

        var stateCode = await GetStateCode(entity.IdEstado, cancellationToken);
        if (stateCode != RentalConstants.Active)
            return Conflict(Failure<RentaResponseDto>("Solo una renta activa puede modificarse."));

        if (await _context.Pagos.AsNoTracking().AnyAsync(
                x => x.IdEmpresa == idEmpresa && x.IdRenta == id && x.Activo,
                cancellationToken))
            return Conflict(Failure<RentaResponseDto>(
                "No se puede modificar una renta que ya tiene pagos activos."));

        if (!TrySetRowVersion(entity, request.RowVersion, out var versionError))
            return BadRequest(Failure<RentaResponseDto>(versionError!));

        var preparation = await PrepareRental(
            request.IdCliente, request.IdVehiculo,
            request.FechaInicio.UtcDateTime, request.FechaFin.UtcDateTime,
            request.Impuestos, request.Descuentos, request.Deposito,
            request.TasaCambioAplicada, idEmpresa, id, entity.IdReservacion,
            cancellationToken);
        if (!preparation.IsValid)
            return preparation.IsConflict
                ? Conflict(Failure<RentaResponseDto>(preparation.Error!))
                : BadRequest(Failure<RentaResponseDto>(preparation.Error!));

        _mapper.Map(request, entity);
        ApplySnapshot(entity, preparation, idEmpresa, entity.IdUsuarioCreacion, preserveCreation: true);
        entity.FechaActualizacion = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(Failure<RentaResponseDto>(
                "La renta fue modificada por otro usuario. Recarga los datos e inténtalo nuevamente."));
        }

        var result = await GetReadModel(id, idEmpresa, cancellationToken);
        return Ok(Success(MapResponse(result!), "Renta actualizada correctamente."));
    }

    [HttpPatch("{id:int}/finalizar")]
    [Authorize(Roles = ManagerRoles)]
    public async Task<ActionResult<ApiResponse<RentaResponseDto>>> Complete(
        int id,
        [FromBody] FinalizarRentaDto request,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        var entity = await _context.Rentas.FirstOrDefaultAsync(
            x => x.IdRenta == id && x.IdEmpresa == idEmpresa, cancellationToken);
        if (entity is null)
            return NotFound(Failure<RentaResponseDto>("No se encontró la renta solicitada."));

        var stateCode = await GetStateCode(entity.IdEstado, cancellationToken);
        if (stateCode != RentalConstants.Active)
            return Conflict(Failure<RentaResponseDto>("Solo una renta activa puede finalizarse."));
        if (request.FechaEntregaReal.UtcDateTime < entity.FechaInicio)
            return BadRequest(Failure<RentaResponseDto>(
                "La fecha de entrega real no puede ser anterior al inicio de la renta."));
        if (!TrySetRowVersion(entity, request.RowVersion, out var versionError))
            return BadRequest(Failure<RentaResponseDto>(versionError!));

        var paid = await _context.Pagos.AsNoTracking()
            .Where(x => x.IdEmpresa == idEmpresa && x.IdRenta == id && x.Activo)
            .SumAsync(x => (decimal?)x.MontoMonedaLocal, cancellationToken) ?? 0m;
        if (paid < entity.TotalMonedaLocal)
            return Conflict(Failure<RentaResponseDto>(
                $"La renta tiene un balance pendiente de {entity.TotalMonedaLocal - paid:N2} en moneda local."));

        var completedId = await GetStateId(
            RentalConstants.Category, RentalConstants.Completed, cancellationToken);
        if (!completedId.HasValue)
            return BadRequest(Failure<RentaResponseDto>("No existe el estado RENTA/FINALIZADA."));

        entity.IdEstado = completedId.Value;
        entity.FechaEntregaReal = request.FechaEntregaReal.UtcDateTime;
        entity.Observaciones = MergeObservations(entity.Observaciones, request.Observaciones);
        entity.FechaActualizacion = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(Failure<RentaResponseDto>(
                "La renta fue modificada por otro usuario. Recarga los datos."));
        }

        var result = await GetReadModel(id, idEmpresa, cancellationToken);
        return Ok(Success(MapResponse(result!), "Renta finalizada correctamente."));
    }

    [HttpPatch("{id:int}/cancelar")]
    [Authorize(Roles = ManagerRoles)]
    public Task<ActionResult<ApiResponse<RentaResponseDto>>> Cancel(
        int id,
        CancellationToken cancellationToken = default) =>
        CancelInternal(id, cancellationToken);

    [HttpDelete("{id:int}")]
    [Authorize(Roles = ManagerRoles)]
    public Task<ActionResult<ApiResponse<RentaResponseDto>>> Delete(
        int id,
        CancellationToken cancellationToken = default) =>
        CancelInternal(id, cancellationToken);

    [HttpGet("{id:int}/resumen")]
    public async Task<ActionResult<ApiResponse<object>>> GetSummary(
        int id,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();
        var rental = await _context.Rentas.AsNoTracking().FirstOrDefaultAsync(
            x => x.IdRenta == id && x.IdEmpresa == idEmpresa, cancellationToken);
        if (rental is null)
            return NotFound(Failure<object>("No se encontró la renta solicitada."));

        var paid = await _context.Pagos.AsNoTracking()
            .Where(x => x.IdEmpresa == idEmpresa && x.IdRenta == id && x.Activo)
            .SumAsync(x => (decimal?)x.MontoMonedaLocal, cancellationToken) ?? 0m;

        return Ok(Success<object>(new
        {
            rental.IdRenta,
            rental.Total,
            rental.IdMoneda,
            rental.TotalMonedaLocal,
            TotalPagadoMonedaLocal = paid,
            BalancePendienteMonedaLocal = Math.Max(rental.TotalMonedaLocal - paid, 0m),
            MontoSobrepagoMonedaLocal = Math.Max(paid - rental.TotalMonedaLocal, 0m)
        }, "Resumen de renta obtenido correctamente."));
    }

    private async Task<ActionResult<ApiResponse<RentaResponseDto>>> CancelInternal(
        int id,
        CancellationToken cancellationToken)
    {
        var idEmpresa = GetEmpresaId();
        var entity = await _context.Rentas.FirstOrDefaultAsync(
            x => x.IdRenta == id && x.IdEmpresa == idEmpresa, cancellationToken);
        if (entity is null)
            return NotFound(Failure<RentaResponseDto>("No se encontró la renta solicitada."));

        var stateCode = await GetStateCode(entity.IdEstado, cancellationToken);
        if (stateCode == RentalConstants.Cancelled)
            return Conflict(Failure<RentaResponseDto>("La renta ya está cancelada."));
        if (stateCode == RentalConstants.Completed)
            return Conflict(Failure<RentaResponseDto>("Una renta finalizada no puede cancelarse."));
        if (await _context.Pagos.AsNoTracking().AnyAsync(
                x => x.IdEmpresa == idEmpresa && x.IdRenta == id && x.Activo,
                cancellationToken))
            return Conflict(Failure<RentaResponseDto>(
                "Anula o reversa los pagos activos antes de cancelar la renta."));

        var cancelledId = await GetStateId(
            RentalConstants.Category, RentalConstants.Cancelled, cancellationToken);
        if (!cancelledId.HasValue)
            return BadRequest(Failure<RentaResponseDto>("No existe el estado RENTA/CANCELADA."));

        entity.IdEstado = cancelledId.Value;
        entity.FechaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var result = await GetReadModel(id, idEmpresa, cancellationToken);
        return Ok(Success(MapResponse(result!), "Renta cancelada correctamente."));
    }

    private async Task<RentalPreparation> PrepareRental(
        int clientId,
        int vehicleId,
        DateTime startUtc,
        DateTime endUtc,
        decimal taxes,
        decimal discounts,
        decimal deposit,
        decimal requestedRate,
        int companyId,
        int? excludedRentalId,
        int? excludedReservationId,
        CancellationToken cancellationToken)
    {
        var dateError = RentaValidator.ValidateDates(startUtc, endUtc);
        if (dateError is not null) return RentalPreparation.Invalid(dateError);

        var clientExists = await _context.Clientes.AsNoTracking().AnyAsync(x =>
            x.IdCliente == clientId && x.IdEmpresa == companyId && x.Activo,
            cancellationToken);
        if (!clientExists)
            return RentalPreparation.Invalid("El cliente no existe, está inactivo o pertenece a otra empresa.");

        var vehicle = await _context.Vehiculos.AsNoTracking().FirstOrDefaultAsync(x =>
            x.IdVehiculo == vehicleId && x.IdEmpresa == companyId && x.Activo,
            cancellationToken);
        if (vehicle is null)
            return RentalPreparation.Invalid("El vehículo no existe, está inactivo o pertenece a otra empresa.");

        var currency = await _context.Monedas.AsNoTracking().FirstOrDefaultAsync(x =>
            x.IdMoneda == vehicle.IdMonedaTarifa && x.Activo, cancellationToken);
        if (currency is null)
            return RentalPreparation.Invalid("La moneda de tarifa del vehículo no existe o está inactiva.");

        var exchangeRate = currency.Codigo.Equals(
            RentalConstants.LocalCurrencyCode, StringComparison.OrdinalIgnoreCase)
            ? 1m
            : requestedRate;
        var amountError = RentaValidator.ValidateAmounts(
            vehicle.PrecioPorDia, taxes, discounts, deposit, exchangeRate);
        if (amountError is not null) return RentalPreparation.Invalid(amountError);

        int? providerId = null;
        int? agreementId = null;
        if (vehicle.TipoPropiedad == TipoPropiedadVehiculo.Tercero)
        {
            if (!vehicle.IdProveedorVehiculo.HasValue)
                return RentalPreparation.Invalid("El vehículo de tercero no tiene proveedor asociado.");

            var providerExists = await _context.ProveedoresVehiculos.AsNoTracking().AnyAsync(x =>
                x.IdProveedorVehiculo == vehicle.IdProveedorVehiculo.Value &&
                x.IdEmpresa == companyId && x.Activo, cancellationToken);
            if (!providerExists)
                return RentalPreparation.Invalid("El proveedor del vehículo no existe o está inactivo.");

            agreementId = await _context.Set<AcuerdoVehiculoProveedor>().AsNoTracking()
                .Where(x => x.IdEmpresa == companyId && x.IdVehiculo == vehicleId &&
                            x.IdProveedorVehiculo == vehicle.IdProveedorVehiculo.Value &&
                            x.Activo && x.FechaInicioVigencia <= startUtc &&
                            (!x.FechaFinVigencia.HasValue || x.FechaFinVigencia.Value >= endUtc))
                .OrderByDescending(x => x.FechaInicioVigencia)
                .Select(x => (int?)x.IdAcuerdoVehiculo)
                .FirstOrDefaultAsync(cancellationToken);
            if (!agreementId.HasValue)
                return RentalPreparation.Invalid("No existe un acuerdo vigente para el vehículo de tercero.");
            providerId = vehicle.IdProveedorVehiculo;
        }

        var hasOverlap = excludedRentalId.HasValue
            ? await HasOverlapExcludingRental(companyId, vehicleId, startUtc, endUtc,
                excludedRentalId.Value, excludedReservationId, cancellationToken)
            : await _availability.HasOverlapAsync(companyId, vehicleId, startUtc, endUtc,
                excludedReservationId, cancellationToken);
        if (hasOverlap)
            return RentalPreparation.Conflict("El vehículo no está disponible para el periodo indicado.");

        var activeStateId = await GetStateId(
            RentalConstants.Category, RentalConstants.Active, cancellationToken);
        if (!activeStateId.HasValue)
            return RentalPreparation.Invalid("No existe el estado RENTA/ACTIVA.");

        var days = RentaValidator.CalculateDays(startUtc, endUtc);
        var subtotal = RentaValidator.RoundMoney(vehicle.PrecioPorDia * days);
        if (discounts > subtotal + taxes)
            return RentalPreparation.Invalid("El descuento no puede superar el subtotal más impuestos.");
        var total = RentaValidator.RoundMoney(subtotal + taxes - discounts);

        return RentalPreparation.Valid(
            activeStateId.Value, vehicle.PrecioPorDia, days, subtotal, total,
            vehicle.IdMonedaTarifa, exchangeRate,
            RentaValidator.RoundMoney(total * exchangeRate), providerId, agreementId);
    }

    private async Task<bool> HasOverlapExcludingRental(
        int companyId, int vehicleId, DateTime startUtc, DateTime endUtc,
        int excludedRentalId, int? excludedReservationId,
        CancellationToken cancellationToken)
    {
        var reservationOverlap = await (
            from reservation in _context.Reservaciones.AsNoTracking()
            join state in _context.Estados.AsNoTracking() on reservation.IdEstado equals state.IdEstado
            where reservation.IdEmpresa == companyId && reservation.IdVehiculo == vehicleId &&
                  (!excludedReservationId.HasValue || reservation.IdReservacion != excludedReservationId.Value) &&
                  state.Categoria == ReservationConstants.Category &&
                  state.Codigo != ReservationConstants.Cancelled &&
                  state.Codigo != ReservationConstants.Converted &&
                  reservation.FechaInicio < endUtc && reservation.FechaFin > startUtc
            select reservation.IdReservacion).AnyAsync(cancellationToken);
        if (reservationOverlap) return true;

        return await (
            from rental in _context.Rentas.AsNoTracking()
            join state in _context.Estados.AsNoTracking() on rental.IdEstado equals state.IdEstado
            where rental.IdEmpresa == companyId && rental.IdVehiculo == vehicleId &&
                  rental.IdRenta != excludedRentalId && state.Categoria == RentalConstants.Category &&
                  state.Codigo != RentalConstants.Cancelled &&
                  state.Codigo != RentalConstants.Completed &&
                  rental.FechaInicio < endUtc && rental.FechaFin > startUtc
            select rental.IdRenta).AnyAsync(cancellationToken);
    }

    private void ApplySnapshot(
        Renta entity,
        RentalPreparation preparation,
        int companyId,
        int userId,
        bool preserveCreation = false)
    {
        entity.IdEmpresa = companyId;
        entity.IdEstado = preparation.StateId;
        entity.PrecioPorDia = preparation.PricePerDay;
        entity.CantidadDias = preparation.Days;
        entity.Subtotal = preparation.Subtotal;
        entity.Total = preparation.Total;
        entity.IdMoneda = preparation.CurrencyId;
        entity.TasaCambioAplicada = preparation.ExchangeRate;
        entity.TotalMonedaLocal = preparation.LocalTotal;
        entity.IdProveedorVehiculo = preparation.ProviderId;
        entity.IdAcuerdoVehiculo = preparation.AgreementId;
        if (!preserveCreation)
        {
            entity.IdUsuarioCreacion = userId;
            entity.FechaCreacion = DateTime.UtcNow;
        }
    }

    private IQueryable<RentalReadModel> BuildReadQuery(int companyId) =>
        from rental in _context.Rentas.AsNoTracking()
        join client in _context.Clientes.AsNoTracking() on rental.IdCliente equals client.IdCliente
        join vehicle in _context.Vehiculos.AsNoTracking() on rental.IdVehiculo equals vehicle.IdVehiculo
        join state in _context.Estados.AsNoTracking() on rental.IdEstado equals state.IdEstado
        join currency in _context.Monedas.AsNoTracking() on rental.IdMoneda equals currency.IdMoneda
        where rental.IdEmpresa == companyId && client.IdEmpresa == companyId && vehicle.IdEmpresa == companyId
        select new RentalReadModel
        {
            Entity = rental,
            ClientName = client.Nombre + " " + client.Apellido,
            VehicleDescription = vehicle.Marca + " " + vehicle.Modelo + " · " + vehicle.Placa,
            StateCode = state.Codigo,
            StateName = state.Nombre,
            CurrencyCode = currency.Codigo,
            CurrencySymbol = currency.Simbolo
        };

    private Task<RentalReadModel?> GetReadModel(
        int id, int companyId, CancellationToken cancellationToken) =>
        BuildReadQuery(companyId).FirstOrDefaultAsync(x => x.Entity.IdRenta == id, cancellationToken);

    private RentaResponseDto MapResponse(RentalReadModel row)
    {
        var dto = _mapper.Map<RentaResponseDto>(row.Entity);
        dto.ClienteNombre = row.ClientName.Trim();
        dto.VehiculoDescripcion = row.VehicleDescription;
        dto.EstadoCodigo = row.StateCode;
        dto.EstadoNombre = row.StateName;
        dto.MonedaCodigo = row.CurrencyCode;
        dto.MonedaSimbolo = row.CurrencySymbol;
        return dto;
    }

    private async Task<int?> GetStateId(
        string category, string code, CancellationToken cancellationToken) =>
        await _context.Estados.AsNoTracking()
            .Where(x => x.Activo && x.Categoria == category && x.Codigo == code)
            .Select(x => (int?)x.IdEstado)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<string?> GetStateCode(int stateId, CancellationToken cancellationToken) =>
        await _context.Estados.AsNoTracking()
            .Where(x => x.IdEstado == stateId)
            .Select(x => x.Codigo)
            .FirstOrDefaultAsync(cancellationToken);

    private bool TrySetRowVersion(Renta entity, string encoded, out string? error)
    {
        try
        {
            var version = Convert.FromBase64String(encoded);
            _context.Entry(entity).Property(x => x.RowVersion).OriginalValue = version;
            error = null;
            return true;
        }
        catch (FormatException)
        {
            error = "RowVersion no tiene un formato Base64 válido.";
            return false;
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? MergeObservations(string? current, string? addition)
    {
        var normalized = Normalize(addition);
        if (normalized is null) return current;
        return string.IsNullOrWhiteSpace(current)
            ? normalized
            : $"{current.Trim()} | Finalización: {normalized}";
    }

    private static ApiResponse<T> Success<T>(T data, string message) =>
        new() { Success = true, Message = message, Data = data };

    private static ApiResponse<T> Failure<T>(string message, object? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };

    private sealed class RentalReadModel
    {
        public required Renta Entity { get; init; }
        public required string ClientName { get; init; }
        public required string VehicleDescription { get; init; }
        public required string StateCode { get; init; }
        public required string StateName { get; init; }
        public required string CurrencyCode { get; init; }
        public required string CurrencySymbol { get; init; }
    }

    private sealed record RentalPreparation(
        bool IsValid,
        bool IsConflict,
        string? Error,
        int StateId,
        decimal PricePerDay,
        int Days,
        decimal Subtotal,
        decimal Total,
        int CurrencyId,
        decimal ExchangeRate,
        decimal LocalTotal,
        int? ProviderId,
        int? AgreementId)
    {
        public static RentalPreparation Invalid(string error) =>
            new(false, false, error, 0, 0, 0, 0, 0, 0, 0, 0, null, null);

        public static RentalPreparation Conflict(string error) =>
            new(false, true, error, 0, 0, 0, 0, 0, 0, 0, 0, null, null);

        public static RentalPreparation Valid(
            int stateId, decimal pricePerDay, int days, decimal subtotal,
            decimal total, int currencyId, decimal exchangeRate,
            decimal localTotal, int? providerId, int? agreementId) =>
            new(true, false, null, stateId, pricePerDay, days, subtotal, total,
                currencyId, exchangeRate, localTotal, providerId, agreementId);
    }
}

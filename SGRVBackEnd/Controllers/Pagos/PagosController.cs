using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGRVBackEnd.Data;
using SGRVBackEnd.DTOs.Pagos;
using SGRVBackEnd.Models.Pago;
using SGRVBackEnd.Shared;
using SGRVBackEnd.Validators;

namespace SGRVBackEnd.Controllers.Pagos;

[ApiController]
[Authorize]
[Route("api/pagos")]
public sealed class PagosController : BaseApiController
{
    private const string CategoriaEstadoPago = "PAGO";
    private const string CodigoEstadoInicial = "APLICADO";
    private const string CodigoMonedaLocal = "DOP";

    private readonly AppDbContext _context;

    public PagosController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/pagos
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<PagoResponseDto>>>>
        GetAll(
            [FromQuery] PagoSearchDto search,
            CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();

        var query = _context.Pagos
            .AsNoTracking()
            .Where(p => p.IdEmpresa == idEmpresa);

        if (search.IdRenta.HasValue)
            query = query.Where(p => p.IdRenta == search.IdRenta.Value);
        if (search.IdMetodoPago.HasValue)
            query = query.Where(p => p.IdMetodoPago == search.IdMetodoPago.Value);
        if (search.IdEstado.HasValue)
            query = query.Where(p => p.IdEstado == search.IdEstado.Value);
        if (search.FechaDesde.HasValue)
            query = query.Where(p => p.FechaPago >= search.FechaDesde.Value);
        if (search.FechaHasta.HasValue)
            query = query.Where(p => p.FechaPago <= search.FechaHasta.Value);
        if (!search.IncluirInactivos)
        {
            query = query.Where(p => p.Activo);
        }

        var term = search.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(p =>
                (p.Referencia != null && p.Referencia.Contains(term)) ||
                (p.Observaciones != null && p.Observaciones.Contains(term)));

        var total = await query.CountAsync(cancellationToken);

        var pagos = await (
            from pago in query
            join moneda in _context.Monedas.AsNoTracking() on pago.IdMoneda equals moneda.IdMoneda
            join metodo in _context.MetodosPago.AsNoTracking() on pago.IdMetodoPago equals metodo.IdMetodoPago
            join estado in _context.Estados.AsNoTracking() on pago.IdEstado equals estado.IdEstado
            join renta in _context.Rentas.AsNoTracking() on pago.IdRenta equals renta.IdRenta
            join cliente in _context.Clientes.AsNoTracking() on renta.IdCliente equals cliente.IdCliente
            join vehiculo in _context.Vehiculos.AsNoTracking() on renta.IdVehiculo equals vehiculo.IdVehiculo
            where renta.IdEmpresa == idEmpresa && cliente.IdEmpresa == idEmpresa && vehiculo.IdEmpresa == idEmpresa
            orderby pago.FechaPago descending
            select new PagoResponseDto
            {
                IdPago = pago.IdPago,
                IdRenta = pago.IdRenta,
                IdMetodoPago = pago.IdMetodoPago,
                MetodoPagoNombre = metodo.Nombre,
                IdEstado = pago.IdEstado,
                EstadoCodigo = estado.Codigo,
                EstadoNombre = estado.Nombre,
                IdMoneda = pago.IdMoneda,
                CodigoMoneda = moneda.Codigo,
                SimboloMoneda = moneda.Simbolo,
                ClienteNombre = cliente.Nombre + " " + cliente.Apellido,
                VehiculoDescripcion = vehiculo.Marca + " " + vehiculo.Modelo + " · " + vehiculo.Placa,
                Monto = pago.Monto,
                TasaCambioAplicada = pago.TasaCambioAplicada,
                MontoMonedaLocal = pago.MontoMonedaLocal,
                FechaPago = pago.FechaPago,
                Referencia = pago.Referencia,
                Observaciones = pago.Observaciones,
                Activo = pago.Activo,
                FechaRegistro = pago.FechaRegistro,
                FechaActualizacion = pago.FechaActualizacion
            })
            .Skip((search.PageNumber - 1) * search.PageSize)
            .Take(search.PageSize)
            .ToListAsync(cancellationToken);

        Response.Headers.Append("X-Total-Count", total.ToString());
        Response.Headers.Append("X-Page-Number", search.PageNumber.ToString());
        Response.Headers.Append("X-Page-Size", search.PageSize.ToString());

        return Ok(
            Success<IEnumerable<PagoResponseDto>>(
                pagos,
                "Pagos obtenidos correctamente."));
    }

    // GET: api/pagos/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<PagoResponseDto>>> GetById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();

        var pago = await LoadResponse(id, idEmpresa, cancellationToken);

        if (pago is null)
        {
            return NotFound(
                Failure<PagoResponseDto>(
                    "No se encontró el pago solicitado."));
        }

        return Ok(
            Success(
                pago,
                "Pago obtenido correctamente."));
    }

    // GET: api/pagos/renta/5
    [HttpGet("renta/{idRenta:int}")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<PagoResponseDto>>>>
        GetByRenta(
            int idRenta,
            CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();

        var rentaExiste = await _context.Rentas
            .AsNoTracking()
            .AnyAsync(
                renta =>
                    renta.IdRenta == idRenta &&
                    renta.IdEmpresa == idEmpresa,
                cancellationToken);

        if (!rentaExiste)
        {
            return NotFound(
                Failure<IEnumerable<PagoResponseDto>>(
                    "No se encontró la renta solicitada."));
        }

        var pagos = await (
            from pago in _context.Pagos.AsNoTracking()
            join moneda in _context.Monedas.AsNoTracking() on pago.IdMoneda equals moneda.IdMoneda
            join metodo in _context.MetodosPago.AsNoTracking() on pago.IdMetodoPago equals metodo.IdMetodoPago
            join estado in _context.Estados.AsNoTracking() on pago.IdEstado equals estado.IdEstado
            join renta in _context.Rentas.AsNoTracking() on pago.IdRenta equals renta.IdRenta
            join cliente in _context.Clientes.AsNoTracking() on renta.IdCliente equals cliente.IdCliente
            join vehiculo in _context.Vehiculos.AsNoTracking() on renta.IdVehiculo equals vehiculo.IdVehiculo
            where pago.IdEmpresa == idEmpresa &&
                  pago.IdRenta == idRenta &&
                  pago.Activo && renta.IdEmpresa == idEmpresa &&
                  cliente.IdEmpresa == idEmpresa && vehiculo.IdEmpresa == idEmpresa
            orderby pago.FechaPago descending
            select new PagoResponseDto
            {
                IdPago = pago.IdPago,
                IdRenta = pago.IdRenta,
                IdMetodoPago = pago.IdMetodoPago,
                MetodoPagoNombre = metodo.Nombre,
                IdEstado = pago.IdEstado,
                EstadoCodigo = estado.Codigo,
                EstadoNombre = estado.Nombre,
                IdMoneda = pago.IdMoneda,
                CodigoMoneda = moneda.Codigo,
                SimboloMoneda = moneda.Simbolo,
                ClienteNombre = cliente.Nombre + " " + cliente.Apellido,
                VehiculoDescripcion = vehiculo.Marca + " " + vehiculo.Modelo + " · " + vehiculo.Placa,
                Monto = pago.Monto,
                TasaCambioAplicada = pago.TasaCambioAplicada,
                MontoMonedaLocal = pago.MontoMonedaLocal,
                FechaPago = pago.FechaPago,
                Referencia = pago.Referencia,
                Observaciones = pago.Observaciones,
                Activo = pago.Activo,
                FechaRegistro = pago.FechaRegistro,
                FechaActualizacion = pago.FechaActualizacion
            })
            .ToListAsync(cancellationToken);

        return Ok(
            Success<IEnumerable<PagoResponseDto>>(
                pagos,
                "Pagos de la renta obtenidos correctamente."));
    }

    // POST: api/pagos
    [HttpPost]
    [Authorize(Roles = "ADMIN,SUPADMIN")]
    public async Task<ActionResult<ApiResponse<PagoResponseDto>>> Create(
        [FromBody] PagoCreateDto request,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();

        var requestError = PagoValidator.Validate(request);
        if (requestError is not null)
            return BadRequest(Failure<PagoResponseDto>(requestError));

        var validation = await ValidateRequest(
            request,
            idEmpresa,
            cancellationToken);

        if (!validation.IsValid)
        {
            return BadRequest(
                Failure<PagoResponseDto>(
                    validation.ErrorMessage!));
        }

        var idEstadoInicial = await _context.Estados
            .AsNoTracking()
            .Where(estado =>
                estado.Categoria == CategoriaEstadoPago &&
                estado.Codigo == CodigoEstadoInicial &&
                estado.Activo)
            .Select(estado => (int?)estado.IdEstado)
            .FirstOrDefaultAsync(cancellationToken);

        if (!idEstadoInicial.HasValue)
        {
            return BadRequest(
                Failure<PagoResponseDto>(
                    $"No existe el estado {CategoriaEstadoPago}/" +
                    $"{CodigoEstadoInicial}."));
        }

        var tasaCambioAplicada = CalculateExchangeRate(
            validation.CurrencyCode!,
            request.TasaCambioAplicada);

        var montoMonedaLocal = CalculateLocalAmount(
            request.Monto,
            tasaCambioAplicada);

        var pago = new Pago
        {
            IdEmpresa = idEmpresa,
            IdRenta = request.IdRenta,
            IdMetodoPago = request.IdMetodoPago,
            IdEstado = idEstadoInicial.Value,
            IdMoneda = request.IdMoneda,
            Monto = request.Monto,
            TasaCambioAplicada = tasaCambioAplicada,
            MontoMonedaLocal = montoMonedaLocal,
            FechaPago = request.FechaPago ?? DateTime.UtcNow,
            Referencia = NormalizeNullable(request.Referencia),
            Observaciones = NormalizeNullable(request.Observaciones),
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };

        _context.Pagos.Add(pago);
        await _context.SaveChangesAsync(cancellationToken);

        var response = await LoadResponse(pago.IdPago, idEmpresa, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = pago.IdPago },
            Success(
                response!,
                "Pago registrado correctamente."));
    }

    // PUT: api/pagos/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = "ADMIN,SUPADMIN")]
    public async Task<ActionResult<ApiResponse<PagoResponseDto>>> Update(
        int id,
        [FromBody] PagoUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();

        var requestError = PagoValidator.Validate(request);
        if (requestError is not null)
            return BadRequest(Failure<PagoResponseDto>(requestError));

        var pago = await _context.Pagos
            .FirstOrDefaultAsync(
                item =>
                    item.IdPago == id &&
                    item.IdEmpresa == idEmpresa,
                cancellationToken);

        if (pago is null)
        {
            return NotFound(
                Failure<PagoResponseDto>(
                    "No se encontró el pago solicitado."));
        }

        var validation = await ValidateRequest(
            request,
            idEmpresa,
            cancellationToken,
            id);

        if (!validation.IsValid)
        {
            return BadRequest(
                Failure<PagoResponseDto>(
                    validation.ErrorMessage!));
        }

        var estadoValido = await _context.Estados
            .AsNoTracking()
            .AnyAsync(
                estado =>
                    estado.IdEstado == request.IdEstado &&
                    estado.Categoria == CategoriaEstadoPago &&
                    estado.Activo,
                cancellationToken);

        if (!estadoValido)
        {
            return BadRequest(
                Failure<PagoResponseDto>(
                    "El estado indicado no corresponde a pagos."));
        }

        var tasaCambioAplicada = CalculateExchangeRate(
            validation.CurrencyCode!,
            request.TasaCambioAplicada);

        pago.IdRenta = request.IdRenta;
        pago.IdMetodoPago = request.IdMetodoPago;
        pago.IdEstado = request.IdEstado;
        pago.IdMoneda = request.IdMoneda;
        pago.Monto = request.Monto;
        pago.TasaCambioAplicada = tasaCambioAplicada;
        pago.MontoMonedaLocal = CalculateLocalAmount(
            request.Monto,
            tasaCambioAplicada);
        pago.FechaPago = request.FechaPago ?? pago.FechaPago;
        pago.Referencia = NormalizeNullable(request.Referencia);
        pago.Observaciones = NormalizeNullable(request.Observaciones);
        pago.Activo = request.Activo;
        pago.FechaActualizacion = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(
            Success(
                (await LoadResponse(pago.IdPago, idEmpresa, cancellationToken))!,
                "Pago actualizado correctamente."));
    }

    // DELETE: api/pagos/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "ADMIN,SUPADMIN")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int id,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();

        var pago = await _context.Pagos
            .FirstOrDefaultAsync(
                item =>
                    item.IdPago == id &&
                    item.IdEmpresa == idEmpresa,
                cancellationToken);

        if (pago is null)
        {
            return NotFound(
                Failure<object>(
                    "No se encontró el pago solicitado."));
        }

        if (!pago.Activo)
        {
            return Conflict(
                Failure<object>(
                    "El pago ya se encuentra anulado."));
        }

        pago.Activo = false;
        pago.FechaActualizacion = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(
            Success<object>(
                new
                {
                    pago.IdPago,
                    pago.Activo
                },
                "Pago anulado correctamente."));
    }

    // PATCH: api/pagos/5/restaurar
    [HttpPatch("{id:int}/restaurar")]
    [Authorize(Roles = "ADMIN,SUPADMIN")]
    public async Task<ActionResult<ApiResponse<PagoResponseDto>>> Restore(
        int id,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();

        var pago = await _context.Pagos
            .FirstOrDefaultAsync(
                item =>
                    item.IdPago == id &&
                    item.IdEmpresa == idEmpresa,
                cancellationToken);

        if (pago is null)
        {
            return NotFound(
                Failure<PagoResponseDto>(
                    "No se encontró el pago solicitado."));
        }

        if (pago.Activo)
        {
            return Conflict(
                Failure<PagoResponseDto>(
                    "El pago ya se encuentra activo."));
        }

        var codigoMoneda = await _context.Monedas
            .AsNoTracking()
            .Where(moneda =>
                moneda.IdMoneda == pago.IdMoneda &&
                moneda.Activo)
            .Select(moneda => moneda.Codigo)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(codigoMoneda))
        {
            return BadRequest(
                Failure<PagoResponseDto>(
                    "La moneda del pago no existe o está inactiva."));
        }

        var restoreValidation = await ValidateRequest(
            new PagoCreateDto
            {
                IdRenta = pago.IdRenta,
                IdMetodoPago = pago.IdMetodoPago,
                IdMoneda = pago.IdMoneda,
                Monto = pago.Monto,
                TasaCambioAplicada = pago.TasaCambioAplicada,
                FechaPago = pago.FechaPago,
                Referencia = pago.Referencia,
                Observaciones = pago.Observaciones
            },
            idEmpresa,
            cancellationToken);

        if (!restoreValidation.IsValid)
            return Conflict(Failure<PagoResponseDto>(restoreValidation.ErrorMessage!));

        pago.Activo = true;
        pago.FechaActualizacion = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(
            Success(
                (await LoadResponse(pago.IdPago, idEmpresa, cancellationToken))!,
                "Pago restaurado correctamente."));
    }

    // GET: api/pagos/renta/5/resumen
    [HttpGet("renta/{idRenta:int}/resumen")]
    public async Task<ActionResult<ApiResponse<PagoSummaryDto>>> GetSummary(
        int idRenta,
        CancellationToken cancellationToken = default)
    {
        var idEmpresa = GetEmpresaId();

        var renta = await _context.Rentas
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item =>
                    item.IdRenta == idRenta &&
                    item.IdEmpresa == idEmpresa,
                cancellationToken);

        if (renta is null)
        {
            return NotFound(
                Failure<PagoSummaryDto>(
                    "No se encontró la renta solicitada."));
        }

        var totalPagadoMonedaLocal = await _context.Pagos
            .AsNoTracking()
            .Where(pago =>
                pago.IdEmpresa == idEmpresa &&
                pago.IdRenta == idRenta &&
                pago.Activo)
            .SumAsync(
                pago => (decimal?)pago.MontoMonedaLocal,
                cancellationToken) ?? 0m;

        var totalRentaMonedaLocal = renta.TotalMonedaLocal;

        var balancePendiente = Math.Max(
            totalRentaMonedaLocal - totalPagadoMonedaLocal,
            0m);

        var resumen = new PagoSummaryDto
        {
            IdRenta = idRenta,
            TotalRentaMonedaLocal = totalRentaMonedaLocal,
            TotalPagadoMonedaLocal = totalPagadoMonedaLocal,
            BalancePendienteMonedaLocal = balancePendiente,
            TieneSobrepago =
                totalPagadoMonedaLocal > totalRentaMonedaLocal,
            MontoSobrepagoMonedaLocal = Math.Max(
                totalPagadoMonedaLocal - totalRentaMonedaLocal,
                0m)
        };

        return Ok(
            Success(
                resumen,
                "Resumen de pagos obtenido correctamente."));
    }

    private async Task<PaymentValidationResult> ValidateRequest(
        PagoCreateDto request,
        int idEmpresa,
        CancellationToken cancellationToken,
        int? excludedPaymentId = null)
    {
        var renta = await (
            from item in _context.Rentas.AsNoTracking()
            join estado in _context.Estados.AsNoTracking()
                on item.IdEstado equals estado.IdEstado
            where item.IdRenta == request.IdRenta &&
                  item.IdEmpresa == idEmpresa
            select new
            {
                item.TotalMonedaLocal,
                EstadoCodigo = estado.Codigo
            }).FirstOrDefaultAsync(cancellationToken);

        if (renta is null)
        {
            return PaymentValidationResult.Invalid(
                "La renta no existe o no pertenece a la empresa.");
        }

        if (renta.EstadoCodigo.Equals("CANCELADA", StringComparison.OrdinalIgnoreCase))
            return PaymentValidationResult.Invalid("No se pueden registrar pagos en una renta cancelada.");

        var metodoPagoExiste = await _context.MetodosPago
            .AsNoTracking()
            .AnyAsync(
                metodo =>
                    metodo.IdMetodoPago == request.IdMetodoPago &&
                    metodo.Activo,
                cancellationToken);

        if (!metodoPagoExiste)
        {
            return PaymentValidationResult.Invalid(
                "El método de pago no existe o está inactivo.");
        }

        var codigoMoneda = await _context.Monedas
            .AsNoTracking()
            .Where(moneda =>
                moneda.IdMoneda == request.IdMoneda &&
                moneda.Activo)
            .Select(moneda => moneda.Codigo)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(codigoMoneda))
        {
            return PaymentValidationResult.Invalid(
                "La moneda no existe o está inactiva.");
        }

        if (request.Monto <= 0)
        {
            return PaymentValidationResult.Invalid(
                "El monto debe ser mayor que cero.");
        }

        if (!codigoMoneda.Equals(
                CodigoMonedaLocal,
                StringComparison.OrdinalIgnoreCase) &&
            request.TasaCambioAplicada <= 0)
        {
            return PaymentValidationResult.Invalid(
                "Debe indicar una tasa de cambio válida.");
        }

        var tasa = CalculateExchangeRate(codigoMoneda, request.TasaCambioAplicada);
        var montoLocal = CalculateLocalAmount(request.Monto, tasa);
        var pagado = await _context.Pagos.AsNoTracking()
            .Where(item => item.IdEmpresa == idEmpresa &&
                           item.IdRenta == request.IdRenta &&
                           item.Activo &&
                           (!excludedPaymentId.HasValue || item.IdPago != excludedPaymentId.Value))
            .SumAsync(item => (decimal?)item.MontoMonedaLocal, cancellationToken) ?? 0m;

        var balance = Math.Max(renta.TotalMonedaLocal - pagado, 0m);
        if (montoLocal > balance)
            return PaymentValidationResult.Invalid(
                $"El pago excede el balance pendiente de {balance:N2} DOP.");

        return PaymentValidationResult.Valid(codigoMoneda);
    }

    private static decimal CalculateExchangeRate(
        string currencyCode,
        decimal requestedRate)
    {
        return currencyCode.Equals(
            CodigoMonedaLocal,
            StringComparison.OrdinalIgnoreCase)
            ? 1m
            : requestedRate;
    }

    private static decimal CalculateLocalAmount(
        decimal amount,
        decimal exchangeRate)
    {
        return decimal.Round(
            amount * exchangeRate,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static string? NormalizeNullable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private Task<PagoResponseDto?> LoadResponse(
        int idPago,
        int idEmpresa,
        CancellationToken cancellationToken) =>
        (from pago in _context.Pagos.AsNoTracking()
         join moneda in _context.Monedas.AsNoTracking() on pago.IdMoneda equals moneda.IdMoneda
         join metodo in _context.MetodosPago.AsNoTracking() on pago.IdMetodoPago equals metodo.IdMetodoPago
         join estado in _context.Estados.AsNoTracking() on pago.IdEstado equals estado.IdEstado
         join renta in _context.Rentas.AsNoTracking() on pago.IdRenta equals renta.IdRenta
         join cliente in _context.Clientes.AsNoTracking() on renta.IdCliente equals cliente.IdCliente
         join vehiculo in _context.Vehiculos.AsNoTracking() on renta.IdVehiculo equals vehiculo.IdVehiculo
         where pago.IdPago == idPago && pago.IdEmpresa == idEmpresa &&
               renta.IdEmpresa == idEmpresa && cliente.IdEmpresa == idEmpresa &&
               vehiculo.IdEmpresa == idEmpresa
         select new PagoResponseDto
         {
             IdPago = pago.IdPago,
             IdRenta = pago.IdRenta,
             IdMetodoPago = pago.IdMetodoPago,
             MetodoPagoNombre = metodo.Nombre,
             IdEstado = pago.IdEstado,
             EstadoCodigo = estado.Codigo,
             EstadoNombre = estado.Nombre,
             IdMoneda = pago.IdMoneda,
             CodigoMoneda = moneda.Codigo,
             SimboloMoneda = moneda.Simbolo,
             ClienteNombre = cliente.Nombre + " " + cliente.Apellido,
             VehiculoDescripcion = vehiculo.Marca + " " + vehiculo.Modelo + " · " + vehiculo.Placa,
             Monto = pago.Monto,
             TasaCambioAplicada = pago.TasaCambioAplicada,
             MontoMonedaLocal = pago.MontoMonedaLocal,
             FechaPago = pago.FechaPago,
             Referencia = pago.Referencia,
             Observaciones = pago.Observaciones,
             Activo = pago.Activo,
             FechaRegistro = pago.FechaRegistro,
             FechaActualizacion = pago.FechaActualizacion
         }).FirstOrDefaultAsync(cancellationToken);

    private static ApiResponse<T> Success<T>(
        T data,
        string message)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    private static ApiResponse<T> Failure<T>(
        string message,
        object? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }

    private sealed record PaymentValidationResult(
        bool IsValid,
        string? CurrencyCode,
        string? ErrorMessage)
    {
        public static PaymentValidationResult Valid(
            string currencyCode)
        {
            return new PaymentValidationResult(
                true,
                currencyCode,
                null);
        }

        public static PaymentValidationResult Invalid(
            string errorMessage)
        {
            return new PaymentValidationResult(
                false,
                null,
                errorMessage);
        }
    }
}

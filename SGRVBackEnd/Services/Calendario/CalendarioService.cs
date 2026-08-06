using Microsoft.EntityFrameworkCore;
using SGRVBackEnd.Data;
using SGRVBackEnd.DTOs.Calendario;
using SGRVBackEnd.Helpers;

namespace SGRVBackEnd.Services.Calendario;

public sealed class CalendarioService : ICalendarioService
{
    private const string TipoReservacion = "RESERVACION";
    private const string TipoRenta = "RENTA";
    private readonly AppDbContext _context;

    public CalendarioService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CalendarioResumenResponseDto> ObtenerAsync(
        int idEmpresa,
        CalendarioSearchDto search,
        CancellationToken cancellationToken = default)
    {
        var desde = search.FechaDesde.UtcDateTime;
        var hasta = search.FechaHasta.UtcDateTime;
        var tipo = search.Tipo?.Trim().ToUpperInvariant();
        var eventos = new List<CalendarioEventoResponseDto>();

        if (tipo is null or "" or TipoReservacion)
        {
            var reservaciones = await (
                from reservacion in _context.Reservaciones.AsNoTracking()
                join vehiculo in _context.Vehiculos.AsNoTracking()
                    on reservacion.IdVehiculo equals vehiculo.IdVehiculo
                join cliente in _context.Clientes.AsNoTracking()
                    on reservacion.IdCliente equals cliente.IdCliente
                join estado in _context.Estados.AsNoTracking()
                    on reservacion.IdEstado equals estado.IdEstado
                where reservacion.IdEmpresa == idEmpresa &&
                      vehiculo.IdEmpresa == idEmpresa &&
                      cliente.IdEmpresa == idEmpresa &&
                      estado.Categoria == ReservationConstants.Category &&
                      reservacion.FechaInicio < hasta &&
                      reservacion.FechaFin >= desde &&
                      (!search.IdVehiculo.HasValue ||
                       reservacion.IdVehiculo == search.IdVehiculo.Value)
                select new CalendarioEventoResponseDto
                {
                    Id = "RES-" + reservacion.IdReservacion,
                    Tipo = TipoReservacion,
                    IdEntidad = reservacion.IdReservacion,
                    IdVehiculo = reservacion.IdVehiculo,
                    Vehiculo = vehiculo.Marca + " " + vehiculo.Modelo +
                        " (" + vehiculo.Placa + ")",
                    IdCliente = reservacion.IdCliente,
                    Cliente = cliente.Nombre + " " + cliente.Apellido,
                    EstadoCodigo = estado.Codigo,
                    Estado = estado.Nombre,
                    FechaInicio = reservacion.FechaInicio,
                    FechaFin = reservacion.FechaFin,
                    Observacion = reservacion.Observacion
                }).ToListAsync(cancellationToken);

            eventos.AddRange(reservaciones);
        }

        if (tipo is null or "" or TipoRenta)
        {
            var rentas = await (
                from renta in _context.Rentas.AsNoTracking()
                join vehiculo in _context.Vehiculos.AsNoTracking()
                    on renta.IdVehiculo equals vehiculo.IdVehiculo
                join cliente in _context.Clientes.AsNoTracking()
                    on renta.IdCliente equals cliente.IdCliente
                join estado in _context.Estados.AsNoTracking()
                    on renta.IdEstado equals estado.IdEstado
                where renta.IdEmpresa == idEmpresa &&
                      vehiculo.IdEmpresa == idEmpresa &&
                      cliente.IdEmpresa == idEmpresa &&
                      estado.Categoria == RentalConstants.Category &&
                      renta.FechaInicio < hasta &&
                      renta.FechaFin >= desde &&
                      (!search.IdVehiculo.HasValue ||
                       renta.IdVehiculo == search.IdVehiculo.Value)
                select new CalendarioEventoResponseDto
                {
                    Id = "REN-" + renta.IdRenta,
                    Tipo = TipoRenta,
                    IdEntidad = renta.IdRenta,
                    IdVehiculo = renta.IdVehiculo,
                    Vehiculo = vehiculo.Marca + " " + vehiculo.Modelo +
                        " (" + vehiculo.Placa + ")",
                    IdCliente = renta.IdCliente,
                    Cliente = cliente.Nombre + " " + cliente.Apellido,
                    EstadoCodigo = estado.Codigo,
                    Estado = estado.Nombre,
                    FechaInicio = renta.FechaInicio,
                    FechaFin = renta.FechaFin,
                    Observacion = renta.Observaciones
                }).ToListAsync(cancellationToken);

            eventos.AddRange(rentas);
        }

        var ordenados = eventos
            .OrderBy(x => x.FechaInicio)
            .ThenBy(x => x.Tipo)
            .ToList();

        return new CalendarioResumenResponseDto
        {
            FechaDesde = desde,
            FechaHasta = hasta,
            TotalEventos = ordenados.Count,
            TotalReservaciones = ordenados.Count(x => x.Tipo == TipoReservacion),
            TotalRentas = ordenados.Count(x => x.Tipo == TipoRenta),
            Eventos = ordenados
        };
    }
}

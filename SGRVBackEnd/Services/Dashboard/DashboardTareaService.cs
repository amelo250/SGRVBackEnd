using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SGRVBackEnd.Data;
using SGRVBackEnd.DTOs.Dashboard;
using SGRVBackEnd.Helpers;

namespace SGRVBackEnd.Services.Dashboard;

public sealed class DashboardTareaService : IDashboardTareaService
{
    private readonly AppDbContext _context;
    private readonly string _connectionString;

    public DashboardTareaService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No existe DefaultConnection.");
    }

    public async Task<DashboardResumenResponseDto> GetResumenAsync(
        int idEmpresa,
        DateTime fechaLocal,
        CancellationToken cancellationToken = default)
    {
        var hoy = DateTime.SpecifyKind(fechaLocal.Date, DateTimeKind.Unspecified);
        var manana = hoy.AddDays(1);
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);

        var reservasHoy = await _context.Reservaciones.AsNoTracking()
            .CountAsync(x => x.IdEmpresa == idEmpresa &&
                x.FechaInicio >= hoy && x.FechaInicio < manana, cancellationToken);

        var vehiculosAlquilados = await (
            from renta in _context.Rentas.AsNoTracking()
            join estado in _context.Estados.AsNoTracking() on renta.IdEstado equals estado.IdEstado
            where renta.IdEmpresa == idEmpresa && estado.Categoria == RentalConstants.Category &&
                  estado.Codigo == RentalConstants.Active
            select renta.IdVehiculo).Distinct().CountAsync(cancellationToken);

        var pagosMes = await _context.Pagos.AsNoTracking()
            .Where(x => x.IdEmpresa == idEmpresa && x.Activo &&
                x.FechaPago >= inicioMes && x.FechaPago < manana)
            .Select(x => new { x.FechaPago, x.MontoMonedaLocal })
            .ToListAsync(cancellationToken);

        var clientesActivos = await _context.Clientes.AsNoTracking()
            .CountAsync(x => x.IdEmpresa == idEmpresa && x.Activo, cancellationToken);

        var reservasRecientes = await (
            from reserva in _context.Reservaciones.AsNoTracking()
            join cliente in _context.Clientes.AsNoTracking() on reserva.IdCliente equals cliente.IdCliente
            join vehiculo in _context.Vehiculos.AsNoTracking() on reserva.IdVehiculo equals vehiculo.IdVehiculo
            join estado in _context.Estados.AsNoTracking() on reserva.IdEstado equals estado.IdEstado
            where reserva.IdEmpresa == idEmpresa && cliente.IdEmpresa == idEmpresa &&
                  vehiculo.IdEmpresa == idEmpresa
            orderby reserva.FechaCreacion descending
            select new DashboardReservaRecienteDto
            {
                IdReservacion = reserva.IdReservacion,
                Cliente = cliente.Nombre + " " + cliente.Apellido,
                Vehiculo = vehiculo.Marca + " " + vehiculo.Modelo + " (" + vehiculo.Placa + ")",
                FechaInicio = reserva.FechaInicio,
                EstadoCodigo = estado.Codigo,
                EstadoNombre = estado.Nombre
            }).Take(5).ToListAsync(cancellationToken);

        var categorias = await (
            from vehiculo in _context.Vehiculos.AsNoTracking()
            join tipo in _context.Tipos.AsNoTracking() on vehiculo.IdTipo equals tipo.IdTipo
            where vehiculo.IdEmpresa == idEmpresa && vehiculo.Activo
            group vehiculo by tipo.nombre into grupo
            orderby grupo.Count() descending
            select new { Categoria = grupo.Key, Cantidad = grupo.Count() })
            .ToListAsync(cancellationToken);
        var totalVehiculos = categorias.Sum(x => x.Cantidad);

        var actividadRentas = await (
            from renta in _context.Rentas.AsNoTracking()
            join cliente in _context.Clientes.AsNoTracking() on renta.IdCliente equals cliente.IdCliente
            where renta.IdEmpresa == idEmpresa && cliente.IdEmpresa == idEmpresa
            orderby renta.FechaCreacion descending
            select new DashboardActividadDto
            {
                Tipo = "RENTA",
                IdEntidad = renta.IdRenta,
                Titulo = "Renta registrada",
                Detalle = cliente.Nombre + " " + cliente.Apellido,
                Fecha = renta.FechaCreacion
            }).Take(5).ToListAsync(cancellationToken);

        var actividadReservas = await (
            from reserva in _context.Reservaciones.AsNoTracking()
            join cliente in _context.Clientes.AsNoTracking() on reserva.IdCliente equals cliente.IdCliente
            where reserva.IdEmpresa == idEmpresa && cliente.IdEmpresa == idEmpresa
            orderby reserva.FechaCreacion descending
            select new DashboardActividadDto
            {
                Tipo = "RESERVACION",
                IdEntidad = reserva.IdReservacion,
                Titulo = "Reservación registrada",
                Detalle = cliente.Nombre + " " + cliente.Apellido,
                Fecha = reserva.FechaCreacion
            }).Take(5).ToListAsync(cancellationToken);

        var pagosRecientes = await _context.Pagos.AsNoTracking()
            .Where(x => x.IdEmpresa == idEmpresa && x.Activo)
            .OrderByDescending(x => x.FechaPago)
            .Select(x => new { x.IdPago, x.MontoMonedaLocal, x.FechaPago })
            .Take(5).ToListAsync(cancellationToken);
        var actividadPagos = pagosRecientes.Select(x => new DashboardActividadDto
        {
            Tipo = "PAGO",
            IdEntidad = x.IdPago,
            Titulo = "Pago registrado",
            Detalle = x.MontoMonedaLocal.ToString("N2") + " DOP",
            Fecha = x.FechaPago
        });

        return new DashboardResumenResponseDto
        {
            ReservasHoy = reservasHoy,
            VehiculosAlquilados = vehiculosAlquilados,
            IngresosHoy = pagosMes.Where(x => x.FechaPago >= hoy).Sum(x => x.MontoMonedaLocal),
            ClientesActivos = clientesActivos,
            IngresosMes = pagosMes.Sum(x => x.MontoMonedaLocal),
            IngresosDiarios = Enumerable.Range(0, hoy.Day)
                .Select(offset => inicioMes.AddDays(offset))
                .Select(fecha => new DashboardIngresoDiarioDto
                {
                    Fecha = fecha,
                    Monto = pagosMes.Where(x => x.FechaPago.Date == fecha.Date)
                        .Sum(x => x.MontoMonedaLocal)
                }).ToList(),
            ReservasRecientes = reservasRecientes,
            VehiculosPorCategoria = categorias.Select(x => new DashboardCategoriaVehiculoDto
            {
                Categoria = x.Categoria,
                Cantidad = x.Cantidad,
                Porcentaje = totalVehiculos == 0 ? 0 :
                    Math.Round(x.Cantidad * 100m / totalVehiculos, 1)
            }).ToList(),
            ActividadReciente = actividadRentas.Concat(actividadReservas)
                .Concat(actividadPagos).OrderByDescending(x => x.Fecha).Take(6).ToList()
        };
    }

    public async Task<DashboardTareasResponseDto> GetAsync(
        int idEmpresa,
        DateTime fechaLocal,
        int dias,
        int limite,
        CancellationToken cancellationToken = default)
    {
        var desde = DateTime.SpecifyKind(fechaLocal.Date, DateTimeKind.Unspecified);
        var hasta = desde.AddDays(dias + 1);
        var tareas = new List<DashboardTareaResponseDto>();

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
                  estado.Codigo != RentalConstants.Cancelled &&
                  ((renta.FechaInicio >= desde && renta.FechaInicio < hasta) ||
                   (renta.FechaFin >= desde && renta.FechaFin < hasta))
            select new
            {
                renta.IdRenta,
                renta.IdVehiculo,
                Vehiculo = vehiculo.Marca + " " + vehiculo.Modelo +
                    " (" + vehiculo.Placa + ")",
                renta.IdCliente,
                Cliente = cliente.Nombre + " " + cliente.Apellido,
                renta.FechaInicio,
                renta.FechaFin,
                Estado = estado.Codigo
            }).ToListAsync(cancellationToken);

        foreach (var renta in rentas)
        {
            if (renta.FechaInicio >= desde && renta.FechaInicio < hasta)
                tareas.Add(CreateRentalTask(renta.IdRenta, renta.IdVehiculo,
                    renta.Vehiculo, renta.IdCliente, renta.Cliente,
                    renta.FechaInicio, renta.Estado, "ENTREGA"));
            if (renta.FechaFin >= desde && renta.FechaFin < hasta)
                tareas.Add(CreateRentalTask(renta.IdRenta, renta.IdVehiculo,
                    renta.Vehiculo, renta.IdCliente, renta.Cliente,
                    renta.FechaFin, renta.Estado, "RECEPCION"));
        }

        await AddMaintenanceTasksAsync(
            tareas, idEmpresa, desde, hasta, cancellationToken);

        var ordered = tareas.OrderBy(x => x.Fecha).ThenBy(x => x.Tipo).ToList();
        var tomorrow = desde.AddDays(1);
        var today = ordered.Where(x => x.Fecha >= desde && x.Fecha < tomorrow)
            .Take(limite).ToList();
        var upcoming = ordered.Where(x => x.Fecha >= tomorrow)
            .Take(limite).ToList();
        foreach (var item in today) item.EsHoy = true;

        return new DashboardTareasResponseDto
        {
            TotalHoy = ordered.Count(x => x.Fecha >= desde && x.Fecha < tomorrow),
            TotalProximas = ordered.Count(x => x.Fecha >= tomorrow),
            Hoy = today,
            Proximas = upcoming
        };
    }

    private async Task AddMaintenanceTasksAsync(
        ICollection<DashboardTareaResponseDto> tasks,
        int idEmpresa,
        DateTime desde,
        DateTime hasta,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT m.IdMantenimiento, m.IdVehiculo,
                   v.Marca + ' ' + v.Modelo + ' (' + v.Placa + ')' Vehiculo,
                   m.Fecha, t.nombre TipoNombre, m.Taller
              FROM dbo.Mantenimientos m
              INNER JOIN dbo.Vehiculos v ON v.IdVehiculo=m.IdVehiculo
              INNER JOIN dbo.Tipos t ON t.IdTipo=m.IdTipoMantenimiento
             WHERE v.IdEmpresa=@IdEmpresa
               AND m.Fecha>=@Desde AND m.Fecha<@Hasta
             ORDER BY m.Fecha;
            """;
        command.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
        command.Parameters.Add("@Desde", SqlDbType.DateTime2).Value = desde;
        command.Parameters.Add("@Hasta", SqlDbType.DateTime2).Value = hasta;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt32(reader.GetOrdinal("IdMantenimiento"));
            tasks.Add(new DashboardTareaResponseDto
            {
                Id = $"MAN-{id}",
                Tipo = "MANTENIMIENTO",
                IdEntidad = id,
                IdVehiculo = reader.GetInt32(reader.GetOrdinal("IdVehiculo")),
                Vehiculo = reader.GetString(reader.GetOrdinal("Vehiculo")),
                Fecha = reader.GetDateTime(reader.GetOrdinal("Fecha")),
                Titulo = reader.GetString(reader.GetOrdinal("TipoNombre")),
                Detalle = reader.IsDBNull(reader.GetOrdinal("Taller"))
                    ? null : reader.GetString(reader.GetOrdinal("Taller")),
                EstadoCodigo = "REGISTRADO"
            });
        }
    }

    private static DashboardTareaResponseDto CreateRentalTask(
        int idRenta,
        int idVehiculo,
        string vehiculo,
        int idCliente,
        string cliente,
        DateTime fecha,
        string estado,
        string tipo) => new()
    {
        Id = $"{(tipo == "ENTREGA" ? "ENT" : "REC")}-{idRenta}",
        Tipo = tipo,
        IdEntidad = idRenta,
        IdVehiculo = idVehiculo,
        Vehiculo = vehiculo,
        IdCliente = idCliente,
        Cliente = cliente,
        Fecha = fecha,
        Titulo = tipo == "ENTREGA" ? "Entrega de vehículo" : "Recepción de vehículo",
        Detalle = cliente,
        EstadoCodigo = estado
    };
}

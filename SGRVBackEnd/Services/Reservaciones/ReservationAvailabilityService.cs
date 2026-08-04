using Microsoft.EntityFrameworkCore;
using SGRVBackEnd.Data;
using SGRVBackEnd.Helpers;

namespace SGRVBackEnd.Services.Reservations;

public sealed class ReservationAvailabilityService : IReservationAvailabilityService
{
    private readonly AppDbContext _context;

    public ReservationAvailabilityService(AppDbContext context) => _context = context;

    public async Task<bool> HasOverlapAsync(
        int companyId,
        int vehicleId,
        DateTime startUtc,
        DateTime endUtc,
        int? excludedReservationId,
        CancellationToken cancellationToken)
    {
        var reservationOverlap = await (
            from reservation in _context.Reservaciones.AsNoTracking()
            join state in _context.Estados.AsNoTracking()
                on reservation.IdEstado equals state.IdEstado
            where reservation.IdEmpresa == companyId &&
                  reservation.IdVehiculo == vehicleId &&
                  (!excludedReservationId.HasValue ||
                   reservation.IdReservacion != excludedReservationId.Value) &&
                  state.Categoria == ReservationConstants.Category &&
                  state.Codigo != ReservationConstants.Cancelled &&
                  state.Codigo != ReservationConstants.Converted &&
                  reservation.FechaInicio < endUtc &&
                  reservation.FechaFin > startUtc
            select reservation.IdReservacion)
            .AnyAsync(cancellationToken);

        if (reservationOverlap)
            return true;

        return await (
            from rental in _context.Rentas.AsNoTracking()
            join state in _context.Estados.AsNoTracking()
                on rental.IdEstado equals state.IdEstado
            where rental.IdEmpresa == companyId &&
                  rental.IdVehiculo == vehicleId &&
                  state.Categoria == ReservationConstants.RentalCategory &&
                  state.Codigo != ReservationConstants.RentalCancelled &&
                  state.Codigo != ReservationConstants.RentalCompleted &&
                  rental.FechaInicio < endUtc &&
                  rental.FechaFin > startUtc
            select rental.IdRenta)
            .AnyAsync(cancellationToken);
    }
}

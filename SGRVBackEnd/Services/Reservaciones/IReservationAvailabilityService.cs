namespace SGRVBackEnd.Services.Reservations;

public interface IReservationAvailabilityService
{
    Task<bool> HasOverlapAsync(
        int companyId,
        int vehicleId,
        DateTime startUtc,
        DateTime endUtc,
        int? excludedReservationId,
        CancellationToken cancellationToken);
}

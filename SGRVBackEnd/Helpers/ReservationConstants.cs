namespace SGRVBackEnd.Helpers;

public static class ReservationConstants
{
    public const string Category = "RESERVACION";
    public const string Pending = "PENDIENTE";
    public const string Confirmed = "CONFIRMADA";
    public const string Cancelled = "CANCELADA";
    public const string Converted = "CONVERTIDA";

    public const string RentalCategory = "RENTA";
    public const string RentalCancelled = "CANCELADA";
    public const string RentalCompleted = "FINALIZADA";
}

public static class ApplicationRoles
{
    // Deben coincidir exactamente con Rol.Codigo y los claims emitidos por JWT.
    public const string ReservationManagers = "ADMIN,SUPADMIN";
}

using SGRVBackEnd.DTOs.Mantenimientos;

namespace SGRVBackEnd.Validators;

public static class MantenimientoValidator
{
    public static string? Validate(MantenimientoCreateDto request)
    {
        if (request.IdVehiculo <= 0) return "Debe seleccionar un vehículo.";
        if (request.IdTipoMantenimiento <= 0)
            return "Debe seleccionar un tipo de mantenimiento.";
        if (request.Fecha == default) return "Debe indicar la fecha.";
        if (request.Fecha > DateTime.UtcNow.AddDays(1))
            return "La fecha de un mantenimiento realizado no puede ser futura.";
        if (request.Kilometraje is < 0) return "El kilometraje no puede ser negativo.";
        if (request.Costo < 0) return "El costo no puede ser negativo.";
        if (request.Taller?.Length > 150) return "El taller admite hasta 150 caracteres.";
        return null;
    }

    public static string? ValidateSearch(MantenimientoSearchDto search)
    {
        if (search.PageNumber < 1) return "pageNumber debe ser mayor que cero.";
        if (search.PageSize is < 1 or > 100)
            return "pageSize debe estar entre 1 y 100.";
        if (search.FechaDesde.HasValue && search.FechaHasta.HasValue &&
            search.FechaHasta < search.FechaDesde)
            return "El rango de fechas no es válido.";
        return null;
    }
}

using SGRVBackEnd.DTOs.Gastos;

namespace SGRVBackEnd.Validators;

public static class GastoValidator
{
    public static string? Validate(GastoCreateDto request)
    {
        if (request.IdTipoGasto <= 0) return "Debe seleccionar un tipo de gasto válido.";
        if (request.IdMoneda <= 0) return "Debe seleccionar una moneda válida.";
        if (request.IdVehiculo is <= 0) return "El vehículo seleccionado no es válido.";
        if (request.Fecha == default) return "Debe indicar la fecha del gasto.";
        if (request.Fecha.Date > DateTime.UtcNow.Date) return "La fecha del gasto no puede estar en el futuro.";
        if (string.IsNullOrWhiteSpace(request.Concepto)) return "Debe indicar el concepto del gasto.";
        if (request.Concepto.Trim().Length > 200) return "El concepto admite hasta 200 caracteres.";
        if (request.Monto <= 0) return "El monto debe ser mayor que cero.";
        if (request.TasaCambioAplicada <= 0) return "La tasa de cambio debe ser mayor que cero.";
        if (request.Kilometraje is < 0) return "El kilometraje no puede ser negativo.";
        return null;
    }

    public static bool TryDecodeRowVersion(string value, out byte[] rowVersion)
    {
        try
        {
            rowVersion = Convert.FromBase64String(value);
            return rowVersion.Length == 8;
        }
        catch (FormatException)
        {
            rowVersion = [];
            return false;
        }
    }
}

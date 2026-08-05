namespace SGRVBackEnd.Validators;

public static class RentaValidator
{
    public static string? ValidateDates(DateTime startUtc, DateTime endUtc)
    {
        if (startUtc == default || endUtc == default)
            return "Debe indicar las fechas de inicio y fin.";
        if (endUtc <= startUtc)
            return "La fecha final debe ser posterior a la fecha inicial.";
        return null;
    }

    public static string? ValidateAmounts(
        decimal pricePerDay,
        decimal taxes,
        decimal discounts,
        decimal deposit,
        decimal exchangeRate)
    {
        if (pricePerDay <= 0) return "La tarifa diaria debe ser mayor que cero.";
        if (taxes < 0) return "Los impuestos no pueden ser negativos.";
        if (discounts < 0) return "Los descuentos no pueden ser negativos.";
        if (deposit < 0) return "El depósito no puede ser negativo.";
        if (exchangeRate <= 0) return "La tasa de cambio debe ser mayor que cero.";
        return null;
    }

    public static int CalculateDays(DateTime startUtc, DateTime endUtc) =>
        Math.Max(1, (int)Math.Ceiling((endUtc - startUtc).TotalDays));

    public static decimal RoundMoney(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}

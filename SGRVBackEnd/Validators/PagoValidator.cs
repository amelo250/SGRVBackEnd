using SGRVBackEnd.DTOs.Pagos;

namespace SGRVBackEnd.Validators;

public static class PagoValidator
{
    public static string? Validate(PagoCreateDto request)
    {
        if (request.IdRenta <= 0)
            return "Debe seleccionar una renta válida.";
        if (request.IdMetodoPago <= 0)
            return "Debe seleccionar un método de pago válido.";
        if (request.IdMoneda <= 0)
            return "Debe seleccionar una moneda válida.";
        if (request.Monto <= 0)
            return "El monto debe ser mayor que cero.";
        if (request.FechaPago.HasValue && request.FechaPago.Value > DateTime.UtcNow.AddMinutes(5))
            return "La fecha del pago no puede estar en el futuro.";

        return null;
    }
}

using SGRVBackEnd.DTOs.Calendario;

namespace SGRVBackEnd.Services.Calendario;

public interface ICalendarioService
{
    Task<CalendarioResumenResponseDto> ObtenerAsync(
        int idEmpresa,
        CalendarioSearchDto search,
        CancellationToken cancellationToken = default);
}

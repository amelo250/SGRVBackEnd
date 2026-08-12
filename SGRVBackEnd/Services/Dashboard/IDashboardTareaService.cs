using SGRVBackEnd.DTOs.Dashboard;

namespace SGRVBackEnd.Services.Dashboard;

public interface IDashboardTareaService
{
    Task<DashboardResumenResponseDto> GetResumenAsync(
        int idEmpresa,
        DateTime fechaLocal,
        CancellationToken cancellationToken = default);

    Task<DashboardTareasResponseDto> GetAsync(
        int idEmpresa,
        DateTime fechaLocal,
        int dias,
        int limite,
        CancellationToken cancellationToken = default);
}

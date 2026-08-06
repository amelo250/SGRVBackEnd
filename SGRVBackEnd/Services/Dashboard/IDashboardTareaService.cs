using SGRVBackEnd.DTOs.Dashboard;

namespace SGRVBackEnd.Services.Dashboard;

public interface IDashboardTareaService
{
    Task<DashboardTareasResponseDto> GetAsync(
        int idEmpresa,
        DateTime fechaLocal,
        int dias,
        int limite,
        CancellationToken cancellationToken = default);
}

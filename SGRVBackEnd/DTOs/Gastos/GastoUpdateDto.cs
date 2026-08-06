namespace SGRVBackEnd.DTOs.Gastos;

public sealed class GastoUpdateDto : GastoCreateDto
{
    public string RowVersion { get; set; } = string.Empty;
}

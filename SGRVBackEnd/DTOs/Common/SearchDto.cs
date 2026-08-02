using System.ComponentModel.DataAnnotations;

namespace SGRVBackEnd.DTOs.Common;

public abstract class SearchDto
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public string? Search { get; set; }

    public bool IncluirInactivos { get; set; }
}
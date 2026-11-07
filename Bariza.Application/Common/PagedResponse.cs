namespace Bariza.Application.Common;

/// <summary>
/// Public pagination shape used by the marketplace API. It uses PageNumber
/// instead of the admin PagedResult.Page name so the public contract stays stable.
/// </summary>
public class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

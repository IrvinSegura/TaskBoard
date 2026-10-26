namespace TaskBoard.Application.Common.Paging;

/// <summary>
/// A 1-based page of a list. Out-of-range values from the query string are clamped instead of rejected.
/// </summary>
public sealed record PageRequest
{
    public const int DefaultPageSize = 50;

    public const int MaxPageSize = 200;

    // Keeps (Page - 1) * PageSize inside an int.
    private const int MaxPage = int.MaxValue / MaxPageSize;

    private PageRequest(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    public int Page { get; }

    public int PageSize { get; }

    public int Skip => (Page - 1) * PageSize;

    public static PageRequest Create(int? page, int? pageSize) =>
        new(Math.Clamp(page ?? 1, 1, MaxPage), Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize));
}

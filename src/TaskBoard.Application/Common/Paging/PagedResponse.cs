using Microsoft.EntityFrameworkCore;

namespace TaskBoard.Application.Common.Paging;

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public bool HasNextPage => (long)Page * PageSize < TotalCount;
}

public static class PagingExtensions
{
    // The query must be ordered by a unique key last, or rows can repeat or go missing between pages.
    public static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(
        this IQueryable<T> query,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip(page.Skip).Take(page.PageSize).ToListAsync(cancellationToken);

        return new PagedResponse<T>(items, page.Page, page.PageSize, totalCount);
    }
}

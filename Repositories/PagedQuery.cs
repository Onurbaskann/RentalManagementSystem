using KiraTakip.Models.Common;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

internal static class PagedQuery
{
    private const int MaximumPageSize = 200;

    public static Task<PagedResult<TResult>> CreateAsync<TSource, TResult>(
        IQueryable<TSource> countQuery,
        IQueryable<TResult> orderedItemsQuery,
        TableQuery query,
        CancellationToken cancellationToken = default)
        => CreateAsync(
            countQuery,
            orderedItemsQuery,
            query.Page,
            query.SafeSize,
            cancellationToken);

    public static async Task<PagedResult<TResult>> CreateAsync<TSource, TResult>(
        IQueryable<TSource> countQuery,
        IQueryable<TResult> orderedItemsQuery,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePageSize = Math.Clamp(pageSize, 1, MaximumPageSize);
        var total = await countQuery.CountAsync(cancellationToken);
        var totalPages = total == 0
            ? 1
            : (int)Math.Ceiling(total / (double)safePageSize);
        var safePage = Math.Clamp(page, 1, totalPages);
        var items = await orderedItemsQuery
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TResult>
        {
            Items = items,
            Total = total,
            Page = safePage,
            Size = safePageSize
        };
    }
}

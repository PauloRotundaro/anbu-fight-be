using AnbuFight.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AnbuFight.Application.Common.Extensions;

public static class QueryableExtensions
{
    /// <summary>
    /// Counts and fetches a single page. Call it on an already projected query so the database
    /// returns only the columns the DTO needs.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? PagingDefaults.Page : page;
        pageSize = Math.Clamp(pageSize, 1, PagingDefaults.MaxPageSize);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        if (totalCount == 0)
        {
            return PagedResult<T>.Empty(page, pageSize);
        }

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<T>(items, page, pageSize, totalCount);
    }
}

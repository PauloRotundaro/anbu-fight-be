namespace AnbuFight.Application.Common.Models;

/// <summary>Paging parameters shared by every list query.</summary>
public interface IPagedQuery
{
    int Page { get; }

    int PageSize { get; }
}

public static class PagingDefaults
{
    public const int Page = 1;

    public const int PageSize = 20;

    /// <summary>Hard ceiling so a single request can never scan the whole table.</summary>
    public const int MaxPageSize = 100;
}

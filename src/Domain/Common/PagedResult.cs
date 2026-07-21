namespace KitchenwareBot.Domain.Common;

/// <summary>A page of results plus the metadata needed to render pagination controls.</summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int Page { get; }
    public int PageSize { get; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page < 1 ? 1 : page;
        PageSize = pageSize;
    }

    public static PagedResult<T> Empty(int page, int pageSize)
        => new(Array.Empty<T>(), 0, page, pageSize);
}

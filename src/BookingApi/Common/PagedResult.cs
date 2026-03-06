namespace BookingApi.Common;

/// <summary>Paged result wrapper returned by all list endpoints.</summary>
public sealed record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrev => Page > 1;
}

/// <summary>Uniform error envelope returned on validation/not-found failures.</summary>
public sealed record ApiError(string Code, string Message, IEnumerable<string>? Details = null);

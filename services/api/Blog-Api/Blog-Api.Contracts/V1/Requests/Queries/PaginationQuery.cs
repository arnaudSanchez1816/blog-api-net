namespace BlogApi.Contracts.V1.Requests.Queries;

public record PaginationQuery
{
    public const int MaxPageSize = 50;
    public const int DefaultPageSize = 20;

    public int PageNumber
    {
        get;
        init => field = Math.Max(value, 1);
    } = 1;

    public int PageSize
    {
        get;
        init => field = Math.Clamp(value, 1, MaxPageSize);
    } = DefaultPageSize;

    public PaginationQuery()
    {
        PageNumber = 1;
        PageSize = DefaultPageSize;
    }

    public PaginationQuery(int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        PageSize = Math.Clamp(pageSize, 1, MaxPageSize);
    }

    public void Deconstruct(out int pageNumber, out int pageSize)
    {
        pageNumber = PageNumber;
        pageSize = PageSize;
    }
}
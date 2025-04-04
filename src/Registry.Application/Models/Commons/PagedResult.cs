namespace Application.Models.Commons;

public class PagedResult<T>
{
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }
    public int TotalItems { get; set; }
    public IEnumerable<T> Items { get; set; } = [];
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}
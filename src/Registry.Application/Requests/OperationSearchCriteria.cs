using Application.Consts;

namespace Application.Requests;

public class OperationSearchCriteria
{
    public string? OperationName { get; set; } = OperationAction.Insert;

    public string? OperationApprovalStatus { get; set; } = "PENDING|APPROVED";

    public string[]? OperationProcessStatus { get; set; } = { ProcessStatus.Sent, ProcessStatus.Failed };

    public bool? FetchSystemGeneratedOperation { get; set; } = false;

    public DateTime? PublishedAt { get; set; } = null;

    // If true, filter for operations with PublishedAt > PublishedAt value; if false, filter for PublishedAt < PublishedAt value.
    public bool? PublishedAtGreaterThan { get; set; } = null;
    
    // If true, get operations where PublishedAt is null.
    public bool? IncludeNullPublishedAt { get; set; } = false;

    public int PageNumber { get; set; }

    public int PageSize { get; set; }
}

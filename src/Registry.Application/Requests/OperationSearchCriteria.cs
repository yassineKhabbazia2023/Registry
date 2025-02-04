using Application.Consts;

namespace Application.Requests;

public class OperationSearchCriteria
{
    public string? OperationName { get; set; } = "INSERT";

    public string? Status { get; set; } = "PENDING|APPROVED";

    public string[]? OperationProcessStatus { get; set; } = { ProcessStatus.Sent, ProcessStatus.Failed };
}

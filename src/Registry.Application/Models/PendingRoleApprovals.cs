namespace Application.Models;

public class PendingRoleApprovals
{
    public string AccountNumber { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;

    public IEnumerable<PendingRoleApprovalsDetails> Operations { get; set; } = [];
}
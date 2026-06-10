namespace Application.Models;

public class PendingRoleApprovalsDetails
{
    public int OperationId { get; set; }
    public DateTime CreationDate { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string LegalName { get; set; }
    public bool IsSignatory { get; set; }
    public string? MobilePhone { get; set; }
}
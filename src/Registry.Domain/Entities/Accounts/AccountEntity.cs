namespace Domain.Entities.Accounts
{
    public class AccountEntity
    {
        public Guid? AccountGlobalUniqueId { get; set; }
        public required int AccountId { get; set; }
        public required string AccountNumber { get; set; }
        public string? ModifiedBy { get; set; }

        public DateTime? DeploymentDate { get; set; }

        public string? DeploymentStatus { get; set; }

        public string? CreatedBy { get; set; }
    }
}

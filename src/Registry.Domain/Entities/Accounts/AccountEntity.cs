namespace Domain.Entities.Accounts
{
    public class AccountEntity
    {
        public Guid? AccountGlobalUniqueId { get; set; }
        public required int AccountId { get; set; }
        public required string AccountNumber { get; set; }
        public string? ModifiedBy { get; set; }
        public string? CreatedBy { get; set; }
    }
}

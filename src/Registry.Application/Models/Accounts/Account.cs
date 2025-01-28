namespace Application.Models.Accounts
{
    public class Account
    {
        public Guid? AccountGlobalUniqueId { get; set; }
        public required int AccountId { get; set; }
        public required string AccountNumber { get; set; }
        public string? LegalName { get; set; }
        public string? SiretNumber { get; set; }
        public string? Status { get; set; }
        public bool? IsActive { get; set; } = true;
        public DateTime CreationDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}

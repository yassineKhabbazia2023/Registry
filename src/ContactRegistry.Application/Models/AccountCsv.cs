namespace Application.Models
{
    /// <summary>
    /// AccountCsv.
    /// </summary>
    /// <param name="Id">Id.</param>
    /// <param name="AccountNumber">AccountNumber.</param>
    /// <param name="LegalName">LegalName.</param>
    /// <param name="AccountFlagESCActif">AccountFlagESCActif.</param>
    public record AccountCsv(Guid Id, string AccountNumber, string LegalName, bool AccountFlagESCActif);
}

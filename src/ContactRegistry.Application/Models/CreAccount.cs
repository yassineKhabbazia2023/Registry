// <copyright file="CreAccount.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models
{
    public class CreAccount
    {
        public Guid AccountGlobalUniqueIdentifier { get; set; }

        public DateTime? Updated { get; set; }

        public string LegalName { get; set; }

        public string? AccountNumber { get; set; }

        public bool AccountFlagEscActif { get; set; }
        public string? AccountCommercialName { get; set; }
        public string? AccountType { get; set; }
        public string? AccountEmail { get; set; }
        public string? AccountNafIdentifier { get; set; }
        public string? AccountSectorCode { get; set; }
        public string? AccountTaxeValeurAjoutee { get; set; }
        public string? AccountDeliveryEmail { get; set; }
        public string? AccountBillingEmail { get; set; }
        public string? AccountTaxationSystem { get; set; }
        public string? AccountSourceName { get; set; }
        public string? AccountISIN { get; set; }
        public string? AccountRegisterIdentification1 { get; set; }
        public string? AccountStaffSize { get; set; }
        public string? AccountDeliveryFax { get; set; }
        public string? AccountBillingFax { get; set; }
        public string? AccountTurnoverSlice { get; set; }
        public string? AccountRegimeFiscal { get; set; }
        public string? AccountTypeTenueComptable { get; set; }
        public string? AccountFormeJuridique { get; set; }
        public string? AccountStaffSizeSlice { get; set; }
        public string? AccountEscCategory { get; set; }
        public string? AccountCodeFormeJuridique { get; set; }
        public DateTime? AccountInsertedDate { get; set; }
        public DateTime? AccountUpdatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        public string? DeliveryAddressLine1 { get; set; }
        public string? DeliveryAddressLine2 { get; set; }
        public string? DeliveryAddressLine3 { get; set; }
        public string? DeliveryCity { get; set; }
        public string? DeliveryZipCode { get; set; }
        public string? DeliveryCountry { get; set; }
        public string? DeliveryState { get; set; }
        public string? BillingAddressLine1 { get; set; }
        public string? BillingAddressLine2 { get; set; }
        public string? BillingAddressLine3 { get; set; }
        public string? BillingCity { get; set; }
        public string? BillingZipCode { get; set; }
        public string? BillingCountry { get; set; }
        public string? BillingState { get; set; }
        public string? DeploymentStatus { get; set; }
        public DateTime? DeploymentDate { get; set; }
    }
}

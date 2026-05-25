// <copyright file="MapAkuiteoCustomerCreation.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Application.Requests;

namespace Application.Mappers;

/// <summary>
/// Maps Registry Akuiteo customer payloads to the downstream Akuiteo contract.
/// </summary>
public static class MapAkuiteoCustomerCreation
{
    /// <summary>
    /// Maps the Registry request to the Akuiteo external contract.
    /// </summary>
    /// <param name="source">The Registry request.</param>
    /// <param name="caseManagerEmail">The resolved case-manager email.</param>
    /// <param name="accountManagerEmail">The resolved account-manager email.</param>
    /// <returns>The downstream Akuiteo request payload.</returns>
    public static AkuiteoCreateCustomerRequest MapToAkuiteoCreateCustomerRequest(
        this AkuiteoCustomerCreationRequest source,
        string caseManagerEmail,
        string accountManagerEmail)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseManagerEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountManagerEmail);

        return new AkuiteoCreateCustomerRequest
        {
            LegalName = source.LegalName!,
            Siren = source.Siren!,
            Siret = source.Siret!,
            LegalStructure = source.LegalStructure!,
            LegalFormCode = source.LegalForm!,
            NafCode = source.NafCode!,
            Address = new AkuiteoCreateCustomerAddressRequest
            {
                Line1 = source.Address!,
                ZipCode = source.ZipCode!,
                City = source.City!,
                DepartmentCode = source.DepartmentCode!,
                RegionCode = source.RegionCode!,
                CountryCode = source.CountryCode!
            },
            CaseManagerEmail = caseManagerEmail,
            AccountManagerEmail = accountManagerEmail
        };
    }
}

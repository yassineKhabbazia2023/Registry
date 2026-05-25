// <copyright file="MapAkuiteoContactCreation.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Application.Requests;

namespace Application.Mappers;

/// <summary>
/// Maps Registry Akuiteo contact payloads to the downstream Akuiteo contract.
/// </summary>
public static class MapAkuiteoContactCreation
{
    /// <summary>
    /// Maps the Registry request to the Akuiteo external contract.
    /// </summary>
    /// <param name="source">The Registry request.</param>
    /// <returns>The downstream Akuiteo request payload.</returns>
    public static AkuiteoCreateContactRequest MapToAkuiteoCreateContactRequest(this AkuiteoContactCreationRequest source)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(source.ContactTypes);

        return new AkuiteoCreateContactRequest
        {
            AccountNumber = source.AccountNumber!,
            Title = source.Title!,
            LastName = source.LastName!,
            FirstName = source.FirstName!,
            JobTitle = source.JobTitle!,
            ContactDepartment = source.ContactDepartment!,
            CompanyRole = source.CompanyRole!,
            ContactTypes = new AkuiteoCreateContactTypesRequest
            {
                IsDigitalVaultContact = source.ContactTypes.IsDigitalVaultContact ?? false,
                IsDebtCollectionContact = source.ContactTypes.IsDebtCollectionContact ?? false,
                IsMandateSignatory = source.ContactTypes.IsMandateSignatory ?? false
            },
            Email = source.Email!,
            MobilePhone = source.MobilePhone!
        };
    }
}

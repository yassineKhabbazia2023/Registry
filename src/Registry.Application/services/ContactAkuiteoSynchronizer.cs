// <copyright file="ContactAkuiteoSynchronizer.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Enums;
using Application.Exceptions;
using Application.Interfaces;
using Application.Models.Contacts;
using Application.Models.Results;
using Application.Requests;
using Microsoft.Extensions.Logging;
using Pulse.Back.Events.IntegrationEvents.EventsData;

namespace Application.Services;

/// <summary>
/// Resolves Registry contact data and sends the create-or-attach request to Akuiteo.
/// </summary>
public class ContactAkuiteoSynchronizer : IContactAkuiteoSynchronizer
{
    private static readonly HashSet<string> AllowedTitles = new(StringComparer.Ordinal)
    {
        "M",
        "Mme",
        "Dr",
        "Pr"
    };

    private readonly IContactRepository contactRepository;
    private readonly IAkuiteoContactService akuiteoContactService;
    private readonly ILogger<ContactAkuiteoSynchronizer> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContactAkuiteoSynchronizer"/> class.
    /// </summary>
    /// <param name="contactRepository">The Registry contact repository.</param>
    /// <param name="akuiteoContactService">The Akuiteo contact service.</param>
    /// <param name="logger">The logger.</param>
    public ContactAkuiteoSynchronizer(
        IContactRepository contactRepository,
        IAkuiteoContactService akuiteoContactService,
        ILogger<ContactAkuiteoSynchronizer> logger)
    {
        this.contactRepository = contactRepository;
        this.akuiteoContactService = akuiteoContactService;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ContactAkuiteoSynchronizationResult> SynchronizeAsync(
        RoleCreatedEventData role,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            ValidateRole(role);
        }
        catch (AkuiteoContactCreationTechnicalException exception)
        {
            return Failed(exception.Message);
        }

        var contact = await contactRepository.GetContactByEmailOrIdAsync(contactId: role.ContactId);
        try
        {
            ValidateContact(role, contact);
        }
        catch (AkuiteoContactCreationTechnicalException exception)
        {
            return Failed(exception.Message);
        }
        cancellationToken.ThrowIfCancellationRequested();

        if (!HasValidTitle(contact!.Title))
        {
            logger.LogWarning(
                "Skipping Akuiteo contact synchronization because the Registry contact title is missing or unsupported. ContactId: {ContactId}, Email: {Email}, AccountNumber: {AccountNumber}, ContactType: {ContactType}, Title: {Title}",
                contact.ContactId,
                contact.Email,
                role.AccountNumber,
                contact.Type,
                contact.Title);
            return Failed($"The Registry contact title '{contact.Title}' is missing or unsupported.");
        }

        logger.LogInformation(
            "Synchronizing contact with Akuiteo. ContactId: {ContactId}, AccountNumber: {AccountNumber}, Email: {Email}",
            role.ContactId,
            role.AccountNumber,
            role.ContactEmail);

        try
        {
            var response = await akuiteoContactService.CreateContactAsync(CreateRequest(role, contact!));

            logger.LogInformation(
                "Contact synchronization with Akuiteo completed. ContactId: {ContactId}, AccountNumber: {AccountNumber}",
                role.ContactId,
                role.AccountNumber);

            return new ContactAkuiteoSynchronizationResult
            {
                Outcome = ContactAkuiteoSynchronizationOutcome.Sent,
                AkuiteoContactId = response.ContactId
            };
        }
        catch (AkuiteoContactCreationTechnicalException exception)
        {
            logger.LogError(
                exception,
                "Contact synchronization with Akuiteo failed. ContactId: {ContactId}, AccountNumber: {AccountNumber}",
                role.ContactId,
                role.AccountNumber);
            return Failed(exception.Message);
        }
    }

    private static ContactAkuiteoSynchronizationResult Failed(string error)
    {
        return new ContactAkuiteoSynchronizationResult
        {
            Outcome = ContactAkuiteoSynchronizationOutcome.Failed,
            Error = error
        };
    }

    /// <summary>
    /// Validates the event fields required to call Akuiteo.
    /// </summary>
    /// <param name="role">The role event data.</param>
    /// <exception cref="AkuiteoContactCreationTechnicalException">Thrown when required event data is missing.</exception>
    private static void ValidateRole(RoleCreatedEventData role)
    {
        if (string.IsNullOrWhiteSpace(role.AccountNumber))
        {
            throw new AkuiteoContactCreationTechnicalException(
                $"Cannot synchronize contact {role.ContactId} with Akuiteo because the account number is missing.");
        }

        if (string.IsNullOrWhiteSpace(role.ContactEmail))
        {
            throw new AkuiteoContactCreationTechnicalException(
                $"Cannot synchronize contact {role.ContactId} with Akuiteo because the contact email is missing.");
        }
    }

    /// <summary>
    /// Validates the Registry contact resolved for the role.
    /// </summary>
    /// <param name="role">The role event data.</param>
    /// <param name="contact">The resolved Registry contact.</param>
    /// <exception cref="AkuiteoContactCreationTechnicalException">Thrown when the contact is missing or inconsistent.</exception>
    private static void ValidateContact(RoleCreatedEventData role, Contact? contact)
    {
        if (contact is null)
        {
            throw new AkuiteoContactCreationTechnicalException(
                $"Cannot synchronize contact {role.ContactId} with Akuiteo because it does not exist in Registry.");
        }

        if (string.IsNullOrWhiteSpace(contact.FirstName) || string.IsNullOrWhiteSpace(contact.LastName))
        {
            throw new AkuiteoContactCreationTechnicalException(
                $"Cannot synchronize contact {role.ContactId} with Akuiteo because its first name or last name is missing.");
        }

        if (!string.Equals(contact.Email, role.ContactEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new AkuiteoContactCreationTechnicalException(
                $"Cannot synchronize contact {role.ContactId} with Akuiteo because its Registry email does not match the role event.");
        }
    }

    /// <summary>
    /// Determines whether the contact title is supported by the Akuiteo contract.
    /// </summary>
    /// <param name="title">The Registry contact title.</param>
    /// <returns><see langword="true"/> when the title is supported; otherwise <see langword="false"/>.</returns>
    private static bool HasValidTitle(string? title)
    {
        return !string.IsNullOrWhiteSpace(title) && AllowedTitles.Contains(title);
    }

    /// <summary>
    /// Creates the Registry request sent to the existing Akuiteo contact service.
    /// </summary>
    /// <param name="role">The role event data.</param>
    /// <param name="contact">The resolved Registry contact.</param>
    /// <returns>The Akuiteo contact creation request.</returns>
    private static AkuiteoContactCreationRequest CreateRequest(
        RoleCreatedEventData role,
        Contact contact)
    {
        return new AkuiteoContactCreationRequest
        {
            AccountNumber = role.AccountNumber,
            Title = contact.Title,
            LastName = contact.LastName,
            FirstName = contact.FirstName,
            JobTitle = string.Empty,
            ContactDepartment = string.Empty,
            CompanyRole = string.Empty,
            ContactTypes = new AkuiteoContactTypesRequest
            {
                IsDigitalVaultContact = role.ContactFlagPortailFactures == true,
                IsDebtCollectionContact = false,
                IsMandateSignatory = role.IsSignatory == true
            },
            Email = role.ContactEmail,
            MobilePhone = contact.MobilePhone ?? string.Empty
        };
    }
}

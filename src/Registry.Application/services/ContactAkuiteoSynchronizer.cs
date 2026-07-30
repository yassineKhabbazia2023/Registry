// <copyright file="ContactAkuiteoSynchronizer.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Interfaces;
using Application.Models.Contacts;
using Application.Requests;
using Microsoft.Extensions.Logging;
using Pulse.Back.Events.IntegrationEvents.EventsData;

namespace Application.Services;

/// <summary>
/// Resolves Registry contact data and sends the create-or-attach request to Akuiteo.
/// </summary>
public class ContactAkuiteoSynchronizer : IContactAkuiteoSynchronizer
{
    private const string DefaultTitle = "M";
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
    public async Task SynchronizeAsync(
        RoleCreatedEventData role,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateRole(role);

        var contact = await contactRepository.GetContactByEmailOrIdAsync(contactId: role.ContactId);
        if (contact is null)
        {
            contact = await contactRepository.GetContactByEmailOrIdAsync(email: role.ContactEmail);
        }

        ValidateContact(role, contact);
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation(
            "Synchronizing contact with Akuiteo. ContactId: {ContactId}, AccountNumber: {AccountNumber}, Email: {Email}",
            role.ContactId,
            role.AccountNumber,
            role.ContactEmail);

        try
        {
            await akuiteoContactService.CreateContactAsync(CreateRequest(role, contact!));
        }
        catch (AkuiteoContactCreationTechnicalException exception)
        {
            logger.LogError(
                exception,
                "Contact synchronization with Akuiteo failed and will be skipped. ContactId: {ContactId}, AccountNumber: {AccountNumber}",
                role.ContactId,
                role.AccountNumber);
            return;
        }

        logger.LogInformation(
            "Contact synchronization with Akuiteo completed. ContactId: {ContactId}, AccountNumber: {AccountNumber}",
            role.ContactId,
            role.AccountNumber);
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
            Title = DefaultTitle,
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
            MobilePhone = string.Empty
        };
    }
}

// <copyright file="AkuiteoCustomerService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Interfaces;
using Application.Mappers;
using Application.Models.Results;
using Application.Requests;
using Kpmg.ExceptionMiddleware.AdvancedException;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <summary>
/// Orchestrates Akuiteo customer creation for Registry.
/// </summary>
public class AkuiteoCustomerService : IAkuiteoCustomerService
{
    private readonly IAkuiteoCustomerProvider akuiteoCustomerProvider;
    private readonly IContactRepository contactRepository;
    private readonly ILogger<AkuiteoCustomerService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoCustomerService"/> class.
    /// </summary>
    /// <param name="akuiteoCustomerProvider">The Akuiteo HTTP provider.</param>
    /// <param name="contactRepository">The contact repository.</param>
    /// <param name="logger">The logger.</param>
    public AkuiteoCustomerService(
        IAkuiteoCustomerProvider akuiteoCustomerProvider,
        IContactRepository contactRepository,
        ILogger<AkuiteoCustomerService> logger)
    {
        this.akuiteoCustomerProvider = akuiteoCustomerProvider;
        this.contactRepository = contactRepository;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AkuiteoCustomerCreationResponse> CreateCustomerAsync(AkuiteoCustomerCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        logger.LogDebug(
            "Starting Akuiteo customer creation. Siret: {Siret}",
            request.Siret);

        try
        {
            var caseManagerEmail = await ResolveContactEmailAsync(request.CaseManagerContactId);
            var accountManagerEmail = await ResolveContactEmailAsync(request.AccountManagerContactId);

            var providerResult = await akuiteoCustomerProvider.CreateCustomerAsync(
                request.MapToAkuiteoCreateCustomerRequest(caseManagerEmail, accountManagerEmail));

            if (!providerResult.IsSuccess || string.IsNullOrWhiteSpace(providerResult.AccountNumber))
            {
                logger.LogError(
                    "Akuiteo customer creation failed. Siret: {Siret}, StatusCode: {StatusCode}, Error: {Error}",
                    request.Siret,
                    providerResult.StatusCode,
                    providerResult.ErrorMessage);
                throw new AkuiteoCustomerCreationTechnicalException(
                    string.IsNullOrWhiteSpace(providerResult.ErrorMessage)
                        ? "Akuiteo customer creation failed."
                        : providerResult.ErrorMessage);
            }

            logger.LogInformation(
                "Akuiteo customer created successfully. Siret: {Siret}, AccountNumber: {AccountNumber}",
                request.Siret,
                providerResult.AccountNumber);

            return new AkuiteoCustomerCreationResponse
            {
                AccountNumber = providerResult.AccountNumber
            };
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(
                ex,
                "Akuiteo customer creation failed because the external service was unreachable. Siret: {Siret}",
                request.Siret);
            throw new AkuiteoCustomerCreationTechnicalException("Akuiteo is unavailable.", ex);
        }
        catch (AkuiteoAuthenticationTechnicalException ex)
        {
            logger.LogError(
                ex,
                "Akuiteo customer creation failed because the Microsoft token could not be retrieved. Siret: {Siret}",
                request.Siret);
            throw new AkuiteoCustomerCreationTechnicalException(ex.Message, ex);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(
                ex,
                "Akuiteo customer creation timed out. Siret: {Siret}",
                request.Siret);
            throw new AkuiteoCustomerCreationTechnicalException("Akuiteo timed out.", ex);
        }
    }

    /// <summary>
    /// Resolves a contact email from the Contact.Contacts repository.
    /// </summary>
    /// <param name="contactId">The contact identifier.</param>
    /// <returns>The resolved contact email.</returns>
    private async Task<string> ResolveContactEmailAsync(int? contactId)
    {
        if (!contactId.HasValue || contactId.Value <= 0)
        {
            logger.LogError(
                "Akuiteo customer creation stopped because contact id is invalid. ContactId: {ContactId}",
                contactId);
            throw new BadRequestException(
                Errors.InvalidContactId,
                BuildInvalidContactIdMessage(contactId));
        }

        var contact = await contactRepository.GetContactByEmailOrIdAsync(contactId: contactId);
        if (string.IsNullOrWhiteSpace(contact?.Email))
        {
            logger.LogError(
                "Akuiteo customer creation stopped because contact email could not be resolved. ContactId: {ContactId}",
                contactId);
            throw new BadRequestException(
                Errors.InvalidContactId,
                BuildInvalidContactIdMessage(contactId));
        }

        return contact.Email;
    }

    /// <summary>
    /// Builds the invalid contact identifier error message.
    /// </summary>
    /// <param name="contactId">The invalid contact identifier.</param>
    /// <returns>The formatted error message.</returns>
    private static string BuildInvalidContactIdMessage(int? contactId)
    {
        return Errors.InvalidContactIdMessage.Replace("{contactId}", contactId?.ToString() ?? string.Empty, StringComparison.Ordinal);
    }
}

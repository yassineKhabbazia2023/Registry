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
using Newtonsoft.Json.Linq;

namespace Application.Services;

/// <summary>
/// Orchestrates Akuiteo customer creation and account updates for Registry.
/// </summary>
public class AkuiteoCustomerService : IAkuiteoCustomerService
{
    private readonly IAkuiteoCustomerProvider akuiteoCustomerProvider;
    private readonly IAccountService accountService;
    private readonly IContactRepository contactRepository;
    private readonly ILogger<AkuiteoCustomerService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoCustomerService"/> class.
    /// </summary>
    /// <param name="akuiteoCustomerProvider">The Akuiteo HTTP provider.</param>
    /// <param name="accountService">The Registry account service.</param>
    /// <param name="contactRepository">The contact repository.</param>
    /// <param name="logger">The logger.</param>
    public AkuiteoCustomerService(
        IAkuiteoCustomerProvider akuiteoCustomerProvider,
        IAccountService accountService,
        IContactRepository contactRepository,
        ILogger<AkuiteoCustomerService> logger)
    {
        this.akuiteoCustomerProvider = akuiteoCustomerProvider;
        this.accountService = accountService;
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

    /// <inheritdoc/>
    public async Task<AkuiteoPaymentInformationsDataResponse> GetPaymentInformationsAsync(int accountId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(accountId);

        logger.LogDebug(
            "Starting Akuiteo customer payment-information retrieval. AccountId: {AccountId}, Operation: {Operation}",
            accountId,
            "DoSearchPaymentCondition");

        var accountNumber = await ResolveAccountNumberAsync(accountId);

        try
        {
            var providerResult = await akuiteoCustomerProvider.GetPaymentInformationsAsync(accountNumber);
            if (!providerResult.IsSuccess || providerResult.Response?.Data is null)
            {
                logger.LogError(
                    "Akuiteo customer payment-information retrieval failed. AccountId: {AccountId}, StatusCode: {StatusCode}",
                    accountId,
                    providerResult.StatusCode);
                throw new AkuiteoAccountOperationTechnicalException(
                    string.IsNullOrWhiteSpace(providerResult.ErrorMessage)
                        ? "Akuiteo customer payment-information retrieval failed."
                        : providerResult.ErrorMessage);
            }

            logger.LogInformation(
                "Akuiteo customer payment information retrieved. AccountId: {AccountId}, StatusCode: {StatusCode}",
                accountId,
                providerResult.StatusCode);
            return providerResult.Response.Data;
        }
        catch (AkuiteoAuthenticationTechnicalException exception)
        {
            logger.LogError(
                exception,
                "Akuiteo customer payment-information retrieval failed because the Microsoft token could not be retrieved. AccountId: {AccountId}",
                accountId);
            throw new AkuiteoAccountOperationTechnicalException(exception.Message, exception);
        }
        catch (AkuiteoResponseDeserializationTechnicalException exception)
        {
            logger.LogError(
                exception,
                "Akuiteo customer payment-information retrieval returned malformed JSON. AccountId: {AccountId}",
                accountId);
            throw new AkuiteoAccountOperationTechnicalException(exception.Message, exception);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(
                exception,
                "Akuiteo customer payment-information retrieval failed because the external service was unreachable. AccountId: {AccountId}",
                accountId);
            throw new AkuiteoAccountOperationTechnicalException("Akuiteo is unavailable.", exception);
        }
        catch (TaskCanceledException exception)
        {
            logger.LogError(
                exception,
                "Akuiteo customer payment-information retrieval timed out. AccountId: {AccountId}",
                accountId);
            throw new AkuiteoAccountOperationTechnicalException("Akuiteo timed out.", exception);
        }
    }

    /// <inheritdoc/>
    public Task<AkuiteoAccountOperationResponse> UpdateBankingInformationsAsync(
        int accountId,
        IReadOnlyCollection<AkuiteoBankingInformationRequest> request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return ExecuteAccountOperationAsync(
            accountId,
            "DoUpdateBankingInformation",
            accountNumber => akuiteoCustomerProvider.UpdateBankingInformationsAsync(accountNumber, request));
    }

    /// <inheritdoc/>
    public Task<AkuiteoAccountOperationResponse> PatchAccountAsync(int accountId, JObject request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return ExecuteAccountOperationAsync(
            accountId,
            "Update customer by id",
            accountNumber => akuiteoCustomerProvider.PatchAccountAsync(accountNumber, request));
    }

    /// <summary>
    /// Executes an Akuiteo customer account operation and applies the shared Registry failure mapping.
    /// </summary>
    /// <param name="accountId">The Registry account identifier.</param>
    /// <param name="operationName">The downstream operation name.</param>
    /// <param name="operation">The provider operation receiving the resolved Akuiteo account number.</param>
    /// <returns>The successful Akuiteo metadata response.</returns>
    private async Task<AkuiteoAccountOperationResponse> ExecuteAccountOperationAsync(
        int accountId,
        string operationName,
        Func<string, Task<AkuiteoAccountOperationProviderResult>> operation)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(accountId);

        logger.LogDebug(
            "Starting Akuiteo customer account operation. AccountId: {AccountId}, Operation: {Operation}",
            accountId,
            operationName);

        var accountNumber = await ResolveAccountNumberAsync(accountId);

        try
        {
            var providerResult = await operation(accountNumber);
            if (!providerResult.IsSuccess || providerResult.Response is null)
            {
                logger.LogError(
                    "Akuiteo customer account operation failed. AccountId: {AccountId}, Operation: {Operation}, StatusCode: {StatusCode}",
                    accountId,
                    operationName,
                    providerResult.StatusCode);
                throw new AkuiteoAccountOperationTechnicalException(
                    string.IsNullOrWhiteSpace(providerResult.ErrorMessage)
                        ? "Akuiteo customer account operation failed."
                        : providerResult.ErrorMessage);
            }

            logger.LogInformation(
                "Akuiteo customer account operation succeeded. AccountId: {AccountId}, Operation: {Operation}, StatusCode: {StatusCode}",
                accountId,
                operationName,
                providerResult.StatusCode);

            return providerResult.Response;
        }
        catch (AkuiteoAuthenticationTechnicalException exception)
        {
            logger.LogError(
                exception,
                "Akuiteo customer account operation failed because the Microsoft token could not be retrieved. AccountId: {AccountId}, Operation: {Operation}",
                accountId,
                operationName);
            throw new AkuiteoAccountOperationTechnicalException(exception.Message, exception);
        }
        catch (AkuiteoResponseDeserializationTechnicalException exception)
        {
            logger.LogError(
                exception,
                "Akuiteo customer account operation returned malformed JSON. AccountId: {AccountId}, Operation: {Operation}",
                accountId,
                operationName);
            throw new AkuiteoAccountOperationTechnicalException(exception.Message, exception);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(
                exception,
                "Akuiteo customer account operation failed because the external service was unreachable. AccountId: {AccountId}, Operation: {Operation}",
                accountId,
                operationName);
            throw new AkuiteoAccountOperationTechnicalException("Akuiteo is unavailable.", exception);
        }
        catch (TaskCanceledException exception)
        {
            logger.LogError(
                exception,
                "Akuiteo customer account operation timed out. AccountId: {AccountId}, Operation: {Operation}",
                accountId,
                operationName);
            throw new AkuiteoAccountOperationTechnicalException("Akuiteo timed out.", exception);
        }
    }

    /// <summary>
    /// Resolves the Akuiteo account number associated with a Registry account identifier.
    /// </summary>
    /// <param name="accountId">The Registry account identifier.</param>
    /// <returns>The Akuiteo account number.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the Registry account cannot be resolved.</exception>
    private async Task<string> ResolveAccountNumberAsync(int accountId)
    {
        var accountNumber = await accountService.GetAccountNumberByIdAsync(accountId);
        if (!string.IsNullOrWhiteSpace(accountNumber))
        {
            return accountNumber;
        }

        logger.LogWarning(
            "Akuiteo customer account operation stopped because the Registry account was not found. AccountId: {AccountId}",
            accountId);
        throw new KeyNotFoundException($"Registry account {accountId} was not found.");
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

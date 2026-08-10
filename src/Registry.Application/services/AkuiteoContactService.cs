// <copyright file="AkuiteoContactService.cs" company="Pulse">
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
/// Orchestrates Akuiteo contact operations for Registry.
/// </summary>
public class AkuiteoContactService : IAkuiteoContactService
{
    private const string InternalErrorMessage = "Internal error";
    private readonly IAkuiteoContactProvider akuiteoContactProvider;
    private readonly ILogger<AkuiteoContactService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoContactService"/> class.
    /// </summary>
    /// <param name="akuiteoContactProvider">The Akuiteo HTTP provider.</param>
    /// <param name="logger">The logger.</param>
    public AkuiteoContactService(
        IAkuiteoContactProvider akuiteoContactProvider,
        ILogger<AkuiteoContactService> logger)
    {
        this.akuiteoContactProvider = akuiteoContactProvider;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AkuiteoContactCreationResponse> CreateContactAsync(AkuiteoContactCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        logger.LogDebug(
            "Starting Akuiteo contact creation. AccountNumber: {AccountNumber}, Email: {Email}",
            request.AccountNumber,
            request.Email);

        try
        {
            var providerResult = await akuiteoContactProvider.CreateContactAsync(request.MapToAkuiteoCreateContactRequest());

            if (!providerResult.IsSuccess || string.IsNullOrWhiteSpace(providerResult.ContactId))
            {
                if (IsInternalErrorWithoutDetails(providerResult.ErrorMessage))
                {
                    logger.LogWarning(
                        "Akuiteo returned an internal error without details. The account may not exist in Akuiteo. AccountNumber: {AccountNumber}",
                        request.AccountNumber);
                }

                logger.LogError(
                    "Akuiteo contact creation failed. AccountNumber: {AccountNumber}, StatusCode: {StatusCode}, Error: {Error}",
                    request.AccountNumber,
                    providerResult.StatusCode,
                    providerResult.ErrorMessage);
                throw new AkuiteoContactCreationTechnicalException(
                    string.IsNullOrWhiteSpace(providerResult.ErrorMessage)
                        ? "Akuiteo contact creation failed."
                        : providerResult.ErrorMessage);
            }

            logger.LogInformation(
                "Akuiteo contact created successfully. AccountNumber: {AccountNumber}, ContactId: {ContactId}",
                request.AccountNumber,
                providerResult.ContactId);

            return new AkuiteoContactCreationResponse
            {
                ContactId = providerResult.ContactId
            };
        }
        catch (AkuiteoAuthenticationTechnicalException exception)
        {
            logger.LogError(
                exception,
                "Akuiteo contact creation failed because the Microsoft token could not be retrieved. AccountNumber: {AccountNumber}",
                request.AccountNumber);
            throw new AkuiteoContactCreationTechnicalException(exception.Message, exception);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(
                exception,
                "Akuiteo contact creation failed because the external service was unreachable. AccountNumber: {AccountNumber}",
                request.AccountNumber);
            throw new AkuiteoContactCreationTechnicalException("Akuiteo is unavailable.", exception);
        }
        catch (TaskCanceledException exception)
        {
            logger.LogError(
                exception,
                "Akuiteo contact creation timed out. AccountNumber: {AccountNumber}",
                request.AccountNumber);
            throw new AkuiteoContactCreationTechnicalException("Akuiteo timed out.", exception);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<AkuiteoContactSearchDataResponse>> SearchContactsAsync(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        try
        {
            var providerResult = await akuiteoContactProvider.SearchContactsAsync(email.Trim());
            if (!providerResult.IsSuccess)
            {
                logger.LogError(
                    "Akuiteo contact search failed. StatusCode: {StatusCode}, Error: {Error}",
                    providerResult.StatusCode,
                    providerResult.ErrorMessage);
                throw new AkuiteoContactSearchTechnicalException(
                    string.IsNullOrWhiteSpace(providerResult.ErrorMessage)
                        ? "Akuiteo contact search failed."
                        : providerResult.ErrorMessage);
            }

            return providerResult.Contacts;
        }
        catch (AkuiteoAuthenticationTechnicalException exception)
        {
            logger.LogError(exception, "Akuiteo contact search failed because the Microsoft token could not be retrieved");
            throw new AkuiteoContactSearchTechnicalException(exception.Message, exception);
        }
        catch (AkuiteoResponseDeserializationTechnicalException exception)
        {
            logger.LogError(exception, "Akuiteo contact search returned malformed JSON");
            throw new AkuiteoContactSearchTechnicalException(exception.Message, exception);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Akuiteo contact search failed because the external service was unreachable");
            throw new AkuiteoContactSearchTechnicalException("Akuiteo is unavailable.", exception);
        }
        catch (TaskCanceledException exception)
        {
            logger.LogError(exception, "Akuiteo contact search timed out");
            throw new AkuiteoContactSearchTechnicalException("Akuiteo timed out.", exception);
        }
    }

    /// <summary>
    /// Determines whether Akuiteo returned only its generic internal-error message.
    /// </summary>
    /// <param name="errorMessage">The downstream error message.</param>
    /// <returns><see langword="true"/> when the message contains no detail beyond <c>Internal error</c>; otherwise <see langword="false"/>.</returns>
    private static bool IsInternalErrorWithoutDetails(string? errorMessage)
    {
        var normalizedMessage = errorMessage?.Trim().Trim('"');
        return InternalErrorMessage.Equals(normalizedMessage, StringComparison.OrdinalIgnoreCase);
    }
}

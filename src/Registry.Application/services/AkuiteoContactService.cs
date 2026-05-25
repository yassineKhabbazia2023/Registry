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
/// Orchestrates Akuiteo contact creation for Registry.
/// </summary>
public class AkuiteoContactService : IAkuiteoContactService
{
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
}

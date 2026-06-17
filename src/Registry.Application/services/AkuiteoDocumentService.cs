// <copyright file="AkuiteoDocumentService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Interfaces;
using Application.Models;
using Application.Models.Results;
using Kpmg.ExceptionMiddleware.AdvancedException;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <summary>
/// Orchestrates Akuiteo document uploads for Registry.
/// </summary>
public class AkuiteoDocumentService : IAkuiteoDocumentService
{
    private const long MaxDocumentSizeInBytes = 5 * 1024 * 1024;
    private const string PdfContentType = "application/pdf";

    private readonly IAkuiteoDocumentProvider akuiteoDocumentProvider;
    private readonly ILogger<AkuiteoDocumentService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoDocumentService"/> class.
    /// </summary>
    /// <param name="akuiteoDocumentProvider">The Akuiteo document HTTP provider.</param>
    /// <param name="logger">The logger.</param>
    public AkuiteoDocumentService(
        IAkuiteoDocumentProvider akuiteoDocumentProvider,
        ILogger<AkuiteoDocumentService> logger)
    {
        this.akuiteoDocumentProvider = akuiteoDocumentProvider;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AkuiteoDocumentUploadResponse> UploadDocumentAsync(AkuiteoDocumentUploadRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        logger.LogDebug(
            "Starting Akuiteo document upload. AccountNumber: {AccountNumber}, DocumentName: {DocumentName}",
            request.AccountNumber,
            request.DocumentName);

        try
        {
            var providerResult = await akuiteoDocumentProvider.UploadDocumentAsync(request);

            if (!providerResult.IsSuccess)
            {
                logger.LogError(
                    "Akuiteo document upload failed. AccountNumber: {AccountNumber}, DocumentName: {DocumentName}, StatusCode: {StatusCode}, Error: {Error}, AkuiteoResponseBody: {AkuiteoResponseBody}",
                    request.AccountNumber,
                    request.DocumentName,
                    providerResult.StatusCode,
                    providerResult.ErrorMessage,
                    providerResult.RawResponseBody);
                throw new AkuiteoDocumentUploadTechnicalException(
                    string.IsNullOrWhiteSpace(providerResult.ErrorMessage)
                        ? "Akuiteo document upload failed."
                        : providerResult.ErrorMessage);
            }

            logger.LogInformation(
                "Akuiteo document uploaded successfully. AccountNumber: {AccountNumber}, DocumentName: {DocumentName}",
                request.AccountNumber,
                request.DocumentName);

            return new AkuiteoDocumentUploadResponse
            {
                AccountNumber = request.AccountNumber,
                DocumentName = request.DocumentName,
                IsUploaded = true
            };
        }
        catch (AkuiteoAuthenticationTechnicalException exception)
        {
            logger.LogError(
                exception,
                "Akuiteo document upload failed because the Microsoft token could not be retrieved. AccountNumber: {AccountNumber}, DocumentName: {DocumentName}",
                request.AccountNumber,
                request.DocumentName);
            throw new AkuiteoDocumentUploadTechnicalException(exception.Message, exception);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(
                exception,
                "Akuiteo document upload failed because the external service was unreachable. AccountNumber: {AccountNumber}, DocumentName: {DocumentName}",
                request.AccountNumber,
                request.DocumentName);
            throw new AkuiteoDocumentUploadTechnicalException("Akuiteo is unavailable.", exception);
        }
        catch (TaskCanceledException exception)
        {
            logger.LogError(
                exception,
                "Akuiteo document upload timed out. AccountNumber: {AccountNumber}, DocumentName: {DocumentName}",
                request.AccountNumber,
                request.DocumentName);
            throw new AkuiteoDocumentUploadTechnicalException("Akuiteo timed out.", exception);
        }
    }

    /// <summary>
    /// Validates the document upload request before sending it to Akuiteo.
    /// </summary>
    /// <param name="request">The document upload request.</param>
    private static void ValidateRequest(AkuiteoDocumentUploadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AccountNumber))
        {
            throw new BadRequestException(Errors.InvalidAkuiteoDocumentUploadCode, "The accountNumber route parameter is required.");
        }

        if (request.Content is null || request.Content == Stream.Null || !request.Content.CanRead)
        {
            throw new BadRequestException(Errors.InvalidAkuiteoDocumentUploadCode, "The document file is required.");
        }

        if (string.IsNullOrWhiteSpace(request.DocumentName))
        {
            throw new BadRequestException(Errors.InvalidAkuiteoDocumentUploadCode, "The document file name is required.");
        }

        if (request.Length <= 0)
        {
            throw new BadRequestException(Errors.InvalidAkuiteoDocumentUploadCode, "The document file is required.");
        }

        if (request.Length > MaxDocumentSizeInBytes)
        {
            throw new BadRequestException(Errors.InvalidAkuiteoDocumentUploadCode, "The document file size must not exceed 5 Mo.");
        }

        if (!IsSupportedContentType(request.ContentType))
        {
            throw new BadRequestException(Errors.InvalidAkuiteoDocumentUploadCode, "The document content type must be application/pdf or image/*.");
        }
    }

    /// <summary>
    /// Indicates whether the uploaded document content type is supported by Akuiteo.
    /// </summary>
    /// <param name="contentType">The uploaded document content type.</param>
    /// <returns><see langword="true"/> when the content type is supported; otherwise <see langword="false"/>.</returns>
    private static bool IsSupportedContentType(string? contentType)
    {
        return PdfContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase)
            || (contentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true);
    }
}

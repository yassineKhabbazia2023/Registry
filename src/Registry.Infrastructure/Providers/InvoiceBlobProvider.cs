// <copyright file="InvoiceBlobProvider.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Interfaces;
using Application.Models.Results;
using Azure;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Application.Providers;

/// <summary>
/// Builds the blob client of an invoice pdf from its absolute url.
/// </summary>
/// <param name="blobUri">Url of the pdf, as stored in <c>ref.Invoice.DocumentPath</c>.</param>
public delegate BlobClient InvoiceBlobClientFactory(Uri blobUri);

/// <summary>
/// Downloads invoice pdf documents from the storage account carried by their document path.
/// </summary>
public class InvoiceBlobProvider : IInvoiceBlobProvider
{
    private const string AllowedHostSuffix = ".blob.core.windows.net";
    private const string DefaultFileName = "invoice.pdf";

    private readonly InvoiceBlobClientFactory blobClientFactory;
    private readonly ILogger<InvoiceBlobProvider> logger;

    public InvoiceBlobProvider(
        InvoiceBlobClientFactory blobClientFactory,
        ILogger<InvoiceBlobProvider> logger)
    {
        this.blobClientFactory = blobClientFactory;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public async Task<InvoiceContentResponse?> GetPdfAsync(string documentPath)
    {
        var blobUri = ParseDocumentPath(documentPath);
        var blobClient = this.CreateBlobClient(blobUri);

        try
        {
            var download = await blobClient.DownloadStreamingAsync();

            return new InvoiceContentResponse(download.Value, download.Value.Content, BuildFileName(blobClient.Name));
        }
        catch (RequestFailedException exception) when (exception.Status == (int)HttpStatusCode.NotFound)
        {
            this.logger.LogWarning(
                "Invoice pdf missing from {AccountHost}: {Container}/{BlobName}",
                blobClient.Uri.Host,
                blobClient.BlobContainerName,
                blobClient.Name);
            return null;
        }
        catch (RequestFailedException exception)
        {
            this.logger.LogError(
                exception,
                "Invoice pdf download failed on {AccountHost}: {Container}/{BlobName}, status {Status}, code {ErrorCode}",
                blobClient.Uri.Host,
                blobClient.BlobContainerName,
                blobClient.Name,
                exception.Status,
                exception.ErrorCode);

            throw new InvoiceDownloadTechnicalException(
                $"Invoice pdf download failed for blob {blobClient.BlobContainerName}/{blobClient.Name}: status {exception.Status}.",
                exception);
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Invoice pdf download failed on {AccountHost}: {Container}/{BlobName}",
                blobClient.Uri.Host,
                blobClient.BlobContainerName,
                blobClient.Name);

            throw new InvoiceDownloadTechnicalException(
                $"Invoice pdf download failed for blob {blobClient.BlobContainerName}/{blobClient.Name}.",
                exception);
        }
    }

    /// <summary>
    /// Builds the blob client of a validated document path url.
    /// </summary>
    private BlobClient CreateBlobClient(Uri blobUri)
    {
        try
        {
            return this.blobClientFactory(blobUri);
        }
        catch (Exception exception)
        {
            this.logger.LogError(exception, "Invoice pdf blob client creation failed on {AccountHost}", blobUri.Host);

            throw new InvoiceDownloadTechnicalException(
                $"Invoice pdf download failed on host {blobUri.Host}.",
                exception);
        }
    }

    /// <summary>
    /// Validates a ref.Invoice document path and turns it into a blob url.
    /// </summary>
    private static Uri ParseDocumentPath(string documentPath)
    {
        if (!Uri.TryCreate(documentPath, UriKind.Absolute, out var blobUri)
            || blobUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvoiceDownloadTechnicalException($"Invalid invoice document path: '{documentPath}'.");
        }

        // The path comes from the invoices csv: the managed identity token must never be presented
        // to a host that is not an Azure blob endpoint.
        if (!blobUri.Host.EndsWith(AllowedHostSuffix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvoiceDownloadTechnicalException(
                $"Invoice document path host '{blobUri.Host}' is not an Azure blob endpoint.");
        }

        var segments = blobUri.AbsolutePath.Trim('/').Split('/', 2);

        if (segments.Length < 2 || string.IsNullOrWhiteSpace(segments[1]))
        {
            throw new InvoiceDownloadTechnicalException($"Invalid invoice document path: '{documentPath}'.");
        }

        return blobUri;
    }

    /// <summary>
    /// Builds the file name exposed to the client from the blob name.
    /// </summary>
    private static string BuildFileName(string blobName)
    {
        var segment = blobName[(blobName.LastIndexOf('/') + 1)..];

        var sanitized = new string(segment.Where(character => !char.IsControl(character)).ToArray()).Trim();

        return string.IsNullOrWhiteSpace(sanitized) ? DefaultFileName : sanitized;
    }
}

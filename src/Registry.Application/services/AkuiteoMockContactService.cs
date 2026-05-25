// <copyright file="AkuiteoMockContactService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models.Results;
using Application.Options;
using Application.Requests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services;

/// <summary>
/// Simulates Akuiteo contact creation without any outbound HTTP call.
/// </summary>
public class AkuiteoMockContactService : IAkuiteoContactService
{
    private readonly AkuiteoOptions akuiteoOptions;
    private readonly ILogger<AkuiteoMockContactService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoMockContactService"/> class.
    /// </summary>
    /// <param name="akuiteoOptions">The Akuiteo configuration.</param>
    /// <param name="logger">The logger.</param>
    public AkuiteoMockContactService(
        IOptions<AkuiteoOptions> akuiteoOptions,
        ILogger<AkuiteoMockContactService> logger)
    {
        this.akuiteoOptions = akuiteoOptions?.Value ?? throw new ArgumentNullException(nameof(akuiteoOptions));
        this.logger = logger;
    }

    /// <inheritdoc/>
    public Task<AkuiteoContactCreationResponse> CreateContactAsync(AkuiteoContactCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var mockedContactId = ResolveMockContactId();
        logger.LogInformation(
            "Akuiteo contact creation completed in mock mode. AccountNumber: {AccountNumber}, ContactId: {ContactId}",
            request.AccountNumber,
            mockedContactId);

        return Task.FromResult(new AkuiteoContactCreationResponse
        {
            ContactId = mockedContactId
        });
    }

    /// <summary>
    /// Resolves the mocked contact identifier returned by the mocked mode.
    /// </summary>
    /// <returns>The mocked contact identifier.</returns>
    private string ResolveMockContactId()
    {
        return string.IsNullOrWhiteSpace(akuiteoOptions.MockContactId)
            ? "500145940"
            : akuiteoOptions.MockContactId;
    }
}

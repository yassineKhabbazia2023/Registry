// <copyright file="AkuiteoMockCustomerService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.Globalization;
using Application.Interfaces;
using Application.Models.Results;
using Application.Options;
using Application.Requests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services;

/// <summary>
/// Simulates Akuiteo customer creation without any outbound HTTP call.
/// </summary>
public class AkuiteoMockCustomerService : IAkuiteoCustomerService
{
    private readonly ILogger<AkuiteoMockCustomerService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoMockCustomerService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public AkuiteoMockCustomerService(
        IOptions<AkuiteoOptions> akuiteoOptions,
        ILogger<AkuiteoMockCustomerService> logger)
    {
        ArgumentNullException.ThrowIfNull(akuiteoOptions);
        this.logger = logger;
    }

    /// <inheritdoc/>
    public Task<AkuiteoCustomerCreationResponse> CreateCustomerAsync(AkuiteoCustomerCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var mockedAccountNumber = GenerateMockAccountNumber();
        logger.LogInformation(
            "Akuiteo customer creation completed in mock mode. Siret: {Siret}, AccountNumber: {AccountNumber}",
            request.Siret,
            mockedAccountNumber);

        return Task.FromResult(new AkuiteoCustomerCreationResponse
        {
            AccountNumber = mockedAccountNumber
        });
    }

    /// <summary>
    /// Generates a random mocked account number.
    /// </summary>
    /// <returns>The generated mocked account number.</returns>
    private static string GenerateMockAccountNumber()
    {
        return Random.Shared.NextInt64(9_000_000_000, 10_000_000_000).ToString(CultureInfo.InvariantCulture);
    }
}

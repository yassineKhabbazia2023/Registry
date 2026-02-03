// <copyright file="HubSpotService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.Globalization;
using Application.Interfaces;
using Application.Options;
using Application.Requests;
using Application.Models.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services;

public class HubSpotService : IHubSpotService
{
    private readonly IHubSpotProvider hubSpotProvider;
    private readonly IInvoiceDematerializationNotifier dematerializationNotifier;
    private readonly HubSpotOptions hubSpotOptions;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<HubSpotService> logger;

    public HubSpotService(
        IHubSpotProvider hubSpotProvider,
        IInvoiceDematerializationNotifier dematerializationNotifier,
        IOptions<HubSpotOptions> hubSpotOptions,
        TimeProvider timeProvider,
        ILogger<HubSpotService> logger)
    {
        this.hubSpotProvider = hubSpotProvider;
        this.dematerializationNotifier = dematerializationNotifier;
        this.hubSpotOptions = hubSpotOptions?.Value ?? throw new ArgumentNullException(nameof(hubSpotOptions));
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<HubSpotSubmissionResult> SubmitIntegrationAsync(string? accountNumber, HubSpotSubmissionInputRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        logger.LogInformation(
            "Starting HubSpot integration submission. AccountNumber: {AccountNumber}, RequesterEmail: {RequesterEmail}",
            accountNumber,
            request.RequesterEmail);

        var payload = new HubSpotSubmissionRequest
        {
            Fields = BuildFields(accountNumber, request)
        };

        logger.LogDebug(
            "Submitting to HubSpot. PortalId: {PortalId}, FormGuid: {FormGuid}, FieldsCount: {FieldsCount}",
            hubSpotOptions.PortalId,
            hubSpotOptions.FormGuid,
            payload.Fields.Count);

        var result = await hubSpotProvider.SubmitIntegrationAsync(hubSpotOptions.PortalId, hubSpotOptions.FormGuid, payload);

        if (result.IsSuccess)
        {
            logger.LogInformation(
                "HubSpot submission successful. AccountNumber: {AccountNumber}, sending history event",
                accountNumber);
            await dematerializationNotifier.NotifyDematerializationCreatedAsync(accountNumber, request);
        }
        else
        {
            logger.LogWarning(
                "HubSpot submission failed. AccountNumber: {AccountNumber}, ErrorMessage: {ErrorMessage}",
                accountNumber,
                result.ErrorMessage);
        }

        return result;
    }

    private List<HubSpotFieldRequest> BuildFields(string? accountNumber, HubSpotSubmissionInputRequest request)
    {
        var fields = new List<HubSpotFieldRequest>();

        AddField(fields, "code_client", accountNumber);
        AddField(fields, "e_mail_de_reception", request.DematerializationEmail);
        AddField(fields, "firstname", request.FirstName);
        AddField(fields, "lastname", request.LastName);
        AddField(fields, "e_mail_de_connexion", request.VaultEmail);
        AddField(fields, "email", request.RequesterEmail);
        var billingRequestDate = timeProvider.GetUtcNow().UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        AddField(fields, "formulaire_facturation_date_demande", billingRequestDate);

        return fields;
    }

    private static void AddField(List<HubSpotFieldRequest> fields, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        fields.Add(new HubSpotFieldRequest { Name = name, Value = value });
    }
}

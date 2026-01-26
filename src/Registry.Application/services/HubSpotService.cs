// <copyright file="HubSpotService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.Globalization;
using Application.Interfaces;
using Application.Options;
using Application.Requests;
using Application.Models.Results;
using Microsoft.Extensions.Options;

namespace Application.Services;

public class HubSpotService : IHubSpotService
{
    private readonly IHubSpotProvider hubSpotProvider;
    private readonly HubSpotOptions hubSpotOptions;

    public HubSpotService(IHubSpotProvider hubSpotProvider, IOptions<HubSpotOptions> hubSpotOptions)
    {
        this.hubSpotProvider = hubSpotProvider;
        this.hubSpotOptions = hubSpotOptions?.Value ?? throw new ArgumentNullException(nameof(hubSpotOptions));
    }

    public Task<HubSpotSubmissionResult> SubmitIntegrationAsync(string? accountNumber, HubSpotSubmissionInputRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var payload = new HubSpotSubmissionRequest
        {
            Fields = BuildFields(accountNumber, request)
        };

        return hubSpotProvider.SubmitIntegrationAsync(hubSpotOptions.PortalId, hubSpotOptions.FormGuid, payload);
    }

    private static List<HubSpotFieldRequest> BuildFields(string? accountNumber, HubSpotSubmissionInputRequest request)
    {
        var fields = new List<HubSpotFieldRequest>();

        AddField(fields, "code_client", accountNumber);
        AddField(fields, "email_dematerialisation", request.DematerializationEmail);
        AddField(fields, "firstname", request.FirstName);
        AddField(fields, "lastname", request.LastName);
        AddField(fields, "coffre_fort_email", request.VaultEmail);
        AddField(fields, "email_demandeur", request.RequesterEmail);
        var billingRequestDate = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
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

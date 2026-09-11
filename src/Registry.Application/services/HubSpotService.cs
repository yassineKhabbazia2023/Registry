// <copyright file="HubSpotService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models.Results;
using Application.Options;
using Application.Requests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pulse.Registry.Domain.Entities;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Services;

public class HubSpotService : IHubSpotService
{
    private readonly IHubSpotProvider hubSpotProvider;
    private readonly IInvoiceDematerializationNotifier dematerializationNotifier;
    private readonly IHubSpotFormRepository hubSpotFormRepository;
    private readonly IAccountService accountService;
    private readonly HubSpotOptions hubSpotOptions;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<HubSpotService> logger;
    private static readonly JsonSerializerOptions FormDataSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public HubSpotService(
        IHubSpotProvider hubSpotProvider,
        IInvoiceDematerializationNotifier dematerializationNotifier,
        IHubSpotFormRepository hubSpotFormRepository,
        IAccountService accountService,
        IOptions<HubSpotOptions> hubSpotOptions,
        TimeProvider timeProvider,
        ILogger<HubSpotService> logger)
    {
        this.hubSpotProvider = hubSpotProvider;
        this.dematerializationNotifier = dematerializationNotifier;
        this.hubSpotFormRepository = hubSpotFormRepository;
        this.accountService = accountService;
        this.hubSpotOptions = hubSpotOptions?.Value ?? throw new ArgumentNullException(nameof(hubSpotOptions));
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<HubSpotFormSubmissionResult> SubmitIntegrationAsync(int accountId, HubSpotSubmissionInputRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var accountNumber = await accountService.GetAccountNumberByIdAsync(accountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountNumber);

        if (await hubSpotFormRepository.HasSuccessfulSubmissionAsync(accountNumber))
        {
            logger.LogInformation(
                "HubSpot form submission rejected because a successful submission already exists. AccountNumber: {AccountNumber}",
                accountNumber);
            return HubSpotFormSubmissionResult.Rejected();
        }

        logger.LogInformation(
            "Starting HubSpot integration submission. AccountNumber: {AccountNumber}, RequesterEmail: {RequesterEmail}",
            accountNumber,
            request.RequesterEmail);

        var submittedAt = timeProvider.GetUtcNow().UtcDateTime;
        request.SubmittedAt = submittedAt;

        var payload = new HubSpotSubmissionRequest
        {
            Fields = BuildFields(accountNumber, request, submittedAt)
        };

        logger.LogDebug(
            "Submitting to HubSpot. PortalId: {PortalId}, FormGuid: {FormGuid}, FieldsCount: {FieldsCount}",
            hubSpotOptions.PortalId,
            hubSpotOptions.FormGuid,
            payload.Fields.Count);

        var result = await hubSpotProvider.SubmitIntegrationAsync(hubSpotOptions.PortalId, hubSpotOptions.FormGuid, payload);
        var dispatchState = result.IsSuccess;

        var formData = JsonSerializer.Serialize(request, FormDataSerializerOptions);
        await hubSpotFormRepository.AddSubmissionAsync(new HubSpotFormEntity
        {
            AccountNumber = accountNumber,
            SubmittedBy = request.RequesterEmail ?? string.Empty,
            SubmittedAt = submittedAt,
            HubSpotDispatchState = dispatchState,
            FormData = formData
        });

        if (dispatchState)
        {
            logger.LogInformation(
                "HubSpot submission successful. AccountNumber: {AccountNumber}, sending history event",
                accountNumber);
            try
            {
                await dematerializationNotifier.NotifyDematerializationCreatedAsync(accountNumber, request);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "History event publishing failed after HubSpot submission. AccountNumber: {AccountNumber}",
                    accountNumber);
            }
        }
        else
        {
            logger.LogWarning(
                "HubSpot submission failed. AccountNumber: {AccountNumber}, ErrorMessage: {ErrorMessage}",
                accountNumber,
                result.ErrorMessage);
        }

        return HubSpotFormSubmissionResult.Created(dispatchState);
    }

    public async Task<HubSpotSubmissionStateResult> GetSubmissionStateAsync(int accountId)
    {
        var accountNumber = await accountService.GetAccountNumberByIdAsync(accountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountNumber);

        var hasSuccessfulSubmission = await hubSpotFormRepository.HasSuccessfulSubmissionAsync(accountNumber);
        return hasSuccessfulSubmission
            ? HubSpotSubmissionStateResult.Found()
            : HubSpotSubmissionStateResult.NotFound();
    }

    public async Task<HubSpotSubmissionResetResult> ResetSubmissionsAsync(int accountId)
    {
        var accountNumber = await accountService.GetAccountNumberByIdAsync(accountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountNumber);

        await hubSpotFormRepository.DeleteSubmissionsAsync(accountNumber);

        logger.LogInformation(
            "HubSpot form submissions reset for QA. AccountNumber: {AccountNumber}",
            accountNumber);

        return HubSpotSubmissionResetResult.Success();
    }

    private static List<HubSpotFieldRequest> BuildFields(string? accountNumber, HubSpotSubmissionInputRequest request, DateTime submittedAt)
    {
        var fields = new List<HubSpotFieldRequest>();

        AddField(fields, "code_client", accountNumber);
        AddField(fields, "e_mail_de_reception", request.DematerializationEmail);
        AddField(fields, "firstname", request.FirstName);
        AddField(fields, "lastname", request.LastName);
        AddField(fields, "e_mail_de_connexion", request.VaultEmail);
        AddField(fields, "email", request.RequesterEmail);
        var billingRequestDate = submittedAt.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
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

// <copyright file="MissionReceptionService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Helpers;
using Application.Interfaces;
using Application.Models;
using Application.Models.Results;
using CsvHelper;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Application.Services;

public class MissionReceptionService : IMissionReceptionService
{
    private const string BlobEndpointName = "Mission";

    private readonly IBlobStorageManager _blobStorageManager;
    private readonly IMissionService _missionService;
    private readonly IMissionEventPublisher _missionEventPublisher;
    private readonly IValidationHelper<MissionCsv> _validationHelper;
    private readonly ILogger<MissionReceptionService> _logger;

    public MissionReceptionService(
        IBlobStorageManager blobStorageManager,
        IMissionService missionService,
        IMissionEventPublisher missionEventPublisher,
        IValidationHelper<MissionCsv> validationHelper,
        ILogger<MissionReceptionService> logger)
    {
        _blobStorageManager = blobStorageManager;
        _missionService = missionService;
        _missionEventPublisher = missionEventPublisher;
        _validationHelper = validationHelper;
        _logger = logger;
    }

    public async Task<MissionCsvReceptionOutcome> ReceiveAsync(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return MissionCsvReceptionOutcome.Rejected("Invalid data: The input data cannot be null or empty.");
        }

        string blobName;
        try
        {
            blobName = await _blobStorageManager.SaveFileAsync(BlobEndpointName, data);
        }
        catch (BlobStorageOperationException ex)
        {
            _logger.LogError(ex, "Mission csv storage failed");
            return MissionCsvReceptionOutcome.Rejected($"Something went wrong when saving received csv {ex.InnerException}");
        }

        List<(MissionCsv, int, string[])> csvLines;
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
        {
            try
            {
                csvLines = CsvFileReader.ReadStreamAsync<MissionCsv>(stream).ToList();
            }
            catch (HeaderValidationException ex)
            {
                _logger.LogWarning(ex, "Mission csv rejected: missing columns in header");
                return MissionCsvReceptionOutcome.Rejected("Invalid data: Missing columns in header");
            }

            if (!CsvConfig.IsValidCsvFormat(csvLines, typeof(MissionCsv), out var messageError))
            {
                _logger.LogWarning("Mission csv rejected: invalid format ({Reason})", messageError);
                return MissionCsvReceptionOutcome.Rejected("Invalid data: " + messageError);
            }
        }

        var result = _validationHelper.Validate(csvLines.Select(l => l.Item1));

        var lineNumbers = csvLines
            .Select((l, i) => (Model: l.Item1, Line: i + 1))
            .ToDictionary(x => x.Model, x => x.Line);

        RejectDuplicateLines(result, lineNumbers);
        await RejectAlreadySavedLinesAsync(result, lineNumbers);

        if (result.ValidateModels.Count != 0)
        {
            await _missionService.SaveMissionsAsync(result.ValidateModels);

            try
            {
                await _missionEventPublisher.SendMissionLinesBatchEvent(blobName);
            }
            catch (ServiceBusOperationException ex)
            {
                // Self-healing trigger: the scheduled pass picks up the lines still READY.
                _logger.LogError(ex, "Mission lines queue trigger failed for blob {BlobName}", blobName);
            }
        }

        return MissionCsvReceptionOutcome.Accepted(new MissionCsvReceptionResult
        {
            AcceptedLines = result.ValidateModels.Count,
            RejectedLines = result.Errors.Count,
            Errors = result.Errors,
        });
    }

    // UQ_Missions_Operation_EngagementCode rejects duplicates at insert with a raw SQL exception:
    // surface them as validation errors instead. First occurrence wins, the others are rejected.
    private static void RejectDuplicateLines(LightValidationResult<MissionCsv> result, Dictionary<MissionCsv, int> lineNumbers)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var kept = new List<MissionCsv>(result.ValidateModels.Count);

        foreach (var model in result.ValidateModels)
        {
            if (seen.Add(EngagementKey(model)))
            {
                kept.Add(model);
            }
            else
            {
                result.Errors.Add(new LightValidationError
                {
                    LineNumber = lineNumbers[model],
                    Errors = [$"Duplicate EngagementCode '{model.EngagementCode}' for operation '{model.Operation}'"],
                });
            }
        }

        result.ValidateModels = kept;
    }

    // Same constraint, other side of it: a line whose (Operation, EngagementCode) is already
    // persisted (typically the same file sent twice) must be rejected, not crash the deposit.
    private async Task RejectAlreadySavedLinesAsync(LightValidationResult<MissionCsv> result, Dictionary<MissionCsv, int> lineNumbers)
    {
        if (result.ValidateModels.Count == 0)
        {
            return;
        }

        var existingKeys = await _missionService.GetExistingEngagementKeysAsync(result.ValidateModels);
        if (existingKeys is null || existingKeys.Count == 0)
        {
            return;
        }

        var existing = new HashSet<string>(
            existingKeys.Select(k => $"{k.Operation}|{k.EngagementCode}"),
            StringComparer.OrdinalIgnoreCase);
        var kept = new List<MissionCsv>(result.ValidateModels.Count);

        foreach (var model in result.ValidateModels)
        {
            if (existing.Contains(EngagementKey(model)))
            {
                result.Errors.Add(new LightValidationError
                {
                    LineNumber = lineNumbers[model],
                    Errors = [$"EngagementCode '{model.EngagementCode}' with operation '{model.Operation}' already exists"],
                });
            }
            else
            {
                kept.Add(model);
            }
        }

        result.ValidateModels = kept;
    }

    private static string EngagementKey(MissionCsv model) => $"{model.Operation}|{model.EngagementCode}";
}

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
}

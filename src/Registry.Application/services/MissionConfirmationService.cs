// <copyright file="MissionConfirmationService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <inheritdoc cref="IMissionConfirmationService"/>
public class MissionConfirmationService : IMissionConfirmationService
{
    private readonly IMissionRepository _missionRepository;
    private readonly IMissionProcessingRepository _missionProcessingRepository;
    private readonly ILogger<MissionConfirmationService> _logger;

    public MissionConfirmationService(IMissionRepository missionRepository, IMissionProcessingRepository missionProcessingRepository, ILogger<MissionConfirmationService> logger)
    {
        _missionRepository = missionRepository ?? throw new ArgumentNullException(nameof(missionRepository));
        _missionProcessingRepository = missionProcessingRepository ?? throw new ArgumentNullException(nameof(missionProcessingRepository));
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task ConfirmAsync(string engagementCode, string operation, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(engagementCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        var mission = await _missionRepository.GetByEngagementCodeAndOperationAsync(engagementCode, operation, cancellationToken);
        if (mission is null)
        {
            // Nothing to confirm. Returning instead of throwing, otherwise the message would be
            // rejected and redelivered forever over a line that will never exist.
            _logger.LogWarning("No {Operation} mission line found for engagement code {EngagementCode}; acknowledgement ignored", operation, engagementCode);
            return;
        }

        var processing = await _missionProcessingRepository.GetByRegistryMissionIdAsync(mission.RegistryMissionId, cancellationToken);
        if (processing is null)
        {
            _logger.LogWarning("Mission line {RegistryMissionId} has no processing row; acknowledgement ignored", mission.RegistryMissionId);
            return;
        }

        // Idempotent: a confirmation replayed after a lost message lands on a line already
        // SUCCEEDED and must change nothing.
        if (processing.Status == ProcessStatus.Succeeded)
        {
            _logger.LogInformation("Mission line {RegistryMissionId} already acknowledged", mission.RegistryMissionId);
            return;
        }

        processing.Status = ProcessStatus.Succeeded;
        processing.ProcessedOn = DateTime.UtcNow;
        processing.Reason = null;

        await _missionProcessingRepository.UpdateAsync([processing], cancellationToken);

        _logger.LogInformation("Mission line {RegistryMissionId} acknowledged for {Operation} on engagement code {EngagementCode}", mission.RegistryMissionId, operation, engagementCode);
    }
}

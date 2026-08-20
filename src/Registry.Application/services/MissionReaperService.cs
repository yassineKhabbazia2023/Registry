// <copyright file="MissionReaperService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services;

/// <inheritdoc cref="IMissionReaperService"/>
public class MissionReaperService : IMissionReaperService
{
    private readonly IMissionProcessingRepository _missionProcessingRepository;
    private readonly MissionOptions _options;
    private readonly ILogger<MissionReaperService> _logger;

    public MissionReaperService(IMissionProcessingRepository missionProcessingRepository, IOptions<MissionOptions> options, ILogger<MissionReaperService> logger)
    {
        _missionProcessingRepository = missionProcessingRepository ?? throw new ArgumentNullException(nameof(missionProcessingRepository));
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task ReapUnacknowledgedMissionsAsync(CancellationToken cancellationToken = default)
    {
        var failedCount = await _missionProcessingRepository.MarkUnacknowledgedAsFailedAsync(_options.AckTimeoutMinutes, cancellationToken);

        if (failedCount > 0)
        {
            _logger.LogWarning("Mission reaper flipped {FailedCount} unacknowledged SENT lines to FAILED (timeout {AckTimeoutMinutes} min)", failedCount, _options.AckTimeoutMinutes);
        }
    }
}

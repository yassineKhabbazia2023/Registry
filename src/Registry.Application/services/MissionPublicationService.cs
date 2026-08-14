// <copyright file="MissionPublicationService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Interfaces;
using Application.Mappers;
using Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pulse.Registry.Domain.Entities;

namespace Application.Services;

/// <summary>
/// Publishes towards Offer the engagement lines waiting in mission.Processing.
/// Reads the READY lines, checks the accounts exist, publishes the matching contract and
/// moves the published lines to SENT. Transposition of InvoicePublicationService.
/// </summary>
public class MissionPublicationService : IMissionPublicationService
{
    private readonly IMissionProcessingRepository _missionProcessingRepository;
    private readonly IMissionRepository _missionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IMissionEventPublisher _missionEventPublisher;
    private readonly BackGroundJobOptions _options;
    private readonly ILogger<MissionPublicationService> _logger;

    public MissionPublicationService(IMissionProcessingRepository missionProcessingRepository, IMissionRepository missionRepository, IAccountRepository accountRepository, IMissionEventPublisher missionEventPublisher, IOptions<BackGroundJobOptions> options, ILogger<MissionPublicationService> logger)
    {
        _missionProcessingRepository = missionProcessingRepository ?? throw new ArgumentNullException(nameof(missionProcessingRepository));
        _missionRepository = missionRepository ?? throw new ArgumentNullException(nameof(missionRepository));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _missionEventPublisher = missionEventPublisher ?? throw new ArgumentNullException(nameof(missionEventPublisher));
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Walks the READY lines once, from the lowest RegistryMissionId to the highest.
    /// <para>
    /// The walk is a keyset cursor, not a repeated "take the first N READY". A line whose
    /// account is still unknown deliberately stays READY, so re-asking for the first N would
    /// hand back the very same chunk on every turn and the loop would never end. Moving the
    /// cursor forward makes each pass strictly progress: the lines left behind are retried by
    /// the next run of the job, never inside the current one.
    /// </para>
    /// </summary>
    public async Task ProcessPendingLinesAsync(CancellationToken cancellationToken = default)
    {
        var afterRegistryMissionId = 0;

        while (true)
        {
            var chunk = await _missionProcessingRepository.GetByStatusAsync(
                ProcessStatus.Ready,
                afterRegistryMissionId,
                _options.Chunk,
                cancellationToken);

            if (chunk.Count == 0)
            {
                break;
            }

            afterRegistryMissionId = chunk[^1].RegistryMissionId;

            await ProcessChunkAsync(chunk, cancellationToken);
        }
    }

    private async Task ProcessChunkAsync(List<MissionProcessingEntity> chunk, CancellationToken cancellationToken)
    {
        var registryMissionIds = chunk.Select(m => m.RegistryMissionId).ToList();
        var missions = await _missionRepository.GetByRegistryMissionIdsAsync(registryMissionIds, cancellationToken);
        var missionMap = missions.ToDictionary(m => m.RegistryMissionId);

        var existingAccounts = await GetExistingAccountsAsync(missions, cancellationToken);

        var publishable = new List<(MissionProcessingEntity Processing, MissionEntity Mission)>();
        var postponed = new List<(MissionProcessingEntity Processing, string Reason)>();

        foreach (var missionProcessing in chunk)
        {
            if (!missionMap.TryGetValue(missionProcessing.RegistryMissionId, out var mission))
            {
                _logger.LogWarning("Mission processing record {RegistryMissionId} has no corresponding mission.Missions entity", missionProcessing.RegistryMissionId);
                postponed.Add((missionProcessing, "Missing mission.Missions entity"));
                continue;
            }

            if (!existingAccounts.Contains(mission.AccountNumber))
            {
                _logger.LogWarning("Mission line {RegistryMissionId} postponed: unknown account {AccountNumber}", missionProcessing.RegistryMissionId, mission.AccountNumber);
                postponed.Add((missionProcessing, $"Account {mission.AccountNumber} does not exist in Accounts.Account"));
                continue;
            }

            publishable.Add((missionProcessing, mission));
        }

        var created = publishable.Where(l => l.Mission.Operation == OperationAction.Insert).ToList();
        if (created.Count > 0)
        {
            await _missionEventPublisher.BulkPublishAsync(
                created.Select(l => l.Mission.MapMissionToCreatedEventData()).ToList());
        }

        var removed = publishable.Where(l => l.Mission.Operation == OperationAction.Delete).ToList();
        if (removed.Count > 0)
        {
            await _missionEventPublisher.BulkPublishRemovedAsync(
                removed.Select(l => l.Mission.MapMissionToRemovedEventData()).ToList());
        }

        if (publishable.Count > 0)
        {
            foreach (var (processing, _) in publishable)
            {
                processing.Status = ProcessStatus.Sent;
                processing.PublishedOn = DateTime.UtcNow;
            }

            await _missionProcessingRepository.UpdateAsync(publishable.Select(l => l.Processing).ToList(), cancellationToken);
        }

        // Postponed lines stay READY, with their reason: they leave on the next run of the job,
        // with no intervention, as soon as their account is known.
        if (postponed.Count > 0)
        {
            foreach (var (processing, reason) in postponed)
            {
                processing.Status = ProcessStatus.Ready;
                processing.Reason = reason;
            }

            await _missionProcessingRepository.UpdateAsync(postponed.Select(l => l.Processing).ToList(), cancellationToken);
        }

        _logger.LogInformation("Mission lines chunk processed: {PublishedCount} published, {PostponedCount} postponed", publishable.Count, postponed.Count);
    }

    private async Task<HashSet<string>> GetExistingAccountsAsync(IEnumerable<MissionEntity> missions, CancellationToken cancellationToken)
    {
        var accountNumbers = missions.Select(m => m.AccountNumber).Distinct(StringComparer.OrdinalIgnoreCase);
        var existing = await _accountRepository.GetExistingAccountNumbersAsync(accountNumbers);

        return new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
    }

}

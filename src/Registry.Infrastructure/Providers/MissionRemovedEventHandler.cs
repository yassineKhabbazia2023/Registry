// <copyright file="MissionRemovedEventHandler.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;

namespace Infrastructure.Providers;

/// <summary>
/// Handles MissionRemovedEvent, published by Offer once an engagement has been deactivated.
/// Confirms the matching DELETE line of mission.Processing.
/// <para>
/// Offer answers here even when it found no engagement to deactivate: when the mapping was
/// missing at INSERT time no row was ever created on its side, so there is nothing to revoke and
/// the target state is already reached.
/// </para>
/// </summary>
public class MissionRemovedEventHandler : MissionConfirmationEventHandler<MissionRemovedEvent, MissionRemovedEventData>
{
    public MissionRemovedEventHandler(
        ILogger<MissionRemovedEventHandler> logger,
        IMissionConfirmationService missionConfirmationService)
        : base(logger, missionConfirmationService)
    {
    }

    protected override string Operation => OperationAction.Delete;
}

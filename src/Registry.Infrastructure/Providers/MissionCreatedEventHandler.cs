// <copyright file="MissionCreatedEventHandler.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;

namespace Infrastructure.Providers;

/// <summary>
/// Handles MissionCreatedEvent, published by Offer once the subscription of an engagement has
/// been created. Confirms the matching INSERT line of mission.Processing.
/// </summary>
public class MissionCreatedEventHandler : MissionConfirmationEventHandler<MissionCreatedEvent, MissionCreatedEventData>
{
    public MissionCreatedEventHandler(
        ILogger<MissionCreatedEventHandler> logger,
        IMissionConfirmationService missionConfirmationService)
        : base(logger, missionConfirmationService)
    {
    }

    protected override string Operation => OperationAction.Insert;
}

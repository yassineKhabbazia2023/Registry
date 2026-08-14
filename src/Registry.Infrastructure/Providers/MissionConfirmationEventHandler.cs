// <copyright file="MissionConfirmationEventHandler.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents.EventsData;

namespace Infrastructure.Providers;

/// <summary>
/// Shared shape of the mission acknowledgements sent back by Offer. The two events differ only by
/// the operation they settle, which is why the operation is the single abstract member.
/// <para>
/// Deserializes with Newtonsoft: BaseEvent&lt;T&gt; has no parameterless constructor and its only
/// constructor takes an "eventData" parameter binding to no property, so System.Text.Json cannot
/// read the envelope. This is also why the handlers are keyed IEventHandler implementations driven
/// by the SubscriptionProcessor rather than Azure Functions binding onto the event POCO.
/// </para>
/// </summary>
/// <typeparam name="TEvent">Acknowledgement event published by Offer.</typeparam>
/// <typeparam name="TData">Payload of that event, carrying the engagement code.</typeparam>
public abstract class MissionConfirmationEventHandler<TEvent, TData> : IEventHandler
    where TEvent : BaseEvent<TData>
    where TData : BaseMissionEventData
{
    private readonly ILogger _logger;
    private readonly IMissionConfirmationService _missionConfirmationService;

    protected MissionConfirmationEventHandler(ILogger logger, IMissionConfirmationService missionConfirmationService)
    {
        _logger = logger;
        _missionConfirmationService = missionConfirmationService ?? throw new ArgumentNullException(nameof(missionConfirmationService));
    }

    /// <summary>
    /// Operation of the mission.Missions line this acknowledgement settles. The same engagement
    /// code carries one line per operation, so it is the type of the event received that says
    /// which one to close.
    /// </summary>
    protected abstract string Operation { get; }

    public async Task HandleAsync(string message)
    {
        var eventName = typeof(TEvent).Name;

        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogError("{EventName} received with an empty message; acknowledgement ignored", eventName);
            return;
        }

        var missionEvent = JsonConvert.DeserializeObject<TEvent>(message);

        if (string.IsNullOrWhiteSpace(missionEvent?.Data?.EngagementCode))
        {
            _logger.LogError("{EventName} received without an engagement code; acknowledgement ignored", eventName);
            return;
        }

        _logger.LogInformation("Processing {EventName} for engagement code {EngagementCode}", eventName, missionEvent.Data.EngagementCode);

        await _missionConfirmationService.ConfirmAsync(missionEvent.Data.EngagementCode, Operation);
    }
}

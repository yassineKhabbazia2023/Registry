// <copyright file="ContactAkuiteoSynchronizationResult.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Enums;

namespace Application.Models.Results;

public class ContactAkuiteoSynchronizationResult
{
    public required ContactAkuiteoSynchronizationOutcome Outcome { get; init; }

    public string? AkuiteoContactId { get; init; }

    public string? Error { get; init; }
}

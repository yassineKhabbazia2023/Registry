// <copyright file="BaseEvent.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Domain.Common;

public class BaseEvent
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTimeOffset CreationDate { get; protected set; } = DateTimeOffset.UtcNow;

    public string Version { get; protected set; } = "1.0";

    public object Data { get; protected set; } = default!;
}

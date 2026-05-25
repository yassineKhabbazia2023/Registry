// <copyright file="AkuiteoMessageResponse.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

/// <summary>
/// Represents an Akuiteo metadata message.
/// </summary>
public class AkuiteoMessageResponse
{
    /// <summary>
    /// Gets or sets the Akuiteo message timestamp.
    /// </summary>
    public string? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the Akuiteo message code.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Gets or sets the Akuiteo message level.
    /// </summary>
    public string? Level { get; set; }

    /// <summary>
    /// Gets or sets the Akuiteo message text.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the Akuiteo technical message.
    /// </summary>
    public string? Message { get; set; }
}

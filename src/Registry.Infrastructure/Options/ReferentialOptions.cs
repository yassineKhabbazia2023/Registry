// <copyright file="ReferentialOptions.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>


namespace Application.Options;

public class ReferentialOptions
{
    public required string Authorization { get; set; }

    public required string ClientId { get; set; }

    public required string ClientSecret { get; set; }
}
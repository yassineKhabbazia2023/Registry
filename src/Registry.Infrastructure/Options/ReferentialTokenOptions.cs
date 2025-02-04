// <copyright file="ReferentialTokenOptions.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Options;
public class ReferentialTokenOptions
{
    public required string TokenUrl { get; set; }
    public required string GrantType { get; set; }
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
    public required string UserName { get; set; }
    public required string Password { get; set; }
    public required string Scope { get; set; }
}


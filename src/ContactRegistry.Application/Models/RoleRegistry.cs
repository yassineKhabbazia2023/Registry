// <copyright file="RoleRegistry.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models;

public partial class RoleRegistry
{
    public string? ContactCode { get; set; }

    public required string ContactEmailOffice { get; set; }

    public required string RoleFunctionDescription { get; set; }

    public required string AccountNumber { get; set; }

    public bool RoleFlagStatus { get; set; }

    public required string RoleSourceName { get; set; }
}
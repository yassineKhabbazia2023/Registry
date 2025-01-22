// <copyright file="RoleCsv.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models;

public class RefRoleCsv
{

    public required string ContactEmail { get; set; }

    public required string AccountNumber { get; set; }

    public int RoleFlagStatus { get; set; }

    public string? Description { get; set; }

    public required string Operation { get; set; }
}

// <copyright file="RegProcessDeltaTrigger.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Domain.Entities;

public class RegProcessDeltaTrigger
{
    public int Id { get; set; }

    public bool Account { get; set; }

    public bool Role { get; set; }

    public bool Contact { get; set; }
}

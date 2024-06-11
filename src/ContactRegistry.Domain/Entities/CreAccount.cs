// <copyright file="CreAccountEntity.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Domain.Entities;

public class CreAccount
{
    public Guid Id { get; set; }

    public DateTime? Updated { get; set; }

    public string LegalName { get; set; }

    public string AccountNumber { get; set; }
}
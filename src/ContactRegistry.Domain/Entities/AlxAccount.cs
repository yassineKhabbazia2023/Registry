// <copyright file="CreAccountEntity.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Domain.Entities;

public class AlxAccount
{
    public Guid Id { get; set; }

    public bool AccountFlagEscActif { get; set; }

    public string LegalName { get; set; }

    public string AccountNumber { get; set; }
}
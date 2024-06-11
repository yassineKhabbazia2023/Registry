// <copyright file="RoleCsv.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models
{
    /// <summary>
    /// RoleCsv.
    /// </summary>
    /// <param name="Id">Id.</param>
    /// <param name="ContactId">ContactId.</param>
    /// <param name="AccountId">AccountId.</param>
    /// <param name="Onboarded">Onboarded.</param>
    public record RoleCsv(Guid Id, Guid ContactId, Guid AccountId, bool Onboarded);
}

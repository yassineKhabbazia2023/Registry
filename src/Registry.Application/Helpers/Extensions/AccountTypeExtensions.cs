// <copyright file="AccountTypeExtensions.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;

namespace Application.Helpers.Extensions;

/// <summary>
/// Provides helper methods for account type checks.
/// </summary>
public static class AccountTypeExtensions
{
    /// <summary>
    /// Determines whether the provided account type is a prospect account.
    /// </summary>
    /// <param name="accountType">The account type to evaluate.</param>
    /// <returns><c>true</c> when the account type is prospect; otherwise, <c>false</c>.</returns>
    public static bool IsProspectAccount(this string? accountType)
        => string.Equals(accountType, AccountTypes.Prospect, StringComparison.OrdinalIgnoreCase);
}

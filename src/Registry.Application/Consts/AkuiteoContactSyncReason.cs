// <copyright file="AkuiteoContactSyncReason.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Consts;

public static class AkuiteoContactSyncReason
{
    public const string RoleCreatedFromPulse = "ROLE_CREATED_FROM_PULSE";
    public const string RoleFromAkuiteo = "ROLE_FROM_AKUITEO";

    // Kept as a source-compatible alias for existing consumers.
    public const string RoleCreated = RoleCreatedFromPulse;
}

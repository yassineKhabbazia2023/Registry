// <copyright file="QueryBatching.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Infrastructure.Repository;

/// <summary>
/// Batching bound shared by the csv-driven lookups.
/// </summary>
internal static class QueryBatching
{
    internal const int BatchSize = 2000;
}

// <copyright file="MissionCsvReceptionOutcome.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

public class MissionCsvReceptionOutcome
{
    public bool IsAccepted { get; private init; }

    public string? ErrorMessage { get; private init; }

    public MissionCsvReceptionResult? Summary { get; private init; }

    public static MissionCsvReceptionOutcome Rejected(string errorMessage)
    {
        return new MissionCsvReceptionOutcome { ErrorMessage = errorMessage };
    }

    public static MissionCsvReceptionOutcome Accepted(MissionCsvReceptionResult summary)
    {
        return new MissionCsvReceptionOutcome { IsAccepted = true, Summary = summary };
    }
}

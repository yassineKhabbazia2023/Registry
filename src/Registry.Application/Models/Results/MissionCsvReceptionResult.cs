// <copyright file="MissionCsvReceptionResult.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

public class MissionCsvReceptionResult
{
    public int AcceptedLines { get; set; }

    public int RejectedLines { get; set; }

    public List<LightValidationError> Errors { get; set; } = [];
}

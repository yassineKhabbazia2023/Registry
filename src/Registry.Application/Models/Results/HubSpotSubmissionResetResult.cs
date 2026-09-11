// <copyright file="HubSpotSubmissionResetResult.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

public sealed class HubSpotSubmissionResetResult
{
    private HubSpotSubmissionResetResult(bool isSuccess, int statusCode)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
    }

    public bool IsSuccess { get; }

    public int StatusCode { get; }

    public static HubSpotSubmissionResetResult Success()
        => new(true, 204);
}

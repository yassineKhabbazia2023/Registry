namespace Application.Models.Results;

public sealed class HubSpotFormSubmissionResult
{
    private HubSpotFormSubmissionResult(bool isSuccess, int statusCode, string? errorMessage, bool hubSpotDispatchState)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        ErrorMessage = errorMessage;
        HubSpotDispatchState = hubSpotDispatchState;
    }

    public bool IsSuccess { get; }

    public int StatusCode { get; }

    public string? ErrorMessage { get; }

    public bool HubSpotDispatchState { get; }

    public static HubSpotFormSubmissionResult Created(bool hubSpotDispatchState)
        => new(true, 201, null, hubSpotDispatchState);

    public static HubSpotFormSubmissionResult Rejected()
        => new(false, 422, null, false);
}

namespace Application.Models.Results;

public sealed class HubSpotSubmissionStateResult
{
    private HubSpotSubmissionStateResult(bool isSuccess, int statusCode)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
    }

    public bool IsSuccess { get; }

    public int StatusCode { get; }

    public static HubSpotSubmissionStateResult Found()
        => new(true, 200);

    public static HubSpotSubmissionStateResult NotFound()
        => new(false, 404);
}

namespace Application.Models.Results;

public class HubSpotSubmissionResult
{
    public required bool IsSuccess { get; set; }

    public required int StatusCode { get; set; }

    public string? ErrorMessage { get; set; }
}

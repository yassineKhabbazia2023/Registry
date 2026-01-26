using System.ComponentModel.DataAnnotations;
using Application.Requests;

namespace Registry.Application.Tests.Requests;

public class HubSpotSubmissionInputRequestTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validation_Fails_WhenFirstNameIsMissing(string? firstName)
    {
        var request = new HubSpotSubmissionInputRequest
        {
            FirstName = firstName,
            LastName = "Dupont"
        };

        var isValid = TryValidate(request, out var results);

        Assert.False(isValid);
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(HubSpotSubmissionInputRequest.FirstName)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validation_Fails_WhenLastNameIsMissing(string? lastName)
    {
        var request = new HubSpotSubmissionInputRequest
        {
            FirstName = "Marie",
            LastName = lastName
        };

        var isValid = TryValidate(request, out var results);

        Assert.False(isValid);
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(HubSpotSubmissionInputRequest.LastName)));
    }

    [Fact]
    public void Validation_Succeeds_WhenNamesProvided()
    {
        var request = new HubSpotSubmissionInputRequest
        {
            FirstName = "Marie",
            LastName = "Dupont"
        };

        var isValid = TryValidate(request, out _);

        Assert.True(isValid);
    }

    private static bool TryValidate(object instance, out List<ValidationResult> results)
    {
        var context = new ValidationContext(instance);
        results = new List<ValidationResult>();
        return Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
    }
}

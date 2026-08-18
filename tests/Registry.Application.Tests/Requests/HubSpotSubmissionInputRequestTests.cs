using System.ComponentModel.DataAnnotations;
using Application.Requests;

namespace Registry.Application.Tests.Requests;

public class HubSpotSubmissionInputRequestTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validation_Succeeds_WhenFirstNameIsMissing(string? firstName)
    {
        // Step 1 du formulaire (mail libre) : le front n'a pas encore de contact selectionne, donc pas de FirstName.
        var request = new HubSpotSubmissionInputRequest
        {
            FirstName = firstName,
            LastName = "Dupont"
        };

        var isValid = TryValidate(request, out _);

        Assert.True(isValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validation_Succeeds_WhenLastNameIsMissing(string? lastName)
    {
        // Step 1 du formulaire (mail libre) : le front n'a pas encore de contact selectionne, donc pas de LastName.
        var request = new HubSpotSubmissionInputRequest
        {
            FirstName = "Marie",
            LastName = lastName
        };

        var isValid = TryValidate(request, out _);

        Assert.True(isValid);
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

using System.Net;
using System.Text.Json;
using Application.Models;
using Application.Providers;
using Moq;
using Moq.Protected;

namespace Registry.Infrastructure.Tests.Providers;

/// <summary>
/// Tests for <see cref="AkuiteoContactProvider"/>.
/// </summary>
public class AkuiteoContactProviderTests
{
    #region CreateContactAsync

    /// <summary>
    /// Ensures the provider posts to the Akuiteo contact endpoint with the expected payload.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_WhenSuccessful_ShouldPostExpectedRequest()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        string? capturedRequestBody = null;
        var response = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("""{"meta":{"status":"succeeded","messages":[]},"data":{"contactId":"500145940"}}""")
        };

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
            {
                capturedRequest = request;
                capturedRequestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            })
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.akuiteo.local/")
        };
        httpClient.DefaultRequestHeaders.Add("X-Client-Id", "rate-client-id");
        httpClient.DefaultRequestHeaders.Add("X-Client-Secret", "rate-client-secret");

        var provider = new AkuiteoContactProvider(httpClient);
        var request = CreateProviderRequest();

        // Act
        var result = await provider.CreateContactAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("500145940", result.ContactId);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal(new Uri("https://api.akuiteo.local/akuiteo/contact"), capturedRequest.RequestUri);

        Assert.NotNull(capturedRequestBody);
        using var jsonDocument = JsonDocument.Parse(capturedRequestBody);
        Assert.Equal("9000120123", jsonDocument.RootElement.GetProperty("accountNumber").GetString());
        Assert.Equal("M", jsonDocument.RootElement.GetProperty("title").GetString());
        Assert.Equal("Jean", jsonDocument.RootElement.GetProperty("lastName").GetString());
        Assert.Equal("Dupont", jsonDocument.RootElement.GetProperty("firstName").GetString());
        Assert.Equal("Gérant", jsonDocument.RootElement.GetProperty("jobTitle").GetString());
        Assert.Equal("Direction", jsonDocument.RootElement.GetProperty("contactDepartment").GetString());
        Assert.Equal("Président", jsonDocument.RootElement.GetProperty("companyRole").GetString());
        Assert.Equal("o.dbira@boulangerie.fr", jsonDocument.RootElement.GetProperty("email").GetString());
        Assert.Equal("06 12 34 56 78", jsonDocument.RootElement.GetProperty("mobilePhone").GetString());

        var contactTypes = jsonDocument.RootElement.GetProperty("contactTypes");
        Assert.True(contactTypes.GetProperty("isDigitalVaultContact").GetBoolean());
        Assert.False(contactTypes.GetProperty("isDebtCollectionContact").GetBoolean());
        Assert.True(contactTypes.GetProperty("isMandateSignatory").GetBoolean());
    }

    /// <summary>
    /// Ensures a downstream non-success response is mapped to a failure result.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_WhenDownstreamFails_ShouldReturnFailureResult()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("Akuiteo unavailable")
        };

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.akuiteo.local/")
        };

        var provider = new AkuiteoContactProvider(httpClient);

        // Act
        var result = await provider.CreateContactAsync(CreateProviderRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal((int)HttpStatusCode.BadGateway, result.StatusCode);
        Assert.Equal("Akuiteo unavailable", result.ErrorMessage);
    }

    /// <summary>
    /// Ensures a successful response without contact identifier is treated as a failure.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_WhenSuccessResponseHasNoContactId_ShouldReturnFailureResult()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("""{"meta":{"status":"succeeded","messages":[]},"data":{}}""")
        };

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.akuiteo.local/")
        };

        var provider = new AkuiteoContactProvider(httpClient);

        // Act
        var result = await provider.CreateContactAsync(CreateProviderRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal((int)HttpStatusCode.Created, result.StatusCode);
        Assert.Equal("Akuiteo returned an empty contact id.", result.ErrorMessage);
        Assert.Null(result.ContactId);
    }

    /// <summary>
    /// Ensures a 2xx downstream response with a failed Akuiteo metadata status is mapped to a failure result.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_WhenAkuiteoMetaStatusFails_ShouldReturnFailureResult()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"meta":{"status":"failed","messages":[{"timestamp":"2026-04-13T12:44:49.244+0000","code":"BAD_REQUEST","level":"error","text":"contact department is invalid"}]},"data":{"contactId":"500145940"}}""")
        };

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.akuiteo.local/")
        };

        var provider = new AkuiteoContactProvider(httpClient);

        // Act
        var result = await provider.CreateContactAsync(CreateProviderRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("contact department is invalid", result.ErrorMessage);
        Assert.Null(result.ContactId);
    }

    /// <summary>
    /// Ensures an invalid 2xx downstream payload is mapped to a failure result.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_WhenSuccessResponsePayloadIsInvalid_ShouldReturnFailureResult()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-json")
        };

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.akuiteo.local/")
        };

        var provider = new AkuiteoContactProvider(httpClient);

        // Act
        var result = await provider.CreateContactAsync(CreateProviderRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("Akuiteo returned an invalid response.", result.ErrorMessage);
        Assert.Null(result.ContactId);
    }

    #endregion

    /// <summary>
    /// Creates a valid provider request.
    /// </summary>
    /// <returns>A valid request instance.</returns>
    private static AkuiteoCreateContactRequest CreateProviderRequest()
    {
        return new AkuiteoCreateContactRequest
        {
            AccountNumber = "9000120123",
            Title = "M",
            LastName = "Jean",
            FirstName = "Dupont",
            JobTitle = "Gérant",
            ContactDepartment = "Direction",
            CompanyRole = "Président",
            ContactTypes = new AkuiteoCreateContactTypesRequest
            {
                IsDigitalVaultContact = true,
                IsDebtCollectionContact = false,
                IsMandateSignatory = true
            },
            Email = "o.dbira@boulangerie.fr",
            MobilePhone = "06 12 34 56 78"
        };
    }
}

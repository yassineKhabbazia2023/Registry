using System.Net;
using System.Text.Json;
using Application.Models;
using Application.Providers;
using Moq;
using Moq.Protected;

namespace Registry.Infrastructure.Tests.Providers;

/// <summary>
/// Tests for <see cref="AkuiteoCustomerProvider"/>.
/// </summary>
public class AkuiteoCustomerProviderTests
{
    #region CreateCustomerAsync

    /// <summary>
    /// Ensures the provider posts to the Akuiteo endpoint with the expected payload.
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_WhenSuccessful_ShouldPostExpectedRequest()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        string? capturedRequestBody = null;
        var response = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("""{"meta":{"status":"succeeded","messages":[]},"data":{"accountNumber":"9010001713"}}""")
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

        var provider = new AkuiteoCustomerProvider(httpClient);
        var request = CreateProviderRequest();

        // Act
        var result = await provider.CreateCustomerAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("9010001713", result.AccountNumber);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal(new Uri("https://api.akuiteo.local/akuiteo/account"), capturedRequest.RequestUri);

        Assert.NotNull(capturedRequestBody);
        using var jsonDocument = JsonDocument.Parse(capturedRequestBody);
        Assert.Equal("Cloud & Data Solutions", jsonDocument.RootElement.GetProperty("legalName").GetString());
        Assert.Equal("834601478", jsonDocument.RootElement.GetProperty("siren").GetString());
        Assert.Equal("83460147800011", jsonDocument.RootElement.GetProperty("siret").GetString());
        Assert.Equal("Personne morale", jsonDocument.RootElement.GetProperty("legalStructure").GetString());
        Assert.Equal("EURL", jsonDocument.RootElement.GetProperty("legalFormCode").GetString());
        Assert.Equal("8559A", jsonDocument.RootElement.GetProperty("nafCode").GetString());

        var address = jsonDocument.RootElement.GetProperty("address");
        Assert.Equal("21B rue du moulin", address.GetProperty("line1").GetString());
        Assert.Equal("75010", address.GetProperty("zipCode").GetString());
        Assert.Equal("Paris", address.GetProperty("city").GetString());
        Assert.Equal("75", address.GetProperty("departmentCode").GetString());
        Assert.Equal("11", address.GetProperty("regionCode").GetString());
        Assert.Equal("FR", address.GetProperty("countryCode").GetString());

        Assert.Equal("case.manager@rydge.fr", jsonDocument.RootElement.GetProperty("caseManagerEmail").GetString());
        Assert.Equal("account.manager@rydge.fr", jsonDocument.RootElement.GetProperty("accountManagerEmail").GetString());
    }

    /// <summary>
    /// Ensures a downstream non-success response is mapped to a failure result.
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_WhenDownstreamFails_ShouldReturnFailureResult()
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

        var provider = new AkuiteoCustomerProvider(httpClient);

        // Act
        var result = await provider.CreateCustomerAsync(CreateProviderRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal((int)HttpStatusCode.BadGateway, result.StatusCode);
        Assert.Equal("Akuiteo unavailable", result.ErrorMessage);
    }

    /// <summary>
    /// Ensures a successful response without account number is still treated as a failure.
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_WhenSuccessResponseHasNoAccountNumber_ShouldReturnFailureResult()
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

        var provider = new AkuiteoCustomerProvider(httpClient);

        // Act
        var result = await provider.CreateCustomerAsync(CreateProviderRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal((int)HttpStatusCode.Created, result.StatusCode);
        Assert.Equal("Akuiteo returned an empty account number.", result.ErrorMessage);
    }

    /// <summary>
    /// Ensures a 2xx downstream response with a failed Akuiteo metadata status is mapped to a failure result.
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_WhenAkuiteoMetaStatusFails_ShouldReturnFailureResult()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"meta":{"status":"failed","messages":[{"timestamp":"2026-04-13T12:44:49.244+0000","code":"BAD_REQUEST","level":"error","text":"/legalStructure PersonneMorale is not a valid enum value"}]},"data":{"accountNumber":"9010001713"}}""")
        };

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.akuiteo.local/")
        };

        var provider = new AkuiteoCustomerProvider(httpClient);

        // Act
        var result = await provider.CreateCustomerAsync(CreateProviderRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("/legalStructure PersonneMorale is not a valid enum value", result.ErrorMessage);
        Assert.Null(result.AccountNumber);
    }

    /// <summary>
    /// Ensures an unsuccessful downstream response with Akuiteo metadata keeps the Akuiteo message text.
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_WhenDownstreamFailsWithAkuiteoMessage_ShouldReturnMessageText()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"meta":{"status":"failed","messages":[{"timestamp":"2026-04-13T12:44:49.244+0000","code":"BAD_REQUEST","level":"error","text":"/legalStructure PersonneMorale is not a valid enum value"}]}}""")
        };

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.akuiteo.local/")
        };

        var provider = new AkuiteoCustomerProvider(httpClient);

        // Act
        var result = await provider.CreateCustomerAsync(CreateProviderRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("/legalStructure PersonneMorale is not a valid enum value", result.ErrorMessage);
    }

    /// <summary>
    /// Ensures an Akuiteo technical message field is used when the text field is not returned.
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_WhenDownstreamFailsWithAkuiteoTechnicalMessage_ShouldReturnMessage()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"meta":{"status":"failed","messages":[{"status":0,"type":"https://docs.akuiteo.fr/api/","timestamp":"2026-04-12T22:00:00.000+0000","name":"T9RuntimeException","message":"L'APE n'existe pas","className":"com.itnsa.fwk.exception.T9RuntimeException"}]},"data":{}}""")
        };

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.akuiteo.local/")
        };

        var provider = new AkuiteoCustomerProvider(httpClient);

        // Act
        var result = await provider.CreateCustomerAsync(CreateProviderRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("L'APE n'existe pas", result.ErrorMessage);
    }

    /// <summary>
    /// Ensures an invalid 2xx downstream payload is mapped to a failure result.
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_WhenSuccessResponsePayloadIsInvalid_ShouldReturnFailureResult()
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

        var provider = new AkuiteoCustomerProvider(httpClient);

        // Act
        var result = await provider.CreateCustomerAsync(CreateProviderRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("Akuiteo returned an invalid response.", result.ErrorMessage);
        Assert.Null(result.AccountNumber);
    }

    #endregion

    /// <summary>
    /// Creates a valid provider request.
    /// </summary>
    /// <returns>A valid request instance.</returns>
    private static AkuiteoCreateCustomerRequest CreateProviderRequest()
    {
        return new AkuiteoCreateCustomerRequest
        {
            LegalName = "Cloud & Data Solutions",
            Siren = "834601478",
            Siret = "83460147800011",
            LegalStructure = "Personne morale",
            LegalFormCode = "EURL",
            NafCode = "8559A",
            Address = new AkuiteoCreateCustomerAddressRequest
            {
                Line1 = "21B rue du moulin",
                ZipCode = "75010",
                City = "Paris",
                DepartmentCode = "75",
                RegionCode = "11",
                CountryCode = "FR"
            },
            CaseManagerEmail = "case.manager@rydge.fr",
            AccountManagerEmail = "account.manager@rydge.fr"
        };
    }
}

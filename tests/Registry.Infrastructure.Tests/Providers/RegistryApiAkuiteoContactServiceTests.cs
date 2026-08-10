// <copyright file="RegistryApiAkuiteoContactServiceTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Providers;
using Application.Requests;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace Registry.Infrastructure.Tests.Providers;

/// <summary>
/// Tests for <see cref="RegistryApiAkuiteoContactService"/>.
/// </summary>
public class RegistryApiAkuiteoContactServiceTests
{
    #region CreateContactAsync

    /// <summary>
    /// Ensures the service posts the complete contact payload to the Registry Akuiteo endpoint.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_WhenRegistryApiSucceeds_ShouldReturnContactIdentifier()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        string? capturedRequestBody = null;
        var response = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("""{"contactId":"500145940"}""")
        };
        var service = CreateService(
            (_, request) =>
            {
                capturedRequest = request;
                capturedRequestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return Task.FromResult(response);
            });

        // Act
        var result = await service.CreateContactAsync(CreateRequest());

        // Assert
        Assert.Equal("500145940", result.ContactId);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal(new Uri("https://registry.local/api/akuiteo/contacts"), capturedRequest.RequestUri);
        Assert.Equal("application/json", capturedRequest.Content!.Headers.ContentType?.MediaType);

        Assert.NotNull(capturedRequestBody);
        using var jsonDocument = JsonDocument.Parse(capturedRequestBody);
        var root = jsonDocument.RootElement;
        Assert.Equal("9000120123", root.GetProperty("accountNumber").GetString());
        Assert.Equal("M", root.GetProperty("title").GetString());
        Assert.Equal("Dupont", root.GetProperty("lastName").GetString());
        Assert.Equal("Jean", root.GetProperty("firstName").GetString());
        Assert.Equal(string.Empty, root.GetProperty("jobTitle").GetString());
        Assert.Equal(string.Empty, root.GetProperty("contactDepartment").GetString());
        Assert.Equal(string.Empty, root.GetProperty("companyRole").GetString());
        Assert.Equal("jean.dupont@example.com", root.GetProperty("email").GetString());
        Assert.Equal("+33612345678", root.GetProperty("mobilePhone").GetString());
        var contactTypes = root.GetProperty("contactTypes");
        Assert.True(contactTypes.GetProperty("isDigitalVaultContact").GetBoolean());
        Assert.False(contactTypes.GetProperty("isDebtCollectionContact").GetBoolean());
        Assert.True(contactTypes.GetProperty("isMandateSignatory").GetBoolean());
    }

    /// <summary>
    /// Ensures the Registry problem title is propagated for a rejected request.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_WhenRegistryApiReturnsConflict_ShouldPropagateProblemTitle()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent("""{"status":409,"title":"Akuiteo contact creation failed."}""")
        };
        var service = CreateService((_, _) => Task.FromResult(response));

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoContactCreationTechnicalException>(
            () => service.CreateContactAsync(CreateRequest()));

        // Assert
        Assert.Equal("Akuiteo contact creation failed.", exception.Message);
    }

    /// <summary>
    /// Ensures an unsuccessful response without a valid problem payload uses a stable fallback message.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_WhenRegistryApiReturnsInvalidErrorPayload_ShouldUseFallbackMessage()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("not-json")
        };
        var service = CreateService((_, _) => Task.FromResult(response));

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoContactCreationTechnicalException>(
            () => service.CreateContactAsync(CreateRequest()));

        // Assert
        Assert.Equal("Registry API returned status code 400 while creating an Akuiteo contact.", exception.Message);
    }

    /// <summary>
    /// Ensures a successful response without an Akuiteo identifier is rejected.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_WhenRegistryApiReturnsEmptyContactId_ShouldFail()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("""{"contactId":""}""")
        };
        var service = CreateService((_, _) => Task.FromResult(response));

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoContactCreationTechnicalException>(
            () => service.CreateContactAsync(CreateRequest()));

        // Assert
        Assert.Equal("Registry API returned an empty Akuiteo contact identifier.", exception.Message);
    }

    /// <summary>
    /// Ensures malformed successful responses are exposed as technical failures.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_WhenRegistryApiReturnsInvalidSuccessPayload_ShouldFail()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("not-json")
        };
        var service = CreateService((_, _) => Task.FromResult(response));

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoContactCreationTechnicalException>(
            () => service.CreateContactAsync(CreateRequest()));

        // Assert
        Assert.Equal("Registry API returned an invalid Akuiteo contact creation response.", exception.Message);
        Assert.IsType<JsonException>(exception.InnerException);
    }

    /// <summary>
    /// Ensures an unreachable Registry API is exposed as a stable technical failure.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_WhenRegistryApiIsUnavailable_ShouldFail()
    {
        // Arrange
        var service = CreateService((_, _) => throw new HttpRequestException("network failure"));

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoContactCreationTechnicalException>(
            () => service.CreateContactAsync(CreateRequest()));

        // Assert
        Assert.Equal("Registry API is unavailable.", exception.Message);
        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    /// <summary>
    /// Ensures a Registry API timeout is exposed as a stable technical failure.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_WhenRegistryApiTimesOut_ShouldFail()
    {
        // Arrange
        var service = CreateService((_, _) => throw new TaskCanceledException("timeout"));

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoContactCreationTechnicalException>(
            () => service.CreateContactAsync(CreateRequest()));

        // Assert
        Assert.Equal("Registry API timed out.", exception.Message);
        Assert.IsType<TaskCanceledException>(exception.InnerException);
    }

    #endregion

    #region SearchContactsAsync

    /// <summary>
    /// Ensures the Azure Functions proxy explicitly rejects the unsupported contact-search workflow.
    /// </summary>
    [Fact]
    public async Task SearchContactsAsync_ShouldNotBeSupported()
    {
        // Arrange
        var service = CreateService((_, _) => throw new InvalidOperationException("HTTP should not be called."));

        // Act
        var exception = await Assert.ThrowsAsync<NotSupportedException>(
            () => service.SearchContactsAsync("contact@example.com"));

        // Assert
        Assert.Equal(
            "Akuiteo contact search is not supported from Registry Azure Functions.",
            exception.Message);
    }

    #endregion

    /// <summary>
    /// Creates the service with a controlled Registry API handler.
    /// </summary>
    /// <param name="sendAsync">The HTTP response factory.</param>
    /// <returns>The service under test.</returns>
    private static RegistryApiAkuiteoContactService CreateService(
        Func<CancellationToken, HttpRequestMessage, Task<HttpResponseMessage>> sendAsync)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(
                (request, cancellationToken) => sendAsync(cancellationToken, request));

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://registry.local/")
        };
        var httpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        httpClientFactory
            .Setup(factory => factory.CreateClient("RegistryApi"))
            .Returns(httpClient);

        return new RegistryApiAkuiteoContactService(
            httpClientFactory.Object,
            Mock.Of<ILogger<RegistryApiAkuiteoContactService>>());
    }

    /// <summary>
    /// Creates a complete contact request.
    /// </summary>
    /// <returns>The contact request.</returns>
    private static AkuiteoContactCreationRequest CreateRequest()
    {
        return new AkuiteoContactCreationRequest
        {
            AccountNumber = "9000120123",
            Title = "M",
            LastName = "Dupont",
            FirstName = "Jean",
            JobTitle = string.Empty,
            ContactDepartment = string.Empty,
            CompanyRole = string.Empty,
            ContactTypes = new AkuiteoContactTypesRequest
            {
                IsDigitalVaultContact = true,
                IsDebtCollectionContact = false,
                IsMandateSignatory = true
            },
            Email = "jean.dupont@example.com",
            MobilePhone = "+33612345678"
        };
    }
}

using Application.Exceptions;
using Application.Interfaces;
using Application.Models.Results;
using Application.Requests;
using Kpmg.ExceptionMiddleware.AdvancedException;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Registry.WebApi.Controllers;

namespace Registry.WebApi.Tests.Controllers;

/// <summary>
/// Tests for <see cref="AkuiteoController"/>.
/// </summary>
public class AkuiteoControllerTests
{
    private const int AccountId = 12345;

    #region CreateCustomerAsync

    /// <summary>
    /// Ensures the controller returns 201 with the created account number.
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_ShouldReturnCreated()
    {
        // Arrange
        var request = CreateRequest();
        var response = new AkuiteoCustomerCreationResponse
        {
            AccountNumber = "9010001713"
        };

        var serviceMock = new Mock<IAkuiteoCustomerService>();
        var contactServiceMock = new Mock<IAkuiteoContactService>();
        serviceMock
            .Setup(service => service.CreateCustomerAsync(request))
            .ReturnsAsync(response);

        var controller = CreateController(serviceMock.Object, contactServiceMock.Object);

        // Act
        var result = await controller.CreateCustomerAsync(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        var payload = Assert.IsType<AkuiteoCustomerCreationResponse>(objectResult.Value);
        Assert.Equal("9010001713", payload.AccountNumber);
        serviceMock.Verify(service => service.CreateCustomerAsync(request), Times.Once);
    }

    /// <summary>
    /// Ensures the controller returns 201 with the created contact identifier.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_ShouldReturnCreated()
    {
        // Arrange
        var request = CreateContactRequest();
        var response = new AkuiteoContactCreationResponse
        {
            ContactId = "500145940"
        };

        var serviceMock = new Mock<IAkuiteoContactService>();
        var customerServiceMock = new Mock<IAkuiteoCustomerService>();
        serviceMock
            .Setup(service => service.CreateContactAsync(request))
            .ReturnsAsync(response);

        var controller = CreateController(customerServiceMock.Object, serviceMock.Object);

        // Act
        var result = await controller.CreateContactAsync(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        var payload = Assert.IsType<AkuiteoContactCreationResponse>(objectResult.Value);
        Assert.Equal("500145940", payload.ContactId);
        serviceMock.Verify(service => service.CreateContactAsync(request), Times.Once);
    }

    /// <summary>
    /// Ensures the controller translates technical Akuiteo failures to 409.
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_WhenServiceThrowsConflict_ShouldReturnConflict()
    {
        // Arrange
        var request = CreateRequest();

        var serviceMock = new Mock<IAkuiteoCustomerService>();
        var contactServiceMock = new Mock<IAkuiteoContactService>();
        serviceMock
            .Setup(service => service.CreateCustomerAsync(request))
            .ThrowsAsync(new AkuiteoCustomerCreationTechnicalException("technical failure"));

        var controller = CreateController(serviceMock.Object, contactServiceMock.Object);

        // Act
        var result = await controller.CreateCustomerAsync(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status409Conflict, problemDetails.Status);
        Assert.Equal("technical failure", problemDetails.Title);
    }

    /// <summary>
    /// Ensures the controller translates technical Akuiteo contact failures to 409.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_WhenServiceThrowsConflict_ShouldReturnConflict()
    {
        // Arrange
        var request = CreateContactRequest();

        var serviceMock = new Mock<IAkuiteoContactService>();
        var customerServiceMock = new Mock<IAkuiteoCustomerService>();
        serviceMock
            .Setup(service => service.CreateContactAsync(request))
            .ThrowsAsync(new AkuiteoContactCreationTechnicalException("technical failure"));

        var controller = CreateController(customerServiceMock.Object, serviceMock.Object);

        // Act
        var result = await controller.CreateContactAsync(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status409Conflict, problemDetails.Status);
        Assert.Equal("technical failure", problemDetails.Title);
    }

    /// <summary>
    /// Ensures the controller translates invalid Akuiteo payload failures to 400.
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_WhenServiceThrowsBadRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var request = CreateRequest();

        var serviceMock = new Mock<IAkuiteoCustomerService>();
        var contactServiceMock = new Mock<IAkuiteoContactService>();
        serviceMock
            .Setup(service => service.CreateCustomerAsync(request))
            .ThrowsAsync(new BadRequestException(Errors.InvalidContactId, "invalid contact"));

        var controller = CreateController(serviceMock.Object, contactServiceMock.Object);

        // Act
        var result = await controller.CreateCustomerAsync(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    /// <summary>
    /// Ensures the controller translates invalid Akuiteo contact payload failures to 400.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_WhenServiceThrowsBadRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var request = CreateContactRequest();

        var serviceMock = new Mock<IAkuiteoContactService>();
        var customerServiceMock = new Mock<IAkuiteoCustomerService>();
        serviceMock
            .Setup(service => service.CreateContactAsync(request))
            .ThrowsAsync(new BadRequestException(Errors.InvalidContactId, "invalid contact"));

        var controller = CreateController(customerServiceMock.Object, serviceMock.Object);

        // Act
        var result = await controller.CreateContactAsync(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    #endregion

    #region SearchContactsAsync

    /// <summary>
    /// Ensures the contact-search endpoint exposes the expected route and returns the service result.
    /// </summary>
    [Fact]
    public async Task SearchContactsAsync_ShouldReturnContacts()
    {
        // Arrange
        const string email = "contact@test.fr";
        IReadOnlyCollection<AkuiteoContactSearchDataResponse> contacts =
        [
            new AkuiteoContactSearchDataResponse
            {
                Title = "M",
                LastName = "Dupont",
                FirstName = "Jean",
                Email = email,
                MobilePhone = "+33612345678"
            }
        ];
        var contactServiceMock = new Mock<IAkuiteoContactService>();
        contactServiceMock
            .Setup(service => service.SearchContactsAsync(email))
            .ReturnsAsync(contacts);
        var method = typeof(AkuiteoController).GetMethod(nameof(AkuiteoController.SearchContactsAsync));
        var route = Assert.Single(method!.GetCustomAttributes(typeof(HttpGetAttribute), false).Cast<HttpGetAttribute>());
        var controller = CreateController(akuiteoContactService: contactServiceMock.Object);

        // Act
        var result = await controller.SearchContactsAsync(email);

        // Assert
        Assert.Equal("contacts", route.Template);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(contacts, okResult.Value);
        contactServiceMock.Verify(service => service.SearchContactsAsync(email), Times.Once);
    }

    /// <summary>
    /// Ensures technical Akuiteo search failures are translated to conflict responses.
    /// </summary>
    [Fact]
    public async Task SearchContactsAsync_WhenServiceFails_ShouldReturnConflict()
    {
        // Arrange
        var contactServiceMock = new Mock<IAkuiteoContactService>();
        contactServiceMock
            .Setup(service => service.SearchContactsAsync(It.IsAny<string>()))
            .ThrowsAsync(new AkuiteoContactSearchTechnicalException("technical failure"));
        var controller = CreateController(akuiteoContactService: contactServiceMock.Object);

        // Act
        var result = await controller.SearchContactsAsync("contact@test.fr");

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("technical failure", problemDetails.Title);
    }

    #endregion

    #region AccountOperations

    /// <summary>
    /// Ensures the payment-information endpoint exposes the expected route and forwards the account identifier.
    /// </summary>
    [Fact]
    public async Task GetPaymentInformationsAsync_ShouldReturnPaymentInformation()
    {
        // Arrange
        var response = CreatePaymentInformationsDataResponse();
        var customerServiceMock = new Mock<IAkuiteoCustomerService>();
        customerServiceMock
            .Setup(service => service.GetPaymentInformationsAsync(AccountId))
            .ReturnsAsync(response);
        var method = typeof(AkuiteoController).GetMethod(nameof(AkuiteoController.GetPaymentInformationsAsync));
        var route = Assert.Single(method!.GetCustomAttributes(typeof(HttpGetAttribute), false).Cast<HttpGetAttribute>());
        var controller = CreateController(akuiteoCustomerService: customerServiceMock.Object);

        // Act
        var result = await controller.GetPaymentInformationsAsync(AccountId);

        // Assert
        Assert.Equal("account/{accountId:int:min(1)}/payment-informations", route.Template);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, okResult.Value);
        var payload = SerializeRegistryResponse(okResult.Value);
        Assert.Null(payload["meta"]);
        Assert.Null(payload["data"]);
        Assert.Equal("DIRECT_DEBIT", payload["methodOfPayment"]![0]!.Value<string>());
        customerServiceMock.Verify(service => service.GetPaymentInformationsAsync(AccountId), Times.Once);
    }

    /// <summary>
    /// Ensures an account without payment preferences is serialized as an empty Registry response object.
    /// </summary>
    [Fact]
    public async Task GetPaymentInformationsAsync_WhenNoPreferenceIsConfigured_ShouldSerializeEmptyObject()
    {
        // Arrange
        var response = new AkuiteoPaymentInformationsDataResponse();
        var customerServiceMock = new Mock<IAkuiteoCustomerService>();
        customerServiceMock
            .Setup(service => service.GetPaymentInformationsAsync(AccountId))
            .ReturnsAsync(response);
        var controller = CreateController(akuiteoCustomerService: customerServiceMock.Object);

        // Act
        var result = await controller.GetPaymentInformationsAsync(AccountId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = SerializeRegistryResponse(okResult.Value);
        Assert.Empty(payload.Properties());
    }

    /// <summary>
    /// Ensures payment-information downstream failures use the shared Akuiteo conflict mapping.
    /// </summary>
    [Fact]
    public async Task GetPaymentInformationsAsync_WhenServiceFails_ShouldReturnConflict()
    {
        // Arrange
        var customerServiceMock = new Mock<IAkuiteoCustomerService>();
        customerServiceMock
            .Setup(service => service.GetPaymentInformationsAsync(AccountId))
            .ThrowsAsync(new AkuiteoAccountOperationTechnicalException("payment lookup failed"));
        var controller = CreateController(akuiteoCustomerService: customerServiceMock.Object);

        // Act
        var result = await controller.GetPaymentInformationsAsync(AccountId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("payment lookup failed", problemDetails.Title);
    }

    /// <summary>
    /// Ensures an unknown Registry account returns not found for payment information.
    /// </summary>
    [Fact]
    public async Task GetPaymentInformationsAsync_WhenAccountDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var customerServiceMock = new Mock<IAkuiteoCustomerService>();
        customerServiceMock
            .Setup(service => service.GetPaymentInformationsAsync(AccountId))
            .ThrowsAsync(new KeyNotFoundException());
        var controller = CreateController(akuiteoCustomerService: customerServiceMock.Object);

        // Act
        var result = await controller.GetPaymentInformationsAsync(AccountId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    /// <summary>
    /// Ensures each supported banking action is forwarded to the account service.
    /// </summary>
    /// <param name="action">The supported Akuiteo action.</param>
    [Theory]
    [InlineData("ADD")]
    [InlineData("UPDATE")]
    [InlineData("REMOVE")]
    public async Task UpdateBankingInformationsAsync_WithSupportedAction_ShouldForwardRequest(string action)
    {
        // Arrange
        var request = new[] { CreateBankingRequest(action) };
        var response = CreateAccountOperationResponse();
        var customerServiceMock = new Mock<IAkuiteoCustomerService>();
        customerServiceMock
            .Setup(service => service.UpdateBankingInformationsAsync(AccountId, request))
            .ReturnsAsync(response);
        var controller = CreateController(akuiteoCustomerService: customerServiceMock.Object);

        // Act
        var result = await controller.UpdateBankingInformationsAsync(AccountId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, okResult.Value);
        customerServiceMock.Verify(
            service => service.UpdateBankingInformationsAsync(AccountId, request),
            Times.Once);
    }

    /// <summary>
    /// Ensures the banking endpoint exposes the expected POST route.
    /// </summary>
    [Fact]
    public void UpdateBankingInformationsAsync_ShouldExposeExpectedRoute()
    {
        // Arrange
        var method = typeof(AkuiteoController).GetMethod(nameof(AkuiteoController.UpdateBankingInformationsAsync));

        // Act
        var route = Assert.Single(method!.GetCustomAttributes(typeof(HttpPostAttribute), false).Cast<HttpPostAttribute>());

        // Assert
        Assert.Equal("account/{accountId:int:min(1)}/banking-informations", route.Template);
    }

    /// <summary>
    /// Ensures a banking downstream failure is mapped to the standard conflict response.
    /// </summary>
    [Fact]
    public async Task UpdateBankingInformationsAsync_WhenServiceFails_ShouldReturnConflict()
    {
        // Arrange
        var request = new[] { CreateBankingRequest("ADD") };
        var customerServiceMock = new Mock<IAkuiteoCustomerService>();
        customerServiceMock
            .Setup(service => service.UpdateBankingInformationsAsync(AccountId, request))
            .ThrowsAsync(new AkuiteoAccountOperationTechnicalException("downstream failure"));
        var controller = CreateController(akuiteoCustomerService: customerServiceMock.Object);

        // Act
        var result = await controller.UpdateBankingInformationsAsync(AccountId, request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("downstream failure", problemDetails.Title);
    }

    /// <summary>
    /// Ensures an unknown Registry account returns not found for banking updates.
    /// </summary>
    [Fact]
    public async Task UpdateBankingInformationsAsync_WhenAccountDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var request = new[] { CreateBankingRequest("ADD") };
        var customerServiceMock = new Mock<IAkuiteoCustomerService>();
        customerServiceMock
            .Setup(service => service.UpdateBankingInformationsAsync(AccountId, request))
            .ThrowsAsync(new KeyNotFoundException());
        var controller = CreateController(akuiteoCustomerService: customerServiceMock.Object);

        // Act
        var result = await controller.UpdateBankingInformationsAsync(AccountId, request);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    /// <summary>
    /// Ensures generic nested JSON, arrays, explicit nulls, and omitted fields are forwarded unchanged.
    /// </summary>
    [Fact]
    public async Task PatchAccountAsync_ShouldPreserveGenericJson()
    {
        // Arrange
        var request = JObject.Parse(
            """{"conditionOfPayment":{"deadLine":"030","day":29},"items":[1,true,{"value":null}],"explicitNull":null}""");
        JObject? capturedRequest = null;
        var response = CreateAccountOperationResponse();
        var customerServiceMock = new Mock<IAkuiteoCustomerService>();
        customerServiceMock
            .Setup(service => service.PatchAccountAsync(AccountId, It.IsAny<JObject>()))
            .Callback<int, JObject>((_, payload) => capturedRequest = payload)
            .ReturnsAsync(response);
        var controller = CreateController(akuiteoCustomerService: customerServiceMock.Object);

        // Act
        var result = await controller.PatchAccountAsync(AccountId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, okResult.Value);
        Assert.NotNull(capturedRequest);
        Assert.Equal(request.ToString(Formatting.None), capturedRequest!.ToString(Formatting.None));
        Assert.False(capturedRequest.TryGetValue("methodOfPayment", out _));
        Assert.Equal(JTokenType.Null, capturedRequest["explicitNull"]!.Type);
        Assert.Equal(JTokenType.Array, capturedRequest["items"]!.Type);
        customerServiceMock.Verify(
            service => service.PatchAccountAsync(AccountId, It.IsAny<JObject>()),
            Times.Once);
    }

    /// <summary>
    /// Ensures the generic account endpoint exposes the expected PATCH route.
    /// </summary>
    [Fact]
    public void PatchAccountAsync_ShouldExposeExpectedRoute()
    {
        // Arrange
        var method = typeof(AkuiteoController).GetMethod(nameof(AkuiteoController.PatchAccountAsync));

        // Act
        var route = Assert.Single(method!.GetCustomAttributes(typeof(HttpPatchAttribute), false).Cast<HttpPatchAttribute>());

        // Assert
        Assert.Equal("account/{accountId:int:min(1)}", route.Template);
    }

    /// <summary>
    /// Ensures a generic patch downstream failure is mapped to the standard conflict response.
    /// </summary>
    [Fact]
    public async Task PatchAccountAsync_WhenServiceFails_ShouldReturnConflict()
    {
        // Arrange
        var request = JObject.Parse("""{"methodOfPayment":"OTHER"}""");
        var customerServiceMock = new Mock<IAkuiteoCustomerService>();
        customerServiceMock
            .Setup(service => service.PatchAccountAsync(AccountId, It.IsAny<JObject>()))
            .ThrowsAsync(new AkuiteoAccountOperationTechnicalException("downstream failure"));
        var controller = CreateController(akuiteoCustomerService: customerServiceMock.Object);

        // Act
        var result = await controller.PatchAccountAsync(AccountId, request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
    }

    /// <summary>
    /// Ensures an unknown Registry account returns not found for generic updates.
    /// </summary>
    [Fact]
    public async Task PatchAccountAsync_WhenAccountDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var request = JObject.Parse("{}");
        var customerServiceMock = new Mock<IAkuiteoCustomerService>();
        customerServiceMock
            .Setup(service => service.PatchAccountAsync(AccountId, request))
            .ThrowsAsync(new KeyNotFoundException());
        var controller = CreateController(akuiteoCustomerService: customerServiceMock.Object);

        // Act
        var result = await controller.PatchAccountAsync(AccountId, request);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    /// Creates an Akuiteo controller with optional service overrides.
    /// </summary>
    /// <param name="akuiteoCustomerService">The Akuiteo customer service.</param>
    /// <param name="akuiteoContactService">The Akuiteo contact service.</param>
    /// <returns>The configured controller.</returns>
    private static AkuiteoController CreateController(
        IAkuiteoCustomerService? akuiteoCustomerService = null,
        IAkuiteoContactService? akuiteoContactService = null)
    {
        return new AkuiteoController(
            akuiteoCustomerService ?? Mock.Of<IAkuiteoCustomerService>(),
            akuiteoContactService ?? Mock.Of<IAkuiteoContactService>());
    }

    /// <summary>
    /// Creates a valid Akuiteo request.
    /// </summary>
    /// <returns>A valid request instance.</returns>
    private static AkuiteoCustomerCreationRequest CreateRequest()
    {
        return new AkuiteoCustomerCreationRequest
        {
            LegalName = "Boulangerie du coin",
            Siret = "72200393604516",
            Siren = "722003936",
            LegalStructure = "PersonneMorale",
            LegalForm = "SAS",
            NafCode = "10.71C",
            Address = "221B Baker Street",
            ZipCode = "69002",
            City = "Lyon",
            DepartmentCode = "69",
            RegionCode = "84",
            CountryCode = "FR",
            CaseManagerContactId = 10,
            AccountManagerContactId = 20
        };
    }

    /// <summary>
    /// Creates a valid Akuiteo contact request.
    /// </summary>
    /// <returns>A valid contact request instance.</returns>
    private static AkuiteoContactCreationRequest CreateContactRequest()
    {
        return new AkuiteoContactCreationRequest
        {
            AccountNumber = "9000120123",
            Title = "M",
            LastName = "Jean",
            FirstName = "Dupont",
            JobTitle = "Gérant",
            ContactDepartment = "Direction",
            CompanyRole = "Président",
            ContactTypes = new AkuiteoContactTypesRequest
            {
                IsDigitalVaultContact = true,
                IsDebtCollectionContact = false,
                IsMandateSignatory = true
            },
            Email = "o.dbira@boulangerie.fr",
            MobilePhone = "06 12 34 56 78"
        };
    }

    /// <summary>
    /// Creates a banking-information request for the specified action.
    /// </summary>
    /// <param name="action">The Akuiteo banking action.</param>
    /// <returns>The banking-information request.</returns>
    private static AkuiteoBankingInformationRequest CreateBankingRequest(string action)
    {
        return new AkuiteoBankingInformationRequest
        {
            Action = action,
            Sepa = new AkuiteoSepaRequest
            {
                BankDetails = new AkuiteoBankDetailsRequest { Entity = "30006" },
                Bic = new AkuiteoBicRequest { Country = "FR" },
                Iban = new AkuiteoIbanRequest { Country = "FR" }
            },
            StatusChangeDate = "2024-05-01T09:00:00.000+0000",
            StatusChangeArgument = new AkuiteoStatusChangeArgumentRequest
            {
                Comment = "TEST_CA",
                Status = "VALIDATED"
            }
        };
    }

    /// <summary>
    /// Creates a successful Akuiteo account operation response.
    /// </summary>
    /// <returns>The successful response.</returns>
    private static AkuiteoAccountOperationResponse CreateAccountOperationResponse()
    {
        return new AkuiteoAccountOperationResponse
        {
            Meta = new AkuiteoMetaResponse
            {
                Status = "succeeded",
                Messages = Array.Empty<AkuiteoMessageResponse>()
            }
        };
    }

    /// <summary>
    /// Creates a successful payment-information response.
    /// </summary>
    /// <returns>The payment-information response.</returns>
    private static AkuiteoPaymentInformationsDataResponse CreatePaymentInformationsDataResponse()
    {
        return new AkuiteoPaymentInformationsDataResponse
        {
            ConditionOfPayment = Array.Empty<AkuiteoConditionOfPaymentResponse>(),
            MethodOfPayment = new[] { "DIRECT_DEBIT" },
            BankingInformations = Array.Empty<IEnumerable<AkuiteoBankingInformationResponse>>()
        };
    }

    /// <summary>
    /// Serializes a controller value with the Registry Newtonsoft JSON response settings.
    /// </summary>
    /// <param name="value">The controller response value.</param>
    /// <returns>The serialized JSON object.</returns>
    private static JObject SerializeRegistryResponse(object? value)
    {
        return JObject.Parse(JsonConvert.SerializeObject(
            value,
            new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
                NullValueHandling = NullValueHandling.Ignore,
                DateParseHandling = DateParseHandling.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            }));
    }
}

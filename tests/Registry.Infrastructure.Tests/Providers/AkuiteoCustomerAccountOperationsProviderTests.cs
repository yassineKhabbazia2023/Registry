using System.Net;
using System.Text.Json;
using Application.Exceptions;
using Application.Models.Results;
using Application.Providers;
using Application.Requests;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Registry.Infrastructure.Tests.Providers;

/// <summary>
/// Tests Akuiteo customer account operations on <see cref="AkuiteoCustomerProvider"/>.
/// </summary>
public class AkuiteoCustomerAccountOperationsProviderTests
{
    #region AccountOperations

    /// <summary>
    /// Ensures the provider sends the required empty JSON body and maps the complete payment-information response.
    /// </summary>
    [Fact]
    public async Task GetPaymentInformationsAsync_WhenSuccessful_ShouldGetAndMapExpectedResponse()
    {
        // Arrange
        var capture = new RequestCapture();
        var provider = CreateProvider(HttpStatusCode.OK, PaymentInformationsResponse, capture);

        // Act
        var result = await provider.GetPaymentInformationsAsync("9010001695");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Get, capture.Method);
        Assert.Equal(new Uri("https://api.akuiteo.local/akuiteo/account/9010001695/payment-informations"), capture.Uri);
        Assert.Equal("{}", capture.Body);
        Assert.Equal("application/json", capture.ContentType);

        var data = Assert.IsType<AkuiteoPaymentInformationsDataResponse>(result.Response!.Data);
        var condition = Assert.Single(data.ConditionOfPayment!);
        Assert.Equal(" le 0 par Prélèvement", condition.Code);
        Assert.Null(condition.DeadLine);
        Assert.Null(condition.Term);
        Assert.Equal(0, condition.Day);
        Assert.Null(condition.ReferenceOnBankStatement);
        Assert.Equal("DIRECT_DEBIT", Assert.Single(data.MethodOfPayment!));

        var bankingGroup = Assert.Single(data.BankingInformations!);
        var banking = Assert.Single(bankingGroup);
        Assert.Equal("500257755", banking.Id);
        Assert.Equal(JTokenType.Null, banking.NoneSepa!.Type);
        Assert.Null(banking.StatusChangeDate);
        Assert.Null(banking.StatusChangeArgument!.Comment);
        Assert.Equal("VALIDATED", banking.StatusChangeArgument.Status);
        Assert.Equal(JTokenType.Null, banking.Validator!.Type);
        Assert.Null(banking.ValidatorId);
        Assert.Null(banking.Action);
        Assert.Equal("SEPA", banking.Type);
        Assert.Equal("30006", banking.Sepa!.BankDetails!.Entity);
        Assert.Equal("00001", banking.Sepa.BankDetails.Counter);
        Assert.Equal("12345678901", banking.Sepa.BankDetails.AccountNumber);
        Assert.Equal("89", banking.Sepa.BankDetails.Key);
        Assert.Equal("Crédit Agricole", banking.Sepa.BankDetails.Domiciliation);
        Assert.Equal("FR", banking.Sepa.Bic!.Country);
        Assert.Equal("AGRI", banking.Sepa.Bic.Bank);
        Assert.Equal("FR", banking.Sepa.Bic.Location);
        Assert.Equal("PP", banking.Sepa.Bic.Branch);
        Assert.Equal("FR", banking.Sepa.Iban!.Country);
        Assert.Equal("76", banking.Sepa.Iban.Key);
        Assert.Equal("30006000011234567890189", banking.Sepa.Iban.AccountNumber);

        var registryPayload = JObject.Parse(JsonConvert.SerializeObject(
            result.Response,
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            }));
        var serializedCondition = registryPayload["data"]!["conditionOfPayment"]![0]!;
        Assert.Equal(JTokenType.Null, serializedCondition["deadLine"]!.Type);
        Assert.Equal(JTokenType.Null, serializedCondition["term"]!.Type);
        Assert.Equal(JTokenType.Null, serializedCondition["referenceOnBankStatement"]!.Type);
        var serializedBanking = registryPayload["data"]!["bankingInformations"]![0]![0]!;
        Assert.Equal(JTokenType.Null, serializedBanking["noneSepa"]!.Type);
        Assert.Equal(JTokenType.Null, serializedBanking["statusChangeDate"]!.Type);
        Assert.Equal(JTokenType.Null, serializedBanking["statusChangeArgument"]!["comment"]!.Type);
        Assert.Equal(JTokenType.Null, serializedBanking["validator"]!.Type);
        Assert.Equal(JTokenType.Null, serializedBanking["validatorId"]!.Type);
        Assert.Equal(JTokenType.Null, serializedBanking["action"]!.Type);
    }

    /// <summary>
    /// Ensures Akuiteo's default no-preference representation is exposed as empty payment data.
    /// </summary>
    [Fact]
    public async Task GetPaymentInformationsAsync_WhenNoPaymentPreferenceIsConfigured_ShouldReturnEmptyData()
    {
        // Arrange
        var provider = CreateProvider(HttpStatusCode.OK, NoPaymentPreferenceResponse, new RequestCapture());

        // Act
        var result = await provider.GetPaymentInformationsAsync("9010001695");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("succeeded", result.Response!.Meta!.Status);
        var data = Assert.IsType<AkuiteoPaymentInformationsDataResponse>(result.Response.Data);
        Assert.Null(data.ConditionOfPayment);
        Assert.Null(data.MethodOfPayment);
        Assert.Null(data.BankingInformations);

        var registryPayload = JObject.Parse(JsonConvert.SerializeObject(
            result.Response,
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            }));
        Assert.Empty(Assert.IsType<JObject>(registryPayload["data"]!).Properties());
    }

    /// <summary>
    /// Ensures responses that differ from the complete no-preference signature are not normalized.
    /// </summary>
    /// <param name="caseName">The near-match case description.</param>
    /// <param name="responseBody">The Akuiteo response body.</param>
    [Theory]
    [MemberData(nameof(NoPaymentPreferenceNearMatches))]
    public async Task GetPaymentInformationsAsync_WhenNoPaymentPreferenceSignatureDiffers_ShouldPreserveData(
        string caseName,
        string responseBody)
    {
        // Arrange
        var provider = CreateProvider(HttpStatusCode.OK, responseBody, new RequestCapture());

        // Act
        var result = await provider.GetPaymentInformationsAsync("9010001695");

        // Assert
        Assert.True(result.IsSuccess);
        var data = Assert.IsType<AkuiteoPaymentInformationsDataResponse>(result.Response!.Data);
        Assert.False(
            data.ConditionOfPayment is null
                && data.MethodOfPayment is null
                && data.BankingInformations is null,
            caseName);
    }

    /// <summary>
    /// Ensures payment-information HTTP errors retain the readable Akuiteo message.
    /// </summary>
    [Fact]
    public async Task GetPaymentInformationsAsync_WhenDownstreamFails_ShouldReturnFailure()
    {
        // Arrange
        var provider = CreateProvider(
            HttpStatusCode.BadRequest,
            """{"meta":{"status":"failed","messages":[{"code":"BAD_REQUEST","level":"error","text":"payment lookup failed"}]}}""",
            new RequestCapture());

        // Act
        var result = await provider.GetPaymentInformationsAsync("9010001695");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("payment lookup failed", result.ErrorMessage);
    }

    /// <summary>
    /// Ensures malformed payment-information responses preserve the Newtonsoft JSON exception.
    /// </summary>
    [Fact]
    public async Task GetPaymentInformationsAsync_WhenResponseIsMalformed_ShouldThrowTechnicalException()
    {
        // Arrange
        var provider = CreateProvider(HttpStatusCode.OK, "not-json", new RequestCapture());

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoResponseDeserializationTechnicalException>(
            () => provider.GetPaymentInformationsAsync("9010001695"));

        // Assert
        Assert.Equal("Akuiteo returned an invalid response.", exception.Message);
        Assert.IsAssignableFrom<Newtonsoft.Json.JsonException>(exception.InnerException);
    }

    /// <summary>
    /// Ensures a succeeded payment response without data is treated as a failure.
    /// </summary>
    [Fact]
    public async Task GetPaymentInformationsAsync_WhenDataIsMissing_ShouldReturnFailure()
    {
        // Arrange
        var provider = CreateProvider(HttpStatusCode.OK, SuccessResponse, new RequestCapture());

        // Act
        var result = await provider.GetPaymentInformationsAsync("9010001695");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Akuiteo returned empty payment information.", result.ErrorMessage);
    }

    /// <summary>
    /// Ensures the provider posts the complete banking-information collection to the expected account path.
    /// </summary>
    [Fact]
    public async Task UpdateBankingInformationsAsync_WhenSuccessful_ShouldPostExpectedRequest()
    {
        // Arrange
        var capture = new RequestCapture();
        var provider = CreateProvider(HttpStatusCode.OK, SuccessResponse, capture);
        var request = new[] { CreateBankingRequest() };

        // Act
        var result = await provider.UpdateBankingInformationsAsync("9010001695", request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, capture.Method);
        Assert.Equal(new Uri("https://api.akuiteo.local/akuiteo/account/9010001695/banking-informations"), capture.Uri);
        Assert.Equal("rate-client-id", capture.ClientId);
        Assert.Equal("rate-client-secret", capture.ClientSecret);

        using var json = JsonDocument.Parse(capture.Body!);
        var item = Assert.Single(json.RootElement.EnumerateArray());
        Assert.Equal("ADD", item.GetProperty("action").GetString());
        Assert.Equal("2024-05-01T09:00:00.000+0000", item.GetProperty("statusChangeDate").GetString());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("noneSepa").ValueKind);
        Assert.Equal("TEST_CA", item.GetProperty("statusChangeArgument").GetProperty("comment").GetString());
        Assert.Equal("VALIDATED", item.GetProperty("statusChangeArgument").GetProperty("status").GetString());

        var sepa = item.GetProperty("sepa");
        var bankDetails = sepa.GetProperty("bankDetails");
        Assert.Equal("30006", bankDetails.GetProperty("entity").GetString());
        Assert.Equal("00001", bankDetails.GetProperty("counter").GetString());
        Assert.Equal("12345678901", bankDetails.GetProperty("accountNumber").GetString());
        Assert.Equal("89", bankDetails.GetProperty("key").GetString());
        Assert.Equal("Crédit Agricole", bankDetails.GetProperty("domiciliation").GetString());

        var bic = sepa.GetProperty("bic");
        Assert.Equal("FR", bic.GetProperty("country").GetString());
        Assert.Equal("AGRI", bic.GetProperty("bank").GetString());
        Assert.Equal("FR", bic.GetProperty("location").GetString());
        Assert.Equal("PP", bic.GetProperty("branch").GetString());

        var iban = sepa.GetProperty("iban");
        Assert.Equal("FR", iban.GetProperty("country").GetString());
        Assert.Equal("76", iban.GetProperty("key").GetString());
        Assert.Equal("30006000011234567890189", iban.GetProperty("accountNumber").GetString());
    }

    /// <summary>
    /// Ensures the provider patches the account path without changing generic JSON semantics.
    /// </summary>
    [Fact]
    public async Task PatchAccountAsync_WhenSuccessful_ShouldPreserveGenericPayload()
    {
        // Arrange
        const string payload = """{"conditionOfPayment":{"deadLine":"030","term":"1","day":29,"referenceOnBankStatement":"AMOBILIS"},"items":[1,true],"explicitNull":null}""";
        var request = JObject.Parse(payload);
        var capture = new RequestCapture();
        var provider = CreateProvider(HttpStatusCode.OK, SuccessResponse, capture);

        // Act
        var result = await provider.PatchAccountAsync("9010001710", request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Patch, capture.Method);
        Assert.Equal(new Uri("https://api.akuiteo.local/akuiteo/account/9010001710"), capture.Uri);
        Assert.Equal("application/json", capture.ContentType);
        using var capturedJson = JsonDocument.Parse(capture.Body!);
        Assert.Equal(payload, capturedJson.RootElement.GetRawText());
        Assert.False(capturedJson.RootElement.TryGetProperty("methodOfPayment", out _));
        Assert.Equal(JsonValueKind.Null, capturedJson.RootElement.GetProperty("explicitNull").ValueKind);
    }

    /// <summary>
    /// Ensures downstream HTTP failures retain their status and readable response message.
    /// </summary>
    [Fact]
    public async Task PatchAccountAsync_WhenDownstreamFails_ShouldReturnFailure()
    {
        // Arrange
        var request = JObject.Parse("{}");
        const string errorMessage = "HTTP PATCH on resource 'https://akuiteo.local/api/v1/accounts/500176270' failed: bad request (400).";
        var provider = CreateProvider(
            HttpStatusCode.BadRequest,
            JsonConvert.SerializeObject(new
            {
                meta = new
                {
                    status = "failed",
                    messages = new[]
                    {
                        new
                        {
                            timestamp = "2026-07-21T09:25:02.067+0000",
                            code = "BAD_REQUEST",
                            level = "error",
                            text = errorMessage
                        }
                    }
                }
            }),
            new RequestCapture());

        // Act
        var result = await provider.PatchAccountAsync("9010001710", request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal(errorMessage, result.ErrorMessage);
    }

    /// <summary>
    /// Ensures a successful HTTP response with failed Akuiteo metadata is treated as a failure.
    /// </summary>
    [Fact]
    public async Task PatchAccountAsync_WhenMetaFails_ShouldReturnFailure()
    {
        // Arrange
        var request = JObject.Parse("{}");
        var provider = CreateProvider(
            HttpStatusCode.OK,
            """{"meta":{"status":"failed","messages":[{"text":"invalid account update"}]}}""",
            new RequestCapture());

        // Act
        var result = await provider.PatchAccountAsync("9010001710", request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("invalid account update", result.ErrorMessage);
    }

    /// <summary>
    /// Ensures malformed account-operation responses preserve the System.Text.Json exception.
    /// </summary>
    [Fact]
    public async Task PatchAccountAsync_WhenResponseIsMalformed_ShouldThrowTechnicalException()
    {
        // Arrange
        var request = JObject.Parse("{}");
        var provider = CreateProvider(HttpStatusCode.OK, "not-json", new RequestCapture());

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoResponseDeserializationTechnicalException>(
            () => provider.PatchAccountAsync("9010001710", request));

        // Assert
        Assert.Equal("Akuiteo returned an invalid response.", exception.Message);
        Assert.IsType<System.Text.Json.JsonException>(exception.InnerException);
    }

    /// <summary>
    /// Ensures an empty account-operation response remains a normalized downstream failure.
    /// </summary>
    [Fact]
    public async Task PatchAccountAsync_WhenResponseIsEmpty_ShouldReturnFailure()
    {
        // Arrange
        var request = JObject.Parse("{}");
        var provider = CreateProvider(HttpStatusCode.OK, string.Empty, new RequestCapture());

        // Act
        var result = await provider.PatchAccountAsync("9010001710", request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Akuiteo returned an empty response.", result.ErrorMessage);
    }

    #endregion

    private const string SuccessResponse = """{"meta":{"status":"succeeded","messages":[]}}""";

    private const string PaymentInformationsResponse = """
        {
          "meta":{"status":"succeeded","messages":[]},
          "data":{
            "conditionOfPayment":[{"code":" le 0 par Prélèvement","deadLine":null,"term":null,"day":0,"referenceOnBankStatement":null}],
            "methodOfPayment":["DIRECT_DEBIT"],
            "bankingInformations":[[{"id":"500257755","sepa":{"bankDetails":{"entity":"30006","counter":"00001","accountNumber":"12345678901","key":"89","domiciliation":"Crédit Agricole"},"bic":{"country":"FR","bank":"AGRI","location":"FR","branch":"PP"},"iban":{"country":"FR","key":"76","accountNumber":"30006000011234567890189"}},"noneSepa":null,"statusChangeDate":null,"statusChangeArgument":{"comment":null,"status":"VALIDATED"},"validator":null,"validatorId":null,"action":null,"type":"SEPA"}]]
          }
        }
        """;

    private const string NoPaymentPreferenceResponse = """
        {
          "meta":{"status":"succeeded","messages":[]},
          "data":{
            "conditionOfPayment":[{"code":" le 0","deadLine":null,"term":null,"day":0,"referenceOnBankStatement":null}],
            "methodOfPayment":[null],
            "bankingInformations":[[]]
          }
        }
        """;

    /// <summary>
    /// Gets responses that each differ from one part of Akuiteo's no-preference signature.
    /// </summary>
    /// <returns>The near-match case name and response body.</returns>
    public static IEnumerable<object[]> NoPaymentPreferenceNearMatches()
    {
        yield return CreateNoPaymentPreferenceNearMatch(
            "condition count",
            response => response["data"]!["conditionOfPayment"] = new JArray());
        yield return CreateNoPaymentPreferenceNearMatch(
            "condition code",
            response => response["data"]!["conditionOfPayment"]![0]!["code"] = " le 1");
        yield return CreateNoPaymentPreferenceNearMatch(
            "deadline",
            response => response["data"]!["conditionOfPayment"]![0]!["deadLine"] = "030");
        yield return CreateNoPaymentPreferenceNearMatch(
            "term",
            response => response["data"]!["conditionOfPayment"]![0]!["term"] = "1");
        yield return CreateNoPaymentPreferenceNearMatch(
            "day",
            response => response["data"]!["conditionOfPayment"]![0]!["day"] = 1);
        yield return CreateNoPaymentPreferenceNearMatch(
            "bank statement reference",
            response => response["data"]!["conditionOfPayment"]![0]!["referenceOnBankStatement"] = "REFERENCE");
        yield return CreateNoPaymentPreferenceNearMatch(
            "payment method count",
            response => response["data"]!["methodOfPayment"] = new JArray());
        yield return CreateNoPaymentPreferenceNearMatch(
            "payment method",
            response => response["data"]!["methodOfPayment"]![0] = "DIRECT_DEBIT");
        yield return CreateNoPaymentPreferenceNearMatch(
            "banking group count",
            response => response["data"]!["bankingInformations"] = new JArray());
        yield return CreateNoPaymentPreferenceNearMatch(
            "null banking group",
            response => response["data"]!["bankingInformations"]![0] = JValue.CreateNull());
        yield return CreateNoPaymentPreferenceNearMatch(
            "banking information",
            response => response["data"]!["bankingInformations"]![0] = new JArray(new JObject()));
    }

    /// <summary>
    /// Creates one response derived from the no-preference payload.
    /// </summary>
    /// <param name="caseName">The case description.</param>
    /// <param name="mutate">The response mutation.</param>
    /// <returns>The case name and mutated response body.</returns>
    private static object[] CreateNoPaymentPreferenceNearMatch(string caseName, Action<JObject> mutate)
    {
        var response = JObject.Parse(NoPaymentPreferenceResponse);
        mutate(response);
        return new object[] { caseName, response.ToString(Formatting.None) };
    }

    /// <summary>
    /// Creates a provider backed by a capturing HTTP handler.
    /// </summary>
    /// <param name="statusCode">The downstream status code.</param>
    /// <param name="responseBody">The downstream response body.</param>
    /// <param name="capture">The request capture.</param>
    /// <returns>The provider.</returns>
    private static AkuiteoCustomerProvider CreateProvider(
        HttpStatusCode statusCode,
        string responseBody,
        RequestCapture capture)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capture.Read(request))
            .ReturnsAsync(new HttpResponseMessage(statusCode) { Content = new StringContent(responseBody) });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.akuiteo.local/")
        };
        httpClient.DefaultRequestHeaders.Add("X-Client-Id", "rate-client-id");
        httpClient.DefaultRequestHeaders.Add("X-Client-Secret", "rate-client-secret");
        return new AkuiteoCustomerProvider(httpClient);
    }

    /// <summary>
    /// Creates the complete supported SEPA banking-information request.
    /// </summary>
    /// <returns>The request.</returns>
    private static AkuiteoBankingInformationRequest CreateBankingRequest()
    {
        return new AkuiteoBankingInformationRequest
        {
            Sepa = new AkuiteoSepaRequest
            {
                BankDetails = new AkuiteoBankDetailsRequest
                {
                    Entity = "30006",
                    Counter = "00001",
                    AccountNumber = "12345678901",
                    Key = "89",
                    Domiciliation = "Crédit Agricole"
                },
                Bic = new AkuiteoBicRequest
                {
                    Country = "FR",
                    Bank = "AGRI",
                    Location = "FR",
                    Branch = "PP"
                },
                Iban = new AkuiteoIbanRequest
                {
                    Country = "FR",
                    Key = "76",
                    AccountNumber = "30006000011234567890189"
                }
            },
            NoneSepa = null,
            StatusChangeDate = "2024-05-01T09:00:00.000+0000",
            StatusChangeArgument = new AkuiteoStatusChangeArgumentRequest
            {
                Comment = "TEST_CA",
                Status = "VALIDATED"
            },
            Action = "ADD"
        };
    }

    /// <summary>
    /// Captures non-sensitive request metadata and the request body for assertions.
    /// </summary>
    private sealed class RequestCapture
    {
        /// <summary>Gets the captured HTTP method.</summary>
        public HttpMethod? Method { get; private set; }

        /// <summary>Gets the captured request URI.</summary>
        public Uri? Uri { get; private set; }

        /// <summary>Gets the captured body.</summary>
        public string? Body { get; private set; }

        /// <summary>Gets the captured content type.</summary>
        public string? ContentType { get; private set; }

        /// <summary>Gets the configured rate-limit client identifier.</summary>
        public string? ClientId { get; private set; }

        /// <summary>Gets the configured rate-limit client secret used only in this isolated test.</summary>
        public string? ClientSecret { get; private set; }

        /// <summary>
        /// Reads the outgoing request before it is disposed.
        /// </summary>
        /// <param name="request">The outgoing request.</param>
        public void Read(HttpRequestMessage request)
        {
            Method = request.Method;
            Uri = request.RequestUri;
            Body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            ContentType = request.Content?.Headers.ContentType?.MediaType;
            ClientId = request.Headers.GetValues("X-Client-Id").Single();
            ClientSecret = request.Headers.GetValues("X-Client-Secret").Single();
        }
    }
}

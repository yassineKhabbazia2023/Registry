using Application.Interfaces;
using Application.Models.Results;
using Application.Options;
using Application.Requests;
using Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Pulse.Registry.Domain.Entities;

namespace Registry.Application.Tests.Services;

public class HubSpotServiceTest
{
    private readonly Mock<IHubSpotProvider> _providerMock;
    private readonly Mock<IInvoiceDematerializationNotifier> _notifierMock;
    private readonly Mock<IHubSpotFormRepository> _hubSpotFormRepositoryMock;
    private readonly Mock<TimeProvider> _timeProviderMock;
    private readonly Mock<ILogger<HubSpotService>> _loggerMock;
    private readonly IOptions<HubSpotOptions> _options;
    private readonly HubSpotService _service;
    private static readonly DateTimeOffset FixedUtcNow = new(2025, 1, 15, 10, 30, 0, TimeSpan.Zero);

    public HubSpotServiceTest()
    {
        _options = Options.Create(new HubSpotOptions
        {
            BaseUrl = "https://api.hsforms.com",
            PortalId = "143978459",
            FormGuid = "48d828d6-5e31-40ab-afee-416a014879f0"
        });

        _providerMock = new Mock<IHubSpotProvider>();
        _notifierMock = new Mock<IInvoiceDematerializationNotifier>();
        _hubSpotFormRepositoryMock = new Mock<IHubSpotFormRepository>();
        _timeProviderMock = new Mock<TimeProvider>();
        _loggerMock = new Mock<ILogger<HubSpotService>>();
        _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(FixedUtcNow);

        _service = new HubSpotService(
            _providerMock.Object,
            _notifierMock.Object,
            _hubSpotFormRepositoryMock.Object,
            _options,
            _timeProviderMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SubmitIntegrationAsync_Should_CallProvider_WithMappedFields()
    {
        // Arrange
        var request = new HubSpotSubmissionInputRequest
        {
            DematerializationEmail = "facturation@test.fr",
            FirstName = "ehubtest",
            LastName = "elastname",
            VaultEmail = "vault@test.fr",
            RequesterEmail = "requester@test.fr",
        };

        HubSpotSubmissionRequest? captured = null;
        HubSpotFormEntity? capturedForm = null;
        _providerMock
            .Setup(p => p.SubmitIntegrationAsync(_options.Value.PortalId!, _options.Value.FormGuid!, It.IsAny<HubSpotSubmissionRequest>()))
            .Callback<string, string, HubSpotSubmissionRequest>((_, _, payload) => captured = payload)
            .ReturnsAsync(new HubSpotSubmissionResult { IsSuccess = true, StatusCode = 200 });

        _hubSpotFormRepositoryMock
            .Setup(r => r.HasSuccessfulSubmissionAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _hubSpotFormRepositoryMock
            .Setup(r => r.AddSubmissionAsync(It.IsAny<HubSpotFormEntity>()))
            .Callback<HubSpotFormEntity>(entity => capturedForm = entity)
            .Returns(Task.CompletedTask);

        var accountNumber = "ACC-2025-001847";

        // Act
        var result = await _service.SubmitIntegrationAsync(accountNumber, request);

        // Assert
        _providerMock.Verify(p => p.SubmitIntegrationAsync(_options.Value.PortalId!, _options.Value.FormGuid!, It.IsAny<HubSpotSubmissionRequest>()), Times.Once);
        Assert.True(result.IsSuccess);
        Assert.True(result.HubSpotDispatchState);
        Assert.NotNull(captured);
        Assert.NotNull(capturedForm);
        Assert.Equal(accountNumber, capturedForm!.AccountNumber);
        Assert.Equal(request.RequesterEmail, capturedForm.SubmittedBy);
        Assert.Equal(FixedUtcNow.UtcDateTime, capturedForm.SubmittedAt);
        Assert.True(capturedForm.HubSpotDispatchState);
        Assert.Contains("\"requesterEmail\":\"requester@test.fr\"", capturedForm.FormData);
        Assert.Equal(7, captured!.Fields.Count);
        Assert.Contains(captured.Fields, field => field.Name == "code_client" && field.Value == "ACC-2025-001847");
        Assert.Contains(captured.Fields, field => field.Name == "e_mail_de_reception" && field.Value == "facturation@test.fr");
        Assert.Contains(captured.Fields, field => field.Name == "firstname" && field.Value == "ehubtest");
        Assert.Contains(captured.Fields, field => field.Name == "lastname" && field.Value == "elastname");
        Assert.Contains(captured.Fields, field => field.Name == "e_mail_de_connexion" && field.Value == "vault@test.fr");
        Assert.Contains(captured.Fields, field => field.Name == "email" && field.Value == "requester@test.fr");
        var dateField = captured.Fields.First(field => field.Name == "formulaire_facturation_date_demande");
        Assert.Equal("2025-01-15T10:30:00Z", dateField.Value);
    }

    [Fact]
    public async Task SubmitIntegrationAsync_Should_OverrideSubmittedAt_WithServerTime()
    {
        // Arrange
        var request = new HubSpotSubmissionInputRequest
        {
            DematerializationEmail = "facturation@test.fr",
            FirstName = "ehubtest",
            LastName = "elastname",
            VaultEmail = "vault@test.fr",
            RequesterEmail = "requester@test.fr",
            SubmittedAt = FixedUtcNow.UtcDateTime.AddDays(-2)
        };

        _providerMock
            .Setup(p => p.SubmitIntegrationAsync(_options.Value.PortalId!, _options.Value.FormGuid!, It.IsAny<HubSpotSubmissionRequest>()))
            .ReturnsAsync(new HubSpotSubmissionResult { IsSuccess = true, StatusCode = 200 });
        _hubSpotFormRepositoryMock
            .Setup(r => r.HasSuccessfulSubmissionAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        HubSpotFormEntity? capturedForm = null;
        _hubSpotFormRepositoryMock
            .Setup(r => r.AddSubmissionAsync(It.IsAny<HubSpotFormEntity>()))
            .Callback<HubSpotFormEntity>(entity => capturedForm = entity)
            .Returns(Task.CompletedTask);

        var accountNumber = "ACC-2025-001847";

        // Act
        await _service.SubmitIntegrationAsync(accountNumber, request);

        // Assert
        Assert.NotNull(capturedForm);
        Assert.Equal(FixedUtcNow.UtcDateTime, capturedForm!.SubmittedAt);
        Assert.Equal(FixedUtcNow.UtcDateTime, request.SubmittedAt);
    }

    [Fact]
    public async Task SubmitIntegrationAsync_Should_CallNotifier_WhenSubmissionSucceeds()
    {
        // Arrange
        var accountNumber = "ACC-2025-001847";
        var request = new HubSpotSubmissionInputRequest
        {
            DematerializationEmail = "facturation@test.fr",
            FirstName = "Sarah",
            LastName = "TATA",
            VaultEmail = "vault@test.fr",
            RequesterEmail = "sarah.tata@gmail.com"
        };

        _providerMock
            .Setup(p => p.SubmitIntegrationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HubSpotSubmissionRequest>()))
            .ReturnsAsync(new HubSpotSubmissionResult { IsSuccess = true, StatusCode = 200 });
        _hubSpotFormRepositoryMock
            .Setup(r => r.HasSuccessfulSubmissionAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _hubSpotFormRepositoryMock
            .Setup(r => r.AddSubmissionAsync(It.IsAny<HubSpotFormEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SubmitIntegrationAsync(accountNumber, request);

        // Assert
        Assert.True(result.IsSuccess);
        _notifierMock.Verify(n => n.NotifyDematerializationCreatedAsync(accountNumber, request), Times.Once);
        _hubSpotFormRepositoryMock.Verify(r => r.AddSubmissionAsync(It.IsAny<HubSpotFormEntity>()), Times.Once);
    }

    [Fact]
    public async Task SubmitIntegrationAsync_Should_NotCallNotifier_WhenSubmissionFails()
    {
        // Arrange
        var accountNumber = "ACC-2025-001847";
        var request = new HubSpotSubmissionInputRequest
        {
            DematerializationEmail = "facturation@test.fr",
            FirstName = "Sarah",
            LastName = "TATA",
            VaultEmail = "vault@test.fr",
            RequesterEmail = "sarah.tata@gmail.com"
        };

        _providerMock
            .Setup(p => p.SubmitIntegrationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HubSpotSubmissionRequest>()))
            .ReturnsAsync(new HubSpotSubmissionResult { IsSuccess = false, StatusCode = 400, ErrorMessage = "Bad Request" });
        _hubSpotFormRepositoryMock
            .Setup(r => r.HasSuccessfulSubmissionAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        HubSpotFormEntity? capturedForm = null;
        _hubSpotFormRepositoryMock
            .Setup(r => r.AddSubmissionAsync(It.IsAny<HubSpotFormEntity>()))
            .Callback<HubSpotFormEntity>(entity => capturedForm = entity)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SubmitIntegrationAsync(accountNumber, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.HubSpotDispatchState);
        Assert.NotNull(capturedForm);
        Assert.False(capturedForm!.HubSpotDispatchState);
        _notifierMock.Verify(n => n.NotifyDematerializationCreatedAsync(It.IsAny<string>(), It.IsAny<HubSpotSubmissionInputRequest>()), Times.Never);
    }

    [Fact]
    public async Task SubmitIntegrationAsync_Should_Reject_WhenSuccessfulSubmissionExists()
    {
        // Arrange
        var accountNumber = "ACC-2025-001847";
        var request = new HubSpotSubmissionInputRequest
        {
            DematerializationEmail = "facturation@test.fr",
            FirstName = "Sarah",
            LastName = "TATA",
            VaultEmail = "vault@test.fr",
            RequesterEmail = "sarah.tata@gmail.com"
        };

        _hubSpotFormRepositoryMock
            .Setup(r => r.HasSuccessfulSubmissionAsync(accountNumber))
            .ReturnsAsync(true);

        // Act
        var result = await _service.SubmitIntegrationAsync(accountNumber, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(422, result.StatusCode);
        _providerMock.Verify(p => p.SubmitIntegrationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HubSpotSubmissionRequest>()), Times.Never);
        _hubSpotFormRepositoryMock.Verify(r => r.AddSubmissionAsync(It.IsAny<HubSpotFormEntity>()), Times.Never);
    }

    [Fact]
    public async Task SubmitIntegrationAsync_Should_NotFail_WhenHistoryEventThrows()
    {
        // Arrange
        var accountNumber = "ACC-2025-001847";
        var request = new HubSpotSubmissionInputRequest
        {
            DematerializationEmail = "facturation@test.fr",
            FirstName = "Sarah",
            LastName = "TATA",
            VaultEmail = "vault@test.fr",
            RequesterEmail = "sarah.tata@gmail.com"
        };

        _providerMock
            .Setup(p => p.SubmitIntegrationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HubSpotSubmissionRequest>()))
            .ReturnsAsync(new HubSpotSubmissionResult { IsSuccess = true, StatusCode = 200 });
        _hubSpotFormRepositoryMock
            .Setup(r => r.HasSuccessfulSubmissionAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _hubSpotFormRepositoryMock
            .Setup(r => r.AddSubmissionAsync(It.IsAny<HubSpotFormEntity>()))
            .Returns(Task.CompletedTask);
        _notifierMock
            .Setup(n => n.NotifyDematerializationCreatedAsync(accountNumber, request))
            .ThrowsAsync(new InvalidOperationException("History failure"));

        // Act
        var result = await _service.SubmitIntegrationAsync(accountNumber, request);

        // Assert
        Assert.True(result.IsSuccess);
        _hubSpotFormRepositoryMock.Verify(r => r.AddSubmissionAsync(It.IsAny<HubSpotFormEntity>()), Times.Once);
    }

    [Fact]
    public async Task GetSubmissionStateAsync_Should_ReturnOk_WhenSuccessfulSubmissionExists()
    {
        // Arrange
        var accountNumber = "ACC-2025-001847";
        _hubSpotFormRepositoryMock
            .Setup(r => r.HasSuccessfulSubmissionAsync(accountNumber))
            .ReturnsAsync(true);

        // Act
        var result = await _service.GetSubmissionStateAsync(accountNumber);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task GetSubmissionStateAsync_Should_ReturnNotFound_WhenNoSuccessfulSubmission()
    {
        // Arrange
        var accountNumber = "ACC-2025-001847";
        _hubSpotFormRepositoryMock
            .Setup(r => r.HasSuccessfulSubmissionAsync(accountNumber))
            .ReturnsAsync(false);

        // Act
        var result = await _service.GetSubmissionStateAsync(accountNumber);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }
}

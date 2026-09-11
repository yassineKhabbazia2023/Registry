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
    private readonly Mock<IAccountService> _accountServiceMock;
    private readonly Mock<TimeProvider> _timeProviderMock;
    private readonly Mock<ILogger<HubSpotService>> _loggerMock;
    private readonly IOptions<HubSpotOptions> _options;
    private readonly HubSpotService _service;
    private static readonly DateTimeOffset FixedUtcNow = new(2025, 1, 15, 10, 30, 0, TimeSpan.Zero);
    private const int TestAccountId = 12345;
    private const string TestAccountNumber = "ACC-2025-001847";

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
        _accountServiceMock = new Mock<IAccountService>();
        _timeProviderMock = new Mock<TimeProvider>();
        _loggerMock = new Mock<ILogger<HubSpotService>>();
        _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(FixedUtcNow);
        _accountServiceMock.Setup(a => a.GetAccountNumberByIdAsync(TestAccountId)).Returns(Task.FromResult<string?>(TestAccountNumber));

        _service = new HubSpotService(
            _providerMock.Object,
            _notifierMock.Object,
            _hubSpotFormRepositoryMock.Object,
            _accountServiceMock.Object,
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

        // Act
        var result = await _service.SubmitIntegrationAsync(TestAccountId, request);

        // Assert
        _providerMock.Verify(p => p.SubmitIntegrationAsync(_options.Value.PortalId!, _options.Value.FormGuid!, It.IsAny<HubSpotSubmissionRequest>()), Times.Once);
        Assert.True(result.IsSuccess);
        Assert.True(result.HubSpotDispatchState);
        Assert.NotNull(captured);
        Assert.NotNull(capturedForm);
        Assert.Equal(TestAccountNumber, capturedForm!.AccountNumber);
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

        // Act
        await _service.SubmitIntegrationAsync(TestAccountId, request);

        // Assert
        Assert.NotNull(capturedForm);
        Assert.Equal(FixedUtcNow.UtcDateTime, capturedForm!.SubmittedAt);
        Assert.Equal(FixedUtcNow.UtcDateTime, request.SubmittedAt);
    }

    [Fact]
    public async Task SubmitIntegrationAsync_Should_CallNotifier_WhenSubmissionSucceeds()
    {
        // Arrange
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
        var result = await _service.SubmitIntegrationAsync(TestAccountId, request);

        // Assert
        Assert.True(result.IsSuccess);
        _notifierMock.Verify(n => n.NotifyDematerializationCreatedAsync(TestAccountNumber, request), Times.Once);
        _hubSpotFormRepositoryMock.Verify(r => r.AddSubmissionAsync(It.IsAny<HubSpotFormEntity>()), Times.Once);
    }

    [Fact]
    public async Task SubmitIntegrationAsync_Should_NotCallNotifier_WhenSubmissionFails()
    {
        // Arrange
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
        var result = await _service.SubmitIntegrationAsync(TestAccountId, request);

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
        var request = new HubSpotSubmissionInputRequest
        {
            DematerializationEmail = "facturation@test.fr",
            FirstName = "Sarah",
            LastName = "TATA",
            VaultEmail = "vault@test.fr",
            RequesterEmail = "sarah.tata@gmail.com"
        };

        _hubSpotFormRepositoryMock
            .Setup(r => r.HasSuccessfulSubmissionAsync(TestAccountNumber))
            .ReturnsAsync(true);

        // Act
        var result = await _service.SubmitIntegrationAsync(TestAccountId, request);

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
            .Setup(n => n.NotifyDematerializationCreatedAsync(TestAccountNumber, request))
            .ThrowsAsync(new InvalidOperationException("History failure"));

        // Act
        var result = await _service.SubmitIntegrationAsync(TestAccountId, request);

        // Assert
        Assert.True(result.IsSuccess);
        _hubSpotFormRepositoryMock.Verify(r => r.AddSubmissionAsync(It.IsAny<HubSpotFormEntity>()), Times.Once);
    }

    [Fact]
    public async Task GetSubmissionStateAsync_Should_ReturnOk_WhenSuccessfulSubmissionExists()
    {
        // Arrange
        _hubSpotFormRepositoryMock
            .Setup(r => r.HasSuccessfulSubmissionAsync(TestAccountNumber))
            .ReturnsAsync(true);

        // Act
        var result = await _service.GetSubmissionStateAsync(TestAccountId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task GetSubmissionStateAsync_Should_ReturnNotFound_WhenNoSuccessfulSubmission()
    {
        // Arrange
        _hubSpotFormRepositoryMock
            .Setup(r => r.HasSuccessfulSubmissionAsync(TestAccountNumber))
            .ReturnsAsync(false);

        // Act
        var result = await _service.GetSubmissionStateAsync(TestAccountId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task SubmitIntegrationAsync_Should_Throw_WhenAccountIdNotFound()
    {
        // Arrange
        var unknownAccountId = 99999;
        var request = new HubSpotSubmissionInputRequest
        {
            DematerializationEmail = "facturation@test.fr",
            FirstName = "Sarah",
            LastName = "TATA",
            RequesterEmail = "sarah.tata@gmail.com"
        };

        _accountServiceMock
            .Setup(a => a.GetAccountNumberByIdAsync(unknownAccountId))
            .Returns(Task.FromResult<string?>(null));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.SubmitIntegrationAsync(unknownAccountId, request));

        _hubSpotFormRepositoryMock.Verify(r => r.HasSuccessfulSubmissionAsync(It.IsAny<string>()), Times.Never);
        _providerMock.Verify(p => p.SubmitIntegrationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HubSpotSubmissionRequest>()), Times.Never);
    }

    [Fact]
    public async Task GetSubmissionStateAsync_Should_Throw_WhenAccountIdNotFound()
    {
        // Arrange
        var unknownAccountId = 99999;

        _accountServiceMock
            .Setup(a => a.GetAccountNumberByIdAsync(unknownAccountId))
            .Returns(Task.FromResult<string?>(null));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.GetSubmissionStateAsync(unknownAccountId));

        _hubSpotFormRepositoryMock.Verify(r => r.HasSuccessfulSubmissionAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SubmitIntegrationAsync_WithInvalidAccountId_ShouldThrow(int invalidAccountId)
    {
        // Arrange
        var request = new HubSpotSubmissionInputRequest
        {
            DematerializationEmail = "facturation@test.fr",
            FirstName = "Sarah",
            LastName = "TATA",
            RequesterEmail = "sarah.tata@gmail.com"
        };

        _accountServiceMock
            .Setup(a => a.GetAccountNumberByIdAsync(invalidAccountId))
            .Returns(Task.FromResult<string?>(null));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.SubmitIntegrationAsync(invalidAccountId, request));

        _hubSpotFormRepositoryMock.Verify(r => r.HasSuccessfulSubmissionAsync(It.IsAny<string>()), Times.Never);
        _providerMock.Verify(p => p.SubmitIntegrationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HubSpotSubmissionRequest>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetSubmissionStateAsync_WithInvalidAccountId_ShouldThrow(int invalidAccountId)
    {
        // Arrange
        _accountServiceMock
            .Setup(a => a.GetAccountNumberByIdAsync(invalidAccountId))
            .Returns(Task.FromResult<string?>(null));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.GetSubmissionStateAsync(invalidAccountId));

        _hubSpotFormRepositoryMock.Verify(r => r.HasSuccessfulSubmissionAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResetSubmissionsAsync_Should_DeleteSubmissions_AndReturnSuccess()
    {
        // Arrange
        _hubSpotFormRepositoryMock
            .Setup(r => r.DeleteSubmissionsAsync(TestAccountNumber))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ResetSubmissionsAsync(TestAccountId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(204, result.StatusCode);
        _hubSpotFormRepositoryMock.Verify(r => r.DeleteSubmissionsAsync(TestAccountNumber), Times.Once);
    }

    [Fact]
    public async Task ResetSubmissionsAsync_Should_Throw_WhenAccountIdNotFound()
    {
        // Arrange
        var unknownAccountId = 99999;

        _accountServiceMock
            .Setup(a => a.GetAccountNumberByIdAsync(unknownAccountId))
            .Returns(Task.FromResult<string?>(null));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.ResetSubmissionsAsync(unknownAccountId));

        _hubSpotFormRepositoryMock.Verify(r => r.DeleteSubmissionsAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ResetSubmissionsAsync_WithInvalidAccountId_ShouldThrow(int invalidAccountId)
    {
        // Arrange
        _accountServiceMock
            .Setup(a => a.GetAccountNumberByIdAsync(invalidAccountId))
            .Returns(Task.FromResult<string?>(null));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.ResetSubmissionsAsync(invalidAccountId));

        _hubSpotFormRepositoryMock.Verify(r => r.DeleteSubmissionsAsync(It.IsAny<string>()), Times.Never);
    }
}

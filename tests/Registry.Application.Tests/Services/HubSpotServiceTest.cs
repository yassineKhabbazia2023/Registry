using Application.Interfaces;
using Application.Models.Results;
using Application.Options;
using Application.Requests;
using Application.Services;
using Microsoft.Extensions.Options;
using Moq;
using System.Globalization;

namespace Registry.Application.Tests.Services;

public class HubSpotServiceTest
{
    [Fact]
    public async Task SubmitIntegrationAsync_Should_CallProvider_WithMappedFields()
    {
        // Arrange
        var options = Options.Create(new HubSpotOptions
        {
            BaseUrl = "https://api.hsforms.com",
            PortalId = "143978459",
            FormGuid = "48d828d6-5e31-40ab-afee-416a014879f0"
        });

        var request = new HubSpotSubmissionInputRequest
        {
            DematerializationEmail = "facturation@test.fr",
            FirstName = "ehubtest",
            LastName = "elastname",
            VaultEmail = "vault@test.fr",
            RequesterEmail = "requester@test.fr",
        };

        var providerMock = new Mock<IHubSpotProvider>(MockBehavior.Strict);
        HubSpotSubmissionRequest? captured = null;
        providerMock
            .Setup(p => p.SubmitIntegrationAsync(options.Value.PortalId!, options.Value.FormGuid!, It.IsAny<HubSpotSubmissionRequest>()))
            .Callback<string, string, HubSpotSubmissionRequest>((_, _, payload) => captured = payload)
            .ReturnsAsync(new HubSpotSubmissionResult { IsSuccess = true, StatusCode = 200 });

        var service = new HubSpotService(providerMock.Object, options);
        var accountNumber = "ACC-2025-001847";
        var beforeCall = DateTime.UtcNow;

        // Act
        var result = await service.SubmitIntegrationAsync(accountNumber, request);
        var afterCall = DateTime.UtcNow;

        // Assert
        providerMock.Verify(p => p.SubmitIntegrationAsync(options.Value.PortalId!, options.Value.FormGuid!, It.IsAny<HubSpotSubmissionRequest>()), Times.Once);
        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.Equal(7, captured!.Fields.Count);
        Assert.Contains(captured.Fields, field => field.Name == "code_client" && field.Value == "ACC-2025-001847");
        Assert.Contains(captured.Fields, field => field.Name == "e_mail_de_reception" && field.Value == "facturation@test.fr");
        Assert.Contains(captured.Fields, field => field.Name == "firstname" && field.Value == "ehubtest");
        Assert.Contains(captured.Fields, field => field.Name == "lastname" && field.Value == "elastname");
        Assert.Contains(captured.Fields, field => field.Name == "e_mail_de_connexion" && field.Value == "vault@test.fr");
        Assert.Contains(captured.Fields, field => field.Name == "email" && field.Value == "requester@test.fr");
    }
}

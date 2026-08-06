// <copyright file="ContactAkuiteoSynchronizerTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Enums;
using Application.Exceptions;
using Application.Interfaces;
using Application.Models.Contacts;
using Application.Models.Results;
using Application.Requests;
using Application.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Pulse.Back.Events.IntegrationEvents.EventsData;

namespace Registry.Application.Tests.Services;

public class ContactAkuiteoSynchronizerTests
{
    private readonly Mock<IContactRepository> contactRepositoryMock = new();
    private readonly Mock<IAkuiteoContactService> akuiteoContactServiceMock = new();
    private readonly Mock<ILogger<ContactAkuiteoSynchronizer>> loggerMock = new();

    [Fact]
    public async Task SynchronizeAsync_WithValidContact_ShouldMapAndSendRequest()
    {
        AkuiteoContactCreationRequest? capturedRequest = null;
        var role = CreateRole();
        var contact = CreateContact();
        contactRepositoryMock
            .Setup(repository => repository.GetContactByEmailOrIdAsync(null, role.ContactId))
            .ReturnsAsync(contact);
        akuiteoContactServiceMock
            .Setup(service => service.CreateContactAsync(It.IsAny<AkuiteoContactCreationRequest>()))
            .Callback<AkuiteoContactCreationRequest>(request => capturedRequest = request)
            .ReturnsAsync(new AkuiteoContactCreationResponse { ContactId = "500145940" });

        var result = await CreateSynchronizer().SynchronizeAsync(role);

        Assert.Equal(ContactAkuiteoSynchronizationOutcome.Sent, result.Outcome);
        Assert.Equal("500145940", result.AkuiteoContactId);
        Assert.NotNull(capturedRequest);
        Assert.Equal(role.AccountNumber, capturedRequest!.AccountNumber);
        Assert.Equal(contact.Title, capturedRequest.Title);
        Assert.Equal(contact.LastName, capturedRequest.LastName);
        Assert.Equal(contact.FirstName, capturedRequest.FirstName);
        Assert.Equal(role.ContactEmail, capturedRequest.Email);
        Assert.Equal(contact.MobilePhone, capturedRequest.MobilePhone);
        Assert.True(capturedRequest.ContactTypes!.IsDigitalVaultContact);
        Assert.True(capturedRequest.ContactTypes.IsMandateSignatory);
        Assert.False(capturedRequest.ContactTypes.IsDebtCollectionContact);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Mr")]
    public async Task SynchronizeAsync_WithUnsupportedTitle_ShouldFailWithoutCallingAkuiteo(string? title)
    {
        var role = CreateRole();
        var contact = CreateContact();
        contact.Title = title;
        contactRepositoryMock
            .Setup(repository => repository.GetContactByEmailOrIdAsync(null, role.ContactId))
            .ReturnsAsync(contact);

        var result = await CreateSynchronizer().SynchronizeAsync(role);

        Assert.Equal(ContactAkuiteoSynchronizationOutcome.Failed, result.Outcome);
        Assert.Contains("missing or unsupported", result.Error);
        akuiteoContactServiceMock.Verify(
            service => service.CreateContactAsync(It.IsAny<AkuiteoContactCreationRequest>()),
            Times.Never);
    }

    [Fact]
    public async Task SynchronizeAsync_WhenContactIsMissing_ShouldFailAfterSingleLookup()
    {
        var role = CreateRole();
        contactRepositoryMock
            .Setup(repository => repository.GetContactByEmailOrIdAsync(null, role.ContactId))
            .ReturnsAsync((Contact?)null);

        var result = await CreateSynchronizer().SynchronizeAsync(role);

        Assert.Equal(ContactAkuiteoSynchronizationOutcome.Failed, result.Outcome);
        Assert.Contains("does not exist in Registry", result.Error);
        contactRepositoryMock.Verify(
            repository => repository.GetContactByEmailOrIdAsync(null, role.ContactId),
            Times.Once);
        akuiteoContactServiceMock.Verify(
            service => service.CreateContactAsync(It.IsAny<AkuiteoContactCreationRequest>()),
            Times.Never);
    }

    [Theory]
    [InlineData("", "contact@example.com")]
    [InlineData("9010001710", "")]
    public async Task SynchronizeAsync_WhenRequiredRoleDataIsMissing_ShouldFail(
        string accountNumber,
        string contactEmail)
    {
        var role = CreateRole();
        role.AccountNumber = accountNumber;
        role.ContactEmail = contactEmail;

        var result = await CreateSynchronizer().SynchronizeAsync(role);

        Assert.Equal(ContactAkuiteoSynchronizationOutcome.Failed, result.Outcome);
        contactRepositoryMock.Verify(
            repository => repository.GetContactByEmailOrIdAsync(It.IsAny<string>(), It.IsAny<int?>()),
            Times.Never);
    }

    [Fact]
    public async Task SynchronizeAsync_WhenContactEmailDiffers_ShouldFail()
    {
        var role = CreateRole();
        var contact = CreateContact();
        contact.Email = "another@example.com";
        contactRepositoryMock
            .Setup(repository => repository.GetContactByEmailOrIdAsync(null, role.ContactId))
            .ReturnsAsync(contact);

        var result = await CreateSynchronizer().SynchronizeAsync(role);

        Assert.Equal(ContactAkuiteoSynchronizationOutcome.Failed, result.Outcome);
        Assert.Contains("does not match", result.Error);
        akuiteoContactServiceMock.Verify(
            service => service.CreateContactAsync(It.IsAny<AkuiteoContactCreationRequest>()),
            Times.Never);
    }

    [Fact]
    public async Task SynchronizeAsync_WhenAkuiteoFails_ShouldReturnFailed()
    {
        var role = CreateRole();
        contactRepositoryMock
            .Setup(repository => repository.GetContactByEmailOrIdAsync(null, role.ContactId))
            .ReturnsAsync(CreateContact());
        akuiteoContactServiceMock
            .Setup(service => service.CreateContactAsync(It.IsAny<AkuiteoContactCreationRequest>()))
            .ThrowsAsync(new AkuiteoContactCreationTechnicalException("Akuiteo is unavailable."));

        var result = await CreateSynchronizer().SynchronizeAsync(role);

        Assert.Equal(ContactAkuiteoSynchronizationOutcome.Failed, result.Outcome);
        Assert.Equal("Akuiteo is unavailable.", result.Error);
    }

    [Fact]
    public async Task SynchronizeAsync_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => CreateSynchronizer().SynchronizeAsync(CreateRole(), cancellationTokenSource.Token));

        contactRepositoryMock.Verify(
            repository => repository.GetContactByEmailOrIdAsync(It.IsAny<string>(), It.IsAny<int?>()),
            Times.Never);
    }

    private ContactAkuiteoSynchronizer CreateSynchronizer()
    {
        return new ContactAkuiteoSynchronizer(
            contactRepositoryMock.Object,
            akuiteoContactServiceMock.Object,
            loggerMock.Object);
    }

    private static RoleCreatedEventData CreateRole()
    {
        return new RoleCreatedEventData
        {
            AccountId = 792480503,
            AccountNumber = "9010001710",
            ContactId = 123,
            ContactEmail = "contact@example.com",
            IsSignatory = true,
            ContactFlagPortailFactures = true
        };
    }

    private static Contact CreateContact()
    {
        return new Contact
        {
            ContactId = 123,
            Email = "contact@example.com",
            FirstName = "Jean",
            LastName = "Dupont",
            Title = "Mme",
            MobilePhone = "+33612345678"
        };
    }
}

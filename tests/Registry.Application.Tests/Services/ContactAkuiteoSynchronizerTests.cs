// <copyright file="ContactAkuiteoSynchronizerTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

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

/// <summary>
/// Tests for <see cref="ContactAkuiteoSynchronizer"/>.
/// </summary>
public class ContactAkuiteoSynchronizerTests
{
    private readonly Mock<IContactRepository> contactRepositoryMock = new();
    private readonly Mock<IAkuiteoContactService> akuiteoContactServiceMock = new();
    private readonly Mock<ILogger<ContactAkuiteoSynchronizer>> loggerMock = new();

    #region SynchronizeAsync

    /// <summary>
    /// Ensures a null role event is rejected before accessing dependencies.
    /// </summary>
    [Fact]
    public async Task SynchronizeAsync_WithNullRole_ShouldThrowArgumentNullException()
    {
        // Act
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => CreateSynchronizer().SynchronizeAsync(null!));

        // Assert
        contactRepositoryMock.Verify(
            repository => repository.GetContactByEmailOrIdAsync(It.IsAny<string>(), It.IsAny<int?>()),
            Times.Never);
        akuiteoContactServiceMock.Verify(
            service => service.CreateContactAsync(It.IsAny<AkuiteoContactCreationRequest>()),
            Times.Never);
    }

    /// <summary>
    /// Ensures the synchronizer maps all fields required by the Akuiteo create-or-attach contract.
    /// </summary>
    /// <param name="isSignatory">The role signatory flag.</param>
    /// <param name="isDigitalVaultContact">The digital-vault role flag.</param>
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task SynchronizeAsync_WithValidContact_ShouldMapAndSendRequest(
        bool isSignatory,
        bool isDigitalVaultContact)
    {
        // Arrange
        AkuiteoContactCreationRequest? capturedRequest = null;
        var role = CreateRole();
        role.IsSignatory = isSignatory;
        role.ContactFlagPortailFactures = isDigitalVaultContact;
        var contact = CreateContact();

        contactRepositoryMock
            .Setup(repository => repository.GetContactByEmailOrIdAsync(null, role.ContactId))
            .ReturnsAsync(contact);
        akuiteoContactServiceMock
            .Setup(service => service.CreateContactAsync(It.IsAny<AkuiteoContactCreationRequest>()))
            .Callback<AkuiteoContactCreationRequest>(request => capturedRequest = request)
            .ReturnsAsync(new AkuiteoContactCreationResponse
            {
                ContactId = "500145940"
            });

        // Act
        await CreateSynchronizer().SynchronizeAsync(role);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(role.AccountNumber, capturedRequest!.AccountNumber);
        Assert.Equal("M", capturedRequest.Title);
        Assert.Equal(contact.LastName, capturedRequest.LastName);
        Assert.Equal(contact.FirstName, capturedRequest.FirstName);
        Assert.Equal(string.Empty, capturedRequest.JobTitle);
        Assert.Equal(string.Empty, capturedRequest.ContactDepartment);
        Assert.Equal(string.Empty, capturedRequest.CompanyRole);
        Assert.Equal(role.ContactEmail, capturedRequest.Email);
        Assert.Equal(string.Empty, capturedRequest.MobilePhone);
        Assert.NotNull(capturedRequest.ContactTypes);
        Assert.Equal(isDigitalVaultContact, capturedRequest.ContactTypes!.IsDigitalVaultContact);
        Assert.False(capturedRequest.ContactTypes.IsDebtCollectionContact);
        Assert.Equal(isSignatory, capturedRequest.ContactTypes.IsMandateSignatory);
        contactRepositoryMock.Verify(
            repository => repository.GetContactByEmailOrIdAsync(null, role.ContactId),
            Times.Once);
        contactRepositoryMock.Verify(
            repository => repository.GetContactByEmailOrIdAsync(It.IsAny<string>(), null),
            Times.Never);
        akuiteoContactServiceMock.Verify(
            service => service.CreateContactAsync(It.IsAny<AkuiteoContactCreationRequest>()),
            Times.Once);
    }

    /// <summary>
    /// Ensures the email lookup is used when the contact identifier is not yet available locally.
    /// </summary>
    [Fact]
    public async Task SynchronizeAsync_WhenIdLookupReturnsNull_ShouldFallbackToEmail()
    {
        // Arrange
        var role = CreateRole();
        var contact = CreateContact();

        contactRepositoryMock
            .Setup(repository => repository.GetContactByEmailOrIdAsync(null, role.ContactId))
            .ReturnsAsync((Contact?)null);
        contactRepositoryMock
            .Setup(repository => repository.GetContactByEmailOrIdAsync(role.ContactEmail, null))
            .ReturnsAsync(contact);
        akuiteoContactServiceMock
            .Setup(service => service.CreateContactAsync(It.IsAny<AkuiteoContactCreationRequest>()))
            .ReturnsAsync(new AkuiteoContactCreationResponse
            {
                ContactId = "500145940"
            });

        // Act
        await CreateSynchronizer().SynchronizeAsync(role);

        // Assert
        contactRepositoryMock.Verify(
            repository => repository.GetContactByEmailOrIdAsync(role.ContactEmail, null),
            Times.Once);
        akuiteoContactServiceMock.Verify(
            service => service.CreateContactAsync(It.IsAny<AkuiteoContactCreationRequest>()),
            Times.Once);
    }

    /// <summary>
    /// Ensures a missing Registry contact fails the event so that it can be retried.
    /// </summary>
    [Fact]
    public async Task SynchronizeAsync_WhenContactIsMissing_ShouldThrowTechnicalException()
    {
        // Arrange
        var role = CreateRole();
        contactRepositoryMock
            .Setup(repository => repository.GetContactByEmailOrIdAsync(null, role.ContactId))
            .ReturnsAsync((Contact?)null);
        contactRepositoryMock
            .Setup(repository => repository.GetContactByEmailOrIdAsync(role.ContactEmail, null))
            .ReturnsAsync((Contact?)null);

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoContactCreationTechnicalException>(
            () => CreateSynchronizer().SynchronizeAsync(role));

        // Assert
        Assert.Contains("does not exist in Registry", exception.Message);
        akuiteoContactServiceMock.Verify(
            service => service.CreateContactAsync(It.IsAny<AkuiteoContactCreationRequest>()),
            Times.Never);
    }

    /// <summary>
    /// Ensures missing required event fields prevent the Akuiteo call.
    /// </summary>
    /// <param name="accountNumber">The account number under test.</param>
    /// <param name="contactEmail">The contact email under test.</param>
    [Theory]
    [InlineData("", "contact@example.com")]
    [InlineData("9010001710", "")]
    public async Task SynchronizeAsync_WhenRequiredRoleDataIsMissing_ShouldThrowTechnicalException(
        string accountNumber,
        string contactEmail)
    {
        // Arrange
        var role = CreateRole();
        role.AccountNumber = accountNumber;
        role.ContactEmail = contactEmail;

        // Act
        await Assert.ThrowsAsync<AkuiteoContactCreationTechnicalException>(
            () => CreateSynchronizer().SynchronizeAsync(role));

        // Assert
        contactRepositoryMock.Verify(
            repository => repository.GetContactByEmailOrIdAsync(It.IsAny<string>(), It.IsAny<int?>()),
            Times.Never);
        akuiteoContactServiceMock.Verify(
            service => service.CreateContactAsync(It.IsAny<AkuiteoContactCreationRequest>()),
            Times.Never);
    }

    /// <summary>
    /// Ensures an incomplete Registry identity prevents the Akuiteo call.
    /// </summary>
    /// <param name="firstName">The first name under test.</param>
    /// <param name="lastName">The last name under test.</param>
    [Theory]
    [InlineData("", "Dupont")]
    [InlineData("Jean", "")]
    public async Task SynchronizeAsync_WhenContactNameIsMissing_ShouldThrowTechnicalException(
        string firstName,
        string lastName)
    {
        // Arrange
        var role = CreateRole();
        var contact = CreateContact();
        contact.FirstName = firstName;
        contact.LastName = lastName;
        contactRepositoryMock
            .Setup(repository => repository.GetContactByEmailOrIdAsync(null, role.ContactId))
            .ReturnsAsync(contact);

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoContactCreationTechnicalException>(
            () => CreateSynchronizer().SynchronizeAsync(role));

        // Assert
        Assert.Contains("first name or last name is missing", exception.Message);
        akuiteoContactServiceMock.Verify(
            service => service.CreateContactAsync(It.IsAny<AkuiteoContactCreationRequest>()),
            Times.Never);
    }

    /// <summary>
    /// Ensures an email mismatch prevents synchronizing the wrong Registry contact.
    /// </summary>
    [Fact]
    public async Task SynchronizeAsync_WhenContactEmailDiffers_ShouldThrowTechnicalException()
    {
        // Arrange
        var role = CreateRole();
        var contact = CreateContact();
        contact.Email = "another@example.com";
        contactRepositoryMock
            .Setup(repository => repository.GetContactByEmailOrIdAsync(null, role.ContactId))
            .ReturnsAsync(contact);

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoContactCreationTechnicalException>(
            () => CreateSynchronizer().SynchronizeAsync(role));

        // Assert
        Assert.Contains("does not match", exception.Message);
        akuiteoContactServiceMock.Verify(
            service => service.CreateContactAsync(It.IsAny<AkuiteoContactCreationRequest>()),
            Times.Never);
    }

    /// <summary>
    /// Ensures Akuiteo technical failures are logged without interrupting role processing.
    /// </summary>
    [Fact]
    public async Task SynchronizeAsync_WhenAkuiteoFails_ShouldLogAndComplete()
    {
        // Arrange
        var role = CreateRole();
        contactRepositoryMock
            .Setup(repository => repository.GetContactByEmailOrIdAsync(null, role.ContactId))
            .ReturnsAsync(CreateContact());
        akuiteoContactServiceMock
            .Setup(service => service.CreateContactAsync(It.IsAny<AkuiteoContactCreationRequest>()))
            .ThrowsAsync(new AkuiteoContactCreationTechnicalException("Akuiteo is unavailable."));

        // Act
        await CreateSynchronizer().SynchronizeAsync(role);

        // Assert
        loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((value, _) =>
                    value.ToString()!.Contains(
                        "Contact synchronization with Akuiteo failed and will be skipped",
                        StringComparison.Ordinal)),
                It.Is<AkuiteoContactCreationTechnicalException>(exception =>
                    exception.Message == "Akuiteo is unavailable."),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Ensures cancellation is honored before querying Registry or calling Akuiteo.
    /// </summary>
    [Fact]
    public async Task SynchronizeAsync_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => CreateSynchronizer().SynchronizeAsync(CreateRole(), cancellationTokenSource.Token));

        // Assert
        contactRepositoryMock.Verify(
            repository => repository.GetContactByEmailOrIdAsync(It.IsAny<string>(), It.IsAny<int?>()),
            Times.Never);
        akuiteoContactServiceMock.Verify(
            service => service.CreateContactAsync(It.IsAny<AkuiteoContactCreationRequest>()),
            Times.Never);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Creates the synchronizer under test.
    /// </summary>
    /// <returns>The configured synchronizer.</returns>
    private ContactAkuiteoSynchronizer CreateSynchronizer()
    {
        return new ContactAkuiteoSynchronizer(
            contactRepositoryMock.Object,
            akuiteoContactServiceMock.Object,
            loggerMock.Object);
    }

    /// <summary>
    /// Creates valid role event data.
    /// </summary>
    /// <returns>The valid role event data.</returns>
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

    /// <summary>
    /// Creates a valid Registry contact.
    /// </summary>
    /// <returns>The valid Registry contact.</returns>
    private static Contact CreateContact()
    {
        return new Contact
        {
            ContactId = 123,
            Email = "contact@example.com",
            FirstName = "Jean",
            LastName = "Dupont"
        };
    }

    #endregion
}

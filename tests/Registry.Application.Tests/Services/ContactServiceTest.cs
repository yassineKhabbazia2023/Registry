// <copyright file="ContactServiceTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Enums;
using Application.Helpers;
using Application.Interfaces;
using Application.Mappers;
using Application.Models;
using Application.Models.Contacts;
using Application.Models.Results;
using Application.Requests;
using Application.Services;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Registry.Domain.Entities;

namespace Registry.Application.Tests.Services;

public class ContactServiceTest
{
    private readonly Fixture _fixture;
    private readonly Mock<IOperationService> operationServiceMock;
    private readonly Mock<IContactRegistryProvider> registryProviderMock;
    private readonly Mock<IContactRepository> contactReposMock;
    private readonly ILogger<ContactService> loggerMock;

    public ContactServiceTest()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        operationServiceMock = new Mock<IOperationService>();
        registryProviderMock = new Mock<IContactRegistryProvider>();
        contactReposMock = new Mock<IContactRepository>();
        loggerMock = Mock.Of<ILogger<ContactService>>();
    }

    [Fact]
    public async Task InsertContactsAsync_Should_Be_Success()
    {
        var repository = new Mock<IContactRepository>();
        var validationHelperMock = new Mock<IValidationHelper<RefContactCsv>>();

        var contactService = new ContactService(null!, repository.Object, registryProviderMock.Object, operationServiceMock.Object);

        await contactService.InsertContactsAsync(It.IsAny<IEnumerable<RefContactCsv>>());

        repository.Verify(x => x.BulkAddContactsAsync(It.IsAny<IEnumerable<RefContactEntity>>(), null), Times.Once);
    }

    [Fact]
    public async Task InsertContactsAsync_Should_SetSource_When_Provided()
    {
        // Arrange
        var repository = new Mock<IContactRepository>();
        var validationHelperMock = new Mock<IValidationHelper<RefContactCsv>>();
        var contacts = _fixture.Build<RefContactCsv>().CreateMany(2).ToList();
        string expectedSource = DataSources.PENNYLANE.ToString();
        var refContactEntities = contacts.MapContactCsvsToContactEntities();

        var contactService = new ContactService(
            null!,
            repository.Object,
            registryProviderMock.Object,
            operationServiceMock.Object
        );

        // Act
        await contactService.InsertContactsAsync(contacts, source: expectedSource);

        // Assert
        repository.Verify(x =>
            x.BulkAddContactsAsync(
                It.Is<IEnumerable<RefContactEntity>>(entities =>
                    entities.All(e => e.ContactSource == expectedSource)
                ),
                expectedSource
            ),
            Times.Once
        );
    }
    
    [Fact]
    public async Task InsertContactsAsync_Should_SetAccountNumber_When_SourceIsPennylane()
    {
        // Arrange
        var repository = new Mock<IContactRepository>();
        var validationHelperMock = new Mock<IValidationHelper<RefContactCsv>>();
        var contacts = _fixture.Build<RefContactCsv>().CreateMany(2).ToList();

        string expectedSource = DataSources.PENNYLANE.ToString();
        string expectedAccountNumber = "ACC123";

        var contactService = new ContactService(
            null!,
            repository.Object,
            registryProviderMock.Object,
            operationServiceMock.Object
        );

        // Act
        await contactService.InsertContactsAsync(contacts, source: expectedSource, accountNumber: expectedAccountNumber);

        // Assert
        repository.Verify(x =>
            x.BulkAddContactsAsync(
                It.Is<IEnumerable<RefContactEntity>>(entities =>
                    entities.All(e =>
                        e.ContactSource == expectedSource &&
                        e.AccountNumber == expectedAccountNumber
                    )
                ),
                expectedSource
            ),
            Times.Once
        );
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2000, 1)]
    public async Task AddContactAsync_Adds_Contact_With2000Contacts(int contactCsvLenght, int functionTimeCalled)
    {
        // Arrange
        var contacts = _fixture.Build<RefContactCsv>()
            .CreateMany(contactCsvLenght);

        var contactRepository = new Mock<IContactRepository>();
        contactRepository.Setup(r => r.BulkAddContactsAsync(It.IsAny<IEnumerable<RefContactEntity>>(), null)).
            Callback<IEnumerable<RefContactEntity>, string>((data, contactSource) =>
            {
                Assert.Null(contactSource);
                data.Count().Should().BeGreaterThanOrEqualTo(contacts.Count());
            })
            .Returns(Task.CompletedTask);


        var loggerMock = new Mock<ILogger<ContactService>>(MockBehavior.Default);
        var validationHelperMock = new Mock<IValidationHelper<RefContactCsv>>();
        // Act
        var contactService = new ContactService(null!, contactRepository.Object, registryProviderMock.Object, operationServiceMock.Object);
        await contactService.InsertContactsAsync(contacts);

        contactRepository.VerifyAll();
        contactRepository.Verify(a => a.BulkAddContactsAsync(It.IsAny<IEnumerable<RefContactEntity>>(), null), Times.AtLeast(functionTimeCalled));
    }

    [Fact]
    public async Task AddContactAsync_Adds_Contact_With2000Contacts_With_Source()
    {
        // Arrange
        var contacts = _fixture.Build<RefContactCsv>()
            .CreateMany(1);

        var contactRepository = new Mock<IContactRepository>();
        contactRepository.Setup(r => r.BulkAddContactsAsync(It.IsAny<IEnumerable<RefContactEntity>>(), It.IsAny<string>())).
            Callback<IEnumerable<RefContactEntity>, string>((data, contactSource) =>
            {
                Assert.NotNull(contactSource);
                Assert.Equal(DataSources.PENNYLANE.ToString(), contactSource);
                data.Count().Should().BeGreaterThanOrEqualTo(contacts.Count());
            })
            .Returns(Task.CompletedTask);


        var loggerMock = new Mock<ILogger<ContactService>>(MockBehavior.Default);
        var validationHelperMock = new Mock<IValidationHelper<RefContactCsv>>();
        // Act
        var contactService = new ContactService(null!, contactRepository.Object, registryProviderMock.Object, operationServiceMock.Object);
        await contactService.InsertContactsAsync(contacts, DataSources.PENNYLANE.ToString());

        contactRepository.VerifyAll();
        contactRepository.Verify(a => a.BulkAddContactsAsync(It.IsAny<IEnumerable<RefContactEntity>>(), It.IsAny<string>()), Times.Exactly(1));
    }

    [Fact]
    public void ValidateContacts_ShouldReturnErrors_WhenInvalidContactsProvided()
    {
        // Arrange
        var contacts = new List<RefContactCsv>
    {
        new() { Email = "valid@example.com", Operation = "INSERT"  },
        new() { Email = "", Operation = "UPDATE" }, // Email vide
        new() { Email = "invalid@example.com", Operation = "INVALID" } // Operation invalide
    };
        var contactRepository = new Mock<IContactRepository>();

        var loggerMock = new Mock<ILogger<ContactService>>(MockBehavior.Default);
        var validationHelperNoMock = new ValidationHelper<RefContactCsv>();

        var contactService = new ContactService(null!, contactRepository.Object, registryProviderMock.Object, operationServiceMock.Object);

        // Act
        var result = validationHelperNoMock.Validate(contacts);

        // Assert
        Assert.Equal(3, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.Errors.Contains("Email is required"));
        Assert.Contains(result.Errors, e => e.Errors.Contains("Operation type not known!"));
    }

    [Fact]
    public async Task OnCreatedContactEventExecution_ShouldExecuteSuccessfully()
    {
        ContactStateEventData contactStateEventData = _fixture.Create<ContactStateEventData>();
        var contact = contactStateEventData.MapContactStateEventToModel();
        registryProviderMock.Setup(x => x.CreateContactAsync(It.IsAny<ContactRegistry>())).ReturnsAsync(new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.OK, Content = null, ReasonPhrase = string.Empty });

        contactReposMock.Setup(x => x.AddContactAsync(It.IsAny<Contact>())).ReturnsAsync(true);

        operationServiceMock.Setup(x => x.UpdateContactOperations(It.IsAny<OperationSearchCriteria>(), It.IsAny<string>())).ReturnsAsync(true);

        ContactEventResult<Contact> contactEventResult = new ContactEventResult<Contact>()
        {
            EventName = "ContactCreatedEventHandler",
            IsOpeationProcessUpdated = true,
            IsRegisteredInDb = true,
            IsSentToAkuiteo = false,
            Content = contact
        };

        var contactService = new ContactService(loggerMock, contactReposMock.Object, registryProviderMock.Object, operationServiceMock.Object);

        var execution = await contactService.OnCreatedContactEventExecution(contactStateEventData);

        registryProviderMock.Verify(x => x.CreateContactAsync(It.IsAny<ContactRegistry>()), Times.Never);
        contactReposMock.Verify(x => x.AddContactAsync(It.IsAny<Contact>()), Times.Once);
        operationServiceMock.Verify(x => x.UpdateContactOperations(It.IsAny<OperationSearchCriteria>(), It.IsAny<string>()), Times.Once);

        execution.Should().BeEquivalentTo(contactEventResult);
    }

    [Fact]
    public async Task OnCreatedContactEventExecution_ShouldReturnFailure_WhenCreateContactAsyncFails()
    {
        // Arrange
        ContactStateEventData contactStateEventData = _fixture.Create<ContactStateEventData>();
        var contact = contactStateEventData.MapContactStateEventToModel();

        // Simulate API Failure
        registryProviderMock.Setup(x => x.CreateContactAsync(It.IsAny<ContactRegistry>()))
            .ReturnsAsync(new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.BadRequest });

        contactReposMock.Setup(x => x.AddContactAsync(It.IsAny<Contact>())).ReturnsAsync(true);
        operationServiceMock.Setup(x => x.UpdateContactOperations(It.IsAny<OperationSearchCriteria>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var contactService = new ContactService(loggerMock, contactReposMock.Object, registryProviderMock.Object, operationServiceMock.Object);

        // Act
        var execution = await contactService.OnCreatedContactEventExecution(contactStateEventData);

        // Assert
        registryProviderMock.Verify(x => x.CreateContactAsync(It.IsAny<ContactRegistry>()), Times.Never);
        contactReposMock.Verify(x => x.AddContactAsync(It.IsAny<Contact>()), Times.Once);
        operationServiceMock.Verify(x => x.UpdateContactOperations(It.IsAny<OperationSearchCriteria>(), It.IsAny<string>()), Times.Once);

        execution.IsSentToAkuiteo.Should().BeFalse();
        execution.IsRegisteredInDb.Should().BeTrue();
        execution.IsOpeationProcessUpdated.Should().BeTrue();
    }


    [Fact]
    public async Task OnCreatedContactEventExecution_ShouldReturnFailure_WhenAddContactAsyncFails()
    {
        // Arrange
        ContactStateEventData contactStateEventData = _fixture.Create<ContactStateEventData>();
        var contact = contactStateEventData.MapContactStateEventToModel();

        registryProviderMock.Setup(x => x.CreateContactAsync(It.IsAny<ContactRegistry>()))
            .ReturnsAsync(new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.OK });

        // Simulate DB Insert Failure
        contactReposMock.Setup(x => x.AddContactAsync(It.IsAny<Contact>())).ReturnsAsync(false);

        operationServiceMock.Setup(x => x.UpdateContactOperations(It.IsAny<OperationSearchCriteria>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var contactService = new ContactService(loggerMock, contactReposMock.Object, registryProviderMock.Object, operationServiceMock.Object);

        // Act
        var execution = await contactService.OnCreatedContactEventExecution(contactStateEventData);

        // Assert
        registryProviderMock.Verify(x => x.CreateContactAsync(It.IsAny<ContactRegistry>()), Times.Never);
        contactReposMock.Verify(x => x.AddContactAsync(It.IsAny<Contact>()), Times.Once);
        operationServiceMock.Verify(x => x.UpdateContactOperations(It.IsAny<OperationSearchCriteria>(), It.IsAny<string>()), Times.Once);

        execution.IsSentToAkuiteo.Should().BeFalse();
        execution.IsRegisteredInDb.Should().BeFalse();
        execution.IsOpeationProcessUpdated.Should().BeTrue();
    }


    [Fact]
    public async Task OnCreatedContactEventExecution_ShouldReturnFailure_WhenUpdateContactOperationsFails()
    {
        // Arrange
        ContactStateEventData contactStateEventData = _fixture.Create<ContactStateEventData>();
        var contact = contactStateEventData.MapContactStateEventToModel();

        registryProviderMock.Setup(x => x.CreateContactAsync(It.IsAny<ContactRegistry>()))
            .ReturnsAsync(new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.OK });

        contactReposMock.Setup(x => x.AddContactAsync(It.IsAny<Contact>())).ReturnsAsync(true);

        // Simulate Operation Update Failure
        operationServiceMock.Setup(x => x.UpdateContactOperations(It.IsAny<OperationSearchCriteria>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var contactService = new ContactService(loggerMock, contactReposMock.Object, registryProviderMock.Object, operationServiceMock.Object);

        // Act
        var execution = await contactService.OnCreatedContactEventExecution(contactStateEventData);

        // Assert
        registryProviderMock.Verify(x => x.CreateContactAsync(It.IsAny<ContactRegistry>()), Times.Never);
        contactReposMock.Verify(x => x.AddContactAsync(It.IsAny<Contact>()), Times.Once);
        operationServiceMock.Verify(x => x.UpdateContactOperations(It.IsAny<OperationSearchCriteria>(), It.IsAny<string>()), Times.Once);

        execution.IsSentToAkuiteo.Should().BeFalse();
        execution.IsRegisteredInDb.Should().BeTrue();
        execution.IsOpeationProcessUpdated.Should().BeFalse();
    }


    [Fact]
    public async Task OnCreatedContactEventExecution_ShouldReturnFailure_WhenAllOperationsFail()
    {
        // Arrange
        ContactStateEventData contactStateEventData = _fixture.Create<ContactStateEventData>();
        var contact = contactStateEventData.MapContactStateEventToModel();

        // Simulate API Failure
        registryProviderMock.Setup(x => x.CreateContactAsync(It.IsAny<ContactRegistry>()))
            .ReturnsAsync(new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.BadRequest });

        // Simulate DB Insert Failure
        contactReposMock.Setup(x => x.AddContactAsync(It.IsAny<Contact>())).ReturnsAsync(false);

        // Simulate Operation Update Failure
        operationServiceMock.Setup(x => x.UpdateContactOperations(It.IsAny<OperationSearchCriteria>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var contactService = new ContactService(loggerMock, contactReposMock.Object, registryProviderMock.Object, operationServiceMock.Object);

        // Act
        var execution = await contactService.OnCreatedContactEventExecution(contactStateEventData);

        // Assert
        registryProviderMock.Verify(x => x.CreateContactAsync(It.IsAny<ContactRegistry>()), Times.Never);
        contactReposMock.Verify(x => x.AddContactAsync(It.IsAny<Contact>()), Times.Once);
        operationServiceMock.Verify(x => x.UpdateContactOperations(It.IsAny<OperationSearchCriteria>(), It.IsAny<string>()), Times.Once);

        execution.IsSentToAkuiteo.Should().BeFalse();
        execution.IsRegisteredInDb.Should().BeFalse();
        execution.IsOpeationProcessUpdated.Should().BeFalse();
    }


    [Fact]
    public async Task OnUpdatedContactEventExecution_ShouldExecuteSuccessfully()
    {
        // Arrange
        ContactStateEventData contactStateEventData = _fixture.Create<ContactStateEventData>();
        var contact = contactStateEventData.MapContactStateEventToModel();

        registryProviderMock.Setup(x => x.UpdateContactAsync(It.IsAny<ContactRegistry>()))
            .ReturnsAsync(new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.OK });

        contactReposMock.Setup(x => x.UpdateContactAsync(It.IsAny<Contact>())).ReturnsAsync(true);

        operationServiceMock.Setup(x => x.UpdateContactOperations(It.IsAny<OperationSearchCriteria>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        ContactEventResult<Contact> expectedResult = new ContactEventResult<Contact>()
        {
            EventName = "ContactUpdatedEventHandler",
            IsOpeationProcessUpdated = true,
            IsRegisteredInDb = true,
            IsSentToAkuiteo = false,
            Content = contact
        };

        var contactService = new ContactService(loggerMock, contactReposMock.Object, registryProviderMock.Object, operationServiceMock.Object);

        // Act
        var execution = await contactService.OnUpdatedContactEventExecution(contactStateEventData);

        // Assert
        registryProviderMock.Verify(x => x.UpdateContactAsync(It.IsAny<ContactRegistry>()), Times.Never);
        contactReposMock.Verify(x => x.UpdateContactAsync(It.IsAny<Contact>()), Times.Once);
        operationServiceMock.Verify(x => x.UpdateContactOperations(It.IsAny<OperationSearchCriteria>(), It.IsAny<string>()), Times.Once);

        execution.Should().BeEquivalentTo(expectedResult);
    }


    [Fact]
    public async Task OnUpdatedContactEventExecution_ShouldReturnFailure_WhenUpdateContactAsyncFails()
    {
        // Arrange
        ContactStateEventData contactStateEventData = _fixture.Create<ContactStateEventData>();
        var contact = contactStateEventData.MapContactStateEventToModel();

        // Simulate API Failure
        registryProviderMock.Setup(x => x.UpdateContactAsync(It.IsAny<ContactRegistry>()))
            .ReturnsAsync(new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.BadRequest });

        contactReposMock.Setup(x => x.UpdateContactAsync(It.IsAny<Contact>())).ReturnsAsync(true);
        operationServiceMock.Setup(x => x.UpdateContactOperations(It.IsAny<OperationSearchCriteria>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var contactService = new ContactService(loggerMock, contactReposMock.Object, registryProviderMock.Object, operationServiceMock.Object);

        // Act
        var execution = await contactService.OnUpdatedContactEventExecution(contactStateEventData);

        // Assert
        execution.IsSentToAkuiteo.Should().BeFalse();
        execution.IsRegisteredInDb.Should().BeTrue();
        execution.IsOpeationProcessUpdated.Should().BeTrue();
    }


    [Fact]
    public async Task OnUpdatedContactEventExecution_ShouldReturnFailure_WhenUpdateContactInDbFails()
    {
        // Arrange
        ContactStateEventData contactStateEventData = _fixture.Create<ContactStateEventData>();
        var contact = contactStateEventData.MapContactStateEventToModel();

        registryProviderMock.Setup(x => x.UpdateContactAsync(It.IsAny<ContactRegistry>()))
            .ReturnsAsync(new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.OK });

        // Simulate DB Update Failure
        contactReposMock.Setup(x => x.UpdateContactAsync(It.IsAny<Contact>())).ReturnsAsync(false);

        operationServiceMock.Setup(x => x.UpdateContactOperations(It.IsAny<OperationSearchCriteria>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var contactService = new ContactService(loggerMock, contactReposMock.Object, registryProviderMock.Object, operationServiceMock.Object);

        // Act
        var execution = await contactService.OnUpdatedContactEventExecution(contactStateEventData);

        // Assert
        execution.IsSentToAkuiteo.Should().BeFalse();
        execution.IsRegisteredInDb.Should().BeFalse();
        execution.IsOpeationProcessUpdated.Should().BeTrue();
    }


    [Fact]
    public async Task OnUpdatedContactEventExecution_ShouldReturnFailure_WhenUpdateContactOperationsFails()
    {
        // Arrange
        ContactStateEventData contactStateEventData = _fixture.Create<ContactStateEventData>();
        var contact = contactStateEventData.MapContactStateEventToModel();

        registryProviderMock.Setup(x => x.UpdateContactAsync(It.IsAny<ContactRegistry>()))
            .ReturnsAsync(new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.OK });

        contactReposMock.Setup(x => x.UpdateContactAsync(It.IsAny<Contact>())).ReturnsAsync(true);

        // Simulate Operation Update Failure
        operationServiceMock.Setup(x => x.UpdateContactOperations(It.IsAny<OperationSearchCriteria>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var contactService = new ContactService(loggerMock, contactReposMock.Object, registryProviderMock.Object, operationServiceMock.Object);

        // Act
        var execution = await contactService.OnUpdatedContactEventExecution(contactStateEventData);

        // Assert
        execution.IsSentToAkuiteo.Should().BeFalse();
        execution.IsRegisteredInDb.Should().BeTrue();
        execution.IsOpeationProcessUpdated.Should().BeFalse();
    }


    [Fact]
    public async Task OnUpdatedContactEventExecution_ShouldReturnFailure_WhenAllOperationsFail()
    {
        // Arrange
        ContactStateEventData contactStateEventData = _fixture.Create<ContactStateEventData>();
        var contact = contactStateEventData.MapContactStateEventToModel();

        // Simulate API Failure
        registryProviderMock.Setup(x => x.UpdateContactAsync(It.IsAny<ContactRegistry>()))
            .ReturnsAsync(new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.BadRequest });

        // Simulate DB Update Failure
        contactReposMock.Setup(x => x.UpdateContactAsync(It.IsAny<Contact>())).ReturnsAsync(false);

        // Simulate Operation Update Failure
        operationServiceMock.Setup(x => x.UpdateContactOperations(It.IsAny<OperationSearchCriteria>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var contactService = new ContactService(loggerMock, contactReposMock.Object, registryProviderMock.Object, operationServiceMock.Object);

        // Act
        var execution = await contactService.OnUpdatedContactEventExecution(contactStateEventData);

        // Assert
        execution.IsSentToAkuiteo.Should().BeFalse();
        execution.IsRegisteredInDb.Should().BeFalse();
        execution.IsOpeationProcessUpdated.Should().BeFalse();
    }

    [Fact]
    public async Task OnRemovedContactEventExecution_ShouldExecuteSuccessfully()
    {
        // Arrange
        var contactRemovedData = _fixture.Create<ContactRemovedEventData>();
        var contact = _fixture.Create<Contact>();

        contactReposMock.Setup(x => x.GetContactByEmailOrIdAsync(null, contactRemovedData.ContactId))
            .ReturnsAsync(contact);

        contactReposMock.Setup(x => x.DeleteContactByIdAsync(contactRemovedData.ContactId))
            .ReturnsAsync(true);

        operationServiceMock.Setup(x => x.UpdateContactOperations(It.IsAny<OperationSearchCriteria>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var contactService = new ContactService(loggerMock, contactReposMock.Object, registryProviderMock.Object, operationServiceMock.Object);

        // Act
        var execution = await contactService.OnRemovedContactEventExecution(contactRemovedData);

        // Assert
        execution.Should().BeEquivalentTo(new ContactEventResult<Contact>
        {
            EventName = "ContactRemovedEventHandler",
            IsOpeationProcessUpdated = true,
            IsRegisteredInDb = true,
            IsSentToAkuiteo = false, // Not used in this method
            Content = contact
        });

        contactReposMock.Verify(x => x.GetContactByEmailOrIdAsync(null, contactRemovedData.ContactId), Times.Once);
        contactReposMock.Verify(x => x.DeleteContactByIdAsync(contactRemovedData.ContactId), Times.Once);
        operationServiceMock.Verify(x => x.UpdateContactOperations(It.IsAny<OperationSearchCriteria>(), It.IsAny<string>()), Times.Once);
    }


    [Fact]
    public async Task OnRemovedContactEventExecution_ShouldReturnFailure_WhenContactNotFound()
    {
        // Arrange
        var contactRemovedData = _fixture.Create<ContactRemovedEventData>();

        contactReposMock.Setup(x => x.GetContactByEmailOrIdAsync(null, contactRemovedData.ContactId))
            .ReturnsAsync((Contact)null); // Simulate not found

        var contactService = new ContactService(loggerMock, contactReposMock.Object, registryProviderMock.Object, operationServiceMock.Object);

        // Act
        var execution = await contactService.OnRemovedContactEventExecution(contactRemovedData);

        // Assert
        execution.IsRegisteredInDb.Should().BeFalse();
        execution.IsOpeationProcessUpdated.Should().BeFalse();
        execution.Content.Should().BeNull();

        contactReposMock.Verify(x => x.GetContactByEmailOrIdAsync(null, contactRemovedData.ContactId), Times.Once);
        contactReposMock.Verify(x => x.DeleteContactByIdAsync(It.IsAny<int>()), Times.Never);
        operationServiceMock.Verify(x => x.UpdateContactOperations(It.IsAny<OperationSearchCriteria>(), It.IsAny<string>()), Times.Never);
    }



    [Fact]
    public async Task OnRemovedContactEventExecution_ShouldReturnFailure_WhenUpdateOperationsFails()
    {
        // Arrange
        var contactRemovedData = _fixture.Create<ContactRemovedEventData>();
        var contact = _fixture.Create<Contact>();

        contactReposMock.Setup(x => x.GetContactByEmailOrIdAsync(null, contactRemovedData.ContactId))
            .ReturnsAsync(contact);

        contactReposMock.Setup(x => x.DeleteContactByIdAsync(contactRemovedData.ContactId))
            .ReturnsAsync(true);

        // Simulate Operation Update Failure
        operationServiceMock.Setup(x => x.UpdateContactOperations(It.IsAny<OperationSearchCriteria>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var contactService = new ContactService(loggerMock, contactReposMock.Object, registryProviderMock.Object, operationServiceMock.Object);

        // Act
        var execution = await contactService.OnRemovedContactEventExecution(contactRemovedData);

        // Assert
        execution.IsRegisteredInDb.Should().BeTrue();
        execution.IsOpeationProcessUpdated.Should().BeFalse();
        execution.Content.Should().Be(contact);

        contactReposMock.Verify(x => x.GetContactByEmailOrIdAsync(null, contactRemovedData.ContactId), Times.Once);
        contactReposMock.Verify(x => x.DeleteContactByIdAsync(contactRemovedData.ContactId), Times.Once);
        operationServiceMock.Verify(x => x.UpdateContactOperations(It.IsAny<OperationSearchCriteria>(), It.IsAny<string>()), Times.Once);
    }


    [Fact]
    public async Task OnRemovedContactEventExecution_ShouldReturnFailure_WhenDeleteAndUpdateOperationsFail()
    {
        // Arrange
        var contactRemovedData = _fixture.Create<ContactRemovedEventData>();
        var contact = _fixture.Create<Contact>();

        contactReposMock.Setup(x => x.GetContactByEmailOrIdAsync(null, contactRemovedData.ContactId))
            .ReturnsAsync(contact);

        // Simulate DB Deletion Failure
        contactReposMock.Setup(x => x.DeleteContactByIdAsync(contactRemovedData.ContactId))
            .ReturnsAsync(false);

        // Simulate Operation Update Failure
        operationServiceMock.Setup(x => x.UpdateContactOperations(It.IsAny<OperationSearchCriteria>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var contactService = new ContactService(loggerMock, contactReposMock.Object, registryProviderMock.Object, operationServiceMock.Object);

        // Act
        var execution = await contactService.OnRemovedContactEventExecution(contactRemovedData);

        // Assert
        execution.IsRegisteredInDb.Should().BeFalse();
        execution.IsOpeationProcessUpdated.Should().BeFalse();
        execution.Content.Should().Be(contact);

        contactReposMock.Verify(x => x.GetContactByEmailOrIdAsync(null, contactRemovedData.ContactId), Times.Once);
        contactReposMock.Verify(x => x.DeleteContactByIdAsync(contactRemovedData.ContactId), Times.Once);
        operationServiceMock.Verify(x => x.UpdateContactOperations(It.IsAny<OperationSearchCriteria>(), It.IsAny<string>()), Times.Once);
    }
}

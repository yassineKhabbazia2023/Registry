// <copyright file="ContactServiceTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Application.Services;
using AutoFixture;
using Castle.Core.Logging;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace ContactRegistry.Application.Tests.Services;

public class ContactServiceTest
{
    private readonly Fixture _fixture;

    public ContactServiceTest()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2000, 1)]
    public async Task ProcessContactAsync_Adds_Account(int contactCsvLenght, int functionTimeCalled)
    {
        // Arrange
        var contacts = _fixture.CreateMany<ContactCsv>(contactCsvLenght);

        var expectedContacts = contacts
            .Select(
                a => new AlxContact
                {
                    Id = a.Id,
                    Email = a.Email,
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    IsActive = a.IsActive,
                    IsCustomer = a.IsCustomer,
                    MobilePhone = a.MobilePhone,
                    LandPhone = a.LandPhone,
                    JobDescription = a.JobDescription,
                    OfficeId = a.OfficeId,
                }).ToList();

        var contacttRepository = new Mock<IContactRepository>(MockBehavior.Strict);
        contacttRepository.Setup(r => r.AddContactsAsync(It.IsAny<IEnumerable<AlxContact>>())).
            Callback<IEnumerable<AlxContact>>(data =>
            {
                data.Should().BeEquivalentTo(expectedContacts);
            })
            .Returns(Task.CompletedTask);
        contacttRepository.Setup(r => r.GetCountContactActifAsync())
            .ReturnsAsync((22, 44));
        var processDeltaTriggerRepositoryMock = new Mock<IProcessDeltaTriggerRepository>(MockBehavior.Strict);
        processDeltaTriggerRepositoryMock.Setup(p => p.UpdateContactProcessAsync(true)).Returns(Task.CompletedTask);
        var loggerMock = new Mock<ILogger<ContactService>>(MockBehavior.Default);

        // Act
        var contactService = new ContactService(loggerMock.Object, contacttRepository.Object, processDeltaTriggerRepositoryMock.Object);
        await contactService.ProcessContactAsync(contacts);

        contacttRepository.VerifyAll();
        contacttRepository.Verify(a => a.AddContactsAsync(It.IsAny<List<AlxContact>>()), Times.AtLeast(functionTimeCalled));
    }

    [Fact]
    public async Task StreamContactsJsonAsync_Writes_ExpectedData()
    {
        // Arrange
        var contact1 = new Domain.Entities.CreContact
        {
            Id = new Guid("ce0b7b12-a2ba-48ea-8014-92e9657a5bd5"),
            OfficeId = new Guid("72ce225c-c8b7-49eb-9e73-f641fce67811"),
            IsCustomer = true,
            IsActive = true,
            Updated = DateTime.UtcNow,
            Deleted = null,
            FirstName = "Test Contact 1",
            LastName = "Last Name 1",
            Email = "test1@example.com",
            LandPhone = "123-456-7890",
            MobilePhone = "987-654-3210",
            JobDescription = "Job Description 1",
            Source = "Source 1",
            Roles = new List<Domain.Entities.CreRole>
            {
                new Domain.Entities.CreRole { RoleId = new Guid("36029043-76eb-4bbf-a452-8f53ec6c94e9"), AccountId = new Guid("ff05e5c7-22b1-4366-9a67-aaa51d6742a0")},
            }
        };

        IEnumerable<Domain.Entities.CreContact> contacts = new List<Domain.Entities.CreContact> { contact1 };

        var options = new JsonSerializerOptions { WriteIndented = true };
        var expectedJsonData = JsonSerializer.Serialize(contacts, options);

        var contactRepository = new Mock<IContactRepository>();
        contactRepository.Setup(r => r.GetContactsAsync()).Returns(GetAsyncEnumerable(contacts));
        contactRepository.Setup(r => r.GetCountContactActifAsync())
            .ReturnsAsync((22, 44));
        var processDeltaTriggerRepositoryMock = new Mock<IProcessDeltaTriggerRepository>(MockBehavior.Default);

        var stream = new MemoryStream();
        var streamWriter = new StreamWriter(stream);
        var loggerMock = new Mock<ILogger<ContactService>>(MockBehavior.Strict);

        var contactService = new ContactService(loggerMock.Object, contactRepository.Object, processDeltaTriggerRepositoryMock.Object);

        // Act
        await contactService.StreamContactsJsonAsync(streamWriter);

        stream.Position = 0;
        var reader = new StreamReader(stream);
        var jsonData = await reader.ReadToEndAsync();

        // Assert
        contactRepository.Verify(c => c.GetContactsAsync(), Times.Once);
    }

    [Fact]
    public async Task ClearAlxAsync_Should_Be_Success()
    {
        var contactRepository = new Mock<IContactRepository>();
        contactRepository.Setup(a => a.ClearAlxAsync()).Returns(Task.CompletedTask);

        var processDeltaTriggerRepositoryMock = new Mock<IProcessDeltaTriggerRepository>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<ContactService>>(MockBehavior.Default);

        var contactService = new ContactService(loggerMock.Object, contactRepository.Object, processDeltaTriggerRepositoryMock.Object);

        await contactService.ClearAlxAsync();

        contactRepository.Verify(a => a.ClearAlxAsync(), Times.Once);
    }

    private async IAsyncEnumerable<Domain.Entities.CreContact> GetAsyncEnumerable(IEnumerable<Domain.Entities.CreContact> contacts)
    {
        foreach (var contact in contacts)
        {
            yield return contact;
        }
    }

    [Fact]
    public async Task InsertContactsAsync_Should_Be_Success()
    {
        var repository = new Mock<IContactRepository>();
        var contactService = new ContactService(null!, repository.Object, null!);

        await contactService.InsertContactsAsync(It.IsAny<IEnumerable<RefContactCsv>>());

        repository.Verify(x => x.AddContactsAsync(It.IsAny<IEnumerable<RefContactCsv>>()), Times.Once);
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
        contactRepository.Setup(r => r.AddContactsAsync(It.IsAny<IEnumerable<RefContactCsv>>())).
            Callback<IEnumerable<RefContactCsv>>(data =>
            {
                data.Count().Should().BeGreaterThanOrEqualTo(contacts.Count());
            })
            .Returns(Task.CompletedTask);

        var processDeltaTriggerRepositoryMock = new Mock<IProcessDeltaTriggerRepository>();

        var loggerMock = new Mock<ILogger<ContactService>>(MockBehavior.Default);

        // Act
        var contactService = new ContactService(loggerMock.Object, contactRepository.Object, processDeltaTriggerRepositoryMock.Object);
        await contactService.InsertContactsAsync(contacts);

        contactRepository.VerifyAll();
        contactRepository.Verify(a => a.AddContactsAsync(It.IsAny<IEnumerable<RefContactCsv>>()), Times.AtLeast(functionTimeCalled));
    }

    [Fact]
    public void ValidateContacts_ShouldReturnErrors_WhenInvalidContactsProvided()
    {
        // Arrange
        var contacts = new List<RefContactCsv>
    {
        new() { Email = "valid@example.com", Operation = "INSERT" },
        new() { Email = "", Operation = "UPDATE" }, // Email vide
        new() { Email = "invalid@example.com", Operation = "INVALID" } // Operation invalide
    };
        var contactRepository = new Mock<IContactRepository>();

        var processDeltaTriggerRepositoryMock = new Mock<IProcessDeltaTriggerRepository>();

        var loggerMock = new Mock<ILogger<ContactService>>(MockBehavior.Default);
        var service = new ContactService(loggerMock.Object, contactRepository.Object, processDeltaTriggerRepositoryMock.Object);

        // Act
        var errors = service.ValidateContacts(contacts);

        // Assert
        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, e => e.Contains("Email is required"));
        Assert.Contains(errors, e => e.Contains("Invalid Operation 'INVALID'"));
    }
}

// <copyright file="ContactServiceTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Application.Services;
using AutoFixture;
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

    [Fact]
    public async Task InsertContactsAsync_Should_Be_Success()
    {
        var repository = new Mock<IContactRepository>();
        var contactService = new ContactService(null!, repository.Object);

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

      
        var loggerMock = new Mock<ILogger<ContactService>>(MockBehavior.Default);

        // Act
        var contactService = new ContactService(loggerMock.Object, contactRepository.Object);
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

        var loggerMock = new Mock<ILogger<ContactService>>(MockBehavior.Default);
        var service = new ContactService(loggerMock.Object, contactRepository.Object);

        // Act
        var errors = service.ValidateContacts(contacts);

        // Assert
        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, e => e.Contains("Email is required"));
        Assert.Contains(errors, e => e.Contains("Invalid Operation 'INVALID'"));
    }
}

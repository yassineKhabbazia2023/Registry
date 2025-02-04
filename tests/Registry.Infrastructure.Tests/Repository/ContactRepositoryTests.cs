// <copyright file="ContactRepositoryTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using AutoFixture;
using Application.Repository;
using Microsoft.EntityFrameworkCore;
using Pulse.ContactRegistry.Domain.Context;
using Application.Models.Contacts;
using Moq;
using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Domain.Entities.Contacts;
using Application.Mappers;

namespace Registry.Infrastructure.Tests.Repository;

public class ContactRepositoryTests
{
    private readonly Fixture _fixture;
    private readonly ILogger<ContactRepository> _logger;


    public ContactRepositoryTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        _logger = Mock.Of<ILogger<ContactRepository>>();

    }
    private DbContextOptions<RefContext> GetDbOptions()
    {
        return new DbContextOptionsBuilder<RefContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }


    [Fact]
    public async Task AddContactAsync_ShouldReturnFalse_IfContactAlreadyExists()
    {
        var contactEntity = _fixture.Create<ContactEntity>();
        var contact = contactEntity.MapContactEntityToModel();

        using (var context = new RefContext(GetDbOptions()))
        {
            context.Add(contactEntity);
            context.SaveChanges();

            var repos = new ContactRepository(context, _logger);

            var result = await repos.AddContactAsync(contact);

            result.Should().Be(false);
        }
    }


    [Fact]
    public async Task AddContactAsync_ShouldReturnTrue_IfContactDoesNotExists()
    {
        var contactEntity = _fixture.Create<ContactEntity>();
        var contact = contactEntity.MapContactEntityToModel();

        using (var context = new RefContext(GetDbOptions()))
        {

            var repos = new ContactRepository(context, _logger);

            var result = await repos.AddContactAsync(contact);

            result.Should().Be(true);
        }
    }


    [Fact]
    public async Task UpdateContactAsync_ShouldReturnFalse_IfContactDoesNotExists()
    {
        var contactEntity = _fixture.Create<ContactEntity>();
        var contact = contactEntity.MapContactEntityToModel();
        using (var context = new RefContext(GetDbOptions()))
        {
            var repos = new ContactRepository(context, _logger);

            var result = await repos.UpdateContactAsync(contact);

            result.Should().Be(false);
        }
    }

    [Fact]
    public async Task UpdateContactAsync_ShouldReturnTrue_IfContactExists()
    {

        var contactEntity = _fixture.Create<ContactEntity>();
        var contact = contactEntity.MapContactEntityToModel();
        using (var context = new RefContext(GetDbOptions()))
        {

            var repos = new ContactRepository(context, _logger);
            await repos.AddContactAsync(contact);

            var updatedContact = contact;
            updatedContact.PersonaName = "UpdatedPersona";
            updatedContact.FirstName = "UpdatedFirstName";
            var result = await repos.UpdateContactAsync(updatedContact);

            result.Should().Be(true);

            var contactAfterUpdate = await context.ContactEntities.FirstOrDefaultAsync(c => c.ContactId == contact.ContactId);
            contactAfterUpdate.Should().NotBeNull();
            contactAfterUpdate.PersonaName.Should().Be(updatedContact.PersonaName);
            contactAfterUpdate.FirstName.Should().Be(updatedContact.FirstName);
        }
    }

    [Fact]
    public async Task DeleteContactAsync_ShouldReturnFalseIfContactId_IsNull()
    {
        int contactId = 0;

        ContactRepository contactRepository = new ContactRepository(new RefContext(GetDbOptions()), _logger);

        var result = await contactRepository.DeleteContactAsync(contactId);

        result.Should().Be(false);
    }

    [Fact]
    public async Task DeleteContactAsync_ShouldReturnFalse_IfContactDoesNotExist()
    {
        int contactId = 122;

        ContactRepository contactRepository = new ContactRepository(new RefContext(GetDbOptions()), _logger);

        var result = await contactRepository.DeleteContactAsync(contactId);

        result.Should().Be(false);
    }


    [Fact]
    public async Task DeleteContactAsync_ShouldReturnTrue_IfContactExists()
    {
        var contactEntity = _fixture.Create<ContactEntity>();
        var contact = contactEntity.MapContactEntityToModel();

        using (var context = new RefContext(GetDbOptions()))
        {
            context.ContactEntities.Add(contactEntity);
            context.SaveChanges();

            var contactRepos = new ContactRepository(context, _logger);
            var result = await contactRepos.DeleteContactAsync(contact.ContactId);

            result.Should().Be(true);
            var afterDeletedContact = await context.ContactEntities.FirstOrDefaultAsync(x => x.ContactId == contact.ContactId);
            afterDeletedContact.Should().BeNull();
        }
    }

    [Fact]
    public async Task GetContactAsync_ShouldReturnContact_IfEmailExists()
    {
        var contactEntity = _fixture.Create<ContactEntity>();

        using (var context = new RefContext(GetDbOptions()))
        {
            context.ContactEntities.Add(contactEntity);
            context.SaveChanges();

            var contactRepos = new ContactRepository(context, _logger);
            var result = await contactRepos.GetContactAsync(email: contactEntity.Email);

            result.Should().NotBeNull();
            result.Email.Should().Be(contactEntity.Email);
        }
    }

    [Fact]
    public async Task GetContactAsync_ShouldReturnContact_IfContactIdExists()
    {
        var contactEntity = _fixture.Create<ContactEntity>();

        using (var context = new RefContext(GetDbOptions()))
        {
            context.ContactEntities.Add(contactEntity);
            context.SaveChanges();

            var contactRepos = new ContactRepository(context, _logger);
            var result = await contactRepos.GetContactAsync(contactId: contactEntity.ContactId);

            result.Should().NotBeNull();
            result.Email.Should().Be(contactEntity.Email);
        }
    }

    [Fact]
    public async Task GetContactAsync_ShouldReturnNull_IfContactDoesNotExists()
    {
        var contactEntity = _fixture.Create<ContactEntity>();

        using (var context = new RefContext(GetDbOptions()))
        {
            context.ContactEntities.AddRange(contactEntity);
            context.SaveChanges();
            int contactIdToSearch = contactEntity.ContactId - 3;

            var contactRepos = new ContactRepository(context, _logger);
            var result = await contactRepos.GetContactAsync(contactId: contactIdToSearch);

            result.Should().BeNull();
        }
    }

    [Fact]
    public async Task IsContactExisted_ShouldReturnTrue_IfEmailExists()
    {
        var contactEntity = _fixture.Create<ContactEntity>();

        using (var context = new RefContext(GetDbOptions()))
        {
            context.ContactEntities.Add(contactEntity);
            context.SaveChanges();

            var contactRepos = new ContactRepository(context, _logger);
            var result = await contactRepos.IsContactExisted(email: contactEntity.Email);

            result.Should().BeTrue();
        }
    }

    [Fact]
    public async Task IsContactExisted_ShouldReturn_True_IfContactIdExists()
    {
        var contactEntity = _fixture.Create<ContactEntity>();

        using (var context = new RefContext(GetDbOptions()))
        {
            context.ContactEntities.Add(contactEntity);
            context.SaveChanges();

            var contactRepos = new ContactRepository(context, _logger);
            var result = await contactRepos.IsContactExisted(contactId: contactEntity.ContactId);

            result.Should().BeTrue();
        }
    }

    [Fact]
    public async Task IsContactExisted_ShouldReturn_False_IfContactIdOrEmail_DoesNotExists()
    {
        using (var context = new RefContext(GetDbOptions()))
        {
            var contactRepos = new ContactRepository(context, _logger);
            var result = await contactRepos.IsContactExisted(contactId: 4);

            result.Should().BeFalse();
        }
    }
}

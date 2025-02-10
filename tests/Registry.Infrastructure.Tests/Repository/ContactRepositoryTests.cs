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
using Pulse.ContactRegistry.Domain.Entities;
using Registry.Application.Consts;
using Application.Consts;
using Pulse.ContactRegistry.Domain.Entities;
using Registry.Application.Consts;

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
            var guid = Guid.NewGuid();
            updatedContact.ContactGlobalUniqueId = guid;
            var result = await repos.UpdateContactAsync(updatedContact);

            result.Should().Be(true);

            var contactAfterUpdate = await context.ContactEntities.FirstOrDefaultAsync(c => c.ContactId == contact.ContactId);
            contactAfterUpdate.Should().NotBeNull();
            contactAfterUpdate.ContactGlobalUniqueId.Should().Be(updatedContact.ContactGlobalUniqueId);
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

    [Fact]
    public async Task GetRefContactsPagedAsync_ShouldReturnAllRecords_WhenNoLastEntityIdProvided()
    {
        using (var context = new RefContext(GetDbOptions()))
        {
            // Arrange
            var contact1 = _fixture.Build<RefContactEntity>()
                .With(x => x.EntityId, Guid.NewGuid())
                .Create();
            var contact2 = _fixture.Build<RefContactEntity>()
                .With(x => x.EntityId, Guid.NewGuid())
                .Create();
            var contact3 = _fixture.Build<RefContactEntity>()
                .With(x => x.EntityId, Guid.NewGuid())
                .Create();

            context.RefContactEntity.AddRange(contact1, contact2, contact3);
            context.SaveChanges();

            var repos = new ContactRepository(context, _logger);

            // Act
            var results = await repos.GetContactsWithoutOperationsPagedAsync(10);

            // Assert
            results.Should().HaveCount(3);
            results.Select(x => x.EntityId).Should().BeInAscendingOrder();
        }
    }

    [Fact]
    public async Task GetRefContactsPagedAsync_ShouldReturnRecords_GreaterThanLastEntityId()
    {
        using (var context = new RefContext(GetDbOptions()))
        {
            // Arrange
            var guid1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var guid2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var guid3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var guid4 = Guid.Parse("00000000-0000-0000-0000-000000000004");

            var contact1 = _fixture.Build<RefContactEntity>()
                .With(x => x.EntityId, guid1)
                .Create();
            var contact2 = _fixture.Build<RefContactEntity>()
                .With(x => x.EntityId, guid2)
                .Create();
            var contact3 = _fixture.Build<RefContactEntity>()
                .With(x => x.EntityId, guid3)
                .Create();
            var contact4 = _fixture.Build<RefContactEntity>()
                .With(x => x.EntityId, guid4)
                .Create();

            context.RefContactEntity.AddRange(contact1, contact2, contact3, contact4);
            context.SaveChanges();

            var repos = new ContactRepository(context, _logger);
            // Act
            var results = await repos.GetContactsWithoutOperationsPagedAsync(10, guid2);

            // Assert
            results.Should().HaveCount(2);
            results.Select(x => x.EntityId).Should().OnlyContain(id => id.CompareTo(guid2) > 0);
        }
    }

    [Fact]
    public async Task GetRefContactsPagedAsync_ShouldNotReturnEntities_WithAssociatedRegOperation()
    {
        using (var context = new RefContext(GetDbOptions()))
        {
            // Arrange
            var contactWithoutOperation = _fixture.Build<RefContactEntity>()
                .With(x => x.EntityId, Guid.NewGuid())
                .Create();
            var contactWithOperation = _fixture.Build<RefContactEntity>()
                .With(x => x.EntityId, Guid.NewGuid())
                .Create();

            context.RefContactEntity.AddRange(contactWithoutOperation, contactWithOperation);

         
            var regOperation = new RegOperationEntity
            {
                EntityId = contactWithOperation.EntityId,
                ApprovalStatus = ApprovalStatus.Approved,
                CreationDate = DateTime.Now,
                ProcessStatus = ProcessStatus.Ready,
                Operation = OperationName.Insert
            };
            context.RegOperationEntity.Add(regOperation);
            context.SaveChanges();

            var repos = new ContactRepository(context, _logger);

            // Act
            var results = await repos.GetContactsWithoutOperationsPagedAsync(10);

            // Assert
            results.Should().HaveCount(1);
            results.First().EntityId.Should().Be(contactWithoutOperation.EntityId);
        }
    }

    [Fact]
    public async Task IsContactExistsAsync_ShouldReturnTrue_IfMatchingContactExists()
    {
        // Arrange
        var contactEntity = _fixture.Build<ContactEntity>()
            .With(c => c.Email, "alice.smith@example.com")
            .Create();

        using (var context = new RefContext(GetDbOptions()))
        {
            context.ContactEntities.Add(contactEntity);
            context.SaveChanges();

            var repos = new ContactRepository(context, _logger);

            // Act
            var exists = await repos.DoesContactExistAsync("alice.smith@example.com");

            // Assert
            exists.Should().BeTrue();
        }
    }

    [Fact]
    public async Task IsContactExistsAsync_ShouldReturnFalse_IfNoMatchingContactExists()
    {
        using (var context = new RefContext(GetDbOptions()))
        {
            var repos = new ContactRepository(context, _logger);

            // Act
            var exists = await repos.DoesContactExistAsync("bob.brown@example.com");

            // Assert
            exists.Should().BeFalse();
        }
    }

    [Fact]
    public async Task InsertContactNewAudit_ShouldAddAuditRecord()
    {
        // Arrange
        var refContact = _fixture.Build<RefContactEntity>()
            .With(x => x.EntityId, Guid.NewGuid())
            .Create();
        string reason = "Audit for new contact";

        using (var context = new RefContext(GetDbOptions()))
        {
            var repos = new ContactRepository(context, _logger);

            // Act
            await repos.InsertContactNewAudit(refContact, reason);

            // Assert
            var audit = await context.DeepValidationEntities
                .FirstOrDefaultAsync(a => a.EntityId == refContact.EntityId);

            audit.Should().NotBeNull();
            audit.Type.Should().Be("CONTACT");
            audit.Reason.Should().Be(reason);
        }
    }

    [Fact]
    public async Task DoesContactExistsInOperations_ShouldThrowNullIfEmailArgumentsIsNull()
    {
        string email = string.Empty;
        string operationType = "INSERT";
        string processStatus = "READY";
        using (var context = new RefContext(GetDbOptions()))
        {
            var contactRepos = new ContactRepository(context, _logger);
            var action = async () => await contactRepos.DoesContactExistInOperations(email, operationType, processStatus);
            await action.Should().ThrowAsync<ArgumentException>();
        }
    }
    [Fact]
    public async Task DoesContactExistsInOperations_ShouldThrowNullIfOperationArgumentsIsNull()
    {
        string email = "valid@test.com";
        string operationType = string.Empty;
        string processStatus = "READY";
        using (var context = new RefContext(GetDbOptions()))
        {
            var contactRepos = new ContactRepository(context, _logger);
            var action = async () => await contactRepos.DoesContactExistInOperations(email, operationType, processStatus);
            await action.Should().ThrowAsync<ArgumentException>();
        }
    }

    [Fact]
    public async Task DoesContactExistsInOperations_ShouldThrowNullIfProcessStatusArgumentsIsNull()
    {
        string email = "valid@test.com";
        string operationType = "INSERT";
        string? processStatus = null;
        using (var context = new RefContext(GetDbOptions()))
        {
            var contactRepos = new ContactRepository(context, _logger);
            var action = async () => await contactRepos.DoesContactExistInOperations(email, operationType, processStatus);
            await action.Should().ThrowAsync<ArgumentException>();
        }
    }


    [Fact]
    public async Task DoesContactExistsInOperations_ShouldReturnFalseIfEmailDoesNotExists()
    {
        string email = "valid@test.com";
        string operationType = "INSERT";
        string? processStatus = "READY";
        using (var context = new RefContext(GetDbOptions()))
        {
            var contactRepos = new ContactRepository(context, _logger);
            var action =  await contactRepos.DoesContactExistInOperations(email, operationType, processStatus);
            action.Should().BeFalse();
        }
    }

    [Fact]
    public async Task DoesContactExistsInOperations_ShouldReturnTrueIfEmailExistsInOperations()
    {
        string email = "valid@test.com";
        string operationType = "INSERT";
        string? processStatus = "READY";
        using (var context = new RefContext(GetDbOptions()))
        {
            RefContactEntity refContactEntity = new RefContactEntity()
            {
                ContactFlagStatus = 1,
                Email = email,
                EntityId = Guid.NewGuid(),
                FirstName = "Hakouna",
                LastName = "Matata",
                IsCustomer = true,
                JobDescription = "Toilet Paper",
                LandPhone = "09989898",
                MobilePhone = "88768899",
                OfficeCode = "543",
                OperationDate = DateTime.Now,
                OperationType = operationType,
            };

            RegOperationEntity operationEntity = new RegOperationEntity
            {
                ApprovalStatus = ApprovalStatus.Approved,
                EntityId = refContactEntity.EntityId,
                CreationDate = DateTime.Now,
                Id = 1,
                LastStatusApprovalDate = DateTime.Now,
                LastStatusProcessedDate = DateTime.Now,
                Operation = operationType,
                ProcessStatus = processStatus,
                PublishedAt = DateTime.Now,
                Type = "CONTACT"
            };
            context.RefContactEntity.Add(refContactEntity);
            context.RegOperationEntity.Add(operationEntity);
            context.SaveChanges();

            var contactRepos = new ContactRepository(context, _logger);
            var action = await contactRepos.DoesContactExistInOperations(email, operationType, processStatus);
            action.Should().BeTrue();
        }
    }





}

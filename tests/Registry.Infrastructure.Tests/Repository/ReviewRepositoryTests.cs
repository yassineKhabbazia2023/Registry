// <copyright file="ReviewRepositoryTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Pulse.Registry.Domain.Entities.Accounts;
using Pulse.Registry.Domain.Entities.Contacts;
using Registry.Application.Consts;

namespace Registry.Infrastructure.Tests.Repository;

public class ReviewRepositoryTests
{
    private readonly DbContextOptions<RefContext> _dbContextOptions;

    public ReviewRepositoryTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<RefContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .EnableSensitiveDataLogging()
            .Options;
    }

    [Fact]
    public async Task ReviewChangeEmail_CaseSameNameSameTel()
    {
        // Arrange
        using var context = new RefContext(_dbContextOptions);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var repository = new ReviewRepository(context);
        Guid guid1, guid2, guid3;

        guid1 = Guid.NewGuid();
        var operationInsertContact = new RegOperationEntity
        {
            ApprovalStatus = ApprovalStatus.Approved,
            EntityId = guid1,
            Operation = OperationAction.Insert,
            Type = "CONTACT",
            ProcessStatus = ProcessStatus.Ready
        };
        var refInsertContact = new RefContactEntity
        {
            OperationType = OperationAction.Insert,
            EntityId = guid1,
            FirstName = "fname1",
            LastName = "lname1",
            LandPhone = "1234567890",
            Email = "test@test.com",
            OperationDate = DateTime.Now,
        };
        context.RefContactEntity.Add(refInsertContact);
        context.RegOperationEntity.Add(operationInsertContact);

        guid2 = Guid.NewGuid();
        var operationDeleteContact = new RegOperationEntity
        {
            ApprovalStatus = ApprovalStatus.Approved,
            EntityId = guid2,
            Operation = OperationAction.Delete,
            Type = "CONTACT",
            ProcessStatus = ProcessStatus.Ready
        };
        var refDeleteContact = new RefContactEntity
        {
            OperationType = OperationAction.Delete,
            EntityId = guid2,
            FirstName = "fname1",
            LastName = "lname1",
            LandPhone = "1234567890",
            Email = "test2@test.com",
            OperationDate = DateTime.Now,
        };
        context.RefContactEntity.Add(refDeleteContact);
        context.RegOperationEntity.Add(operationDeleteContact);

        guid3 = Guid.NewGuid();
        var operationDeleteRole = new RegOperationEntity
        {
            ApprovalStatus = ApprovalStatus.Approved,
            EntityId = guid3,
            Operation = OperationAction.Insert,
            Type = "ROLE",
            ProcessStatus = ProcessStatus.Ready
        };
        var refDeleteRole = new RefRoleEntity
        {
            EntityId = guid3,
            ContactEmail = "test@test.com",
            AccountNumber = "sdfsd",
            OperationType = OperationAction.Insert,
            OperationDate = DateTime.Now,
        };
        context.RefRoleEntity.Add(refDeleteRole);
        context.RegOperationEntity.Add(operationDeleteRole);

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear(); // Clear tracker in EF of arrange step

        // Act
        await repository.ReviewChangeEmailAsync();
        var operationInsert = context.RegOperationEntity.FirstOrDefault(x => x.Operation == OperationAction.Insert && x.Type == "CONTACT" && x.EntityId == guid1);
        var operationDelete = context.RegOperationEntity.FirstOrDefault(x => x.Operation == OperationAction.Delete && x.EntityId == guid2);
        var operationUpdate = context.RegOperationEntity.FirstOrDefault(x => x.Operation == OperationAction.Update);
        var operationRole = context.RegOperationEntity.FirstOrDefault(x => x.Type == "ROLE" && x.EntityId == guid2);

        // Assert
        Assert.Null(operationInsert);
        Assert.Null(operationDelete);
        Assert.Null(operationRole);
        Assert.NotNull(operationUpdate);
        Assert.Equal(guid1, operationUpdate.EntityId);
        Assert.Equal("test2@test.com", operationUpdate.OldContactEmail);
    }

    [Fact]
    public async Task ReviewChangeEmail_CaseSameNameNoneTel()
    {
        // Arrange
        using var context = new RefContext(_dbContextOptions);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var repository = new ReviewRepository(context);
        Guid guid1, guid2, guid3;

        guid1 = Guid.NewGuid();
        var operationInsertContact = new RegOperationEntity
        {
            ApprovalStatus = ApprovalStatus.Approved,
            EntityId = guid1,
            Operation = OperationAction.Insert,
            Type = "CONTACT",
            ProcessStatus = ProcessStatus.Ready
        };
        var refInsertContact = new RefContactEntity
        {
            OperationType = OperationAction.Insert,
            EntityId = guid1,
            FirstName = "fname2",
            LastName = "lname2",
            LandPhone = null,
            Email = "test@test.com",
            OperationDate = DateTime.Now,
        };
        context.RefContactEntity.Add(refInsertContact);
        context.RegOperationEntity.Add(operationInsertContact);

        guid2 = Guid.NewGuid();
        var operationDeleteContact = new RegOperationEntity
        {
            ApprovalStatus = ApprovalStatus.Approved,
            EntityId = guid2,
            Operation = OperationAction.Delete,
            Type = "CONTACT",
            ProcessStatus = ProcessStatus.Ready
        };
        var refDeleteContact = new RefContactEntity
        {
            OperationType = OperationAction.Delete,
            EntityId = guid2,
            FirstName = "fname2",
            LastName = "lname2",
            LandPhone = null,
            Email = "test2@test.com",
            OperationDate = DateTime.Now,
        };
        context.RefContactEntity.Add(refDeleteContact);
        context.RegOperationEntity.Add(operationDeleteContact);

        guid3 = Guid.NewGuid();
        var operationDeleteRole = new RegOperationEntity
        {
            ApprovalStatus = ApprovalStatus.Approved,
            EntityId = guid3,
            Operation = OperationAction.Insert,
            Type = "ROLE",
            ProcessStatus = ProcessStatus.Ready
        };
        var refDeleteRole = new RefRoleEntity
        {
            EntityId = guid3,
            ContactEmail = "test@test.com",
            AccountNumber = "sdfsd",
            OperationType = OperationAction.Insert,
            OperationDate = DateTime.Now,
        };
        context.RefRoleEntity.Add(refDeleteRole);
        context.RegOperationEntity.Add(operationDeleteRole);

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear(); // Clear tracker in EF of arrange step

        // Act
        await repository.ReviewChangeEmailAsync();
        var operationInsert = context.RegOperationEntity.FirstOrDefault(x => x.Operation == OperationAction.Insert && x.Type == "CONTACT");
        var operationDelete = context.RegOperationEntity.FirstOrDefault(x => x.Operation == OperationAction.Delete);
        var operationUpdate = context.RegOperationEntity.FirstOrDefault(x => x.Operation == OperationAction.Update);
        var operationRole = context.RegOperationEntity.FirstOrDefault(x => x.Type == "ROLE");

        // Assert
        Assert.Null(operationInsert);
        Assert.Null(operationDelete);
        Assert.Null(operationRole);
        Assert.NotNull(operationUpdate);
        Assert.Equal(guid1, operationUpdate.EntityId);
        Assert.Equal("test2@test.com", operationUpdate.OldContactEmail);
    }

    [Fact]
    public async Task ReviewChangeEmail_CaseSameNameDiffTel()
    {
        // Arrange
        using var context = new RefContext(_dbContextOptions);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var repository = new ReviewRepository(context);
        Guid guid1, guid2, guid3;

        guid1 = Guid.NewGuid();
        var operationInsertContact = new RegOperationEntity
        {
            ApprovalStatus = ApprovalStatus.Approved,
            EntityId = guid1,
            Operation = OperationAction.Insert,
            Type = "CONTACT",
            ProcessStatus = ProcessStatus.Ready
        };
        var refInsertContact = new RefContactEntity
        {
            OperationType = OperationAction.Insert,
            EntityId = guid1,
            FirstName = "fname3",
            LastName = "lname3",
            LandPhone = "12345",
            Email = "test@test.com",
            OperationDate = DateTime.Now,
        };
        context.RefContactEntity.Add(refInsertContact);
        context.RegOperationEntity.Add(operationInsertContact);

        guid2 = Guid.NewGuid();
        var operationDeleteContact = new RegOperationEntity
        {
            ApprovalStatus = ApprovalStatus.Approved,
            EntityId = guid2,
            Operation = OperationAction.Delete,
            Type = "CONTACT",
            ProcessStatus = ProcessStatus.Ready
        };
        var refDeleteContact = new RefContactEntity
        {
            OperationType = OperationAction.Delete,
            EntityId = guid2,
            FirstName = "fname3",
            LastName = "lname3",
            LandPhone = null,
            Email = "test2@test.com",
            OperationDate = DateTime.Now,
        };
        context.RefContactEntity.Add(refDeleteContact);
        context.RegOperationEntity.Add(operationDeleteContact);

        var operationDeleteRole = new RegOperationEntity
        {
            ApprovalStatus = ApprovalStatus.Approved,
            EntityId = guid2,
            Operation = OperationAction.Insert,
            Type = "ROLE",
            ProcessStatus = ProcessStatus.Ready
        };
        var refDeleteRole = new RefRoleEntity
        {
            EntityId = guid2,
            ContactEmail = "test@test.com",
            AccountNumber = "sdfsd",
            OperationType = OperationAction.Insert,
            OperationDate = DateTime.Now,
        };
        context.RefRoleEntity.Add(refDeleteRole);
        context.RegOperationEntity.Add(operationDeleteRole);

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear(); // Clear tracker in EF of arrange step

        // Act
        await repository.ReviewChangeEmailAsync();
        var operationInsert = context.RegOperationEntity.FirstOrDefault(x => x.Operation == OperationAction.Insert && x.Type == "CONTACT");
        var operationDelete = context.RegOperationEntity.FirstOrDefault(x => x.Operation == OperationAction.Delete);
        var operationUpdate = context.RegOperationEntity.FirstOrDefault(x => x.Operation == OperationAction.Update);
        var operationRole = context.RegOperationEntity.FirstOrDefault(x => x.Type == "ROLE");

        // Assert
        Assert.NotNull(operationInsert);
        Assert.NotNull(operationDelete);
        Assert.NotNull(operationRole);
        Assert.Null(operationUpdate);
    }

    [Fact]
    public async Task ReviewChangeEmail_CaseSameNameSameTel_UpdateRoleEmail()
    {
        // Arrange
        using var context = new RefContext(_dbContextOptions);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var repository = new ReviewRepository(context);
        Guid guid1, guid2, guid3;

        guid1 = Guid.NewGuid();
        var operationInsertContact = new RegOperationEntity
        {
            ApprovalStatus = ApprovalStatus.Approved,
            EntityId = guid1,
            Operation = OperationAction.Insert,
            Type = "CONTACT",
            ProcessStatus = ProcessStatus.Ready
        };
        var refInsertContact = new RefContactEntity
        {
            OperationType = OperationAction.Insert,
            EntityId = guid1,
            FirstName = "fname1",
            LastName = "lname1",
            LandPhone = "1234567890",
            Email = "test@test.com",
            OperationDate = DateTime.Now,
        };
        context.RefContactEntity.Add(refInsertContact);
        context.RegOperationEntity.Add(operationInsertContact);

        guid2 = Guid.NewGuid();
        var operationDeleteContact = new RegOperationEntity
        {
            ApprovalStatus = ApprovalStatus.Approved,
            EntityId = guid2,
            Operation = OperationAction.Delete,
            Type = "CONTACT",
            ProcessStatus = ProcessStatus.Ready
        };
        var refDeleteContact = new RefContactEntity
        {
            OperationType = OperationAction.Delete,
            EntityId = guid2,
            FirstName = "fname1",
            LastName = "lname1",
            LandPhone = "1234567890",
            Email = "test2@test.com",
            OperationDate = DateTime.Now,
        };
        context.RefContactEntity.Add(refDeleteContact);
        context.RegOperationEntity.Add(operationDeleteContact);

        guid3 = Guid.NewGuid();
        var operationDeleteRole = new RegOperationEntity
        {
            ApprovalStatus = ApprovalStatus.Approved,
            EntityId = guid3,
            Operation = OperationAction.Insert,
            Type = "ROLE",
            ProcessStatus = ProcessStatus.Ready
        };
        var refDeleteRole = new RefRoleEntity
        {
            EntityId = guid3,
            ContactEmail = "test@test.com",
            AccountNumber = "sdfsd",
            OperationType = OperationAction.Insert,
            OperationDate = DateTime.Now,
        };
        context.RefRoleEntity.Add(refDeleteRole);
        context.RegOperationEntity.Add(operationDeleteRole);

        var guidAccount = Guid.NewGuid();
        var account = new AccountEntity
        {
            AccountId = 1,
            AccountNumber = "AccountNumber",
            AccountGlobalUniqueId = guidAccount,
            LegalName = "LegalName",
            CreatedBy = "test",
            ModifiedBy = "test"
        };
        context.AccountEntity.Add(account);

        var guidContact = Guid.NewGuid();
        var contact = new ContactEntity
        {
            ContactId = 1,
            Email = "test2@test.com",
            ContactGlobalUniqueId = guidContact,
            FirstName = "fname1",
            LastName = "lname1",
            Type = "Customer"
        };
        context.ContactEntity.Add(contact);

        var role = new RoleEntity
        {
            AccountId = 1,
            ContactId = 1,
            ContactEmail = "test2@test.com",
            ContactGlobalUniqueId = guidContact,
            AccountGlobalUniqueId = guidAccount,
            AccountNumber = "AccountNumber",
            RoleDuplicatesCounter = 1
        };
        context.RoleEntity.Add(role);

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear(); // Clear tracker in EF of arrange step

        // Act
        await repository.ReviewChangeEmailAsync();
        var operationInsert = context.RegOperationEntity.FirstOrDefault(x => x.Operation == OperationAction.Insert && x.Type == "CONTACT" && x.EntityId == guid1);
        var operationDelete = context.RegOperationEntity.FirstOrDefault(x => x.Operation == OperationAction.Delete && x.EntityId == guid2);
        var operationUpdate = context.RegOperationEntity.FirstOrDefault(x => x.Operation == OperationAction.Update);
        var operationRole = context.RegOperationEntity.FirstOrDefault(x => x.Type == "ROLE" && x.EntityId == guid2);
        var roleUpdated = context.RoleEntity.Where(r => r.AccountId == 1 && r.ContactId == 1).First();

        // Assert
        Assert.Null(operationInsert);
        Assert.Null(operationDelete);
        Assert.Null(operationRole);
        Assert.NotNull(operationUpdate);
        Assert.Equal(guid1, operationUpdate.EntityId);
        Assert.Equal("test2@test.com", operationUpdate.OldContactEmail);
        Assert.Equal("test@test.com", roleUpdated.ContactEmail);
    }
}

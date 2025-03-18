// <copyright file="ReviewRepositoryTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using EFCore.BulkExtensions;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;
using OperationType = EFCore.BulkExtensions.OperationType;

namespace Registry.Infrastructure.Tests.Repository
{
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
        public async Task ReviewChangeEmail_CaseNormal()
        {
            // Arrange
            using var context = new RefContext(_dbContextOptions);
            var repository = new ReviewRepository(context);
            Guid guid1, guid2, guid3;

            guid1 = Guid.NewGuid();
            var operationInsertContact = new RegOperationEntity
            {
                ApprovalStatus = ApprovalStatus.Approved,
                EntityId = guid1,
                Operation = OperationName.Insert,
                Type = "CONTACT",
                ProcessStatus = ProcessStatus.Ready
            };
            var refInsertContact = new RefContactEntity
            {
                OperationType = OperationName.Insert,
                EntityId = guid1,
                FirstName = "fname",
                LastName = "lname",
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
                Operation = OperationName.Delete,
                Type = "CONTACT",
                ProcessStatus = ProcessStatus.Ready
            };
            var refDeleteContact = new RefContactEntity
            {
                OperationType = OperationName.Delete,
                EntityId = guid2,
                FirstName = "fname",
                LastName = "lname",
                LandPhone = "1234567890",
                Email = "test2@test.com",
                OperationDate = DateTime.Now,
            };
            context.RefContactEntity.Add(refDeleteContact);
            context.RegOperationEntity.Add(operationDeleteContact);

            var operationDeleteRole = new RegOperationEntity
            {
                ApprovalStatus = ApprovalStatus.Approved,
                EntityId = guid2,
                Operation = OperationName.Insert,
                Type = "ROLE",
                ProcessStatus = ProcessStatus.Ready
            };
            var refDeleteRole = new RefRoleEntity
            {
                EntityId = guid2,
                ContactEmail = "test@test.com",
                AccountNumber = "sdfsd",
                OperationType = OperationName.Insert,
                OperationDate = DateTime.Now,
            };
            context.RefRoleEntity.Add(refDeleteRole);
            context.RegOperationEntity.Add(operationDeleteRole);

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear(); // Clear tracker in EF of arrange step

            // Act
            await repository.ReviewChangeEmailAsync();
            var operationInsert = context.RegOperationEntity.FirstOrDefault(x => x.Operation == OperationName.Insert && x.Type == "CONTACT");
            var operationDelete = context.RegOperationEntity.FirstOrDefault(x => x.Operation == OperationName.Delete);
            var operationUpdate = context.RegOperationEntity.FirstOrDefault(x => x.Operation == OperationName.Update);
            var operationRole = context.RegOperationEntity.FirstOrDefault(x => x.Type == "ROLE");

            // Assert
            Assert.Null(operationInsert);
            Assert.Null(operationDelete);
            Assert.Null(operationRole);
            Assert.NotNull(operationUpdate);
            Assert.Equal(guid1, operationUpdate.EntityId);
            Assert.Equal("test2@test.com", operationUpdate.OldContactEmail);
        }
    }
}

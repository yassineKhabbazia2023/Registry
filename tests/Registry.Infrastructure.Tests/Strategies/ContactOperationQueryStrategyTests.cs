using Application.Consts;
using Application.Requests;
using FluentAssertions;
using Infrastructure.Strategies;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;

namespace Registry.Infrastructure.Tests.Strategies
{
    public class ContactOperationQueryStrategyTests
    {
        private DbContextOptions<RefContext> CreateInMemoryOptions(string databaseName)
        {
            return new DbContextOptionsBuilder<RefContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
        }

        private ContactOperationQueryStrategy CreateStrategy(RefContext context)
        {
            return new ContactOperationQueryStrategy(context);
        }

        #region BuildQuery Tests

        [Fact]
        public async Task BuildQuery_ReturnsMatchingContactOperation_WhenAllCriteriaMatch()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(BuildQuery_ReturnsMatchingContactOperation_WhenAllCriteriaMatch));
            var expectedOperationName = OperationAction.Insert;
            var expectedApprovalStatus = "PENDING";
            var expectedProcessStatuses = new string[] { ProcessStatus.Sent, ProcessStatus.Failed };
            var expectedEmail = "contact@test.com";
            var entityId = Guid.NewGuid();

            var criteria = new OperationSearchCriteria
            {
                OperationName = expectedOperationName,
                OperationApprovalStatus = expectedApprovalStatus,
                OperationProcessStatus = expectedProcessStatuses,
            };

            using (var context = new RefContext(options))
            {
                var regOperation = new RegOperationEntity
                {
                    Id = 1,
                    Operation = expectedOperationName,
                    CreationDate = DateTime.UtcNow,
                    EntityId = entityId,
                    LastStatusApprovalDate = DateTime.UtcNow,
                    LastStatusApprovalBy = "contact@test.com",
                    PublishedAt = null,
                    ApprovalStatus = expectedApprovalStatus,
                    Type = "CONTACT",
                    ProcessStatus = ProcessStatus.Sent
                };
                context.RegOperationEntity.Add(regOperation);

                var refContact = new RefContactEntity
                {
                    EntityId = entityId,
                    Email = expectedEmail,
                    FirstName = "Contact",
                    LastName = "Tester",
                    OperationType = OperationAction.Insert
                };
                context.RefContactEntity.Add(refContact);

                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new RefContext(options))
            {
                var strategy = CreateStrategy(context);
                var query = strategy.BuildQuery(criteria, expectedEmail, false);
                var results = await query.ToListAsync();

                results.Should().HaveCount(1);
                var result = results.First();
                result.Operation.Should().Be(expectedOperationName);
                result.ApprovalStatus.Should().Be(expectedApprovalStatus);
            }
        }

        [Fact]
        public async Task BuildQuery_FiltersOutContactOperation_WhenApprovalStatusDoesNotMatch()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(BuildQuery_FiltersOutContactOperation_WhenApprovalStatusDoesNotMatch));
            var expectedOperationName = OperationAction.Insert;
            var criteria = new OperationSearchCriteria
            {
                OperationName = expectedOperationName,
                OperationApprovalStatus = "APPROVED",
                OperationProcessStatus = new string[] { ProcessStatus.Sent, ProcessStatus.Failed }
            };
            var expectedEmail = "contact@test.com";
            var entityId = Guid.NewGuid();

            using (var context = new RefContext(options))
            {
                // Seed a record with "PENDING" approval status.
                var regOperation = new RegOperationEntity
                {
                    Id = 2,
                    Operation = expectedOperationName,
                    CreationDate = DateTime.UtcNow,
                    EntityId = entityId,
                    LastStatusApprovalDate = DateTime.UtcNow,
                    LastStatusApprovalBy = "contact@test.com",
                    PublishedAt = null,
                    ApprovalStatus = "PENDING",
                    Type = "CONTACT",
                    ProcessStatus = ProcessStatus.Sent
                };
                context.RegOperationEntity.Add(regOperation);

                var refContact = new RefContactEntity
                {
                    EntityId = entityId,
                    Email = expectedEmail,
                    FirstName = "Contact",
                    LastName = "Tester",
                    OperationType = OperationAction.Insert
                };
                context.RefContactEntity.Add(refContact);

                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new RefContext(options))
            {
                var strategy = CreateStrategy(context);
                var query = strategy.BuildQuery(criteria, expectedEmail, false);
                var results = await query.ToListAsync();
                results.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task BuildQuery_ReturnsOperationsWithPublishedAt_WhenPublishedAtFilterApplied()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(BuildQuery_ReturnsOperationsWithPublishedAt_WhenPublishedAtFilterApplied));
            var expectedOperationName = OperationAction.Insert;
            var expectedApprovalStatus = "PENDING";
            var expectedEmail = "contact@test.com";
            var entityId = Guid.NewGuid();

            var publishedDate = DateTime.UtcNow.AddDays(-1);
            var criteria = new OperationSearchCriteria
            {
                OperationName = expectedOperationName,
                OperationApprovalStatus = expectedApprovalStatus,
                OperationProcessStatus = new string[] { ProcessStatus.Sent, ProcessStatus.Failed },
                PublishedAt = publishedDate,
                PublishedAtGreaterThan = true 
            };

            using (var context = new RefContext(options))
            {
                var regOperation = new RegOperationEntity
                {
                    Id = 3,
                    Operation = expectedOperationName,
                    CreationDate = DateTime.UtcNow,
                    EntityId = entityId,
                    LastStatusApprovalDate = DateTime.UtcNow,
                    LastStatusApprovalBy = "contact@test.com",
                    PublishedAt = DateTime.UtcNow,
                    ApprovalStatus = expectedApprovalStatus,
                    Type = "CONTACT",
                    ProcessStatus = ProcessStatus.Sent
                };
                context.RegOperationEntity.Add(regOperation);

                var refContact = new RefContactEntity
                {
                    EntityId = entityId,
                    Email = expectedEmail,
                    FirstName = "Contact",
                    LastName = "Tester",
                    OperationType = OperationAction.Insert
                };
                context.RefContactEntity.Add(refContact);

                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new RefContext(options))
            {
                var strategy = CreateStrategy(context);
                var query = strategy.BuildQuery(criteria, expectedEmail, false);
                var results = await query.ToListAsync();
                results.Should().HaveCount(1);
            }
        }

        [Fact]
        public async Task BuildQuery_FiltersOutOperation_WhenFilterByPublishedTrueAndPublishedAtIsNull()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(BuildQuery_FiltersOutOperation_WhenFilterByPublishedTrueAndPublishedAtIsNull));
            var expectedOperationName = OperationAction.Insert;
            var criteria = new OperationSearchCriteria
            {
                OperationName = expectedOperationName,
                OperationApprovalStatus = "PENDING",
                OperationProcessStatus = new string[] { ProcessStatus.Sent, ProcessStatus.Failed }
            };
            var expectedEmail = "contact@test.com";
            var entityId = Guid.NewGuid();

            using (var context = new RefContext(options))
            {
                var regOperation = new RegOperationEntity
                {
                    Id = 4,
                    Operation = expectedOperationName,
                    CreationDate = DateTime.UtcNow,
                    EntityId = entityId,
                    LastStatusApprovalDate = DateTime.UtcNow,
                    LastStatusApprovalBy = "contact@test.com",
                    PublishedAt = null,
                    ApprovalStatus = "PENDING",
                    Type = "CONTACT",
                    ProcessStatus = ProcessStatus.Sent
                };
                context.RegOperationEntity.Add(regOperation);

                var refContact = new RefContactEntity
                {
                    EntityId = entityId,
                    Email = expectedEmail,
                    FirstName = "Contact",
                    LastName = "Tester",
                    OperationType = OperationAction.Insert
                };
                context.RefContactEntity.Add(refContact);

                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new RefContext(options))
            {
                var strategy = CreateStrategy(context);
                var query = strategy.BuildQuery(criteria, expectedEmail, true);
                var results = await query.ToListAsync();
                results.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task BuildQuery_FiltersByEmail()
        {
            // Arrange: Test that the email filter works.
            var options = CreateInMemoryOptions(nameof(BuildQuery_FiltersByEmail));
            var expectedOperationName = OperationAction.Insert;
            var criteria = new OperationSearchCriteria
            {
                OperationName = expectedOperationName,
                OperationApprovalStatus = "PENDING",
                OperationProcessStatus = new string[] { ProcessStatus.Sent, ProcessStatus.Failed }
            };
            var expectedEmail = "contact@test.com";
            var entityId = Guid.NewGuid();

            using (var context = new RefContext(options))
            {
                var regOperation = new RegOperationEntity
                {
                    Id = 5,
                    Operation = expectedOperationName,
                    CreationDate = DateTime.UtcNow,
                    EntityId = entityId,
                    LastStatusApprovalDate = DateTime.UtcNow,
                    LastStatusApprovalBy = "contact@test.com",
                    PublishedAt = null,
                    ApprovalStatus = "PENDING",
                    Type = "CONTACT",
                    ProcessStatus = ProcessStatus.Sent
                };
                context.RegOperationEntity.Add(regOperation);

                var refContact = new RefContactEntity
                {
                    EntityId = entityId,
                    Email = expectedEmail,
                    FirstName = "Contact",
                    LastName = "Tester",
                    OperationType = OperationAction.Insert
                };
                context.RefContactEntity.Add(refContact);
                await context.SaveChangesAsync();
            }

            // Act & Assert.
            using (var context = new RefContext(options))
            {
                var strategy = CreateStrategy(context);
                var query = strategy.BuildQuery(criteria, expectedEmail, false);
                var results = await query.ToListAsync();
                results.Should().HaveCount(1);

                query = strategy.BuildQuery(criteria, "nonexistent@test.com", false);
                results = await query.ToListAsync();
                results.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task BuildQuery_NoApprovalStatusFilter_ReturnsAllMatchingOperations()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(BuildQuery_NoApprovalStatusFilter_ReturnsAllMatchingOperations));
            var expectedOperationName = OperationAction.Insert;
            var expectedEmail = "contact@test.com";
            var entityId = Guid.NewGuid();

            var criteria = new OperationSearchCriteria
            {
                OperationName = expectedOperationName,
                OperationProcessStatus = new string[] { ProcessStatus.Sent, ProcessStatus.Failed }
            };

            using (var context = new RefContext(options))
            {
                var regOperation1 = new RegOperationEntity
                {
                    Id = 6,
                    Operation = expectedOperationName,
                    CreationDate = DateTime.UtcNow,
                    EntityId = entityId,
                    LastStatusApprovalDate = DateTime.UtcNow,
                    LastStatusApprovalBy = "contact1@test.com",
                    PublishedAt = null,
                    ApprovalStatus = "PENDING",
                    Type = "CONTACT",
                    ProcessStatus = ProcessStatus.Sent
                };
                var regOperation2 = new RegOperationEntity
                {
                    Id = 7,
                    Operation = expectedOperationName,
                    CreationDate = DateTime.UtcNow,
                    EntityId = entityId,
                    LastStatusApprovalDate = DateTime.UtcNow,
                    LastStatusApprovalBy = "contact2@test.com",
                    PublishedAt = null,
                    ApprovalStatus = "APPROVED",
                    Type = "CONTACT",
                    ProcessStatus = ProcessStatus.Sent
                };
                context.RegOperationEntity.AddRange(regOperation1, regOperation2);

                var refContact = new RefContactEntity
                {
                    EntityId = entityId,
                    Email = expectedEmail,
                    FirstName = "Contact",
                    LastName = "Tester",
                    OperationType = OperationAction.Insert
                };
                context.RefContactEntity.Add(refContact);
                await context.SaveChangesAsync();
            }

            // Act & Assert.
            using (var context = new RefContext(options))
            {
                var strategy = CreateStrategy(context);
                var query = strategy.BuildQuery(criteria, expectedEmail, false);
                var results = await query.ToListAsync();
                results.Should().HaveCount(2);
            }
        }

        #endregion
    }
}

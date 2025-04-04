using Application.Consts;
using Application.Requests;
using FluentAssertions;
using Infrastructure.Strategies;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;

namespace Registry.Infrastructure.Tests.Strategies
{
    public class RoleOperationQueryStrategyTests
    {
        private DbContextOptions<RefContext> CreateInMemoryOptions(string databaseName)
        {
            return new DbContextOptionsBuilder<RefContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
        }

        private RoleOperationQueryStrategy CreateStrategy(RefContext context)
        {
            return new RoleOperationQueryStrategy(context);
        }

        #region BuildQuery Tests

        [Fact]
        public async Task BuildQuery_ReturnsMatchingNonSystemCreatedRoleOperation_WhenAllCriteriaMatch()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(BuildQuery_ReturnsMatchingNonSystemCreatedRoleOperation_WhenAllCriteriaMatch));
            var expectedOperationName = OperationAction.Update;
            var expectedApprovalStatus = ApprovalStatus.Approved;
            var expectedProcessStatuses = new string[] { ProcessStatus.Sent, ProcessStatus.Failed };
            var expectedEmail = "role@test.com";
            var expectedAccountNumber = "ROLE_ACC_1";
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
                    LastStatusApprovalBy = "role@test.com",
                    PublishedAt = null,
                    ApprovalStatus = expectedApprovalStatus,
                    Type = OperationCategory.ROLE,
                    ProcessStatus = ProcessStatus.Sent,
                    CreatedBySystem = false
                };
                context.RegOperationEntity.Add(regOperation);

                var roleRef = new Pulse.Registry.Domain.Entities.RefRoleEntity
                {
                    EntityId = entityId,
                    AccountNumber = expectedAccountNumber,
                    ContactEmail = expectedEmail,
                    OperationType = OperationAction.Update
                };
                context.RefRoleEntity.Add(roleRef);

                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new RefContext(options))
            {
                var strategy = CreateStrategy(context);
                var query = strategy.BuildQuery(criteria, expectedEmail, false, expectedAccountNumber);
                var results = await query.ToListAsync();

                // Assert
                results.Should().HaveCount(1);
                var result = results.First();
                result.Operation.Should().Be(expectedOperationName);
                result.ApprovalStatus.Should().Be(expectedApprovalStatus);
            }
        }

        [Fact]
        public async Task BuildQuery_ReturnsEmpty_WhenEmailFilterDoesNotMatch_NonSystemCreated()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(BuildQuery_ReturnsEmpty_WhenEmailFilterDoesNotMatch_NonSystemCreated));
            var expectedOperationName = OperationAction.Update;
            var criteria = new OperationSearchCriteria
            {
                OperationName = expectedOperationName,
                OperationApprovalStatus = ApprovalStatus.Approved,
                OperationProcessStatus = new string[] { ProcessStatus.Sent, ProcessStatus.Failed }
            };
            var expectedEmail = "role@test.com";
            var expectedAccountNumber = "ROLE_ACC_1";
            var entityId = Guid.NewGuid();

            using (var context = new RefContext(options))
            {
                var regOperation = new RegOperationEntity
                {
                    Id = 2,
                    Operation = expectedOperationName,
                    CreationDate = DateTime.UtcNow,
                    EntityId = entityId,
                    LastStatusApprovalDate = DateTime.UtcNow,
                    LastStatusApprovalBy = "role@test.com",
                    PublishedAt = null,
                    ApprovalStatus = ApprovalStatus.Approved,
                    Type = OperationCategory.ROLE,
                    ProcessStatus = ProcessStatus.Sent,
                    CreatedBySystem = false
                };
                context.RegOperationEntity.Add(regOperation);

                var roleRef = new Pulse.Registry.Domain.Entities.RefRoleEntity
                {
                    EntityId = entityId,
                    AccountNumber = expectedAccountNumber,
                    ContactEmail = "different@test.com", // non-matching email
                    OperationType = OperationAction.Update
                };
                context.RefRoleEntity.Add(roleRef);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new RefContext(options))
            {
                var strategy = CreateStrategy(context);
                var query = strategy.BuildQuery(criteria, expectedEmail, false, expectedAccountNumber);
                var results = await query.ToListAsync();
                results.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task BuildQuery_ReturnsEmpty_WhenAccountNumberFilterDoesNotMatch_NonSystemCreated()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(BuildQuery_ReturnsEmpty_WhenAccountNumberFilterDoesNotMatch_NonSystemCreated));
            var expectedOperationName = OperationAction.Update;
            var criteria = new OperationSearchCriteria
            {
                OperationName = expectedOperationName,
                OperationApprovalStatus = ApprovalStatus.Approved,
                OperationProcessStatus = new string[] { ProcessStatus.Sent, ProcessStatus.Failed }
            };
            var expectedEmail = "role@test.com";
            var expectedAccountNumber = "ROLE_ACC_1";
            var entityId = Guid.NewGuid();

            using (var context = new RefContext(options))
            {
                var regOperation = new RegOperationEntity
                {
                    Id = 3,
                    Operation = expectedOperationName,
                    CreationDate = DateTime.UtcNow,
                    EntityId = entityId,
                    LastStatusApprovalDate = DateTime.UtcNow,
                    LastStatusApprovalBy = "role@test.com",
                    PublishedAt = null,
                    ApprovalStatus = ApprovalStatus.Approved,
                    Type = OperationCategory.ROLE,
                    ProcessStatus = ProcessStatus.Sent,
                    CreatedBySystem = false
                };
                context.RegOperationEntity.Add(regOperation);

                var roleRef = new Pulse.Registry.Domain.Entities.RefRoleEntity
                {
                    EntityId = entityId,
                    AccountNumber = "DIFFERENT_ACC",
                    ContactEmail = expectedEmail,
                    OperationType = OperationAction.Update
                };
                context.RefRoleEntity.Add(roleRef);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new RefContext(options))
            {
                var strategy = CreateStrategy(context);
                var query = strategy.BuildQuery(criteria, expectedAccountNumber, false, expectedAccountNumber);
                var results = await query.ToListAsync();
                results.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task BuildQuery_ReturnsMatchingSystemCreatedRoleOperation_WhenFetchSystemCreatedIsTrue()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(BuildQuery_ReturnsMatchingSystemCreatedRoleOperation_WhenFetchSystemCreatedIsTrue));
            var expectedOperationName = OperationAction.Update;
            var entityId = Guid.NewGuid();

            var criteria = new OperationSearchCriteria
            {
                OperationName = expectedOperationName,
                OperationApprovalStatus = ApprovalStatus.Approved,
                OperationProcessStatus = new string[] { ProcessStatus.Sent, ProcessStatus.Failed },
                FetchSystemGeneratedOperation = true
            };

            using (var context = new RefContext(options))
            {
                var regOperation = new RegOperationEntity
                {
                    Id = 4,
                    Operation = expectedOperationName,
                    CreationDate = DateTime.UtcNow,
                    EntityId = entityId,
                    LastStatusApprovalDate = DateTime.UtcNow,
                    LastStatusApprovalBy = "sysrole@test.com",
                    PublishedAt = null,
                    ApprovalStatus = ApprovalStatus.Approved,
                    Type = OperationCategory.ROLE,
                    ProcessStatus = ProcessStatus.Sent,
                    CreatedBySystem = true
                };
                context.RegOperationEntity.Add(regOperation);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new RefContext(options))
            {
                var strategy = CreateStrategy(context);
                var query = strategy.BuildQuery(criteria, string.Empty, false);
                var results = await query.ToListAsync();
                results.Should().HaveCount(1);
                results.First().ApprovalStatus.Should().Be(ApprovalStatus.Approved);
            }
        }

        [Fact]
        public async Task BuildQuery_ReturnsEmpty_WhenFilterByPublishedTrueAndPublishedAtIsNull_NonSystemCreated()
        {
            // Arrange: For non-system created branch, when filterByPublished is true, only records with non-null PublishedAt should be returned.
            var options = CreateInMemoryOptions(nameof(BuildQuery_ReturnsEmpty_WhenFilterByPublishedTrueAndPublishedAtIsNull_NonSystemCreated));
            var expectedOperationName = OperationAction.Update;
            var criteria = new OperationSearchCriteria
            {
                OperationName = expectedOperationName,
                OperationApprovalStatus = "PENDING",
                OperationProcessStatus = new string[] { ProcessStatus.Sent, ProcessStatus.Failed }
            };
            var expectedEmail = "role@test.com";
            var expectedAccountNumber = "ROLE_ACC_1";
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
                    LastStatusApprovalBy = "role@test.com",
                    PublishedAt = null,
                    ApprovalStatus = "PENDING",
                    Type = OperationCategory.ROLE,
                    ProcessStatus = ProcessStatus.Sent,
                    CreatedBySystem = false
                };
                context.RegOperationEntity.Add(regOperation);

                var roleRef = new Pulse.Registry.Domain.Entities.RefRoleEntity
                {
                    EntityId = entityId,
                    AccountNumber = expectedAccountNumber,
                    ContactEmail = expectedEmail,
                    OperationType = OperationAction.Update
                };
                context.RefRoleEntity.Add(roleRef);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new RefContext(options))
            {
                var strategy = CreateStrategy(context);
                var query = strategy.BuildQuery(criteria, expectedEmail, true, expectedAccountNumber);
                var results = await query.ToListAsync();
                results.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task BuildQuery_ReturnsAllMatchingRecords_WhenApprovalStatusFilterIsEmpty()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(BuildQuery_ReturnsAllMatchingRecords_WhenApprovalStatusFilterIsEmpty));
            var expectedOperationName = OperationAction.Update;
            var expectedEmail = "role@test.com";
            var expectedAccountNumber = "ROLE_ACC_1";
            var entityId = Guid.NewGuid();

            var criteria = new OperationSearchCriteria
            {
                OperationName = expectedOperationName,
                OperationApprovalStatus = "", // No approval status filtering.
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
                    LastStatusApprovalBy = "role1@test.com",
                    PublishedAt = null,
                    ApprovalStatus = "PENDING",
                    Type = OperationCategory.ROLE,
                    ProcessStatus = ProcessStatus.Sent,
                    CreatedBySystem = false
                };
                var regOperation2 = new RegOperationEntity
                {
                    Id = 7,
                    Operation = expectedOperationName,
                    CreationDate = DateTime.UtcNow,
                    EntityId = entityId,
                    LastStatusApprovalDate = DateTime.UtcNow,
                    LastStatusApprovalBy = "role2@test.com",
                    PublishedAt = null,
                    ApprovalStatus = "APPROVED",
                    Type = OperationCategory.ROLE,
                    ProcessStatus = ProcessStatus.Sent,
                    CreatedBySystem = false
                };
                context.RegOperationEntity.AddRange(regOperation1, regOperation2);

                var roleRef = new Pulse.Registry.Domain.Entities.RefRoleEntity
                {
                    EntityId = entityId,
                    AccountNumber = expectedAccountNumber,
                    ContactEmail = expectedEmail,
                    OperationType = OperationAction.Update
                };
                context.RefRoleEntity.Add(roleRef);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new RefContext(options))
            {
                var strategy = CreateStrategy(context);
                var query = strategy.BuildQuery(criteria, expectedEmail, false, expectedAccountNumber);
                var results = await query.ToListAsync();
                results.Should().HaveCount(2);
            }
        }

        #endregion
    }
}

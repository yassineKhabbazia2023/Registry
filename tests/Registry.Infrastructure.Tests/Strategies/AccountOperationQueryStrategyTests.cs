using Application.Consts;
using Application.Requests;
using FluentAssertions;
using Infrastructure.Strategies;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;

namespace Registry.Infrastructure.Tests.Strategies
{
    public class AccountOperationQueryStrategyTests
    {
        private DbContextOptions<RefContext> CreateInMemoryOptions(string databaseName)
        {
            return new DbContextOptionsBuilder<RefContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
        }

        private AccountOperationQueryStrategy CreateStrategy(RefContext context)
        {
            return new AccountOperationQueryStrategy(context);
        }

        #region BuildQuery Tests

        [Fact]
        public async Task BuildQuery_ReturnsMatchingOperation_WhenAllCriteriaMatch()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(BuildQuery_ReturnsMatchingOperation_WhenAllCriteriaMatch));
            var expectedOperationName = OperationAction.Insert;
            var expectedApprovalStatus = "PENDING";
            var expectedProcessStatuses = new string[] { ProcessStatus.Sent, ProcessStatus.Failed };
            var expectedAccountNumber = "ACC123";
            var entityId = Guid.NewGuid();

            var criteria = new OperationSearchCriteria
            {
                OperationName = expectedOperationName,
                OperationApprovalStatus = expectedApprovalStatus,
                OperationProcessStatus = expectedProcessStatuses,
                IncludeNullPublishedAt = true
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
                    LastStatusApprovalBy = "test@account.com",
                    PublishedAt = null,
                    ApprovalStatus = expectedApprovalStatus,
                    Type = OperationCategory.ACCOUNT,
                    ProcessStatus = ProcessStatus.Sent
                };
                context.RegOperationEntity.Add(regOperation);

                var refAccount = new RefAccountEntity
                {
                    EntityId = entityId,
                    AccountNumber = expectedAccountNumber,
                    LegalName = "Test Account",
                    OperationType = OperationAction.Insert
                };
                context.RefAccountEntity.Add(refAccount);

                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new RefContext(options))
            {
                var strategy = CreateStrategy(context);
                var query = strategy.BuildQuery(criteria, expectedAccountNumber, true);
                var results = await query.ToListAsync();

                // Assert
                results.Should().HaveCount(1);
                var result = results.First();
                result.Operation.Should().Be(expectedOperationName);
                result.ApprovalStatus.Should().Be(expectedApprovalStatus);
                
            }
        }

        [Fact]
        public async Task BuildQuery_FiltersOutOperation_WhenApprovalStatusDoesNotMatch()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(BuildQuery_FiltersOutOperation_WhenApprovalStatusDoesNotMatch));
            var expectedOperationName = OperationAction.Insert;
            var criteria = new OperationSearchCriteria
            {
                OperationName = expectedOperationName,
                OperationApprovalStatus = "APPROVED",
                OperationProcessStatus = new string[] { ProcessStatus.Sent, ProcessStatus.Failed }
            };
            var expectedAccountNumber = "ACC123";
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
                    LastStatusApprovalBy = "test@account.com",
                    PublishedAt = null,
                    ApprovalStatus = "PENDING",
                    Type = OperationCategory.ACCOUNT,
                    ProcessStatus = ProcessStatus.Sent
                };
                context.RegOperationEntity.Add(regOperation);

                var refAccount = new RefAccountEntity
                {
                    EntityId = entityId,
                    AccountNumber = expectedAccountNumber,
                    LegalName = "Test Account",
                    OperationType = OperationAction.Insert
                };
                context.RefAccountEntity.Add(refAccount);

                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new RefContext(options))
            {
                var strategy = CreateStrategy(context);
                var query = strategy.BuildQuery(criteria, expectedAccountNumber, true);
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
            var expectedAccountNumber = "ACC123";
            var entityId = Guid.NewGuid();

            var publishedDate = DateTime.UtcNow.AddDays(-1);
            var criteria = new OperationSearchCriteria
            {
                OperationName = expectedOperationName,
                OperationApprovalStatus = expectedApprovalStatus,
                OperationProcessStatus = new string[] { ProcessStatus.Sent, ProcessStatus.Failed },
                PublishedAt = publishedDate,
                PublishedAtGreaterThan = true  // expecting operations with PublishedAt >= publishedDate
            };

            using (var context = new RefContext(options))
            {
                var regOperation = new RegOperationEntity
                {
                    Id = 33,
                    Operation = expectedOperationName,
                    CreationDate = DateTime.UtcNow,
                    EntityId = entityId,
                    LastStatusApprovalDate = DateTime.UtcNow,
                    LastStatusApprovalBy = "test@account.com",
                    PublishedAt = DateTime.UtcNow, // >= publishedDate
                    ApprovalStatus = expectedApprovalStatus,
                    Type = OperationCategory.ACCOUNT,
                    ProcessStatus = ProcessStatus.Sent
                };
                context.RegOperationEntity.Add(regOperation);

                var refAccount = new RefAccountEntity
                {
                    EntityId = entityId,
                    AccountNumber = expectedAccountNumber,
                    LegalName = "Test Account",
                    OperationType = OperationAction.Insert
                };
                context.RefAccountEntity.Add(refAccount);

                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new RefContext(options))
            {
                var strategy = CreateStrategy(context);
                var query = strategy.BuildQuery(criteria, expectedAccountNumber, false);
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
            var expectedAccountNumber = "ACC123";
            var entityId = Guid.NewGuid();

            using (var context = new RefContext(options))
            {
                var regOperation = new RegOperationEntity
                {
                    Id = 44,
                    Operation = expectedOperationName,
                    CreationDate = DateTime.UtcNow,
                    EntityId = entityId,
                    LastStatusApprovalDate = DateTime.UtcNow,
                    LastStatusApprovalBy = "test@account.com",
                    PublishedAt = null,
                    ApprovalStatus = "PENDING",
                    Type = OperationCategory.ACCOUNT,
                    ProcessStatus = ProcessStatus.Sent
                };
                context.RegOperationEntity.Add(regOperation);

                var refAccount = new RefAccountEntity
                {
                    EntityId = entityId,
                    AccountNumber = expectedAccountNumber,
                    LegalName = "Test Account",
                    OperationType = OperationAction.Insert
                };
                context.RefAccountEntity.Add(refAccount);

                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new RefContext(options))
            {
                var strategy = CreateStrategy(context);
                var query = strategy.BuildQuery(criteria, expectedAccountNumber, true);
                var results = await query.ToListAsync();
                results.Should().BeEmpty();
                                
            }
        }

        [Fact]
        public async Task BuildQuery_FiltersByAccountNumber()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(BuildQuery_FiltersByAccountNumber));
            var expectedOperationName = OperationAction.Insert;
            var criteria = new OperationSearchCriteria
            {
                OperationName = expectedOperationName,
                OperationApprovalStatus = "PENDING",
                OperationProcessStatus = new string[] { ProcessStatus.Sent, ProcessStatus.Failed }
            };
           
            var expectedAccountNumber = "ACC123";
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
                    LastStatusApprovalBy = "test@account.com",
                    PublishedAt = null,
                    ApprovalStatus = "PENDING",
                    Type = OperationCategory.ACCOUNT,
                    ProcessStatus = ProcessStatus.Sent
                };
                context.RegOperationEntity.Add(regOperation);

                var refAccount = new RefAccountEntity
                {
                    EntityId = entityId,
                    AccountNumber = "DIFFERENT_ACC",
                    LegalName = "Test Account",
                    OperationType = OperationAction.Insert
                };
                context.RefAccountEntity.Add(refAccount);

                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new RefContext(options))
            {
                var strategy = CreateStrategy(context);
                var query = strategy.BuildQuery(criteria, expectedAccountNumber, true);
                var results = await query.ToListAsync();
                results.Should().BeEmpty();
                                
            }
        }

        [Fact]
        public async Task BuildQuery_NoApprovalStatusFilter_ReturnsAllMatchingOperations()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(BuildQuery_NoApprovalStatusFilter_ReturnsAllMatchingOperations));
            var expectedOperationName = OperationAction.Insert;
            var expectedAccountNumber = "ACC123";
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
                    Id = 66,
                    Operation = expectedOperationName,
                    CreationDate = DateTime.UtcNow,
                    EntityId = entityId,
                    LastStatusApprovalDate = DateTime.UtcNow,
                    LastStatusApprovalBy = "test1@account.com",
                    PublishedAt = null,
                    ApprovalStatus = "PENDING",
                    Type = OperationCategory.ACCOUNT,
                    ProcessStatus = ProcessStatus.Sent
                };
                var regOperation2 = new RegOperationEntity
                {
                    Id = 77,
                    Operation = expectedOperationName,
                    CreationDate = DateTime.UtcNow,
                    EntityId = entityId,
                    LastStatusApprovalDate = DateTime.UtcNow,
                    LastStatusApprovalBy = "test2@account.com",
                    PublishedAt = null,
                    ApprovalStatus = "APPROVED",
                    Type = OperationCategory.ACCOUNT,
                    ProcessStatus = ProcessStatus.Sent
                };
                context.RegOperationEntity.AddRange(regOperation1, regOperation2);

                var refAccount = new RefAccountEntity
                {
                    EntityId = entityId,
                    AccountNumber = expectedAccountNumber,
                    LegalName = "Test Account",
                    OperationType = OperationAction.Insert
                };
                context.RefAccountEntity.Add(refAccount);

                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new RefContext(options))
            {
                var strategy = CreateStrategy(context);
                var query = strategy.BuildQuery(criteria, expectedAccountNumber);
                var results = await query.ToListAsync();
                results.Should().HaveCount(2);
                                
            }
        }

        #endregion
    }
}

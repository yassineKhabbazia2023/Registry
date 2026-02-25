using Application.Consts;
using Application.Enums;
using Application.Models;
using Application.Requests;
using FluentAssertions;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;
using Application.Interfaces;
using Infrastructure.Strategies;
using Kpmg.ExceptionMiddleware.AdvancedExceptions;
using Pulse.Registry.Domain.Entities.Accounts;
using Pulse.Registry.Domain.Entities.Contacts;

namespace Registry.Infrastructure.Tests.Repository;

public class OperationRepositoryTests
{
    // Helper method to create a new in-memory DbContextOptions instance.
    private DbContextOptions<RefContext> CreateInMemoryOptions(string databaseName)
    {
        return new DbContextOptionsBuilder<RefContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
    }

    // Helper method to create the repository with real strategies.
    private OperationRepository CreateRepository(RefContext context)
    {
        var loggerMock = new Mock<ILogger<OperationRepository>>();
        var strategies = new Dictionary<OperationStrategyType, IOperationQueryStrategy>
            {
                { OperationStrategyType.ACCOUNT, new AccountOperationQueryStrategy(context) },
                { OperationStrategyType.CONTACT, new ContactOperationQueryStrategy(context) },
                { OperationStrategyType.ROLE, new RoleOperationQueryStrategy(context) }
            };
        return new OperationRepository(context, loggerMock.Object, strategies);
    }

    #region FetchOperationsByAccountNumberAsync Tests

    [Fact]
    public async Task FetchOperationsByAccountNumberAsync_RefTables_ReturnsOperationDetails()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(FetchOperationsByAccountNumberAsync_RefTables_ReturnsOperationDetails));
        var accountNumber = "19909090";
        var expectedOperationName = OperationAction.Insert;
        var expectedApprovalStatus = "PENDING";
        var expectedEntityId = Guid.NewGuid();

        var searchCriteria = new OperationSearchCriteria
        {
            OperationName = expectedOperationName,
            OperationApprovalStatus = expectedApprovalStatus
        };

        using (var context = new RefContext(options))
        {
            var regOperation = new RegOperationEntity
            {
                Id = 1,
                Operation = expectedOperationName,
                CreationDate = DateTime.UtcNow,
                EntityId = expectedEntityId,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "test@domain.com",
                PublishedAt = null,
                ApprovalStatus = expectedApprovalStatus,
                Type = OperationCategory.ROLE,
                ProcessStatus = ""
            };
            context.RegOperationEntity.Add(regOperation);

            var refRole = new RefRoleEntity
            {
                EntityId = expectedEntityId,
                AccountNumber = accountNumber,
                ContactEmail = "contact@domain.com",
                OperationType = OperationAction.Insert
            };
            context.RefRoleEntity.Add(refRole);

            var refAccount = new RefAccountEntity
            {
                EntityId = expectedEntityId,
                AccountNumber = accountNumber,
                LegalName = "Test Account",
                OperationType = OperationAction.Insert
            };
            context.RefAccountEntity.Add(refAccount);

            var refContact = new RefContactEntity
            {
                EntityId = expectedEntityId,
                Email = "contact@domain.com",
                FirstName = "Test",
                LastName = "User",
                OperationType = OperationAction.Insert
            };
            context.RefContactEntity.Add(refContact);

            await context.SaveChangesAsync();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var fetchedOperationDetails = await repository.FetchOperationsByAccountNumberAsync(accountNumber, searchCriteria, OperationSource.RefTables);
            fetchedOperationDetails.Should().NotBeNull();
            fetchedOperationDetails.Should().HaveCount(1);

            var actualOperationDetail = fetchedOperationDetails.First();
            actualOperationDetail.Should().NotBeNull();
            actualOperationDetail!.OperationName.Should().Be(expectedOperationName);
            actualOperationDetail.Status.Should().Be(expectedApprovalStatus);
            actualOperationDetail.AccountNumber.Should().Be(accountNumber);
        }
    }

    [Fact]
    public async Task FetchOperationsByAccountNumberAsync_MainTables_ReturnsOperationDetails()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(FetchOperationsByAccountNumberAsync_MainTables_ReturnsOperationDetails));
        var accountNumber = "19909090";
        var expectedOperationName = OperationAction.Insert;
        var expectedApprovalStatus = "PENDING";
        var expectedEntityId = Guid.NewGuid();

        var searchCriteria = new OperationSearchCriteria
        {
            OperationName = expectedOperationName,
            OperationApprovalStatus = expectedApprovalStatus
        };

        using (var context = new RefContext(options))
        {
            var regOperation = new RegOperationEntity
            {
                Id = 2,
                Operation = expectedOperationName,
                CreationDate = DateTime.UtcNow,
                EntityId = expectedEntityId,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "main@test.com",
                PublishedAt = null,
                ApprovalStatus = expectedApprovalStatus,
                Type = OperationCategory.ROLE,
                ProcessStatus = ""
            };
            context.RegOperationEntity.Add(regOperation);

            var refRole = new RefRoleEntity
            {
                EntityId = expectedEntityId,
                AccountNumber = accountNumber,
                ContactEmail = "maincontact@test.com",
                OperationType = OperationAction.Insert
            };
            context.RefRoleEntity.Add(refRole);

            var accountEntity = new AccountEntity
            {
                AccountId = 1001,
                AccountNumber = accountNumber,
                AccountGlobalUniqueId = Guid.NewGuid(),
                LegalName = "legal",
            };
            context.AccountEntity.Add(accountEntity);

            var contactEntity = new ContactEntity
            {
                ContactId = 2001,
                Email = "maincontact@test.com",
                FirstName = "Main",
                LastName = "User",
                Type = "CUSTOMER"
            };
            context.ContactEntity.Add(contactEntity);

            await context.SaveChangesAsync();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var fetchedOperationDetails = await repository.FetchOperationsByAccountNumberAsync(accountNumber, searchCriteria, OperationSource.MainTables);
            fetchedOperationDetails.Should().NotBeNull();
            fetchedOperationDetails.Should().HaveCount(1);

            var actualOperationDetail = fetchedOperationDetails.First();
            actualOperationDetail.Should().NotBeNull();
            actualOperationDetail!.OperationName.Should().Be(expectedOperationName);
            actualOperationDetail.Status.Should().Be(expectedApprovalStatus);
            actualOperationDetail.AccountNumber.Should().Be(accountNumber);
        }
    }

    [Fact]
    public async Task FetchOperationsByAccountNumberAsync_AccountMainAndContactRefTables_ReturnsOperationDetails()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(FetchOperationsByAccountNumberAsync_AccountMainAndContactRefTables_ReturnsOperationDetails));
        var accountNumber = "19909090";
        var expectedOperationName = OperationAction.Insert;
        var expectedApprovalStatus = "PENDING";
        var expectedEntityId = Guid.NewGuid();

        var searchCriteria = new OperationSearchCriteria
        {
            OperationName = expectedOperationName,
            OperationApprovalStatus = expectedApprovalStatus
        };

        using (var context = new RefContext(options))
        {
            var regOperation = new RegOperationEntity
            {
                Id = 3,
                Operation = expectedOperationName,
                CreationDate = DateTime.UtcNow,
                EntityId = expectedEntityId,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "amc@test.com",
                PublishedAt = null,
                ApprovalStatus = expectedApprovalStatus,
                Type = OperationCategory.ROLE,
                ProcessStatus = ""
            };
            context.RegOperationEntity.Add(regOperation);

            var refRole = new RefRoleEntity
            {
                EntityId = expectedEntityId,
                AccountNumber = accountNumber,
                ContactEmail = "amccontact@test.com",
                OperationType = OperationAction.Insert
            };
            context.RefRoleEntity.Add(refRole);

            var accountEntity = new AccountEntity
            {
                AccountId = 1002,
                AccountNumber = accountNumber,
                AccountGlobalUniqueId = Guid.NewGuid(),
                LegalName = "legal",
            };
            context.AccountEntity.Add(accountEntity);

            var refContact = new RefContactEntity
            {
                EntityId = expectedEntityId,
                Email = "amccontact@test.com",
                FirstName = "Ref",
                LastName = "Contact",
                OperationType = OperationAction.Insert
            };
            context.RefContactEntity.Add(refContact);

            await context.SaveChangesAsync();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var fetchedOperationDetails = await repository.FetchOperationsByAccountNumberAsync(accountNumber, searchCriteria, OperationSource.AccountMainAndContactRefTables);
            fetchedOperationDetails.Should().NotBeNull();
            fetchedOperationDetails.Should().HaveCount(1);

            var actualOperationDetail = fetchedOperationDetails.First();
            actualOperationDetail.Should().NotBeNull();
            actualOperationDetail!.OperationName.Should().Be(expectedOperationName);
            actualOperationDetail.Status.Should().Be(expectedApprovalStatus);
            actualOperationDetail.AccountNumber.Should().Be(accountNumber);
        }
    }

    [Fact]
    public async Task FetchOperationsByAccountNumberAsync_InvalidSource_ThrowsArgumentException()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(FetchOperationsByAccountNumberAsync_InvalidSource_ThrowsArgumentException));
        var accountNumber = "19909090";
        var searchCriteria = new OperationSearchCriteria
        {
            OperationName = OperationAction.Insert,
            OperationApprovalStatus = "PENDING"
        };

        using (var context = new RefContext(options))
        {
            var regOperation = new RegOperationEntity
            {
                Id = 4,
                Operation = OperationAction.Insert,
                CreationDate = DateTime.UtcNow,
                EntityId = Guid.NewGuid(),
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "test@domain.com",
                PublishedAt = null,
                ApprovalStatus = "PENDING",
                Type = OperationCategory.ROLE,
                ProcessStatus = ""
            };
            context.RegOperationEntity.Add(regOperation);
            await context.SaveChangesAsync();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            Func<Task> act = async () => await repository.FetchOperationsByAccountNumberAsync(accountNumber, searchCriteria, (OperationSource)999);
            await act.Should().ThrowAsync<ArgumentException>()
                .Where(e => e.Message.Contains("Invalid operation source"));
        }
    }

    [Fact]
    public async Task FetchOperationsByAccountNumberAsync_ExcludesRecords_WithNonNullPublishedAt()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(FetchOperationsByAccountNumberAsync_ExcludesRecords_WithNonNullPublishedAt));
        var accountNumber = "19909090";
        var expectedOperationName = OperationAction.Insert;
        var expectedApprovalStatus = "PENDING";
        var expectedEntityId = Guid.NewGuid();

        var searchCriteria = new OperationSearchCriteria
        {
            OperationName = expectedOperationName,
            OperationApprovalStatus = expectedApprovalStatus
        };

        using (var context = new RefContext(options))
        {
            var regOperation = new RegOperationEntity
            {
                Id = 5,
                Operation = expectedOperationName,
                CreationDate = DateTime.UtcNow,
                EntityId = expectedEntityId,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "test@domain.com",
                PublishedAt = DateTime.UtcNow, // non-null: record should be excluded
                ApprovalStatus = expectedApprovalStatus,
                Type = OperationCategory.ROLE,
                ProcessStatus = ""
            };
            context.RegOperationEntity.Add(regOperation);

            var refRole = new RefRoleEntity
            {
                EntityId = expectedEntityId,
                AccountNumber = accountNumber,
                ContactEmail = "contact@domain.com",
                OperationType = OperationAction.Insert
            };
            context.RefRoleEntity.Add(refRole);

            var refAccount = new RefAccountEntity
            {
                EntityId = expectedEntityId,
                AccountNumber = accountNumber,
                LegalName = "Test Account",
                OperationType = OperationAction.Insert
            };
            context.RefAccountEntity.Add(refAccount);

            var refContact = new RefContactEntity
            {
                EntityId = expectedEntityId,
                Email = "contact@domain.com",
                FirstName = "Test",
                LastName = "User",
                OperationType = OperationAction.Insert
            };
            context.RefContactEntity.Add(refContact);

            await context.SaveChangesAsync();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var fetchedOperationDetails = await repository.FetchOperationsByAccountNumberAsync(accountNumber, searchCriteria, OperationSource.RefTables);
            fetchedOperationDetails.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task FetchOperationsByAccountNumberAsync_NoApprovalStatusFilter_ReturnsAllMatchingOperations()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(FetchOperationsByAccountNumberAsync_NoApprovalStatusFilter_ReturnsAllMatchingOperations));
        var accountNumber = "19909090";
        var expectedOperationName = OperationAction.Insert;
        var expectedEntityId = Guid.NewGuid();

        var searchCriteria = new OperationSearchCriteria
        {
            OperationName = expectedOperationName,
            OperationApprovalStatus = "" // empty: no approval filtering
        };

        using (var context = new RefContext(options))
        {
            var regOperation1 = new RegOperationEntity
            {
                Id = 6,
                Operation = expectedOperationName,
                CreationDate = DateTime.UtcNow,
                EntityId = expectedEntityId,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "test1@domain.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Pending,
                Type = OperationCategory.ROLE,
                ProcessStatus = ""
            };
            var regOperation2 = new RegOperationEntity
            {
                Id = 7,
                Operation = expectedOperationName,
                CreationDate = DateTime.UtcNow,
                EntityId = expectedEntityId,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "test2@domain.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.ROLE,
                ProcessStatus = ""
            };
            context.RegOperationEntity.AddRange(regOperation1, regOperation2);

            var refRole = new RefRoleEntity
            {
                EntityId = expectedEntityId,
                AccountNumber = accountNumber,
                ContactEmail = "contact@domain.com",
                OperationType = OperationAction.Insert
            };
            context.RefRoleEntity.Add(refRole);

            var refAccount = new RefAccountEntity
            {
                EntityId = expectedEntityId,
                AccountNumber = accountNumber,
                LegalName = "Test Account",
                OperationType = OperationAction.Insert
            };
            context.RefAccountEntity.Add(refAccount);

            var refContact = new RefContactEntity
            {
                EntityId = expectedEntityId,
                Email = "contact@domain.com",
                FirstName = "Test",
                LastName = "User",
                OperationType = OperationAction.Insert
            };
            context.RefContactEntity.Add(refContact);

            await context.SaveChangesAsync();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var fetchedOperationDetails = await repository.FetchOperationsByAccountNumberAsync(accountNumber, searchCriteria, OperationSource.RefTables);
            fetchedOperationDetails.Should().HaveCount(2);
        }
    }

    #endregion

    #region GetOperationByIdAsync Tests

    [Fact]
    public async Task GetOperationByIdAsync_ReturnsOperation_WhenFound()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GetOperationByIdAsync_ReturnsOperation_WhenFound));
        int operationId = 10;
        var expectedEntityId = Guid.NewGuid();
        using (var context = new RefContext(options))
        {
            var regOperation = new RegOperationEntity
            {
                Id = operationId,
                Operation = OperationAction.Update,
                CreationDate = DateTime.UtcNow,
                EntityId = expectedEntityId,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "approver@test.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.ROLE,
                ProcessStatus = ""
            };
            context.RegOperationEntity.Add(regOperation);
            await context.SaveChangesAsync();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var retrievedOperation = await repository.GetOperationByIdAsync(operationId);
            retrievedOperation.Should().NotBeNull();
            retrievedOperation!.Operation.Should().Be(OperationAction.Update);
        }
    }

    [Fact]
    public async Task GetOperationByIdAsync_ThrowsNotFoundException_WhenNotFound()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GetOperationByIdAsync_ThrowsNotFoundException_WhenNotFound));
        using (var context = new RefContext(options))
        {
            await context.SaveChangesAsync();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            Func<Task> act = async () => await repository.GetOperationByIdAsync(999);
            await act.Should().ThrowAsync<NotFoundException>()
                .Where(e => e.Message.Contains("999"));
        }
    }

    #endregion

    #region FetchOperationsByCriteriaAsync Tests

    [Fact]
    public async Task FetchOperationsByCriteriaAsync_ReturnsAccountOperations()
    {
        // Arrange for ACCOUNT strategy.
        var options = CreateInMemoryOptions(nameof(FetchOperationsByCriteriaAsync_ReturnsAccountOperations));
        var expectedOperationName = OperationAction.Insert;
        var expectedAccountNumber = "ACC123";
        var entityId = Guid.NewGuid();

        var searchCriteria = new OperationSearchCriteria
        {
            OperationName = expectedOperationName,
            OperationApprovalStatus = "PENDING",
            OperationProcessStatus = new string[] { ProcessStatus.Sent, ProcessStatus.Failed }
        };

        using (var context = new RefContext(options))
        {
            var regOperation = new RegOperationEntity
            {
                Id = 100,
                Operation = expectedOperationName,
                CreationDate = DateTime.UtcNow,
                EntityId = entityId,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "account@test.com",
                PublishedAt = null,
                ApprovalStatus = "PENDING",
                Type = OperationCategory.ACCOUNT,
                ProcessStatus = ProcessStatus.Sent
            };
            context.RegOperationEntity.Add(regOperation);

            var refRole = new RefRoleEntity
            {
                EntityId = entityId,
                AccountNumber = expectedAccountNumber,
                ContactEmail = "acc@test.com",
                OperationType = OperationAction.Insert
            };
            context.RefRoleEntity.Add(refRole);

            var refAccount = new RefAccountEntity
            {
                EntityId = entityId,
                AccountNumber = expectedAccountNumber,
                LegalName = "Account Name",
                OperationType = OperationAction.Insert
            };
            context.RefAccountEntity.Add(refAccount);

            var refContact = new RefContactEntity
            {
                EntityId = entityId,
                Email = "acc@test.com",
                FirstName = "Acc",
                LastName = "User",
                OperationType = OperationAction.Insert
            };
            context.RefContactEntity.Add(refContact);

            await context.SaveChangesAsync();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var accountOperations = await repository.FetchOperationsByCriteriaAsync(searchCriteria, OperationStrategyType.ACCOUNT, expectedAccountNumber);
            accountOperations.Should().NotBeEmpty();
            accountOperations.First().ApprovalStatus.Should().Be("PENDING");
        }
    }

    [Fact]
    public async Task FetchOperationsByCriteriaAsync_ReturnsContactOperations()
    {
        // Arrange for CONTACT strategy.
        var options = CreateInMemoryOptions(nameof(FetchOperationsByCriteriaAsync_ReturnsContactOperations));
        var expectedOperationName = OperationAction.Insert;
        var expectedEmail = "contact@test.com";
        var entityId = Guid.NewGuid();

        var searchCriteria = new OperationSearchCriteria
        {
            OperationName = expectedOperationName,
            OperationApprovalStatus = "PENDING",
            OperationProcessStatus = new string[] { ProcessStatus.Sent, ProcessStatus.Failed }
        };

        using (var context = new RefContext(options))
        {
            var regOperation = new RegOperationEntity
            {
                Id = 200,
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
            var repository = CreateRepository(context);
            var contactOperations = await repository.FetchOperationsByCriteriaAsync(searchCriteria, OperationStrategyType.CONTACT, expectedEmail);
            contactOperations.Should().NotBeEmpty();
            contactOperations.First().ApprovalStatus.Should().Be("PENDING");
        }
    }

    [Fact]
    public async Task FetchOperationsByCriteriaAsync_ReturnsRoleOperations_ForNonSystemGenerated()
    {
        // Arrange for ROLE strategy (non-system-generated).
        var options = CreateInMemoryOptions(nameof(FetchOperationsByCriteriaAsync_ReturnsRoleOperations_ForNonSystemGenerated));
        var expectedOperationName = OperationAction.Update;
        var expectedEmail = "role@test.com";
        var expectedAccountNumber = "ROLE_ACC";
        var entityId = Guid.NewGuid();

        var searchCriteria = new OperationSearchCriteria
        {
            OperationName = expectedOperationName,
            OperationApprovalStatus = "APPROVED",
            OperationProcessStatus = new string[] { ProcessStatus.Sent, ProcessStatus.Failed },
            FetchSystemGeneratedOperation = false
        };

        using (var context = new RefContext(options))
        {
            var regOperation = new RegOperationEntity
            {
                Id = 300,
                Operation = expectedOperationName,
                CreationDate = DateTime.UtcNow,
                EntityId = entityId,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "role@test.com",
                PublishedAt = null,
                ApprovalStatus = "APPROVED",
                Type = OperationCategory.ROLE,
                ProcessStatus = ProcessStatus.Sent,
                CreatedBySystem = false
            };
            context.RegOperationEntity.Add(regOperation);

            var refRole = new RefRoleEntity
            {
                EntityId = entityId,
                AccountNumber = expectedAccountNumber,
                ContactEmail = expectedEmail,
                OperationType = OperationAction.Update
            };
            context.RefRoleEntity.Add(refRole);

            await context.SaveChangesAsync();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var roleOperations = await repository.FetchOperationsByCriteriaAsync(searchCriteria, OperationStrategyType.ROLE, expectedEmail, false, expectedAccountNumber);
            roleOperations.Should().NotBeEmpty();
            roleOperations.First().ApprovalStatus.Should().Be("APPROVED");
        }
    }

    [Fact]
    public async Task FetchOperationsByCriteriaAsync_ThrowsArgumentException_WhenStrategyMissing()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(FetchOperationsByCriteriaAsync_ThrowsArgumentException_WhenStrategyMissing));
        using (var context = new RefContext(options))
        {
            await context.SaveChangesAsync();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            Func<Task> act = async () => await repository.FetchOperationsByCriteriaAsync(new OperationSearchCriteria(), (OperationStrategyType)999, "filter");
            await act.Should().ThrowAsync<ArgumentException>();
        }
    }

    #endregion

    #region GetAccountOperationDetails & GeContactOperationDetails Tests

    [Fact]
    public void GetAccountOperationDetails_ReturnsQueryableResult()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GetAccountOperationDetails_ReturnsQueryableResult));
        var accountNumber = "19909090";
        var expectedOperationName = OperationAction.Insert;
        var entityId = Guid.NewGuid();

        using (var context = new RefContext(options))
        {
            var regOperation = new RegOperationEntity
            {
                Id = 30,
                Operation = expectedOperationName,
                CreationDate = DateTime.UtcNow,
                EntityId = entityId,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "account@test.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.ACCOUNT,
                ProcessStatus = ""
            };
            context.RegOperationEntity.Add(regOperation);

            var refAccount = new RefAccountEntity
            {
                EntityId = entityId,
                AccountNumber = accountNumber,
                LegalName = "TestAccount",
                OperationType = OperationAction.Insert
            };
            context.RefAccountEntity.Add(refAccount);
            context.SaveChanges();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var accountOperations = repository.GetAccountOperationDetails(expectedOperationName);
            accountOperations.Should().NotBeNull();
            accountOperations.ToList().Should().NotBeEmpty();
        }
    }

    [Fact]
    public void GetAccountOperationDetails_ReturnsEmpty_WhenPublishedAtIsNotNull()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GetAccountOperationDetails_ReturnsEmpty_WhenPublishedAtIsNotNull));
        var accountNumber = "19909090";
        var expectedOperationName = OperationAction.Insert;
        var entityId = Guid.NewGuid();

        using (var context = new RefContext(options))
        {
            var regOperation = new RegOperationEntity
            {
                Id = 31,
                Operation = expectedOperationName,
                CreationDate = DateTime.UtcNow,
                EntityId = entityId,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "test@domain.com",
                PublishedAt = DateTime.UtcNow, // Not null => should be excluded
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.ACCOUNT,
                ProcessStatus = ""
            };
            context.RegOperationEntity.Add(regOperation);

            var refAccount = new RefAccountEntity
            {
                EntityId = entityId,
                AccountNumber = accountNumber,
                LegalName = "TestAccount",
                OperationType = OperationAction.Insert
            };
            context.RefAccountEntity.Add(refAccount);
            context.SaveChanges();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var result = repository.GetAccountOperationDetails(expectedOperationName);
            result.Should().BeEmpty();
        }
    }

    [Fact]
    public void GetAccountOperationDetails_ReturnsEmpty_WhenApprovalStatusIsNotApproved()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GetAccountOperationDetails_ReturnsEmpty_WhenApprovalStatusIsNotApproved));
        var accountNumber = "19909090";
        var expectedOperationName = OperationAction.Insert;
        var entityId = Guid.NewGuid();

        using (var context = new RefContext(options))
        {
            var regOperation = new RegOperationEntity
            {
                Id = 32,
                Operation = expectedOperationName,
                CreationDate = DateTime.UtcNow,
                EntityId = entityId,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "test@domain.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Pending, // Not Approved
                Type = OperationCategory.ACCOUNT,
                ProcessStatus = ""
            };
            context.RegOperationEntity.Add(regOperation);

            var refAccount = new RefAccountEntity
            {
                EntityId = entityId,
                AccountNumber = accountNumber,
                LegalName = "TestAccount",
                OperationType = OperationAction.Insert
            };
            context.RefAccountEntity.Add(refAccount);
            context.SaveChanges();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var result = repository.GetAccountOperationDetails(expectedOperationName);
            result.Should().BeEmpty();
        }
    }

    [Fact]
    public void GetAccountOperationDetails_ReturnsEmpty_WhenTypeIsNotAccount()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GetAccountOperationDetails_ReturnsEmpty_WhenTypeIsNotAccount));
        var accountNumber = "19909090";
        var expectedOperationName = OperationAction.Insert;
        var entityId = Guid.NewGuid();

        using (var context = new RefContext(options))
        {
            var regOperation = new RegOperationEntity
            {
                Id = 33,
                Operation = expectedOperationName,
                CreationDate = DateTime.UtcNow,
                EntityId = entityId,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "test@domain.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.ROLE, // Wrong type
                ProcessStatus = ""
            };
            context.RegOperationEntity.Add(regOperation);

            var refAccount = new RefAccountEntity
            {
                EntityId = entityId,
                AccountNumber = accountNumber,
                LegalName = "TestAccount",
                OperationType = OperationAction.Insert
            };
            context.RefAccountEntity.Add(refAccount);
            context.SaveChanges();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var result = repository.GetAccountOperationDetails(expectedOperationName);
            result.Should().BeEmpty();
        }
    }

    [Fact]
    public void GetAccountOperationDetails_ReturnsEmpty_WhenOperationNameDoesNotMatch()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GetAccountOperationDetails_ReturnsEmpty_WhenOperationNameDoesNotMatch));
        var accountNumber = "19909090";
        var expectedOperationName = OperationAction.Insert; // The search parameter.
        var entityId = Guid.NewGuid();

        using (var context = new RefContext(options))
        {
            var regOperation = new RegOperationEntity
            {
                Id = 34,
                Operation = OperationAction.Delete, // Different operation name.
                CreationDate = DateTime.UtcNow,
                EntityId = entityId,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "test@domain.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.ACCOUNT,
                ProcessStatus = ""
            };
            context.RegOperationEntity.Add(regOperation);

            var refAccount = new RefAccountEntity
            {
                EntityId = entityId,
                AccountNumber = accountNumber,
                LegalName = "TestAccount",
                OperationType = OperationAction.Insert
            };
            context.RefAccountEntity.Add(refAccount);
            context.SaveChanges();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var result = repository.GetAccountOperationDetails(expectedOperationName);
            result.Should().BeEmpty();
        }
    }

    [Fact]
    public void GetAccountOperationDetails_ReturnsMultipleRecords_WhenMultipleMatch()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GetAccountOperationDetails_ReturnsMultipleRecords_WhenMultipleMatch));
        var accountNumber = "19909090";
        var expectedOperationName = OperationAction.Insert;
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();

        using (var context = new RefContext(options))
        {
            var regOperation1 = new RegOperationEntity
            {
                Id = 35,
                Operation = expectedOperationName,
                CreationDate = DateTime.UtcNow,
                EntityId = entityId1,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "test1@domain.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.ACCOUNT,
                ProcessStatus = ""
            };
            var regOperation2 = new RegOperationEntity
            {
                Id = 36,
                Operation = expectedOperationName,
                CreationDate = DateTime.UtcNow,
                EntityId = entityId2,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "test2@domain.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.ACCOUNT,
                ProcessStatus = ""
            };
            context.RegOperationEntity.AddRange(regOperation1, regOperation2);

            var refAccount1 = new RefAccountEntity
            {
                EntityId = entityId1,
                AccountNumber = accountNumber,
                LegalName = "TestAccount1",
                OperationType = OperationAction.Insert
            };
            var refAccount2 = new RefAccountEntity
            {
                EntityId = entityId2,
                AccountNumber = accountNumber,
                LegalName = "TestAccount2",
                OperationType = OperationAction.Insert
            };
            context.RefAccountEntity.AddRange(refAccount1, refAccount2);
            context.SaveChanges();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var results = repository.GetAccountOperationDetails(expectedOperationName);
            results.Should().NotBeNull();
            results.ToList().Should().HaveCount(2);
        }
    }

    [Fact]
    public void GeContactOperationDetails_ReturnsQueryableResult()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GeContactOperationDetails_ReturnsQueryableResult));
        var expectedOperationType = OperationAction.Insert;
        var entityId = Guid.NewGuid();

        using (var context = new RefContext(options))
        {
            var regOperation = new RegOperationEntity
            {
                Id = 40,
                Operation = expectedOperationType,
                CreationDate = DateTime.UtcNow,
                EntityId = entityId,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "contact@test.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.CONTACT,
                ProcessStatus = ""
            };
            context.RegOperationEntity.Add(regOperation);

            var refContact = new RefContactEntity
            {
                EntityId = entityId,
                Email = "contact@test.com",
                FirstName = "Contact",
                LastName = "Tester",
                OperationType = OperationAction.Insert
            };
            context.RefContactEntity.Add(refContact);
            context.SaveChanges();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var contactOperations = repository.GeContactOperationDetails(expectedOperationType);
            contactOperations.Should().NotBeNull();
            contactOperations.ToList().Should().NotBeEmpty();
        }
    }

    [Fact]
    public void GeContactOperationDetails_ReturnsEmpty_WhenPublishedAtIsNotNull()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GeContactOperationDetails_ReturnsEmpty_WhenPublishedAtIsNotNull));
        var expectedOperationType = OperationAction.Insert;
        var entityId = Guid.NewGuid();

        using (var context = new RefContext(options))
        {
            var regOperation = new RegOperationEntity
            {
                Id = 41,
                Operation = expectedOperationType,
                CreationDate = DateTime.UtcNow,
                EntityId = entityId,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "contact@test.com",
                PublishedAt = DateTime.UtcNow,
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.CONTACT,
                ProcessStatus = ""
            };
            context.RegOperationEntity.Add(regOperation);

            var refContact = new RefContactEntity
            {
                EntityId = entityId,
                Email = "contact@test.com",
                FirstName = "Contact",
                LastName = "Tester",
                OperationType = OperationAction.Insert
            };
            context.RefContactEntity.Add(refContact);
            context.SaveChanges();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var result = repository.GeContactOperationDetails(expectedOperationType);
            result.Should().BeEmpty();
        }
    }

    [Fact]
    public void GeContactOperationDetails_ReturnsEmpty_WhenApprovalStatusIsNotApproved()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GeContactOperationDetails_ReturnsEmpty_WhenApprovalStatusIsNotApproved));
        var expectedOperationType = OperationAction.Insert;
        var entityId = Guid.NewGuid();

        using (var context = new RefContext(options))
        {
            var regOperation = new RegOperationEntity
            {
                Id = 42,
                Operation = expectedOperationType,
                CreationDate = DateTime.UtcNow,
                EntityId = entityId,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "contact@test.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Pending,
                Type = OperationCategory.CONTACT,
                ProcessStatus = ""
            };
            context.RegOperationEntity.Add(regOperation);

            var refContact = new RefContactEntity
            {
                EntityId = entityId,
                Email = "contact@test.com",
                FirstName = "Contact",
                LastName = "Tester",
                OperationType = OperationAction.Insert
            };
            context.RefContactEntity.Add(refContact);
            context.SaveChanges();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var result = repository.GeContactOperationDetails(expectedOperationType);
            result.Should().BeEmpty();
        }
    }

    [Fact]
    public void GeContactOperationDetails_ReturnsEmpty_WhenTypeIsNotContact()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GeContactOperationDetails_ReturnsEmpty_WhenTypeIsNotContact));
        var expectedOperationType = OperationAction.Insert;
        var entityId = Guid.NewGuid();

        using (var context = new RefContext(options))
        {
            var regOperation = new RegOperationEntity
            {
                Id = 43,
                Operation = expectedOperationType,
                CreationDate = DateTime.UtcNow,
                EntityId = entityId,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "contact@test.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.ACCOUNT,
                ProcessStatus = ""
            };
            context.RegOperationEntity.Add(regOperation);

            var refContact = new RefContactEntity
            {
                EntityId = entityId,
                Email = "contact@test.com",
                FirstName = "Contact",
                LastName = "Tester",
                OperationType = OperationAction.Insert
            };
            context.RefContactEntity.Add(refContact);
            context.SaveChanges();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var result = repository.GeContactOperationDetails(expectedOperationType);
            result.Should().BeEmpty();
        }
    }

    [Fact]
    public void GeContactOperationDetails_ReturnsEmpty_WhenOperationDoesNotMatch()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GeContactOperationDetails_ReturnsEmpty_WhenOperationDoesNotMatch));
        var expectedOperationType = OperationAction.Insert;
        var entityId = Guid.NewGuid();

        using (var context = new RefContext(options))
        {
            var regOperation = new RegOperationEntity
            {
                Id = 44,
                Operation = OperationAction.Delete,
                CreationDate = DateTime.UtcNow,
                EntityId = entityId,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "contact@test.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.CONTACT,
                ProcessStatus = ""
            };
            context.RegOperationEntity.Add(regOperation);

            var refContact = new RefContactEntity
            {
                EntityId = entityId,
                Email = "contact@test.com",
                FirstName = "Contact",
                LastName = "Tester",
                OperationType = OperationAction.Insert
            };
            context.RefContactEntity.Add(refContact);
            context.SaveChanges();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var result = repository.GeContactOperationDetails(expectedOperationType);
            result.Should().BeEmpty();
        }
    }

    [Fact]
    public void GeContactOperationDetails_ReturnsMultipleRecords_WhenMultipleMatch()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GeContactOperationDetails_ReturnsMultipleRecords_WhenMultipleMatch));
        var expectedOperationType = OperationAction.Insert;
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();

        using (var context = new RefContext(options))
        {
            var regOperation1 = new RegOperationEntity
            {
                Id = 45,
                Operation = expectedOperationType,
                CreationDate = DateTime.UtcNow,
                EntityId = entityId1,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "contact1@test.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.CONTACT,
                ProcessStatus = ""
            };
            var regOperation2 = new RegOperationEntity
            {
                Id = 46,
                Operation = expectedOperationType,
                CreationDate = DateTime.UtcNow,
                EntityId = entityId2,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "contact2@test.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.CONTACT,
                ProcessStatus = ""
            };
            context.RegOperationEntity.AddRange(regOperation1, regOperation2);

            var refContact1 = new RefContactEntity
            {
                EntityId = entityId1,
                Email = "contact1@test.com",
                FirstName = "Contact1",
                LastName = "Tester1",
                OperationType = OperationAction.Insert
            };
            var refContact2 = new RefContactEntity
            {
                EntityId = entityId2,
                Email = "contact2@test.com",
                FirstName = "Contact2",
                LastName = "Tester2",
                OperationType = OperationAction.Insert
            };
            context.RefContactEntity.AddRange(refContact1, refContact2);
            context.SaveChanges();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var results = repository.GeContactOperationDetails(expectedOperationType);
            results.Should().NotBeNull();
            results.ToList().Should().HaveCount(2);
        }
    }

    #endregion

    #region GeRoleOperationDetailsAsync Tests

    [Fact]
    public async Task GeRoleOperationDetailsAsync_ReturnsCorrectRecords_ForNonSystemCreated()
    {
        // Arrange for non-system-created operations.
        var options = CreateInMemoryOptions(nameof(GeRoleOperationDetailsAsync_ReturnsCorrectRecords_ForNonSystemCreated));
        var expectedOperationName = OperationAction.Update;
        var expectedEntityId = Guid.NewGuid();

        using (var context = new RefContext(options))
        {
            var regOperation = new RegOperationEntity
            {
                Id = 50,
                Operation = expectedOperationName,
                CreationDate = DateTime.UtcNow,
                EntityId = expectedEntityId,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "role@test.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.ROLE,
                ProcessStatus = "",
                CreatedBySystem = false  // non-system created
            };
            context.RegOperationEntity.Add(regOperation);

            var refRole = new RefRoleEntity
            {
                EntityId = expectedEntityId,
                AccountNumber = "ROLE_ACC_1",
                ContactEmail = "role@test.com",
                OperationType = OperationAction.Update
            };
            context.RefRoleEntity.Add(refRole);

            await context.SaveChangesAsync();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var roleOperationRecords = await repository.GeRoleOperationDetailsAsync(expectedOperationName, 10, false);
            roleOperationRecords.Should().NotBeNull();
            roleOperationRecords.Should().HaveCount(1);
            roleOperationRecords.First().Operation.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
        }
    }

    [Fact]
    public async Task GeRoleOperationDetailsAsync_ReturnsCorrectRecords_ForSystemCreated()
    {
        // Arrange for system-created operations.
        var options = CreateInMemoryOptions(nameof(GeRoleOperationDetailsAsync_ReturnsCorrectRecords_ForSystemCreated));
        var expectedOperationName = OperationAction.Update;
        var expectedEntityId = Guid.NewGuid();
        using (var context = new RefContext(options))
        {
            var regOperation = new RegOperationEntity
            {
                Id = 60,
                Operation = expectedOperationName,
                CreationDate = DateTime.UtcNow,
                EntityId = expectedEntityId,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "sysrole@test.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.ROLE,
                ProcessStatus = "",
                CreatedBySystem = true  // system created
            };
            context.RegOperationEntity.Add(regOperation);

            var accountEntity = new AccountEntity
            {
                AccountId = 2001,
                AccountNumber = "ROLE_ACC_SYS",
                AccountGlobalUniqueId = expectedEntityId,
                LegalName = "legal",
            };
            context.AccountEntity.Add(accountEntity);

            var roleEntity = new RoleEntity
            {
                ContactId = 3001,
                AccountId = 2001,
                AccountNumber = "ROLE_ACC_SYS",
                ContactEmail = "sysrole@test.com"
            };
            context.RoleEntity.Add(roleEntity);

            await context.SaveChangesAsync();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var roleOperationRecords = await repository.GeRoleOperationDetailsAsync(expectedOperationName, 10, true);
            roleOperationRecords.Should().NotBeNull();
            roleOperationRecords.Should().HaveCount(1);
            roleOperationRecords.First().Operation.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
        }
    }

    [Fact]
    public async Task GeRoleOperationDetailsAsync_RespectsPagination_OnlyReturnsChunkSizeRecords()
    {
        // Arrange: Seed multiple non-system created records.
        var options = CreateInMemoryOptions(nameof(GeRoleOperationDetailsAsync_RespectsPagination_OnlyReturnsChunkSizeRecords));
        var expectedOperationName = OperationAction.Update;
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var entityId3 = Guid.NewGuid();

        using (var context = new RefContext(options))
        {
            var regOperation1 = new RegOperationEntity
            {
                Id = 70,
                Operation = expectedOperationName,
                CreationDate = DateTime.UtcNow,
                EntityId = entityId1,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "role1@test.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.ROLE,
                ProcessStatus = "",
                CreatedBySystem = false
            };
            var regOperation2 = new RegOperationEntity
            {
                Id = 71,
                Operation = expectedOperationName,
                CreationDate = DateTime.UtcNow,
                EntityId = entityId2,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "role2@test.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.ROLE,
                ProcessStatus = "",
                CreatedBySystem = false
            };
            var regOperation3 = new RegOperationEntity
            {
                Id = 72,
                Operation = expectedOperationName,
                CreationDate = DateTime.UtcNow,
                EntityId = entityId3,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "role3@test.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.ROLE,
                ProcessStatus = "",
                CreatedBySystem = false
            };
            context.RegOperationEntity.AddRange(regOperation1, regOperation2, regOperation3);

            var refRole1 = new RefRoleEntity
            {
                EntityId = entityId1,
                AccountNumber = "ROLE_ACC1",
                ContactEmail = "role1@test.com",
                OperationType = OperationAction.Update
            };
            var refRole2 = new RefRoleEntity
            {
                EntityId = entityId2,
                AccountNumber = "ROLE_ACC2",
                ContactEmail = "role2@test.com",
                OperationType = OperationAction.Update
            };
            var refRole3 = new RefRoleEntity
            {
                EntityId = entityId3,
                AccountNumber = "ROLE_ACC3",
                ContactEmail = "role3@test.com",
                OperationType = OperationAction.Update
            };
            context.RefRoleEntity.AddRange(refRole1, refRole2, refRole3);

            await context.SaveChangesAsync();
        }

        // Act & Assert: Set chuckSize (chunkSize) to 2.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var roleOperationRecords = await repository.GeRoleOperationDetailsAsync(expectedOperationName, 2, false);
            roleOperationRecords.Should().NotBeNull();
            roleOperationRecords.Should().HaveCount(2); // Only two records should be returned.
        }
    }

    [Fact]
    public async Task GeRoleOperationDetailsAsync_ReturnsEmpty_WhenNoMatchingRecords()
    {
        // Arrange: No matching records seeded.
        var options = CreateInMemoryOptions(nameof(GeRoleOperationDetailsAsync_ReturnsEmpty_WhenNoMatchingRecords));
        var expectedOperationName = OperationAction.Update;

        using (var context = new RefContext(options))
        {
            var regOperation = new RegOperationEntity
            {
                Id = 80,
                Operation = OperationAction.Insert, // Different from expected.
                CreationDate = DateTime.UtcNow,
                EntityId = Guid.NewGuid(),
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "role@test.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.ROLE,
                ProcessStatus = "",
                CreatedBySystem = false
            };
            context.RegOperationEntity.Add(regOperation);
            await context.SaveChangesAsync();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var roleOperationRecords = await repository.GeRoleOperationDetailsAsync(expectedOperationName, 10, false);
            roleOperationRecords.Should().BeEmpty();
        }
    }

    #endregion

    #region UpdateOperationByIdAsync Tests

    [Fact]
    public async Task UpdateOperationByIdAsync_UpdatesOperation_WhenExists()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(UpdateOperationByIdAsync_UpdatesOperation_WhenExists));
        int operationId = 70;
        var expectedEntityId = Guid.NewGuid();
        using (var context = new RefContext(options))
        {
            var regOperation = new RegOperationEntity
            {
                Id = operationId,
                Operation = OperationAction.Insert,
                CreationDate = DateTime.UtcNow,
                EntityId = expectedEntityId,
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "update@test.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Pending,
                Type = OperationCategory.ROLE,
                ProcessStatus = ""
            };
            context.RegOperationEntity.Add(regOperation);
            await context.SaveChangesAsync();
        }

        // Act.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var updatedOperationModel = new RegOperation
            {
                Id = operationId,
                Operation = OperationAction.Insert,
                Type = OperationCategory.ROLE,
                Status = ApprovalStatus.Approved,
                LastStatusUpdatedDate = DateTime.UtcNow,
                LastStatusUpdatedBy = "update@test.com"
            };
            var updatedOperation = await repository.UpdateOperationByIdAsync(operationId, updatedOperationModel);
            updatedOperation.Status.Should().Be(ApprovalStatus.Approved);
        }
    }

    [Fact]
    public async Task UpdateOperationByIdAsync_ThrowsNotFoundException_WhenOperationDoesNotExist()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(UpdateOperationByIdAsync_ThrowsNotFoundException_WhenOperationDoesNotExist));
        using (var context = new RefContext(options))
        {
            await context.SaveChangesAsync();
        }

        // Act & Assert.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var updatedModel = new RegOperation
            {
                Id = 999,
                Operation = OperationAction.Update,
                Type = OperationCategory.ROLE,
                Status = ApprovalStatus.Approved,
                LastStatusUpdatedDate = DateTime.UtcNow,
                LastStatusUpdatedBy = "notfound@test.com"
            };

            Func<Task> act = async () => await repository.UpdateOperationByIdAsync(999, updatedModel);
            await act.Should().ThrowAsync<NotFoundException>();
        }
    }

    #endregion

    #region BulkUpdateOperationsStatusAsync Tests

    [Fact]
    public async Task BulkUpdateOperationsStatusAsync_UpdatesStatusesSuccessfully()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(BulkUpdateOperationsStatusAsync_UpdatesStatusesSuccessfully));
        string originalStatus = ProcessStatus.Sent;
        string newStatus = ProcessStatus.Succeeded;
        using (var context = new RefContext(options))
        {
            var bulkOp1 = new RegOperationEntity
            {
                Id = 80,
                ProcessStatus = originalStatus,
                Operation = "BULK",
                CreationDate = DateTime.UtcNow,
                EntityId = Guid.NewGuid(),
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "bulk@test.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.ACCOUNT
            };
            var bulkOp2 = new RegOperationEntity
            {
                Id = 81,
                ProcessStatus = originalStatus,
                Operation = "BULK",
                CreationDate = DateTime.UtcNow,
                EntityId = Guid.NewGuid(),
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "bulk@test.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.ACCOUNT
            };
            context.RegOperationEntity.AddRange(bulkOp1, bulkOp2);
            await context.SaveChangesAsync();
        }

        // Act.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            var bulkOperations = context.RegOperationEntity.Where(o => o.Operation == "BULK").ToList();
            var updateResult = await repository.BulkUpdateOperationsStatusAsync(newStatus, bulkOperations);
            updateResult.Should().BeTrue();

            var updatedBulkOperations = context.RegOperationEntity.Where(o => o.Operation == "BULK").ToList();
            updatedBulkOperations.All(op => op.ProcessStatus == newStatus).Should().BeTrue();
        }
    }

    #endregion

    #region CreateOperationAsync Tests

    [Fact]
    public async Task CreateOperationAsync_AddsNewOperation()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(CreateOperationAsync_AddsNewOperation));
        var expectedOperationName = OperationAction.Insert;
        var newOperationEntity = new RegOperationEntity
        {
            Id = 90,
            Operation = expectedOperationName,
            CreationDate = DateTime.UtcNow,
            EntityId = Guid.NewGuid(),
            LastStatusApprovalDate = DateTime.UtcNow,
            LastStatusApprovalBy = "create@test.com",
            PublishedAt = null,
            ApprovalStatus = ApprovalStatus.Pending,
            Type = OperationCategory.CONTACT,
            ProcessStatus = ""
        };

        // Act.
        using (var context = new RefContext(options))
        {
            var repository = CreateRepository(context);
            await repository.CreateOperationAsync(newOperationEntity);
        }

        // Assert.
        using (var context = new RefContext(options))
        {
            var createdOperation = await context.RegOperationEntity.FindAsync(90);
            createdOperation.Should().NotBeNull();
            createdOperation!.Operation.Should().Be(expectedOperationName);
        }
    }

    #endregion

    #region GetPendingRoleApprovalsAsync

    [Fact]
    public async Task GetPendingRoleApprovalsAsync_ShouldReturn_Operations_GrouppedByAccounts_WhenPrimaryDataExists()
    {
        // Arrange
        var accounts = new List<AccountEntity>
            {
                new AccountEntity
                {
                    AccountId = 1,
                    AccountNumber = "ACC1",
                    LegalName = "Account1",
                    AccountGlobalUniqueId = Guid.NewGuid()
                },
                new AccountEntity
                {
                    AccountId = 2,
                    AccountNumber = "ACC2",
                    LegalName = "Account2",
                    AccountGlobalUniqueId = Guid.NewGuid()
                }
            };

        var contact = new ContactEntity
        {
            ContactId = 1,
            FirstName = "Test",
            LastName = "ContactCollan",
            Email = "ContactCollan@email.fr",
            ContactGlobalUniqueId = Guid.NewGuid(),
            Type = "COLLABORATOR",
        };

        var roles = new List<RoleEntity>
            {
                new RoleEntity
                {
                    AccountId = accounts.First().AccountId,
                    ContactId = contact.ContactId,
                    ContactGlobalUniqueId = contact.ContactGlobalUniqueId.Value,
                    AccountGlobalUniqueId = accounts.First().AccountGlobalUniqueId.Value,
                    AccountNumber = accounts.First().AccountNumber,
                    ContactEmail = contact.Email,
                    RoleDuplicatesCounter = 0,
                },
                new RoleEntity
                {
                    AccountId = accounts.Last().AccountId,
                    ContactId = contact.ContactId,
                    ContactGlobalUniqueId = contact.ContactGlobalUniqueId.Value,
                    AccountGlobalUniqueId = accounts.Last().AccountGlobalUniqueId.Value,
                    AccountNumber = accounts.Last().AccountNumber,
                    ContactEmail = contact.Email,
                    RoleDuplicatesCounter = 0,
                }
            };

        var refRoles = new List<RefRoleEntity>
            {
                new RefRoleEntity
                {
                    EntityId = Guid.NewGuid(),
                    AccountNumber = roles.First().AccountNumber,
                    ContactEmail = "user1@email.com",
                    OperationType = OperationAction.Insert
                },
                new RefRoleEntity
                {
                    EntityId = Guid.NewGuid(),
                    AccountNumber = roles.First().AccountNumber,
                    ContactEmail = "user2@email.com",
                    OperationType = OperationAction.Insert
                },
                new RefRoleEntity
                {
                    EntityId = Guid.NewGuid(),
                    AccountNumber = roles.Last().AccountNumber,
                    ContactEmail = "user3@email.com",
                    OperationType = OperationAction.Insert
                },
                new RefRoleEntity
                {
                    EntityId = Guid.NewGuid(),
                    AccountNumber = roles.Last().AccountNumber,
                    ContactEmail = "user4@email.com",
                    OperationType = OperationAction.Insert
                }
            };

        // Primary (Pulse) contacts exist for the first three refRoles.
        var primaryContacts = new List<ContactEntity>
            {
                new ContactEntity
                {
                    ContactId = 2,
                    Email = refRoles[0].ContactEmail,
                    FirstName = "User1",
                    LastName = "User1",
                    Type = "CUSTOMER",
                },
                new ContactEntity
                {
                    ContactId = 3,
                    Email = refRoles[1].ContactEmail,
                    FirstName = "User2",
                    LastName = "User2",
                    Type = "CUSTOMER",
                },
                new ContactEntity
                {
                    ContactId = 4,
                    Email = refRoles[2].ContactEmail,
                    FirstName = "User3",
                    LastName = "User3",
                    Type = "CUSTOMER"
                }
            };

        // Fallback contacts (won't be used here)
        var fallbackContacts = new List<RefContactEntity>
            {
                new RefContactEntity
                {
                    EntityId = Guid.NewGuid(),
                    Email = refRoles[2].ContactEmail,
                    FirstName = "User3",
                    LastName = "User3",
                    OperationType = OperationAction.Insert
                },
                new RefContactEntity
                {
                    EntityId = Guid.NewGuid(),
                    Email = refRoles[3].ContactEmail,
                    FirstName = "User4",
                    LastName = "User4",
                    OperationType = OperationAction.Insert
                }
            };

        var operations = new List<RegOperationEntity>
            {
                new RegOperationEntity
                {
                    Id = 1,
                    EntityId = refRoles[0].EntityId,
                    Operation = OperationAction.Insert,
                    Type = OperationCategory.ROLE,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreationDate = DateTime.UtcNow.AddDays(-2),
                },
                new RegOperationEntity
                {
                    Id = 2,
                    EntityId = refRoles[1].EntityId,
                    Operation = OperationAction.Insert,
                    Type = OperationCategory.ROLE,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreationDate = DateTime.UtcNow, // Latest for ACC1.
                },
                new RegOperationEntity
                {
                    Id = 3,
                    EntityId = refRoles[2].EntityId,
                    Operation = OperationAction.Insert,
                    Type = OperationCategory.ROLE,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreationDate = DateTime.UtcNow.AddDays(-3),
                },
                new RegOperationEntity
                {
                    Id = 4,
                    EntityId = refRoles[3].EntityId,
                    Operation = OperationAction.Insert,
                    Type = OperationCategory.ROLE,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreationDate = DateTime.UtcNow.AddDays(-1),
                },
            };

        var options = CreateInMemoryOptions(nameof(GetPendingRoleApprovalsAsync_ShouldReturn_Operations_GrouppedByAccounts_WhenPrimaryDataExists));
        using (var context = new RefContext(options))
        {
            context.AccountEntity.AddRange(accounts);
            context.ContactEntity.Add(contact);
            context.ContactEntity.AddRange(primaryContacts);
            context.RefContactEntity.AddRange(fallbackContacts);
            context.RoleEntity.AddRange(roles);
            context.RefRoleEntity.AddRange(refRoles);
            context.RegOperationEntity.AddRange(operations);
            await context.SaveChangesAsync();

            await PopulatePendingOperationsFromViewAsync(context);

            var repository = CreateRepository(context);

            // Act
            var response = await repository.GetPendingRoleApprovalsAsync(contact.ContactId, 0, 10, string.Empty);

            // Assert
            response.PendingRoleApprovals.Count().Should().Be(2);
            context.Database.EnsureDeleted();
        }
    }

    [Fact]
    public async Task GetPendingRoleApprovalsAsync_ShouldReturn_Operations_GrouppedByAccounts_FromFallback_WhenPrimaryDataNotExists()
    {
        // Arrange
        var accounts = new List<AccountEntity>
            {
                new AccountEntity
                {
                    AccountId = 1,
                    AccountNumber = "ACC1",
                    LegalName = "Account1",
                    AccountGlobalUniqueId = Guid.NewGuid()
                },
                new AccountEntity
                {
                    AccountId = 2,
                    AccountNumber = "ACC2",
                    LegalName = "Account2",
                    AccountGlobalUniqueId = Guid.NewGuid()
                }
            };

        var contact = new ContactEntity
        {
            ContactId = 1,
            FirstName = "Test",
            LastName = "ContactCollan",
            Email = "ContactCollan@email.fr",
            ContactGlobalUniqueId = Guid.NewGuid(),
            Type = "COLLABORATOR",
        };

        var roles = new List<RoleEntity>
            {
                new RoleEntity
                {
                    AccountId = accounts.First().AccountId,
                    ContactId = contact.ContactId,
                    ContactGlobalUniqueId = contact.ContactGlobalUniqueId.Value,
                    AccountGlobalUniqueId = accounts.First().AccountGlobalUniqueId.Value,
                    AccountNumber = accounts.First().AccountNumber,
                    ContactEmail = contact.Email,
                    RoleDuplicatesCounter = 0,
                },
                new RoleEntity
                {
                    AccountId = accounts.Last().AccountId,
                    ContactId = contact.ContactId,
                    ContactGlobalUniqueId = contact.ContactGlobalUniqueId.Value,
                    AccountGlobalUniqueId = accounts.Last().AccountGlobalUniqueId.Value,
                    AccountNumber = accounts.Last().AccountNumber,
                    ContactEmail = contact.Email,
                    RoleDuplicatesCounter = 0,
                }
            };

        // In this test, only fallback contacts exist.
        var refRoles = new List<RefRoleEntity>
            {
                new RefRoleEntity
                {
                    EntityId = Guid.NewGuid(),
                    AccountNumber = roles.Last().AccountNumber,
                    ContactEmail = "user3@email.com",
                    OperationType = OperationAction.Insert
                },
                new RefRoleEntity
                {
                    EntityId = Guid.NewGuid(),
                    AccountNumber = roles.Last().AccountNumber,
                    ContactEmail = "user4@email.com",
                    OperationType = OperationAction.Insert
                }
            };

        var fallbackContacts = new List<RefContactEntity>
            {
                new RefContactEntity
                {
                    EntityId = Guid.NewGuid(),
                    Email = refRoles[0].ContactEmail,
                    FirstName = "User3",
                    LastName = "User3",
                    OperationType = OperationAction.Insert
                },
                new RefContactEntity
                {
                    EntityId = Guid.NewGuid(),
                    Email = refRoles[1].ContactEmail,
                    FirstName = "User4",
                    LastName = "User4",
                    OperationType = OperationAction.Insert
                }
            };

        var operations = new List<RegOperationEntity>
            {
                new RegOperationEntity
                {
                    Id = 3,
                    EntityId = refRoles[0].EntityId,
                    Operation = OperationAction.Insert,
                    Type = OperationCategory.ROLE,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreationDate = DateTime.UtcNow.AddDays(-3),
                },
                new RegOperationEntity
                {
                    Id = 4,
                    EntityId = refRoles[1].EntityId,
                    Operation = OperationAction.Insert,
                    Type = OperationCategory.ROLE,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreationDate = DateTime.UtcNow.AddDays(-1),
                },
            };

        var options = CreateInMemoryOptions(nameof(GetPendingRoleApprovalsAsync_ShouldReturn_Operations_GrouppedByAccounts_FromFallback_WhenPrimaryDataNotExists));
        using (var context = new RefContext(options))
        {
            context.AccountEntity.AddRange(accounts);
            context.ContactEntity.Add(contact);
            context.RefContactEntity.AddRange(fallbackContacts);
            context.RoleEntity.AddRange(roles);
            context.RefRoleEntity.AddRange(refRoles);
            context.RegOperationEntity.AddRange(operations);
            await context.SaveChangesAsync();

            await PopulatePendingOperationsFromViewAsync(context);

            var repository = CreateRepository(context);

            // Act
            var response = await repository.GetPendingRoleApprovalsAsync(contact.ContactId, 0, 10, string.Empty);

            // Assert: Only one account (ACC2) should be returned.
            response.PendingRoleApprovals.Count().Should().Be(1);
            response.PendingRoleApprovals.First().AccountNumber.Should().Be(accounts.Last().AccountNumber);

            context.Database.EnsureDeleted();
        }
    }

    [Fact]
    public async Task GetPendingRoleApprovalsAsync_ShouldReturn_Operations_GrouppedByAccounts_OrderedByLegalName()
    {
        // Arrange
        var accounts = new List<AccountEntity>
            {
                new AccountEntity
                {
                    AccountId = 1,
                    AccountNumber = "ACC1",
                    LegalName = "Account1",
                    AccountGlobalUniqueId = Guid.NewGuid()
                },
                new AccountEntity
                {
                    AccountId = 2,
                    AccountNumber = "ACC2",
                    LegalName = "Account2",
                    AccountGlobalUniqueId = Guid.NewGuid()
                }
            };

        var contact = new ContactEntity
        {
            ContactId = 1,
            FirstName = "Test",
            LastName = "ContactCollan",
            Email = "ContactCollan@email.fr",
            ContactGlobalUniqueId = Guid.NewGuid(),
            Type = "COLLABORATOR",
        };

        var roles = new List<RoleEntity>
            {
                new RoleEntity
                {
                    AccountId = accounts.First().AccountId,
                    ContactId = contact.ContactId,
                    ContactGlobalUniqueId = contact.ContactGlobalUniqueId.Value,
                    AccountGlobalUniqueId = accounts.First().AccountGlobalUniqueId.Value,
                    AccountNumber = accounts.First().AccountNumber,
                    ContactEmail = contact.Email,
                    RoleDuplicatesCounter = 0,
                },
                new RoleEntity
                {
                    AccountId = accounts.Last().AccountId,
                    ContactId = contact.ContactId,
                    ContactGlobalUniqueId = contact.ContactGlobalUniqueId.Value,
                    AccountGlobalUniqueId = accounts.Last().AccountGlobalUniqueId.Value,
                    AccountNumber = accounts.Last().AccountNumber,
                    ContactEmail = contact.Email,
                    RoleDuplicatesCounter = 0,
                }
            };

        var refRoles = new List<RefRoleEntity>
            {
                new RefRoleEntity
                {
                    EntityId = Guid.NewGuid(),
                    AccountNumber = roles.First().AccountNumber,
                    ContactEmail = "user1@email.com",
                    OperationType = OperationAction.Insert
                },
                new RefRoleEntity
                {
                    EntityId = Guid.NewGuid(),
                    AccountNumber = roles.First().AccountNumber,
                    ContactEmail = "user2@email.com",
                    OperationType = OperationAction.Insert
                },
                new RefRoleEntity
                {
                    EntityId = Guid.NewGuid(),
                    AccountNumber = roles.Last().AccountNumber,
                    ContactEmail = "user3@email.com",
                    OperationType = OperationAction.Insert
                },
                new RefRoleEntity
                {
                    EntityId = Guid.NewGuid(),
                    AccountNumber = roles.Last().AccountNumber,
                    ContactEmail = "user4@email.com",
                    OperationType = OperationAction.Insert
                }
            };

        // Primary contacts for all refRoles.
        var primaryContacts = new List<ContactEntity>
            {
                new ContactEntity
                {
                    ContactId = 2,
                    Email = refRoles[0].ContactEmail,
                    FirstName = "User1",
                    LastName = "User1",
                    Type = "CUSTOMER",
                },
                new ContactEntity
                {
                    ContactId = 3,
                    Email = refRoles[1].ContactEmail,
                    FirstName = "User1",
                    LastName = "User1",
                    Type = "CUSTOMER",
                },
                new ContactEntity
                {
                    ContactId = 4,
                    Email = refRoles[2].ContactEmail,
                    FirstName = "User2",
                    LastName = "User2",
                    Type = "CUSTOMER",
                },
                new ContactEntity
                {
                    ContactId = 5,
                    Email = refRoles[3].ContactEmail,
                    FirstName = "User3",
                    LastName = "User3",
                    Type = "CUSTOMER"
                }
            };

        var operations = new List<RegOperationEntity>
            {
                new RegOperationEntity
                {
                    Id = 1,
                    EntityId = refRoles[0].EntityId,
                    Operation = OperationAction.Insert,
                    Type = OperationCategory.ROLE,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreationDate = DateTime.UtcNow.AddDays(-2),
                },
                new RegOperationEntity
                {
                    Id = 2,
                    EntityId = refRoles[1].EntityId,
                    Operation = OperationAction.Insert,
                    Type = OperationCategory.ROLE,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreationDate = DateTime.UtcNow.AddDays(-4),
                },
                new RegOperationEntity
                {
                    Id = 3,
                    EntityId = refRoles[2].EntityId,
                    Operation = OperationAction.Insert,
                    Type = OperationCategory.ROLE,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreationDate = DateTime.UtcNow.AddDays(-3),
                },
                new RegOperationEntity
                {
                    Id = 4,
                    EntityId = refRoles[3].EntityId,
                    Operation = OperationAction.Insert,
                    Type = OperationCategory.ROLE,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreationDate = DateTime.UtcNow, // Most recent operation belongs to ACC2.
                },
            };

        var options = CreateInMemoryOptions(nameof(GetPendingRoleApprovalsAsync_ShouldReturn_Operations_GrouppedByAccounts_OrderedByLegalName));
        using (var context = new RefContext(options))
        {
            context.AccountEntity.AddRange(accounts);
            context.ContactEntity.Add(contact);
            context.ContactEntity.AddRange(primaryContacts);
            context.RoleEntity.AddRange(roles);
            context.RefRoleEntity.AddRange(refRoles);
            context.RegOperationEntity.AddRange(operations);
            await context.SaveChangesAsync();

            await PopulatePendingOperationsFromViewAsync(context);

            var repository = CreateRepository(context);

            // Act
            var response = await repository.GetPendingRoleApprovalsAsync(contact.ContactId, 0, 10, string.Empty);

            // Assert: Expecting the group with ACC2 (with the most recent operation) to be first.
            response.PendingRoleApprovals.First().AccountNumber.Should().Be(accounts.First().AccountNumber);
            context.Database.EnsureDeleted();
        }
    }

    [Theory]
    [InlineData(0, 1, "ACC1")]
    [InlineData(1, 1, "ACC2")]
    [InlineData(2, 1, "ACC3")]
    public async Task GetPendingRoleApprovalsAsync_ShouldRespectPagination(int skip, int take, string expectedAccountNumber)
    {
        // Arrange
        var accounts = new List<AccountEntity>
            {
                new AccountEntity
                {
                    AccountId = 1,
                    AccountNumber = "ACC1",
                    LegalName = "Account1",
                    AccountGlobalUniqueId = Guid.NewGuid(),
                },
                new AccountEntity
                {
                    AccountId = 2,
                    AccountNumber = "ACC2",
                    LegalName = "Account2",
                    AccountGlobalUniqueId = Guid.NewGuid()
                },
                new AccountEntity
                {
                    AccountId = 3,
                    AccountNumber = "ACC3",
                    LegalName = "Account3",
                    AccountGlobalUniqueId = Guid.NewGuid()
                }
            };

        var contact = new ContactEntity
        {
            ContactId = 1,
            FirstName = "Test",
            LastName = "ContactCollan",
            Email = "ContactCollan@email.fr",
            ContactGlobalUniqueId = Guid.NewGuid(),
            Type = "COLLABORATOR",
        };

        var roles = new List<RoleEntity>
            {
                new RoleEntity
                {
                    AccountId = accounts[0].AccountId,
                    ContactId = contact.ContactId,
                    ContactGlobalUniqueId = contact.ContactGlobalUniqueId.Value,
                    AccountGlobalUniqueId = accounts[0].AccountGlobalUniqueId.Value,
                    AccountNumber = accounts[0].AccountNumber,
                    ContactEmail = contact.Email,
                    RoleDuplicatesCounter = 0,
                },
                new RoleEntity
                {
                    AccountId = accounts[1].AccountId,
                    ContactId = contact.ContactId,
                    ContactGlobalUniqueId = contact.ContactGlobalUniqueId.Value,
                    AccountGlobalUniqueId = accounts[1].AccountGlobalUniqueId.Value,
                    AccountNumber = accounts[1].AccountNumber,
                    ContactEmail = contact.Email,
                    RoleDuplicatesCounter = 0,
                },
                new RoleEntity
                {
                    AccountId = accounts[2].AccountId,
                    ContactId = contact.ContactId,
                    ContactGlobalUniqueId = contact.ContactGlobalUniqueId.Value,
                    AccountGlobalUniqueId = accounts[2].AccountGlobalUniqueId.Value,
                    AccountNumber = accounts[2].AccountNumber,
                    ContactEmail = contact.Email,
                    RoleDuplicatesCounter = 0,
                }
            };

        var refRoles = new List<RefRoleEntity>
            {
                new RefRoleEntity
                {
                    EntityId = Guid.NewGuid(),
                    AccountNumber = roles[0].AccountNumber,
                    ContactEmail = "user1@email.com",
                    OperationType = OperationAction.Insert
                },
                new RefRoleEntity
                {
                    EntityId = Guid.NewGuid(),
                    AccountNumber = roles[1].AccountNumber,
                    ContactEmail = "user2@email.com",
                    OperationType = OperationAction.Insert
                },
                new RefRoleEntity
                {
                    EntityId = Guid.NewGuid(),
                    AccountNumber = roles[2].AccountNumber,
                    ContactEmail = "user3@email.com",
                    OperationType = OperationAction.Insert
                }
            };

        var primaryContacts = new List<ContactEntity>
            {
                new ContactEntity
                {
                    ContactId = 2,
                    Email = refRoles[0].ContactEmail,
                    FirstName = "User1",
                    LastName = "User1",
                    Type = "CUSTOMER",
                },
                new ContactEntity
                {
                    ContactId = 3,
                    Email = refRoles[1].ContactEmail,
                    FirstName = "User2",
                    LastName = "User2",
                    Type = "CUSTOMER",
                },
                new ContactEntity
                {
                    ContactId = 4,
                    Email = refRoles[2].ContactEmail,
                    FirstName = "User3",
                    LastName = "User3",
                    Type = "CUSTOMER"
                }
            };

        var operations = new List<RegOperationEntity>
            {
                new RegOperationEntity
                {
                    Id = 1,
                    EntityId = refRoles[0].EntityId,
                    Operation = OperationAction.Insert,
                    Type = OperationCategory.ROLE,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreationDate = DateTime.UtcNow.AddDays(-2),
                },
                new RegOperationEntity
                {
                    Id = 2,
                    EntityId = refRoles[1].EntityId,
                    Operation = OperationAction.Insert,
                    Type = OperationCategory.ROLE,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreationDate = DateTime.UtcNow, // Most recent, so group "ACC2" comes first.
                },
                new RegOperationEntity
                {
                    Id = 3,
                    EntityId = refRoles[2].EntityId,
                    Operation = OperationAction.Insert,
                    Type = OperationCategory.ROLE,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreationDate = DateTime.UtcNow.AddDays(-3),
                }
            };

        var options = CreateInMemoryOptions($"{nameof(GetPendingRoleApprovalsAsync_ShouldRespectPagination)}_{skip}_{take}_{expectedAccountNumber}");
        using (var context = new RefContext(options))
        {
            context.AccountEntity.AddRange(accounts);
            context.ContactEntity.Add(contact);
            context.ContactEntity.AddRange(primaryContacts);
            context.RoleEntity.AddRange(roles);
            context.RefRoleEntity.AddRange(refRoles);
            context.RegOperationEntity.AddRange(operations);
            await context.SaveChangesAsync();

            await PopulatePendingOperationsFromViewAsync(context);

            var repository = CreateRepository(context);

            // Act
            var response = await repository.GetPendingRoleApprovalsAsync(contact.ContactId, skip, take, string.Empty);

            // Assert: With ordering descending by LatestCreationDate, the expected account group is returned.
            response.PendingRoleApprovals.Count().Should().Be(1);
            response.PendingRoleApprovals.First().AccountNumber.Should().Be(expectedAccountNumber);

            context.Database.EnsureDeleted();
        }
    }
    [Fact]
    public async Task GetPendingRoleApprovalsAsync_WithSearchParam_ReturnsFilteredResults()
    {
        // Arrange
        var accounts = new List<AccountEntity>
            {
                new AccountEntity
                {
                    AccountId = 1,
                    AccountNumber = "ACC1",
                    LegalName = "Account1",
                    AccountGlobalUniqueId = Guid.NewGuid()
                },
                new AccountEntity
                {
                    AccountId = 2,
                    AccountNumber = "ACC2",
                    LegalName = "Account2",
                    AccountGlobalUniqueId = Guid.NewGuid()
                }
            };

        var contact = new ContactEntity
        {
            ContactId = 1,
            FirstName = "Test",
            LastName = "ContactCollan",
            Email = "ContactCollan@email.fr",
            ContactGlobalUniqueId = Guid.NewGuid(),
            Type = "COLLABORATOR",
        };

        var roles = new List<RoleEntity>
            {
                new RoleEntity
                {
                    AccountId = accounts.First().AccountId,
                    ContactId = contact.ContactId,
                    ContactGlobalUniqueId = contact.ContactGlobalUniqueId.Value,
                    AccountGlobalUniqueId = accounts.First().AccountGlobalUniqueId.Value,
                    AccountNumber = accounts.First().AccountNumber,
                    ContactEmail = contact.Email,
                    RoleDuplicatesCounter = 0,
                },
                new RoleEntity
                {
                    AccountId = accounts.Last().AccountId,
                    ContactId = contact.ContactId,
                    ContactGlobalUniqueId = contact.ContactGlobalUniqueId.Value,
                    AccountGlobalUniqueId = accounts.Last().AccountGlobalUniqueId.Value,
                    AccountNumber = accounts.Last().AccountNumber,
                    ContactEmail = contact.Email,
                    RoleDuplicatesCounter = 0,
                }
            };

        var refRoles = new List<RefRoleEntity>
            {
                new RefRoleEntity
                {
                    EntityId = Guid.NewGuid(),
                    AccountNumber = roles.First().AccountNumber,
                    ContactEmail = "user1@email.com",
                    OperationType = OperationAction.Insert
                },
                new RefRoleEntity
                {
                    EntityId = Guid.NewGuid(),
                    AccountNumber = roles.First().AccountNumber,
                    ContactEmail = "user2@email.com",
                    OperationType = OperationAction.Insert
                },
                new RefRoleEntity
                {
                    EntityId = Guid.NewGuid(),
                    AccountNumber = roles.Last().AccountNumber,
                    ContactEmail = "user3@email.com",
                    OperationType = OperationAction.Insert
                },
                new RefRoleEntity
                {
                    EntityId = Guid.NewGuid(),
                    AccountNumber = roles.Last().AccountNumber,
                    ContactEmail = "user4@email.com",
                    OperationType = OperationAction.Insert
                }
            };

        // Primary (Pulse) contacts exist for the first three refRoles.
        var primaryContacts = new List<ContactEntity>
            {
                new ContactEntity
                {
                    ContactId = 2,
                    Email = refRoles[0].ContactEmail,
                    FirstName = "User1",
                    LastName = "User1",
                    Type = "CUSTOMER",
                },
                new ContactEntity
                {
                    ContactId = 3,
                    Email = refRoles[1].ContactEmail,
                    FirstName = "User2",
                    LastName = "User2",
                    Type = "CUSTOMER",
                },
                new ContactEntity
                {
                    ContactId = 4,
                    Email = refRoles[2].ContactEmail,
                    FirstName = "User3",
                    LastName = "User3",
                    Type = "CUSTOMER"
                }
            };

        // Fallback contacts (won't be used here)
        var fallbackContacts = new List<RefContactEntity>
            {
                new RefContactEntity
                {
                    EntityId = Guid.NewGuid(),
                    Email = refRoles[2].ContactEmail,
                    FirstName = "User3",
                    LastName = "User3",
                    OperationType = OperationAction.Insert
                },
                new RefContactEntity
                {
                    EntityId = Guid.NewGuid(),
                    Email = refRoles[3].ContactEmail,
                    FirstName = "User4",
                    LastName = "User4",
                    OperationType = OperationAction.Insert
                }
            };

        var operations = new List<RegOperationEntity>
            {
                new RegOperationEntity
                {
                    Id = 1,
                    EntityId = refRoles[0].EntityId,
                    Operation = OperationAction.Insert,
                    Type = OperationCategory.ROLE,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreationDate = DateTime.UtcNow.AddDays(-2),
                },
                new RegOperationEntity
                {
                    Id = 2,
                    EntityId = refRoles[1].EntityId,
                    Operation = OperationAction.Insert,
                    Type = OperationCategory.ROLE,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreationDate = DateTime.UtcNow, // Latest for ACC1.
                },
                new RegOperationEntity
                {
                    Id = 3,
                    EntityId = refRoles[2].EntityId,
                    Operation = OperationAction.Insert,
                    Type = OperationCategory.ROLE,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreationDate = DateTime.UtcNow.AddDays(-3),
                },
                new RegOperationEntity
                {
                    Id = 4,
                    EntityId = refRoles[3].EntityId,
                    Operation = OperationAction.Insert,
                    Type = OperationCategory.ROLE,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreationDate = DateTime.UtcNow.AddDays(-1),
                },
            };

        var options = CreateInMemoryOptions(nameof(GetPendingRoleApprovalsAsync_WithSearchParam_ReturnsFilteredResults));
        using (var context = new RefContext(options))
        {
            context.AccountEntity.AddRange(accounts);
            context.ContactEntity.Add(contact);
            context.ContactEntity.AddRange(primaryContacts);
            context.RefContactEntity.AddRange(fallbackContacts);
            context.RoleEntity.AddRange(roles);
            context.RefRoleEntity.AddRange(refRoles);
            context.RegOperationEntity.AddRange(operations);
            await context.SaveChangesAsync();

            await PopulatePendingOperationsFromViewAsync(context);

            var repository = CreateRepository(context);

            // Act
            var search = "ACC1";
            var response = await repository.GetPendingRoleApprovalsAsync(contact.ContactId, 0, 10, search);

            // Assert
            response.PendingRoleApprovals.Count().Should().Be(1);
            response.PendingRoleApprovals.First().AccountNumber.Should().Be("ACC1");
            context.Database.EnsureDeleted();
        }
    }

    [Fact]
    public async Task GetPendingRoleApprovalsAsync_WithNullValuesInSearchFields_DoesNotCrash()
    {
        // Arrange
        var accounts = new List<AccountEntity>
            {
                new AccountEntity
                {
                    AccountId = 1,
                    AccountNumber = string.Empty, // Empty AccountNumber
                    LegalName = "Account1",
                    AccountGlobalUniqueId = Guid.NewGuid()
                },
                new AccountEntity
                {
                    AccountId = 2,
                    AccountNumber = "ACC2",
                    LegalName = string.Empty, // Empty LegalName
                    AccountGlobalUniqueId = Guid.NewGuid()
                },
                new AccountEntity
                {
                    AccountId = 3,
                    AccountNumber = "ACC3",
                    LegalName = "Account3",
                    AccountGlobalUniqueId = Guid.NewGuid()
                }
            };

        var contact = new ContactEntity
        {
            ContactId = 1,
            FirstName = "Test",
            LastName = "ContactCollan",
            Email = "ContactCollan@email.fr",
            ContactGlobalUniqueId = Guid.NewGuid(),
            Type = "COLLABORATOR",
        };

        var roles = new List<RoleEntity>
            {
                new RoleEntity
                {
                    AccountId = accounts.First().AccountId,
                    ContactId = contact.ContactId,
                    ContactGlobalUniqueId = contact.ContactGlobalUniqueId.Value,
                    AccountGlobalUniqueId = accounts.First().AccountGlobalUniqueId.Value,
                    AccountNumber = accounts.First().AccountNumber,
                    ContactEmail = contact.Email,
                    RoleDuplicatesCounter = 0,
                },
                new RoleEntity
                {
                    AccountId = accounts[1].AccountId,
                    ContactId = contact.ContactId,
                    ContactGlobalUniqueId = contact.ContactGlobalUniqueId.Value,
                    AccountGlobalUniqueId = accounts[1].AccountGlobalUniqueId.Value,
                    AccountNumber = accounts[1].AccountNumber,
                    ContactEmail = contact.Email,
                    RoleDuplicatesCounter = 0,
                },
                new RoleEntity
                {
                    AccountId = accounts.Last().AccountId,
                    ContactId = contact.ContactId,
                    ContactGlobalUniqueId = contact.ContactGlobalUniqueId.Value,
                    AccountGlobalUniqueId = accounts.Last().AccountGlobalUniqueId.Value,
                    AccountNumber = accounts.Last().AccountNumber,
                    ContactEmail = contact.Email,
                    RoleDuplicatesCounter = 0,
                }
            };

        var refRoles = new List<RefRoleEntity>
            {
                new RefRoleEntity
                {
                    EntityId = Guid.NewGuid(),
                    AccountNumber = roles.First().AccountNumber,
                    ContactEmail = "user1@email.com",
                    OperationType = OperationAction.Insert
                },
                new RefRoleEntity
                {
                    EntityId = Guid.NewGuid(),
                    AccountNumber = roles[1].AccountNumber,
                    ContactEmail = "user2@email.com",
                    OperationType = OperationAction.Insert
                },
                new RefRoleEntity
                {
                    EntityId = Guid.NewGuid(),
                    AccountNumber = roles.Last().AccountNumber,
                    ContactEmail = "user3@email.com",
                    OperationType = OperationAction.Insert
                }
            };

        var primaryContacts = new List<ContactEntity>
            {
                new ContactEntity
                {
                    ContactId = 2,
                    Email = refRoles[0].ContactEmail,
                    FirstName = "User1",
                    LastName = "User1",
                    Type = "CUSTOMER",
                },
                new ContactEntity
                {
                    ContactId = 3,
                    Email = refRoles[1].ContactEmail,
                    FirstName = "User2",
                    LastName = "User2",
                    Type = "CUSTOMER",
                },
                new ContactEntity
                {
                    ContactId = 4,
                    Email = refRoles[2].ContactEmail,
                    FirstName = "User3",
                    LastName = "User3",
                    Type = "CUSTOMER",
                }
            };

        var operations = new List<RegOperationEntity>
            {
                new RegOperationEntity
                {
                    Id = 1,
                    EntityId = refRoles[0].EntityId,
                    Operation = OperationAction.Insert,
                    Type = OperationCategory.ROLE,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreationDate = DateTime.UtcNow.AddDays(-2),
                },
                new RegOperationEntity
                {
                    Id = 2,
                    EntityId = refRoles[1].EntityId,
                    Operation = OperationAction.Insert,
                    Type = OperationCategory.ROLE,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreationDate = DateTime.UtcNow,
                },
                new RegOperationEntity
                {
                    Id = 3,
                    EntityId = refRoles[2].EntityId,
                    Operation = OperationAction.Insert,
                    Type = OperationCategory.ROLE,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreationDate = DateTime.UtcNow.AddDays(-1),
                }
            };

        var options = CreateInMemoryOptions(nameof(GetPendingRoleApprovalsAsync_WithNullValuesInSearchFields_DoesNotCrash));
        using (var context = new RefContext(options))
        {
            context.AccountEntity.AddRange(accounts);
            context.ContactEntity.Add(contact);
            context.ContactEntity.AddRange(primaryContacts);
            context.RoleEntity.AddRange(roles);
            context.RefRoleEntity.AddRange(refRoles);
            context.RegOperationEntity.AddRange(operations);
            await context.SaveChangesAsync();

            await PopulatePendingOperationsFromViewAsync(context);

            // Add a PendingOperationEntity with empty ContactEmail to test empty string handling
            context.PendingOperationEntity.Add(new PendingOperationEntity
            {
                Id = 100,
                AccountNumber = "ACC4",
                LegalName = "Account4",
                ContactEmail = string.Empty, // Empty ContactEmail
                FirstName = "Test",
                LastName = "User",
                CurrentContactId = contact.ContactId,
                CreationDate = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var repository = CreateRepository(context);

            // Act - Search should not crash even with empty ContactEmail in data
            var search = "ACC";
            var response = await repository.GetPendingRoleApprovalsAsync(contact.ContactId, 0, 10, search);

            // Assert
            response.PendingRoleApprovals.Should().NotBeNull();
            response.PendingRoleApprovals.Count().Should().Be(3); // ACC2, ACC3, ACC4
            context.Database.EnsureDeleted();
        }
    }

    [Fact]
    public async Task GetPendingRoleApprovalsAsync_WithSearchByContactEmail_ReturnsFilteredResults()
    {
        // Arrange
        var accounts = new List<AccountEntity>
            {
                new AccountEntity
                {
                    AccountId = 1,
                    AccountNumber = "ACC1",
                    LegalName = "Account1",
                    AccountGlobalUniqueId = Guid.NewGuid()
                },
                new AccountEntity
                {
                    AccountId = 2,
                    AccountNumber = "ACC2",
                    LegalName = "Account2",
                    AccountGlobalUniqueId = Guid.NewGuid()
                }
            };

        var contact = new ContactEntity
        {
            ContactId = 1,
            FirstName = "Test",
            LastName = "ContactCollan",
            Email = "ContactCollan@email.fr",
            ContactGlobalUniqueId = Guid.NewGuid(),
            Type = "COLLABORATOR",
        };

        var roles = new List<RoleEntity>
            {
                new RoleEntity
                {
                    AccountId = accounts.First().AccountId,
                    ContactId = contact.ContactId,
                    ContactGlobalUniqueId = contact.ContactGlobalUniqueId.Value,
                    AccountGlobalUniqueId = accounts.First().AccountGlobalUniqueId.Value,
                    AccountNumber = accounts.First().AccountNumber,
                    ContactEmail = contact.Email,
                    RoleDuplicatesCounter = 0,
                },
                new RoleEntity
                {
                    AccountId = accounts.Last().AccountId,
                    ContactId = contact.ContactId,
                    ContactGlobalUniqueId = contact.ContactGlobalUniqueId.Value,
                    AccountGlobalUniqueId = accounts.Last().AccountGlobalUniqueId.Value,
                    AccountNumber = accounts.Last().AccountNumber,
                    ContactEmail = contact.Email,
                    RoleDuplicatesCounter = 0,
                }
            };

        var refRoles = new List<RefRoleEntity>
            {
                new RefRoleEntity
                {
                    EntityId = Guid.NewGuid(),
                    AccountNumber = roles.First().AccountNumber,
                    ContactEmail = "specific-user@domain.com",
                    OperationType = OperationAction.Insert
                },
                new RefRoleEntity
                {
                    EntityId = Guid.NewGuid(),
                    AccountNumber = roles.First().AccountNumber,
                    ContactEmail = "other-user@email.com",
                    OperationType = OperationAction.Insert
                },
                new RefRoleEntity
                {
                    EntityId = Guid.NewGuid(),
                    AccountNumber = roles.Last().AccountNumber,
                    ContactEmail = "another@email.com",
                    OperationType = OperationAction.Insert
                }
            };

        var primaryContacts = new List<ContactEntity>
            {
                new ContactEntity
                {
                    ContactId = 2,
                    Email = refRoles[0].ContactEmail,
                    FirstName = "Specific",
                    LastName = "User",
                    Type = "CUSTOMER",
                },
                new ContactEntity
                {
                    ContactId = 3,
                    Email = refRoles[1].ContactEmail,
                    FirstName = "Other",
                    LastName = "User",
                    Type = "CUSTOMER",
                },
                new ContactEntity
                {
                    ContactId = 4,
                    Email = refRoles[2].ContactEmail,
                    FirstName = "Another",
                    LastName = "User",
                    Type = "CUSTOMER"
                }
            };

        var operations = new List<RegOperationEntity>
            {
                new RegOperationEntity
                {
                    Id = 1,
                    EntityId = refRoles[0].EntityId,
                    Operation = OperationAction.Insert,
                    Type = OperationCategory.ROLE,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreationDate = DateTime.UtcNow.AddDays(-2),
                },
                new RegOperationEntity
                {
                    Id = 2,
                    EntityId = refRoles[1].EntityId,
                    Operation = OperationAction.Insert,
                    Type = OperationCategory.ROLE,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreationDate = DateTime.UtcNow,
                },
                new RegOperationEntity
                {
                    Id = 3,
                    EntityId = refRoles[2].EntityId,
                    Operation = OperationAction.Insert,
                    Type = OperationCategory.ROLE,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreationDate = DateTime.UtcNow.AddDays(-1),
                },
            };

        var options = CreateInMemoryOptions(nameof(GetPendingRoleApprovalsAsync_WithSearchByContactEmail_ReturnsFilteredResults));
        using (var context = new RefContext(options))
        {
            context.AccountEntity.AddRange(accounts);
            context.ContactEntity.Add(contact);
            context.ContactEntity.AddRange(primaryContacts);
            context.RoleEntity.AddRange(roles);
            context.RefRoleEntity.AddRange(refRoles);
            context.RegOperationEntity.AddRange(operations);
            await context.SaveChangesAsync();

            await PopulatePendingOperationsFromViewAsync(context);

            var repository = CreateRepository(context);

            // Act - Search by email
            var search = "specific-user@domain";
            var response = await repository.GetPendingRoleApprovalsAsync(contact.ContactId, 0, 10, search);

            // Assert
            response.PendingRoleApprovals.Count().Should().Be(1);
            response.PendingRoleApprovals.First().AccountNumber.Should().Be("ACC1");
            response.PendingRoleApprovals.First().Operations.Should().HaveCount(1);
            response.PendingRoleApprovals.First().Operations.First().Email.Should().Be("specific-user@domain.com");
            context.Database.EnsureDeleted();
        }
    }
    #endregion

    #region GetInsertRoleOperationDuplicates Tests

    [Fact]
    public async Task GetInsertRoleOperationDuplicates_NoRefRole_ReturnsEmpty()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GetInsertRoleOperationDuplicates_NoRefRole_ReturnsEmpty));
        var operationId = Guid.NewGuid();
        using (var context = new RefContext(options))
        {
            // no RefRoleEntity seeded for operationId
            await context.SaveChangesAsync();
        }

        // Act
        using (var context = new RefContext(options))
        {
            var repo = CreateRepository(context);
            var approvedOperation = new RegOperation { EntityId = operationId };
            var result = await repo.GetInsertRoleOperationDuplicates(approvedOperation);

            // Assert
            result.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task GetInsertRoleOperationDuplicates_NoDuplicates_ReturnsEmpty()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GetInsertRoleOperationDuplicates_NoDuplicates_ReturnsEmpty));
        var mainId = Guid.NewGuid();
        using (var context = new RefContext(options))
        {
            // seed exactly one RefRoleEntity
            context.RefRoleEntity.Add(new RefRoleEntity
            {
                EntityId = mainId,
                AccountNumber = "ACC1",
                ContactEmail = "user@domain.com",
                OperationType = OperationAction.Insert
            });
            await context.SaveChangesAsync();
        }

        // Act
        using (var context = new RefContext(options))
        {
            var repo = CreateRepository(context);
            var approvedOperation = new RegOperation { EntityId = mainId };
            var result = await repo.GetInsertRoleOperationDuplicates(approvedOperation);

            // Assert
            result.Should().BeEmpty();  // no other RefRoleEntity → no duplicates
        }
    }

    [Fact]
    public async Task GetInsertRoleOperationDuplicates_DuplicatesNonPending_ReturnsEmpty()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GetInsertRoleOperationDuplicates_DuplicatesNonPending_ReturnsEmpty));
        var mainId = Guid.NewGuid();
        var dupId = Guid.NewGuid();
        using (var context = new RefContext(options))
        {
            // seed approved RefRoleEntity
            context.RefRoleEntity.Add(new RefRoleEntity
            {
                EntityId = mainId,
                AccountNumber = "ACC1",
                ContactEmail = "user@domain.com",
                OperationType = OperationAction.Insert
            });
            // seed a duplicate RefRoleEntity
            context.RefRoleEntity.Add(new RefRoleEntity
            {
                EntityId = dupId,
                AccountNumber = "ACC1",
                ContactEmail = "user@domain.com",
                OperationType = OperationAction.Insert
            });
            // but seed its RegOperationEntity with non‐Pending status
            context.RegOperationEntity.Add(new RegOperationEntity
            {
                Id = 1,
                EntityId = dupId,
                ApprovalStatus = ApprovalStatus.Approved,
                Operation = OperationAction.Insert,
                CreationDate = DateTime.UtcNow,
                Type = OperationCategory.ROLE
            });
            await context.SaveChangesAsync();
        }

        // Act
        using (var context = new RefContext(options))
        {
            var repo = CreateRepository(context);
            var approvedOperation = new RegOperation { EntityId = mainId };
            var result = await repo.GetInsertRoleOperationDuplicates(approvedOperation);

            // Assert
            result.Should().BeEmpty();  // duplicate exists but not Pending → filtered out
        }
    }

    [Fact]
    public async Task GetInsertRoleOperationDuplicates_DuplicatesPending_ReturnsList()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(GetInsertRoleOperationDuplicates_DuplicatesPending_ReturnsList));
        var mainId = Guid.NewGuid();
        var dupId = Guid.NewGuid();
        using (var context = new RefContext(options))
        {
            // seed approved RefRoleEntity
            context.RefRoleEntity.Add(new RefRoleEntity
            {
                EntityId = mainId,
                AccountNumber = "ACC1",
                ContactEmail = "user@domain.com",
                OperationType = OperationAction.Insert
            });
            // seed a duplicate RefRoleEntity
            context.RefRoleEntity.Add(new RefRoleEntity
            {
                EntityId = dupId,
                AccountNumber = "ACC1",
                ContactEmail = "user@domain.com",
                OperationType = OperationAction.Insert
            });
            // seed its RegOperationEntity with Pending status
            context.RegOperationEntity.Add(new RegOperationEntity
            {
                Id = 2,
                EntityId = dupId,
                ApprovalStatus = ApprovalStatus.Pending,
                Operation = OperationAction.Insert,
                CreationDate = DateTime.UtcNow,
                Type = OperationCategory.ROLE
            });
            await context.SaveChangesAsync();
        }

        // Act
        using (var context = new RefContext(options))
        {
            var repo = CreateRepository(context);
            var approvedOperation = new RegOperation { EntityId = mainId };
            var result = await repo.GetInsertRoleOperationDuplicates(approvedOperation);

            // Assert
            result.Should().HaveCount(1);
            result.First().EntityId.Should().Be(dupId);
        }
    }

    #endregion

    private async Task PopulatePendingOperationsFromViewAsync(RefContext context)
    {
        var primaryQuery =
            from rop in context.RegOperationEntity
            join refro in context.RefRoleEntity on rop.EntityId equals refro.EntityId
            join aro in context.RoleEntity on refro.AccountNumber equals aro.AccountNumber
            join acc in context.AccountEntity on aro.AccountId equals acc.AccountId
            join refcnt in context.ContactEntity on refro.ContactEmail equals refcnt.Email
            where rop.ApprovalStatus == "PENDING"
                && rop.PublishedAt == null
                && rop.Type == "ROLE"
                && rop.Operation == "INSERT"
            select new PendingOperationEntity
            {
                AccountNumber = acc.AccountNumber,
                LegalName = acc.LegalName ?? "Default Legal Name",
                Id = rop.Id,
                CreationDate = rop.CreationDate,
                ContactEmail = refro.ContactEmail,
                FirstName = refcnt.FirstName ?? "Default First Name",
                LastName = refcnt.LastName ?? "Default Last Name",
                CurrentContactId = aro.ContactId
            };

        var fallbackQuery =
            from rop in context.RegOperationEntity
            join refro in context.RefRoleEntity on rop.EntityId equals refro.EntityId
            join aro in context.RoleEntity on refro.AccountNumber equals aro.AccountNumber
            join acc in context.AccountEntity on aro.AccountId equals acc.AccountId
            join cnt in context.RefContactEntity on refro.ContactEmail equals cnt.Email
            where rop.ApprovalStatus == "PENDING"
                && rop.PublishedAt == null
                && rop.Type == "ROLE"
                && rop.Operation == "INSERT"
            select new PendingOperationEntity
            {
                AccountNumber = acc.AccountNumber,
                LegalName = acc.LegalName ?? "Default Legal Name",
                Id = rop.Id,
                CreationDate = rop.CreationDate,
                ContactEmail = refro.ContactEmail,
                FirstName = cnt.FirstName ?? "Default First Name",
                LastName = cnt.LastName ?? "Default Last Name",
                CurrentContactId = aro.ContactId
            };

        // Union and insert
        var pendingOps = primaryQuery.Union(fallbackQuery).ToList();
        await context.PendingOperationEntity.AddRangeAsync(pendingOps);
        await context.SaveChangesAsync();
    }
}

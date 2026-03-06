using Application.Consts;
using Application.Enums;
using Application.Interfaces;
using Application.Requests;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Pulse.Registry.Domain.Entities.Accounts;
using Pulse.Registry.Domain.Entities.Audits;
using Registry.Application.Consts;

namespace Registry.Infrastructure.Tests.Repository
{
    public class AccountRepositoryTests
    {
        private DbContextOptions<RefContext> CreateInMemoryOptions(string databaseName)
        {
            return new DbContextOptionsBuilder<RefContext>()
                .UseInMemoryDatabase($"{databaseName}_{Guid.NewGuid()}")
                .Options;
        }

        private static AccountRepository CreateRepository(RefContext context, IOperationRepository operationRepository, IDeepValidationRepository deepValidationRepository, ILogger<AccountRepository> logger)
        {
            return new AccountRepository(context, operationRepository, deepValidationRepository, logger);
        }

        [Fact]
        public async Task AddAccountAsync_Should_Add_Account()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(AddAccountAsync_Should_Add_Account));
            using var context = new RefContext(options);
            var operationRepoMock = new Mock<IOperationRepository>();
            var deepValidationRepoMock = new Mock<IDeepValidationRepository>();
            var loggerAccountRepositoryMock = new Mock<ILogger<AccountRepository>>();
            var repository = CreateRepository(context, operationRepoMock.Object, deepValidationRepoMock.Object, loggerAccountRepositoryMock.Object);
            var account = new AccountEntity { AccountId = 11, AccountNumber = "12345", LegalName = "legal" };

            // Act
            await repository.AddAccountAsync(account);
            var result = await context.AccountEntity.FindAsync(account.AccountId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("12345", result.AccountNumber);
        }

        [Fact]
        public async Task GetAccountByNumberOrIdAsync_Using_AccountNumber_Should_Return_Account()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(GetAccountByNumberOrIdAsync_Using_AccountNumber_Should_Return_Account));
            using var context = new RefContext(options);
            var operationRepoMock = new Mock<IOperationRepository>();
            var deepValidationRepoMock = new Mock<IDeepValidationRepository>();
            var loggerAccountRepositoryMock = new Mock<ILogger<AccountRepository>>();
            var repository = CreateRepository(context, operationRepoMock.Object, deepValidationRepoMock.Object, loggerAccountRepositoryMock.Object);
            var account = new AccountEntity { AccountId = 2, AccountNumber = "MyAccount", LegalName = "legal" };
            context.AccountEntity.Add(account);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAccountByNumberOrIdAsync("MyAccount");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MyAccount", result.AccountNumber);
        }

        [Fact]
        public async Task GetAccountByNumberOrIdAsync_Using_AccountId_Should_Return_Account()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(GetAccountByNumberOrIdAsync_Using_AccountId_Should_Return_Account));
            using var context = new RefContext(options);
            var operationRepoMock = new Mock<IOperationRepository>();
            var deepValidationRepoMock = new Mock<IDeepValidationRepository>();
            var loggerAccountRepositoryMock = new Mock<ILogger<AccountRepository>>();
            var repository = CreateRepository(context, operationRepoMock.Object, deepValidationRepoMock.Object, loggerAccountRepositoryMock.Object);
            var account = new AccountEntity { AccountId = 3, AccountNumber = "1000244200", LegalName = "legal" };
            context.AccountEntity.Add(account);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAccountByNumberOrIdAsync("1000244200");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("1000244200", result.AccountNumber);
        }

        [Fact]
        public async Task UpdateAccountAsync_Should_Update_Account()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(UpdateAccountAsync_Should_Update_Account));
            using var context = new RefContext(options);
            var operationRepoMock = new Mock<IOperationRepository>();
            var deepValidationRepoMock = new Mock<IDeepValidationRepository>();
            var loggerAccountRepositoryMock = new Mock<ILogger<AccountRepository>>();
            var repository = CreateRepository(context, operationRepoMock.Object, deepValidationRepoMock.Object, loggerAccountRepositoryMock.Object);
            var account = new AccountEntity { AccountId = 4, AccountNumber = "12345", AccountGlobalUniqueId = Guid.NewGuid(), LegalName = "legal" };
            context.AccountEntity.Add(account);
            await context.SaveChangesAsync();

            // Act
            var newGuid = Guid.NewGuid();
            account.AccountGlobalUniqueId = newGuid;
            await repository.UpdateAccountAsync(account);

            // Assert
            var updatedAccount = await context.AccountEntity.FindAsync(account.AccountId);
            Assert.NotNull(updatedAccount);
            Assert.Equal(newGuid, updatedAccount.AccountGlobalUniqueId);
        }

        [Fact]
        public async Task RemoveAccountAsync_Should_Remove_Account()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(RemoveAccountAsync_Should_Remove_Account));
            using var context = new RefContext(options);
            var operationRepoMock = new Mock<IOperationRepository>();
            var deepValidationRepoMock = new Mock<IDeepValidationRepository>();
            var loggerAccountRepositoryMock = new Mock<ILogger<AccountRepository>>();
            var repository = CreateRepository(context, operationRepoMock.Object, deepValidationRepoMock.Object, loggerAccountRepositoryMock.Object);
            var account = new AccountEntity { AccountId = 5, AccountNumber = "12345", LegalName = "legal" };
            context.AccountEntity.Add(account);
            await context.SaveChangesAsync();

            // Act
            await repository.RemoveAccountAsync(account);

            // Assert
            var updatedAccount = await context.AccountEntity.FindAsync(account.AccountId);
            Assert.Null(updatedAccount);
        }

        // ------------------ Deep Validation Tests ------------------
       
        [Fact]
        public async Task ValidateAccountOperation_InsertAccount_DoesNotExist_ShouldCreateOperation()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ValidateAccountOperation_InsertAccount_DoesNotExist_ShouldCreateOperation));
            using var context = new RefContext(options);
            var guid = Guid.NewGuid();
            var operationRepoMock = new Mock<IOperationRepository>();
            var deepValidationRepoMock = new Mock<IDeepValidationRepository>();
            var loggerAccountRepositoryMock = new Mock<ILogger<AccountRepository>>();
            var repository = CreateRepository(context, operationRepoMock.Object, deepValidationRepoMock.Object, loggerAccountRepositoryMock.Object);

            var refAccount = new RefAccountEntity
            {
                EntityId = guid,
                AccountNumber = "accountNumber_Insert_DoesNotExist",
                OperationType = OperationAction.Insert,
                OperationDate = DateTime.UtcNow,
                ValidationDate = null
            };
            context.RefAccountEntity.Add(refAccount);
            await context.SaveChangesAsync();

            operationRepoMock.Setup(op => op.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.ACCOUNT,
                refAccount.AccountNumber,
                It.IsAny<bool?>(),
                null))
                .ReturnsAsync(new List<RegOperationEntity>())
                .Verifiable();

            operationRepoMock.Setup(op => op.CreateOperationAsync(It.IsAny<RegOperationEntity>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await repository.ValidateAccountOperation();

            // Assert
            operationRepoMock.Verify(op => op.CreateOperationAsync(It.IsAny<RegOperationEntity>()), Times.Once);
            deepValidationRepoMock.Verify(x => x.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()), Times.Never);
        }

        [Fact]
        public async Task ValidateAccountOperation_UpdateAccount_DoesNotExist_ShouldInsertAudit()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ValidateAccountOperation_UpdateAccount_DoesNotExist_ShouldInsertAudit));
            using var context = new RefContext(options);
            var guid = Guid.NewGuid();
            var operationRepoMock = new Mock<IOperationRepository>();
            var deepValidationRepoMock = new Mock<IDeepValidationRepository>();
            var loggerAccountRepositoryMock = new Mock<ILogger<AccountRepository>>();
            var repository = CreateRepository(context, operationRepoMock.Object, deepValidationRepoMock.Object, loggerAccountRepositoryMock.Object);

            var refAccount = new RefAccountEntity
            {
                EntityId = guid,
                AccountNumber = "accountNumber5",
                OperationType = OperationAction.Update,
                OperationDate = DateTime.UtcNow,
                ValidationDate = null
            };
            context.RefAccountEntity.Add(refAccount);
            await context.SaveChangesAsync();

            var expectedReason = $"Operation of Type : {refAccount.OperationType} with this Account Number {refAccount.AccountNumber} account does not exists";

            operationRepoMock.Setup(op => op.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.ACCOUNT,
                refAccount.AccountNumber,
                It.IsAny<bool?>(),
                null))
                .ReturnsAsync(new List<RegOperationEntity>())
                .Verifiable();

            deepValidationRepoMock.Setup(d => d.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()))
                .ReturnsAsync(true)
                .Verifiable();

            // Act
            await repository.ValidateAccountOperation();

            // Assert
            deepValidationRepoMock.Verify(x => x.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()), Times.Once);
        }

        [Fact]
        public async Task ValidateAccountOperation_UpdateAccount_DoesExist_ShouldCreateOperation()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ValidateAccountOperation_UpdateAccount_DoesExist_ShouldCreateOperation));
            using var context = new RefContext(options);
            var guid = Guid.NewGuid();
            var operationRepoMock = new Mock<IOperationRepository>();
            var deepValidationRepoMock = new Mock<IDeepValidationRepository>();
            var loggerAccountRepositoryMock = new Mock<ILogger<AccountRepository>>();
            var repository = CreateRepository(context, operationRepoMock.Object, deepValidationRepoMock.Object, loggerAccountRepositoryMock.Object);

            var refAccount = new RefAccountEntity
            {
                EntityId = guid,
                AccountNumber = "accountNumber6",
                OperationType = OperationAction.Update,
                OperationDate = DateTime.UtcNow,
                ValidationDate = null
            };
            context.RefAccountEntity.Add(refAccount);
            var account = new AccountEntity
            {
                AccountId = 10,
                AccountNumber = "accountNumber6",
                AccountGlobalUniqueId = guid,
                LegalName = "legal",
            };
            context.AccountEntity.Add(account);
            var operationInsert = new RegOperationEntity
            {
                Id = 100,
                EntityId = guid,
                Operation = OperationAction.Insert,
                ApprovalStatus = ApprovalStatus.Approved,
                CreationDate = DateTime.UtcNow,
                ProcessStatus = ProcessStatus.Ready,
                Type = OperationCategory.ACCOUNT
            };
            context.RegOperationEntity.Add(operationInsert);
            await context.SaveChangesAsync();

            operationRepoMock.Setup(op => op.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.ACCOUNT,
                refAccount.AccountNumber,
                It.IsAny<bool?>(),
                null))
                .ReturnsAsync(new List<RegOperationEntity> { operationInsert })
                .Verifiable();

            operationRepoMock.Setup(op => op.CreateOperationAsync(It.IsAny<RegOperationEntity>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await repository.ValidateAccountOperation();

            // Assert
            operationRepoMock.Verify(op => op.CreateOperationAsync(It.IsAny<RegOperationEntity>()), Times.Once);
            deepValidationRepoMock.Verify(x => x.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()), Times.Never);
        }

        [Fact]
        public async Task ValidateAccountOperation_DeleteAccount_DoesExist_WithRelatedRoles_ShouldResetDuplicatesAndCreateOperations()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ValidateAccountOperation_DeleteAccount_DoesExist_WithRelatedRoles_ShouldResetDuplicatesAndCreateOperations));
            using var context = new RefContext(options);
            var guid = Guid.NewGuid();
            var operationRepoMock = new Mock<IOperationRepository>();
            var deepValidationRepoMock = new Mock<IDeepValidationRepository>();
            var loggerAccountRepositoryMock = new Mock<ILogger<AccountRepository>>();
            var repository = CreateRepository(context, operationRepoMock.Object, deepValidationRepoMock.Object, loggerAccountRepositoryMock.Object);

            var refAccount = new RefAccountEntity
            {
                EntityId = guid,
                AccountNumber = "accountNumber7",
                OperationType = OperationAction.Delete,
                OperationDate = DateTime.UtcNow,
                ValidationDate = null
            };
            context.RefAccountEntity.Add(refAccount);
            var account = new AccountEntity
            {
                AccountId = 111,
                AccountNumber = "accountNumber7",
                AccountGlobalUniqueId = guid,
                LegalName = "legal",
            };
            context.AccountEntity.Add(account);
            var role1 = new RoleEntity
            {
                AccountId = 111,
                ContactId = 101,
                AccountGlobalUniqueId = guid,
                RoleDuplicatesCounter = 2,
                AccountNumber = "accountNumber7"
            };
            var role2 = new RoleEntity
            {
                AccountId = 111,
                ContactId = 102,
                AccountGlobalUniqueId = guid,
                RoleDuplicatesCounter = 3,
                AccountNumber = "accountNumber7"
            };
            context.RoleEntity.AddRange(role1, role2);
            await context.SaveChangesAsync();

            var insertOp = new RegOperationEntity
            {
                EntityId = guid,
                ApprovalStatus = ApprovalStatus.Approved,
                ProcessStatus = ProcessStatus.Ready,
                Operation = OperationAction.Insert
            };

            // DoesCreateOperationExist runs first (unconditionally) → return empty so Delete is NOT redirected to Update
            // CreateDeleteAccountOperationAsync then calls FetchOperationsByCriteriaAsync → return Insert op so else branch runs
            operationRepoMock.SetupSequence(op => op.FetchOperationsByCriteriaAsync(
                    It.IsAny<OperationSearchCriteria>(),
                    OperationStrategyType.ACCOUNT,
                    refAccount.AccountNumber,
                    It.IsAny<bool?>(),
                    null))
                .ReturnsAsync(new List<RegOperationEntity>())               // DoesCreateOperationExist → no redirect
                .ReturnsAsync(new List<RegOperationEntity> { insertOp });   // CreateDeleteAccountOperationAsync → proceed

            int createOperationCallCount = 0;
            operationRepoMock.Setup(op => op.CreateOperationAsync(It.IsAny<RegOperationEntity>()))
                .Callback<RegOperationEntity>(_ => createOperationCallCount++)
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await repository.ValidateAccountOperation();

            // Assert — 2 role delete ops + 1 account delete op = 3 total
            Assert.Equal(3, createOperationCallCount);
            var roles = context.RoleEntity.Where(r => r.AccountGlobalUniqueId == guid).ToList();
            Assert.All(roles, r => Assert.Equal(0, r.RoleDuplicatesCounter));
        }

        [Fact]
        public async Task ValidateAccountOperation_DeleteAccount_DoesExist_WithNoRelatedRoles_ShouldCreateOperation()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ValidateAccountOperation_DeleteAccount_DoesExist_WithNoRelatedRoles_ShouldCreateOperation));
            using var context = new RefContext(options);
            var guid = Guid.NewGuid();
            var operationRepoMock = new Mock<IOperationRepository>();
            var deepValidationRepoMock = new Mock<IDeepValidationRepository>();
            var loggerAccountRepositoryMock = new Mock<ILogger<AccountRepository>>();
            var repository = CreateRepository(context, operationRepoMock.Object, deepValidationRepoMock.Object, loggerAccountRepositoryMock.Object);

            var refAccount = new RefAccountEntity
            {
                EntityId = guid,
                AccountNumber = "accountNumber8",
                OperationType = OperationAction.Delete,
                OperationDate = DateTime.UtcNow,
                ValidationDate = null
            };
            context.RefAccountEntity.Add(refAccount);
            var account = new AccountEntity
            {
                AccountId = 222,
                AccountNumber = "accountNumber8",
                AccountGlobalUniqueId = guid,
                LegalName = "legal",
            };
            context.AccountEntity.Add(account);
            await context.SaveChangesAsync();

            operationRepoMock.Setup(op => op.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.ACCOUNT,
                refAccount.AccountNumber,
                It.IsAny<bool?>(),
                null))
                .ReturnsAsync(new List<RegOperationEntity>
                {
                    new RegOperationEntity
                    {
                        EntityId = guid,
                        ApprovalStatus = ApprovalStatus.Approved,
                        ProcessStatus = ProcessStatus.Ready,
                        Operation = OperationAction.Insert
                    }
                })
                .Verifiable();

            operationRepoMock.Setup(op => op.CreateOperationAsync(It.IsAny<RegOperationEntity>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await repository.ValidateAccountOperation();

            // Assert
            operationRepoMock.Verify(op => op.CreateOperationAsync(It.IsAny<RegOperationEntity>()), Times.Once);
        }
        
        [Fact]
        public async Task ValidateAccountOperation_DeleteAccount_DoesNotExist_ShouldCreateAudit()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ValidateAccountOperation_DeleteAccount_DoesNotExist_ShouldCreateAudit));
            using var context = new RefContext(options);
            var guid = Guid.NewGuid();
            var operationRepoMock = new Mock<IOperationRepository>();
            var deepValidationRepoMock = new Mock<IDeepValidationRepository>();
            var loggerAccountRepositoryMock = new Mock<ILogger<AccountRepository>>();
            var repository = CreateRepository(context, operationRepoMock.Object, deepValidationRepoMock.Object, loggerAccountRepositoryMock.Object);

            var refAccount = new RefAccountEntity
            {
                EntityId = guid,
                AccountNumber = "accountNumber_Delete",
                OperationType = OperationAction.Delete,
                OperationDate = DateTime.UtcNow,
                ValidationDate = null
            };
            context.RefAccountEntity.Add(refAccount);
            await context.SaveChangesAsync();

            var expectedReason = $"Operation of Type : {refAccount.OperationType} with this Account Number {refAccount.AccountNumber} account does not exists";

            operationRepoMock.Setup(op => op.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.ACCOUNT,
                refAccount.AccountNumber,
                It.IsAny<bool?>(),
                null))
                .ReturnsAsync(new List<RegOperationEntity>())
                .Verifiable();

            deepValidationRepoMock.Setup(d => d.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()))
                .ReturnsAsync(true)
                .Verifiable();

            // Act
            await repository.ValidateAccountOperation();

            // Assert
            deepValidationRepoMock.Verify(x => x.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()), Times.AtLeastOnce);
            operationRepoMock.Verify(op => op.CreateOperationAsync(It.IsAny<RegOperationEntity>()), Times.Never);
        }

        [Fact]
        public async Task DoesAccountExist_Should_Return_True_When_Account_Exists()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(DoesAccountExist_Should_Return_True_When_Account_Exists));
            using var context = new RefContext(options);
            var operationRepoMock = new Mock<IOperationRepository>();
            var deepValidationRepoMock = new Mock<IDeepValidationRepository>();
            var loggerAccountRepositoryMock = new Mock<ILogger<AccountRepository>>();
            var repository = CreateRepository(context, operationRepoMock.Object, deepValidationRepoMock.Object, loggerAccountRepositoryMock.Object);

            var account = new AccountEntity { AccountId = 6, AccountNumber = "EXISTS123", LegalName = "legal" };
            context.AccountEntity.Add(account);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.DoesAccountExist("EXISTS123");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DoesAccountExist_Should_Return_False_When_Account_Does_Not_Exist()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(DoesAccountExist_Should_Return_False_When_Account_Does_Not_Exist));
            using var context = new RefContext(options);
            var operationRepoMock = new Mock<IOperationRepository>();
            var deepValidationRepoMock = new Mock<IDeepValidationRepository>();
            var loggerAccountRepositoryMock = new Mock<ILogger<AccountRepository>>();
            var repository = CreateRepository(context, operationRepoMock.Object, deepValidationRepoMock.Object, loggerAccountRepositoryMock.Object);

            // Act
            var result = await repository.DoesAccountExist("NOTEXIST999");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetAccountByNumberOrIdAsync_Should_Return_Null_When_Not_Found()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(GetAccountByNumberOrIdAsync_Should_Return_Null_When_Not_Found));
            using var context = new RefContext(options);
            var operationRepoMock = new Mock<IOperationRepository>();
            var deepValidationRepoMock = new Mock<IDeepValidationRepository>();
            var loggerAccountRepositoryMock = new Mock<ILogger<AccountRepository>>();
            var repository = CreateRepository(context, operationRepoMock.Object, deepValidationRepoMock.Object, loggerAccountRepositoryMock.Object);

            // Act
            var result = await repository.GetAccountByNumberOrIdAsync("NONEXISTENT");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ValidateAccountOperation_Should_Skip_Already_Validated_Accounts()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ValidateAccountOperation_Should_Skip_Already_Validated_Accounts));
            using var context = new RefContext(options);
            var guid = Guid.NewGuid();
            var operationRepoMock = new Mock<IOperationRepository>();
            var deepValidationRepoMock = new Mock<IDeepValidationRepository>();
            var loggerAccountRepositoryMock = new Mock<ILogger<AccountRepository>>();
            var repository = CreateRepository(context, operationRepoMock.Object, deepValidationRepoMock.Object, loggerAccountRepositoryMock.Object);

            var refAccount = new RefAccountEntity
            {
                EntityId = guid,
                AccountNumber = "accountNumberValidated",
                OperationType = OperationAction.Insert,
                OperationDate = DateTime.UtcNow,
                ValidationDate = DateTime.UtcNow // Already validated
            };
            context.RefAccountEntity.Add(refAccount);
            await context.SaveChangesAsync();

            // Act
            await repository.ValidateAccountOperation();

            // Assert
            operationRepoMock.Verify(op => op.CreateOperationAsync(It.IsAny<RegOperationEntity>()), Times.Never);
            deepValidationRepoMock.Verify(x => x.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()), Times.Never);
        }

        [Fact]
        public async Task ValidateAccountOperation_ShouldSetValidationDate_OnProcessedRefAccount()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ValidateAccountOperation_ShouldSetValidationDate_OnProcessedRefAccount));
            using var context = new RefContext(options);
            var guid = Guid.NewGuid();
            var operationRepoMock = new Mock<IOperationRepository>();
            var deepValidationRepoMock = new Mock<IDeepValidationRepository>();
            var loggerAccountRepositoryMock = new Mock<ILogger<AccountRepository>>();
            var repository = CreateRepository(context, operationRepoMock.Object, deepValidationRepoMock.Object, loggerAccountRepositoryMock.Object);

            var refAccount = new RefAccountEntity
            {
                EntityId = guid,
                AccountNumber = "accountNumberValidationDateCheck",
                OperationType = OperationAction.Insert,
                OperationDate = DateTime.UtcNow,
                ValidationDate = null
            };
            context.RefAccountEntity.Add(refAccount);
            await context.SaveChangesAsync();

            operationRepoMock.Setup(op => op.FetchOperationsByCriteriaAsync(
                    It.IsAny<OperationSearchCriteria>(),
                    OperationStrategyType.ACCOUNT,
                    refAccount.AccountNumber,
                    It.IsAny<bool?>(),
                    null))
                .ReturnsAsync(new List<RegOperationEntity>());

            operationRepoMock.Setup(op => op.CreateOperationAsync(It.IsAny<RegOperationEntity>()))
                .Returns(Task.CompletedTask);

            // Act
            await repository.ValidateAccountOperation();

            // Assert
            var updatedRefAccount = await context.RefAccountEntity.FirstOrDefaultAsync(r => r.EntityId == guid);
            Assert.NotNull(updatedRefAccount);
            Assert.NotNull(updatedRefAccount.ValidationDate);
        }

        [Fact]
        public async Task ValidateAccountOperation_DeleteAccount_DoesExist_WithRolesHavingZeroDuplicates_ShouldNotUpdateRoles()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ValidateAccountOperation_DeleteAccount_DoesExist_WithRolesHavingZeroDuplicates_ShouldNotUpdateRoles));
            using var context = new RefContext(options);
            var guid = Guid.NewGuid();
            var operationRepoMock = new Mock<IOperationRepository>();
            var deepValidationRepoMock = new Mock<IDeepValidationRepository>();
            var loggerAccountRepositoryMock = new Mock<ILogger<AccountRepository>>();
            var repository = CreateRepository(context, operationRepoMock.Object, deepValidationRepoMock.Object, loggerAccountRepositoryMock.Object);

            var refAccount = new RefAccountEntity
            {
                EntityId = guid,
                AccountNumber = "accountNumberDeleteZeroDuplicates",
                OperationType = OperationAction.Delete,
                OperationDate = DateTime.UtcNow,
                ValidationDate = null
            };
            context.RefAccountEntity.Add(refAccount);
            var account = new AccountEntity
            {
                AccountId = 333,
                AccountNumber = "accountNumberDeleteZeroDuplicates",
                AccountGlobalUniqueId = guid,
                LegalName = "legal"
            };
            context.AccountEntity.Add(account);
            var role = new RoleEntity
            {
                AccountId = 333,
                ContactId = 201,
                AccountGlobalUniqueId = guid,
                RoleDuplicatesCounter = 0,
                AccountNumber = "accountNumberDeleteZeroDuplicates"
            };
            context.RoleEntity.Add(role);
            await context.SaveChangesAsync();

            // DoesCreateOperationExist runs first (unconditionally) — return empty so Delete is NOT redirected to Update
            // Then CreateDeleteAccountOperationAsync calls FetchOperationsByCriteriaAsync — return an Insert op so the else branch runs
            operationRepoMock.SetupSequence(op => op.FetchOperationsByCriteriaAsync(
                    It.IsAny<OperationSearchCriteria>(),
                    OperationStrategyType.ACCOUNT,
                    refAccount.AccountNumber,
                    It.IsAny<bool?>(),
                    null))
                .ReturnsAsync(new List<RegOperationEntity>())                  // DoesCreateOperationExist → no redirect
                .ReturnsAsync(new List<RegOperationEntity>                     // CreateDeleteAccountOperationAsync → proceed
                {
            new RegOperationEntity
            {
                EntityId = guid,
                ApprovalStatus = ApprovalStatus.Approved,
                ProcessStatus = ProcessStatus.Ready,
                Operation = OperationAction.Insert
            }
                });

            int createOperationCallCount = 0;
            operationRepoMock.Setup(op => op.CreateOperationAsync(It.IsAny<RegOperationEntity>()))
                .Callback<RegOperationEntity>(_ => createOperationCallCount++)
                .Returns(Task.CompletedTask);

            // Act
            await repository.ValidateAccountOperation();

            // Assert — 1 role delete op + 1 account delete op = 2 total; role with 0 duplicates is not updated
            Assert.Equal(2, createOperationCallCount);
            var roleInDb = context.RoleEntity.FirstOrDefault(r => r.AccountGlobalUniqueId == guid);
            Assert.NotNull(roleInDb);
            Assert.Equal(0, roleInDb.RoleDuplicatesCounter);
        }

        [Fact]
        public async Task ValidateAccountOperation_MultipleUnvalidatedRecords_ShouldProcessAll()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ValidateAccountOperation_MultipleUnvalidatedRecords_ShouldProcessAll));
            using var context = new RefContext(options);
            var operationRepoMock = new Mock<IOperationRepository>();
            var deepValidationRepoMock = new Mock<IDeepValidationRepository>();
            var loggerAccountRepositoryMock = new Mock<ILogger<AccountRepository>>();
            var repository = CreateRepository(context, operationRepoMock.Object, deepValidationRepoMock.Object, loggerAccountRepositoryMock.Object);

            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();

            context.RefAccountEntity.AddRange(
                new RefAccountEntity
                {
                    EntityId = guid1,
                    AccountNumber = "multiAccount1",
                    OperationType = OperationAction.Insert,
                    OperationDate = DateTime.UtcNow,
                    ValidationDate = null
                },
                new RefAccountEntity
                {
                    EntityId = guid2,
                    AccountNumber = "multiAccount2",
                    OperationType = OperationAction.Insert,
                    OperationDate = DateTime.UtcNow,
                    ValidationDate = null
                }
            );
            await context.SaveChangesAsync();

            operationRepoMock.Setup(op => op.FetchOperationsByCriteriaAsync(
                    It.IsAny<OperationSearchCriteria>(),
                    OperationStrategyType.ACCOUNT,
                    It.IsAny<string>(),
                    It.IsAny<bool?>(),
                    null))
                .ReturnsAsync(new List<RegOperationEntity>());

            operationRepoMock.Setup(op => op.CreateOperationAsync(It.IsAny<RegOperationEntity>()))
                .Returns(Task.CompletedTask);

            // Act
            await repository.ValidateAccountOperation();

            // Assert
            operationRepoMock.Verify(op => op.CreateOperationAsync(It.IsAny<RegOperationEntity>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ValidateAccountOperation_InsertAccount_AccountAlreadyExistsInDb_ShouldRedirectToUpdate()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ValidateAccountOperation_InsertAccount_AccountAlreadyExistsInDb_ShouldRedirectToUpdate));
            using var context = new RefContext(options);
            var guid = Guid.NewGuid();
            var operationRepoMock = new Mock<IOperationRepository>();
            var deepValidationRepoMock = new Mock<IDeepValidationRepository>();
            var loggerAccountRepositoryMock = new Mock<ILogger<AccountRepository>>();
            var repository = CreateRepository(context, operationRepoMock.Object, deepValidationRepoMock.Object, loggerAccountRepositoryMock.Object);

            var existingAccount = new AccountEntity
            {
                AccountId = 20,
                AccountNumber = "accountInsertExists",
                AccountGlobalUniqueId = guid,
                LegalName = "legal"
            };
            context.AccountEntity.Add(existingAccount);

            var refAccount = new RefAccountEntity
            {
                EntityId = guid,
                AccountNumber = "accountInsertExists",
                OperationType = OperationAction.Insert,
                OperationDate = DateTime.UtcNow,
                ValidationDate = null
            };
            context.RefAccountEntity.Add(refAccount);
            await context.SaveChangesAsync();

            // DoesCreateOperationExist returns a pre-existing Insert op → redirects to Update
            // CreateUpdateAccountOperationAsync then calls FetchOperationsByCriteriaAsync again → also return it so the else branch runs
            var existingOp = new RegOperationEntity
            {
                Id = 999,
                EntityId = guid,
                Operation = OperationAction.Insert,
                ApprovalStatus = ApprovalStatus.Approved,
                ProcessStatus = ProcessStatus.Ready,
                Type = OperationCategory.ACCOUNT,
                CreationDate = DateTime.UtcNow
            };

            operationRepoMock.SetupSequence(op => op.FetchOperationsByCriteriaAsync(
                    It.IsAny<OperationSearchCriteria>(),
                    OperationStrategyType.ACCOUNT,
                    refAccount.AccountNumber,
                    It.IsAny<bool?>(),
                    null))
                .ReturnsAsync(new List<RegOperationEntity> { existingOp })    // DoesCreateOperationExist → redirect to Update
                .ReturnsAsync(new List<RegOperationEntity> { existingOp });   // CreateUpdateAccountOperationAsync → proceed to create op

            operationRepoMock.Setup(op => op.CreateOperationAsync(It.IsAny<RegOperationEntity>()))
                .Returns(Task.CompletedTask);

            // Act
            await repository.ValidateAccountOperation();

            // Assert — Update path creates exactly one operation, no audit
            operationRepoMock.Verify(op => op.CreateOperationAsync(It.IsAny<RegOperationEntity>()), Times.Once);
            deepValidationRepoMock.Verify(x => x.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()), Times.Never);

            var updated = await context.RefAccountEntity.FirstOrDefaultAsync(r => r.EntityId == guid);
            Assert.Equal(OperationAction.Update, updated!.OperationType);
        }
        [Fact]
        public async Task ValidateAccountOperation_InsertAccount_PendingInsertOperationExists_ShouldRedirectToUpdate()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ValidateAccountOperation_InsertAccount_PendingInsertOperationExists_ShouldRedirectToUpdate));
            using var context = new RefContext(options);
            var guid = Guid.NewGuid();
            var operationRepoMock = new Mock<IOperationRepository>();
            var deepValidationRepoMock = new Mock<IDeepValidationRepository>();
            var loggerAccountRepositoryMock = new Mock<ILogger<AccountRepository>>();
            var repository = CreateRepository(context, operationRepoMock.Object, deepValidationRepoMock.Object, loggerAccountRepositoryMock.Object);

            var refAccount = new RefAccountEntity
            {
                EntityId = guid,
                AccountNumber = "accountInsertPendingOp",
                OperationType = OperationAction.Insert,
                OperationDate = DateTime.UtcNow,
                ValidationDate = null
            };
            context.RefAccountEntity.Add(refAccount);
            await context.SaveChangesAsync();

            // FetchOperations returns a pre-existing Insert operation
            var existingOp = new RegOperationEntity
            {
                Id = 500,
                EntityId = guid,
                Operation = OperationAction.Insert,
                ApprovalStatus = ApprovalStatus.Approved,
                ProcessStatus = ProcessStatus.Ready,
                Type = OperationCategory.ACCOUNT,
                CreationDate = DateTime.UtcNow
            };

            operationRepoMock.Setup(op => op.FetchOperationsByCriteriaAsync(
                    It.IsAny<OperationSearchCriteria>(),
                    OperationStrategyType.ACCOUNT,
                    refAccount.AccountNumber,
                    It.IsAny<bool?>(),
                    null))
                .ReturnsAsync(new List<RegOperationEntity> { existingOp });

            operationRepoMock.Setup(op => op.CreateOperationAsync(It.IsAny<RegOperationEntity>()))
                .Returns(Task.CompletedTask);

            // Act
            await repository.ValidateAccountOperation();

            // Assert – Update path creates one operation (audit-free)
            operationRepoMock.Verify(op => op.CreateOperationAsync(It.IsAny<RegOperationEntity>()), Times.Once);
            deepValidationRepoMock.Verify(x => x.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()), Times.Never);

            var updated = await context.RefAccountEntity.FirstOrDefaultAsync(r => r.EntityId == guid);
            Assert.Equal(OperationAction.Update, updated!.OperationType);
        }
    }
}

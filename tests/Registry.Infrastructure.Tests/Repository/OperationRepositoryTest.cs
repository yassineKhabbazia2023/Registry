using Application.Consts;
using Application.Exceptions;
using Application.Requests;
using AutoFixture;
using Infrastructure.Repository;
using Kpmg.ExceptionMiddleware.AdvancedExceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Pulse.ContactRegistry.Domain.Context;
using Pulse.ContactRegistry.Domain.Entities;
using Application.Mappers;

namespace Registry.Infrastructure.Tests.Repository;

public class OperationRepositoryTest
{
    private readonly Fixture _fixture;
    private Mock<ILogger<OperationRepository>> _logger;

    public OperationRepositoryTest()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        _logger = new Mock<ILogger<OperationRepository>>(MockBehavior.Strict);
    }

    private static DbContextOptions<RefContext> CreateInMemoryOptions(string databaseName)
    {
        return new DbContextOptionsBuilder<RefContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
    }

    [Fact]
    public async Task GetOperationAsync_Return_OperationList()
    {
        // Arrange
        Guid roleGuidId = new Guid("d0da7087-2e03-4d2d-b399-5e350ea0bdd7");
        var processId = 1;
        var accountNumber = "19909090";
        var Email = "test.contactemail@email.fr";

    //    var operationSearchCriteria = new OperationSearchCriteria()
    //    {
    //        OperationName = "INSERT",
    //        Status = "PENDING"
    //    };
    //    var options = CreateInMemoryOptions(nameof(GetOperationAsync_Return_OperationList));

        var regOperation = new RegOperationEntity()
        {
            Id = processId,
            Operation = "INSERT",
            CreationDate = DateTime.UtcNow,
            EntityId = roleGuidId,
            LastStatusApprovalDate = DateTime.UtcNow,
            LastStatusApprovalBy = "test@email.fr",
            PublishedAt = null!,
            ApprovalStatus = "PENDING",
            Type = "ROLE",
            ProcessStatus = ""
        };

        var refAccount = new RefAccountEntity()
        {
            EntityId = Guid.NewGuid(),
            LegalName = "legalname",
            AccountNumber = accountNumber,
            OperationType = "ACCOUNT",
        };

        var refContact = new RefContactEntity()
        {
            EntityId = Guid.NewGuid(),
            Email = Email,
            FirstName = "firstname",
            LastName = "lastname",
            OperationType = "CONTACT"
        };

        var refRole = new RefRoleEntity()
        {
            EntityId = roleGuidId,
            ContactEmail = refContact.Email,
            AccountNumber = refAccount.AccountNumber,
            OperationType = "ROLE",
        };

        using var context = new RefContext(options);
        context.RefAccountEntity.Add(refAccount);
        context.RefContactEntity.Add(refContact);
        context.RefRoleEntity.Add(refRole);
        context.RegOperationEntity.Add(regOperation);
        context.SaveChanges();

        var creOperationMapped = MapDbEntityToModel.MapDbOperationEntityToOperationDetailModel(regOperation, refRole, refContact, accountNumber);

        // Act
        var repository = new OperationRepository(context,_logger.Object);
        var result = await repository.GetOperationsAsync(accountNumber, operationSearchCriteria);

    //    // Assert
    //    Assert.NotNull(result);
    //    var resultFirst = result.First();
    //    Assert.Equivalent(creOperationMapped, resultFirst);
    //    Assert.Equal(creOperationMapped!.OperationName, resultFirst!.OperationName);
    //    Assert.Equal(creOperationMapped!.OperationType, resultFirst!.OperationType);
    //    Assert.Equal(creOperationMapped!.RoleId, resultFirst!.RoleId);
    //    Assert.Equal(creOperationMapped!.Email, resultFirst!.Email);
    //}

    [Fact]
    public async Task UpdateOperationAsync_Should_ReturnsOkResultAsync()
    {
        var options = CreateInMemoryOptions(nameof(UpdateOperationAsync_Should_ReturnsOkResultAsync));
        using (var context = new RefContext(options))
        {
            // Arrange
            var operationModel = _fixture.Create<RegOperationEntity>();

            context.RegOperationEntity.Add(operationModel);
            await context.SaveChangesAsync();
            var operationRepository = new OperationRepository(context, _logger.Object);

            // Act
            operationModel.ApprovalStatus = "APPROVED";
            await operationRepository.UpdateOperationAsync(operationModel.Id, operationModel.MapEntityToModel()!);

            // Assert
            var updatedOperation = await context.RegOperationEntity.SingleAsync(a => a.Id == operationModel.Id);
            Assert.Equal("APPROVED", updatedOperation!.ApprovalStatus);
            Assert.Equal(operationModel.LastStatusApprovalBy, updatedOperation.LastStatusApprovalBy);
        }
    }

    [Fact]
    public async Task UpdateOperationAsync_WithWrongId_Should_ThrowException()
    {
        var options = CreateInMemoryOptions(nameof(UpdateOperationAsync_WithWrongId_Should_ThrowException));
        using (var context = new RefContext(options))
        {
            // Arrange
            var operationModel = _fixture.Create<RegOperationEntity>();

            context.RegOperationEntity.Add(operationModel);
            await context.SaveChangesAsync();
            var operationRepository = new OperationRepository(context, _logger.Object);

            // Act
            operationModel.ApprovalStatus = "APPROVED";
            Task operation() => operationRepository.UpdateOperationAsync(999, operationModel.MapEntityToModel()!);

            // Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(operation);
            Assert.Equal(Errors.NotFoundOperationCode, exception!.Code);
            Assert.Equal(string.Format(Errors.NotFoundOperationMessage, 999), exception.Message);
        }
    }

    [Fact]
    public async Task FindAccountOperationAsync_Should_Return_Operations()
    {
        // Arrange
        var accountNumber = "19909090";
        var criteria = new OperationSearchCriteria() { OperationName = "INSERT" };
        var options = CreateInMemoryOptions(nameof(FindAccountOperationAsync_Should_Return_Operations));
        var entityId = new Guid("10f84e99-2a85-4cfc-99e2-71360168e4aa");
        using (var context = new RefContext(options))
        {
            var refAccount = new RefAccountEntity()
            {
                AccountNumber = accountNumber,
                EntityId = entityId,
                LegalName = "MyAccount",
                OperationType = "INSERT"
            };

            var operation = new RegOperationEntity()
            {
                EntityId = entityId,
                Type = "ACCOUNT",
                Operation = "INSERT",
                ProcessStatus = ProcessStatus.Sent,
                ApprovalStatus = "APPROVED"
            };

            context.RefAccountEntity.Add(refAccount);
            context.RegOperationEntity.Add(operation);
            await context.SaveChangesAsync();
            var repository = new OperationRepository(context, _logger.Object);

            // Act
            var result = await repository.FindAccountOperationAsync(criteria, accountNumber);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }
    }

    [Fact]
    public async Task FindContactOperationAsync_Should_Return_Operations()
    {
        // Arrange
        var entityId = new Guid("10f84e99-2a85-4cfc-99e2-71360168e4aa");
        var email = "test.contactemail@email.fr";
        var criteria = new OperationSearchCriteria() { OperationName = "INSERT" };
        var options = CreateInMemoryOptions(nameof(FindContactOperationAsync_Should_Return_Operations));

        using (var context = new RefContext(options))
        {
            var refContact = new RefContactEntity()
            {
                Email= email,
                EntityId = entityId,
                OperationType = "INSERT"
            };

            var operation = new RegOperationEntity()
            {
                EntityId = entityId,
                Type = "CONTACT",
                Operation = "INSERT",
                ProcessStatus = ProcessStatus.Sent,
                ApprovalStatus = "APPROVED",
                PublishedAt = DateTime.UtcNow
            };

            context.RefContactEntity.Add(refContact);
            context.RegOperationEntity.Add(operation);
            await context.SaveChangesAsync();
            var repository = new OperationRepository(context, _logger.Object);

            // Act
            var result = await repository.FindContactOperationAsync(criteria, email);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }
    }

    [Fact]
    public async Task FindRoleOperationAsync_Should_Return_Operations()
    {
        // Arrange
        var entityId = new Guid("10f84e99-2a85-4cfc-99e2-71360168e4aa");
        var email = "test.contactemail@email.fr";
        var accountNumber = "19909090";
        var criteria = new OperationSearchCriteria() { OperationName = "INSERT" };
        var options = CreateInMemoryOptions(nameof(FindRoleOperationAsync_Should_Return_Operations));

        using (var context = new RefContext(options))
        {
            var refContact = new RefContactEntity()
            {
                Email = email,
                EntityId = entityId,
                OperationType = "INSERT"
            };

            var refAccount = new RefAccountEntity()
            {
                AccountNumber = accountNumber,
                EntityId = entityId,
                LegalName = "MyAccount",
                OperationType = "INSERT"
            };

            var refRole = new RefRoleEntity()
            {
                EntityId = entityId,
                ContactEmail = email,
                AccountNumber = accountNumber,
                OperationType = "INSERT"
            };
            var operation = new RegOperationEntity()
            {
                EntityId = entityId,
                Type = "ROLE",
                Operation = "INSERT",
                ProcessStatus = ProcessStatus.Sent,
                ApprovalStatus = "APPROVED"
            };
            context.RefAccountEntity.Add(refAccount);
            context.RefContactEntity.Add(refContact);
            context.RefRoleEntity.Add(refRole);
            context.RegOperationEntity.Add(operation);
            await context.SaveChangesAsync();
            var repository = new OperationRepository(context,_logger.Object);

            // Act
            var result = await repository.FindRoleOperationAsync(criteria, email, accountNumber);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }
    }
}

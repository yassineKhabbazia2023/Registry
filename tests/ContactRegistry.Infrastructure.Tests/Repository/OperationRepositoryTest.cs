using Application.Exceptions;
using Application.Requests;
using AutoFixture;
using Domain.Entities;
using Infrastructure.Context;
using Infrastructure.Mappers;
using Infrastructure.Repository;
using Kpmg.ExceptionMiddleware.AdvancedExceptions;
using Microsoft.EntityFrameworkCore;
using Pulse.ContactRegistry.Infrastructure.Context;
using Pulse.ContactRegistry.Infrastructure.Entities;

namespace ContactRegistry.Infrastructure.Tests.Repository;

public class OperationRepositoryTest
{
    private readonly Fixture _fixture;

    public OperationRepositoryTest()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
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
        var processId = 1;
        var accountNumber = "19909090";
        var Email = "test.contactemail@email.fr";

        var operationSearchCriteria = new OperationSearchCriteria()
        {
            OperationName = "INSERT",
            Status = "PENDING"
        };
        var options = CreateInMemoryOptions(nameof(GetOperationAsync_Return_OperationList));

        var regOperation = new RegOperationEntity()
        {
            Id = processId,
            Operation = "INSERT",
            CreationDate = DateTime.UtcNow,
            EntityId = Guid.NewGuid(),
            LastStatusUpdatedDate = DateTime.UtcNow,
            LastStatusUpdatedBy = "test@email.fr",
            PublishedAt = null!,
            Status = "PENDING",
            Type = "ROLE"
        };

        var regAccount = new RegAccountEntity()
        {
            Id = Guid.NewGuid(),
            LegalName = "legalname",
            AccountNumber = accountNumber
        };

        var regContact = new RegContactEntity()
        {
            Id = Guid.NewGuid(),
            Email = Email,
            FirstName = "firstname",
            LastName = "lastname"
        };

        var regRole = new RegRoleEntity()
        {
            AccountId = regAccount.Id,
            ContactId = regContact.Id,
            IsFavorite = true,
            Onboarded = true,
            RoleId = regOperation.EntityId,
            Deleted = null,
            RoleDelegataireEmail = string.Empty,
            RoleSignatory = false,
            ContactEmail = Email,
            AccountNumber = accountNumber,
            AccountNumberNavigation = regAccount,
            ContactEmailNavigation = regContact,
        };

        using var context = new RefContext(options);
        context.RegAccountEntity.Add(regAccount);
        context.RegContactEntity.Add(regContact);
        context.RegRoleEntity.Add(regRole);
        context.RegOperationEntity.Add(regOperation);
        context.SaveChanges();

        var creOperationMapped = MapDbEntityToModel.MapDbOperationEntityToOperationDetailModel(regOperation, regRole, regContact, accountNumber);

        // Act
        var repository = new OperationRepository(context);
        var result = await repository.GetOperationsAsync(accountNumber, operationSearchCriteria);

        // Assert
        Assert.NotNull(result);
        var resultFirst = result.First();
        Assert.Equivalent(creOperationMapped, resultFirst);
        Assert.Equal(creOperationMapped!.OperationName, resultFirst!.OperationName);
        Assert.Equal(creOperationMapped!.OperationType, resultFirst!.OperationType);
        Assert.Equal(creOperationMapped!.RoleId, resultFirst!.RoleId);
        Assert.Equal(creOperationMapped!.Email, resultFirst!.Email);
    }

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
            var operationRepository = new OperationRepository(context);

            // Act
            operationModel.Status = "APPROVED";
            await operationRepository.UpdateOperationAsync(operationModel.Id, operationModel.MapEntityToModel()!);

            // Assert
            var updatedOperation = await context.RegOperationEntity.SingleAsync(a => a.Id == operationModel.Id);
            Assert.Equal("APPROVED", updatedOperation!.Status);
            Assert.Equal(operationModel.LastStatusUpdatedBy, updatedOperation.LastStatusUpdatedBy);
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
            var operationRepository = new OperationRepository(context);

            // Act
            operationModel.Status = "APPROVED";
            Task operation() => operationRepository.UpdateOperationAsync(999, operationModel.MapEntityToModel()!);

            // Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(operation);
            Assert.Equal(Errors.NotFoundOperationCode, exception!.Code);
            Assert.Equal(string.Format(Errors.NotFoundOperationMessage, 999), exception.Message);
        }
    }
}

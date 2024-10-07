using Application.Exceptions;
using Application.Requests;
using Application.Services;
using AutoFixture;
using Azure;
using Domain.Entities;
using Infrastructure.Context;
using Infrastructure.Mappers;
using Infrastructure.Repository;
using Kpmg.ExceptionMiddleware.AdvancedExceptions;
using Microsoft.EntityFrameworkCore;

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

    private static DbContextOptions<ApplicationDbContext> CreateInMemoryOptions(string databaseName)
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
    }

    [Fact]
    public async Task GetOperationAsync_Return_OperationList()
    {
        // Arrange
        var processId = 1;
        var accountNumber = "19909090";

        var operationSearchCriteria = new OperationSearchCriteria()
        {
            OperationName = "INSERT",
            Status = "PENDING"
        };
        var options = CreateInMemoryOptions(nameof(GetOperationAsync_Return_OperationList));
        var creOperation = new CreOperation()
        {
            Id = processId,
            Operation = "INSERT",
            CreationDate = DateTime.UtcNow,
            EntityId = Guid.NewGuid(),
            LastStatusUpdatedDate = DateTime.UtcNow,
            LastStatusUpdatedBy = "test@email.fr",
            PublishedAt = DateTime.UtcNow,
            Status = "Pending",
            Type = "ROLE"
        };

        var creAccount = new CreAccount()
        {
            Id = Guid.NewGuid(),
            LegalName = "legalname",
            AccountNumber = accountNumber
        };

        var creContact = new CreContact()
        {
            Id = Guid.NewGuid(),
            Email = "test@email.fr",
            FirstName = "firstname",
            LastName = "lastname"
        };

        var creRole = new CreRole()
        {
            AccountId = creAccount.Id,
            ContactId = creContact.Id,
            IsFavorite = true,
            Onboarded = true,
            RoleId = creOperation.EntityId,
            Deleted = null,
            RoleDelegataireEmail = string.Empty,
            RoleSignatory = false,
            Account = creAccount,
            Contact = creContact,
        };

        using var context = new ApplicationDbContext(options);
        context.CreAccounts.Add(creAccount);
        context.CreContacts.Add(creContact);
        context.CreRoles.Add(creRole);
        context.CreOperations.Add(creOperation);
        context.SaveChanges();

        var creOperationMapped = MapDbEntityToModel.MapDbOperationEntityToOperationDetailModel(creOperation, creRole, creContact, accountNumber);

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
        using (var context = new ApplicationDbContext(options))
        {
            // Arrange
            var operationModel = _fixture.Create<CreOperation>();

            context.CreOperations.Add(operationModel);
            await context.SaveChangesAsync();
            var operationRepository = new OperationRepository(context);

            // Act
            operationModel.Status = "APPROVED";
            await operationRepository.UpdateOperationAsync(operationModel.Id, operationModel.MapEntityToModel()!);

            // Assert
            var updatedOperation = await context.CreOperations.SingleAsync(a => a.Id == operationModel.Id);
            Assert.Equal("APPROVED", updatedOperation!.Status);
            Assert.Equal(operationModel.LastStatusUpdatedBy, updatedOperation.LastStatusUpdatedBy);
        }
    }

    [Fact]
    public async Task UpdateOperationAsync_WithWrongId_Should_ThrowException()
    {
        var options = CreateInMemoryOptions(nameof(UpdateOperationAsync_WithWrongId_Should_ThrowException));
        using (var context = new ApplicationDbContext(options))
        {
            // Arrange
            var operationModel = _fixture.Create<CreOperation>();

            context.CreOperations.Add(operationModel);
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

using Application.Requests;
using Domain.Entities;
using Infrastructure.Context;
using Infrastructure.Mappers;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;

namespace ContactRegistry.Infrastructure.Tests.Repository;

public class OperationRepositoryTest
{
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
            LastStatusUpdatedBy = 123,
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

        var creOperationMapped = MapDbEntityToModel.MapDbOperationEntityToOperationModel(creOperation, creRole, creContact, accountNumber);

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
}

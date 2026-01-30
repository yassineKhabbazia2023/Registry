using Application.Mappers;
using AutoFixture;
using Pulse.Registry.Domain.Entities;
using Pulse.Registry.Domain.Entities.Contacts;

namespace Registry.Application.Tests.Mappers;

public class MapDbEntityToModelTests
{
    private readonly Fixture _fixture;

    public MapDbEntityToModelTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public void MapDbOperationEntityToOperationDetailModel_ReturnsNull_WhenOperationIsNull()
    {
        // Act
        var result = MapDbEntityToModel.MapDbOperationEntityToOperationDetailModel(
            null!,
            _fixture.Create<RefRoleEntity>(),
            _fixture.Create<RefContactEntity>(),
            "ACC123");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void MapDbOperationEntityToOperationDetailModel_MapsAllFields_WithRefContactEntity()
    {
        // Arrange
        var operation = _fixture.Create<RegOperationEntity>();
        var role = _fixture.Create<RefRoleEntity>();
        var contact = _fixture.Create<RefContactEntity>();
        var accountNumber = "ACC123";

        // Act
        var result = MapDbEntityToModel.MapDbOperationEntityToOperationDetailModel(
            operation,
            role,
            contact,
            accountNumber);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(operation.Id, result!.OperationId);
        Assert.Equal(role.EntityId, result.RoleId);
        Assert.Equal(operation.Operation, result.OperationName);
        Assert.Equal(operation.Type, result.OperationType);
        Assert.Equal(operation.CreationDate, result.CreationDate);
        Assert.Equal(operation.ApprovalStatus, result.Status);
        Assert.Equal(contact.Email, result.Email);
        Assert.Equal(contact.FirstName, result.FirstName);
        Assert.Equal(contact.LastName, result.LastName);
        Assert.Equal(accountNumber, result.AccountNumber);
    }

    [Fact]
    public void MapDbOperationEntityToOperationDetailModel_SetsGuidEmpty_WhenRoleIsNull()
    {
        // Arrange
        var operation = _fixture.Create<RegOperationEntity>();
        var contact = _fixture.Create<RefContactEntity>();

        // Act
        var result = MapDbEntityToModel.MapDbOperationEntityToOperationDetailModel(
            operation,
            null!,
            contact,
            "ACC123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Guid.Empty, result!.RoleId);
    }

    [Fact]
    public void MapDbOperationEntityToOperationDetailModel_MapsAllFields_WithContactEntity()
    {
        // Arrange
        var operation = _fixture.Create<RegOperationEntity>();
        var role = _fixture.Create<RefRoleEntity>();
        var contact = _fixture.Create<ContactEntity>();
        var accountNumber = "ACC456";

        // Act
        var result = MapDbEntityToModel.MapDbOperationEntityToOperationDetailModel(
            operation,
            role,
            contact,
            accountNumber);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(operation.Id, result!.OperationId);
        Assert.Equal(role.EntityId, result.RoleId);
        Assert.Equal(contact.Email, result.Email);
        Assert.Equal(contact.FirstName, result.FirstName);
        Assert.Equal(contact.LastName, result.LastName);
        Assert.Equal(accountNumber, result.AccountNumber);
    }

    [Fact]
    public void MapDbOperationEntityToOperationModel_MapsAllFieldsCorrectly()
    {
        // Arrange
        var entity = _fixture.Create<RegOperationEntity>();

        // Act
        var result = entity.MapDbOperationEntityToOperationModel();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(entity.CreationDate, result.CreationDate);
        Assert.Equal(entity.ApprovalStatus, result.Status);
        Assert.Equal(entity.EntityId, result.EntityId);
        Assert.Equal(entity.LastStatusApprovalBy, result.LastStatusUpdatedBy);
        Assert.Equal(entity.LastStatusApprovalDate, result.LastStatusUpdatedDate);
        Assert.Equal(entity.Operation, result.Operation);
        Assert.Equal(entity.PublishedAt, result.PublishedAt);
        Assert.Equal(entity.Type, result.Type);
    }
}

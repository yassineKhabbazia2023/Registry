using Application.Mappers;
using AutoFixture;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Registry.Domain.Entities.Accounts;

namespace Registry.Application.Tests.Mappers;

public class MapEventDataModelPulseTests
{
    private readonly Fixture _fixture;

    public MapEventDataModelPulseTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public void MapToRoleEntity_ReturnsNull_WhenRoleCreatedEventDataIsNull()
    {
        // Act
        RoleEntity result = MapEventDataModelPulse.MapToRoleEntity((RoleCreatedEventData)null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void MapToRoleEntity_MapsAllFieldsCorrectly_FromRoleCreatedEventData()
    {
        // Arrange
        var source = _fixture.Create<RoleCreatedEventData>();

        // Act
        var result = source.MapToRoleEntity();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(source.AccountGlobalUniqueId, result.AccountGlobalUniqueId);
        Assert.Equal(source.AccountId, result.AccountId);
        Assert.Equal(source.AccountNumber, result.AccountNumber);
        Assert.Equal(source.ContactEmail, result.ContactEmail);
        Assert.Equal(source.ContactGlobalUniqueId, result.ContactGlobalUniqueId);
        Assert.Equal(source.ContactId, result.ContactId);
        Assert.Equal(1, result.RoleDuplicatesCounter);
    }

    [Fact]
    public void MapToRoleEntity_ReturnsNull_WhenRoleDeletedEventDataIsNull()
    {
        // Act
        RoleEntity result = MapEventDataModelPulse.MapToRoleEntity((RoleDeletedEventData)null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void MapToRoleEntity_MapsAllFieldsCorrectly_FromRoleDeletedEventData()
    {
        // Arrange
        var source = _fixture.Create<RoleDeletedEventData>();

        // Act
        var result = source.MapToRoleEntity();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(source.AccountGlobalUniqueId, result.AccountGlobalUniqueId);
        Assert.Equal(source.AccountId, result.AccountId);
        Assert.Equal(source.AccountNumber, result.AccountNumber);
        Assert.Equal(source.ContactEmail, result.ContactEmail);
        Assert.Equal(source.ContactId, result.ContactId);
    }
}

// <copyright file="DeepValidationRepositoryTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using AutoFixture;
using FluentAssertions;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Pulse.Registry.Domain.Entities.Audits;

namespace Registry.Infrastructure.Tests.Repository;

public class DeepValidationRepositoryTests
{
    private readonly Fixture _fixture;
    private readonly ILogger<DeepValidationRepository> _logger;

    public DeepValidationRepositoryTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        _logger = Mock.Of<ILogger<DeepValidationRepository>>();
    }

    private DbContextOptions<RefContext> GetDbOptions()
    {
        return new DbContextOptionsBuilder<RefContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task AddDeepValidationAsync_ShouldReturnTrue_WhenValidationDoesNotExist()
    {
        // Arrange
        var deepValidation = _fixture.Create<DeepValidationEntity>();

        using var context = new RefContext(GetDbOptions());
        var repository = new DeepValidationRepository(context, _logger);

        // Act
        var result = await repository.AddDeepValidationAsync(deepValidation);

        // Assert
        result.Should().BeTrue();
        var savedValidation = await context.Set<DeepValidationEntity>()
            .FirstOrDefaultAsync(x => x.Id == deepValidation.Id);
        savedValidation.Should().NotBeNull();
        savedValidation.Should().BeEquivalentTo(deepValidation);
    }

    [Fact]
    public async Task AddDeepValidationAsync_ShouldThrowArgumentException_WhenValidationAlreadyExists()
    {
        // Arrange
        var deepValidation = _fixture.Create<DeepValidationEntity>();

        using var context = new RefContext(GetDbOptions());
        // Add the validation first
        context.Add(deepValidation);
        await context.SaveChangesAsync();

        var repository = new DeepValidationRepository(context, _logger);

        // Act
        var result = async () => await repository.AddDeepValidationAsync(deepValidation);

        // Assert
        await result.Should().ThrowAsync<ArgumentException>();
    }


    [Fact]
    public async Task AddDeepValidationAsync_ShouldReturnFalse_WhenNullEntityProvided()
    {
        // Arrange
        DeepValidationEntity deepValidation = null;

        using var context = new RefContext(GetDbOptions());
        var repository = new DeepValidationRepository(context, _logger);

        // Act
        var result = async () => await repository.AddDeepValidationAsync(deepValidation);

        // Assert
        await result.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DoesDeepValidationLineExistsAsync_ReturnsTrue_WhenExistsByEntityIdAndType()
    {
        // Arrange
        using var context = new RefContext(GetDbOptions());
        var entityId = Guid.NewGuid();
        var operationType = "CREATE";

        context.DeepValidationEntity.Add(new DeepValidationEntity
        {
            EntityId = entityId,
            Type = operationType,
            Reason = "Test Reason",
        });
        await context.SaveChangesAsync();

        var repository = new DeepValidationRepository(context, _logger);

        // Act
        var result = await repository.DoesDeepValidationLineExistsAsync(entityId, operationType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DoesDeepValidationLineExistsAsync_ReturnsFalse_WhenRoleDoesNotExist()
    {
        // Arrange
        using var context = new RefContext(GetDbOptions());
        var entityId = Guid.NewGuid();

        var repository = new DeepValidationRepository(context, _logger);

        // Act
        var result = await repository.DoesDeepValidationLineExistsAsync(entityId, "UPDATE");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DoesDeepValidationLineExistsAsync_ReturnsTrue_WhenExistsByAccountNumberAndEmail()
    {
        // Arrange
        using var context = new RefContext(GetDbOptions());
        var entityId = Guid.NewGuid();
        var operationType = "DELETE";

        var role = new RefRoleEntity
        {
            EntityId = entityId,
            AccountNumber = "ACC123",
            ContactEmail = "test@mail.com",
            OperationType = "DELETE",
        };

        context.RefRoleEntity.Add(role);

        var deepValidation = new DeepValidationEntity
        {
            EntityId = Guid.NewGuid(),
            Type = operationType,
            Reason = "Test Reason",
        };
        context.DeepValidationEntity.Add(deepValidation);

        context.RefRoleEntity.Add(new RefRoleEntity
        {
            EntityId = deepValidation.EntityId,
            AccountNumber = "ACC123",
            ContactEmail = "test@mail.com",
            OperationType = "INSERT",
        });

        await context.SaveChangesAsync();

        var repository = new DeepValidationRepository(context, _logger);

        // Act
        var result = await repository.DoesDeepValidationLineExistsAsync(entityId, operationType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DoesDeepValidationLineExistsAsync_ReturnsFalse_WhenNoMatchFound()
    {
        // Arrange
        using var context = new RefContext(GetDbOptions());
        var entityId = Guid.NewGuid();
        var operationType = "CREATE";

        context.RefRoleEntity.Add(new RefRoleEntity
        {
            EntityId = entityId,
            AccountNumber = "ACC999",
            ContactEmail = "nope@mail.com",
            OperationType = "UPDATE",
        });

        var deepValidation = new DeepValidationEntity
        {
            EntityId = Guid.NewGuid(),
            Type = operationType,
            Reason = "No Match Test"
        };
        context.DeepValidationEntity.Add(deepValidation);

        context.RefRoleEntity.Add(new RefRoleEntity
        {
            EntityId = deepValidation.EntityId,
            AccountNumber = "OTHER",
            ContactEmail = "other@mail.com",
            OperationType = "INSERT",
        });

        await context.SaveChangesAsync();

        var repository = new DeepValidationRepository(context, _logger);

        // Act
        var result = await repository.DoesDeepValidationLineExistsAsync(entityId, operationType);

        // Assert
        Assert.False(result);
    }
}

using Application.Models;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Pulse.ContactRegistry.Infrastructure.Context;
using Pulse.ContactRegistry.Infrastructure.Entities;

namespace ContactRegistry.Infrastructure.Tests.Repository;

public class RegProcessDeltaTriggerRepositoryTest
{
    private static DbContextOptions<RefContext> CreateInMemoryOptions(string databaseName)
    {
        return new DbContextOptionsBuilder<RefContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
    }

    [Fact]
    public async Task GetProcessAsync_Return_Process()
    {
        // Arrange
        var processId = 1;
        var options = CreateInMemoryOptions(nameof(GetProcessAsync_Return_Process));
        var processDelta = new RegProcessDeltaTriggerEntity()
        {
            Id = processId,
            Account = false,
            Contact = false,
            Role = false
        };

        using var context = new RefContext(options);
        context.RegProcessDeltaTriggerEntity.Add(processDelta);
        context.SaveChanges();

        // Act
        var repository = new RegProcessDeltaTriggerRepository(context);
        var result = await repository.GetProcessAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equivalent(processDelta, result);
        Assert.Equal(processDelta.Id, result.Id);
        Assert.Equal(processDelta.Account, result.Account);
        Assert.Equal(processDelta.Contact, result.Contact);
        Assert.Equal(processDelta.Role, result.Role);
    }

    [Fact]
    public async Task UpdateAccountProcessAsync_Success()
    {
        // Arrange
        var processId = 1;
        var options = CreateInMemoryOptions(nameof(UpdateAccountProcessAsync_Success));
        var processDelta = new RegProcessDeltaTriggerEntity()
        {
            Id = processId,
            Account = false,
            Contact = false,
            Role = false
        };

        using var context = new RefContext(options);
        context.RegProcessDeltaTriggerEntity.Add(processDelta);
        context.SaveChanges();

        // Act
        var repository = new RegProcessDeltaTriggerRepository(context);
        await repository.UpdateAccountProcessAsync(true);

        var result = await repository.GetProcessAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Account);
    }

    [Fact]
    public async Task UpdateContactProcessAsync_Success()
    {
        // Arrange
        var processId = 1;
        var options = CreateInMemoryOptions(nameof(UpdateContactProcessAsync_Success));
        var processDelta = new RegProcessDeltaTriggerEntity()
        {
            Id = processId,
            Account = false,
            Contact = false,
            Role = false
        };

        using var context = new RefContext(options);
        context.RegProcessDeltaTriggerEntity.Add(processDelta);
        context.SaveChanges();

        // Act
        var repository = new RegProcessDeltaTriggerRepository(context);
        await repository.UpdateContactProcessAsync(true);

        var result = await repository.GetProcessAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Contact);
    }

    [Fact]
    public async Task UpdateRoleProcessAsync_Success()
    {
        // Arrange
        var processId = 1;
        var options = CreateInMemoryOptions(nameof(UpdateRoleProcessAsync_Success));
        var processDelta = new RegProcessDeltaTriggerEntity()
        {
            Id = processId,
            Account = false,
            Contact = false,
            Role = false
        };

        using var context = new RefContext(options);
        context.RegProcessDeltaTriggerEntity.Add(processDelta);
        context.SaveChanges();

        // Act
        var repository = new RegProcessDeltaTriggerRepository(context);
        await repository.UpdateRoleProcessAsync(true);

        var result = await repository.GetProcessAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Role);
    }
}

// <copyright file="RoleServiceTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Enums;
using Application.Interfaces;
using Application.Interfaces.RuleValidators;
using Application.Models;
using Application.Options;
using Application.Services;
using AutoFixture;
using Domain.Entities.Accounts;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Pulse.Registry.Domain.Entities;

namespace Registry.Application.Tests.Services;

public class RoleServiceTest
{
    private readonly Fixture _fixture;
    private IOptions<BackGroundJobOptions> _backGroundJobOptions;

    public RoleServiceTest()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        var backGroundJobOptionsData = new BackGroundJobOptions()
        {
            Chunk = 1000,
            ShouldTriggerEvents = true,
            TimeToWaitBeforeEachStep = 3,
            NumberOfDaysToRetryFailedRoles = -7,
        };

        _backGroundJobOptions = Options.Create(backGroundJobOptionsData);
    }

    [Fact]
    public async Task InsertRolesAsync_Should_Be_Success()
    {
        var repository = new Mock<IRoleRepository>();
        var factory = Mock.Of<IRoleDeepValidatorFactory>();
        var roleService = new RoleService(null!, repository.Object, factory, _backGroundJobOptions);

        await roleService.InsertRolesAsync(It.IsAny<IEnumerable<RefRoleCsv>>());

        repository.Verify(x => x.AddRolesAsync(It.IsAny<IEnumerable<RefRoleCsv>>(), null), Times.Once);
    }

    [Fact]
    public async Task InsertRolesAsync_When_RoleSourceIsNotNull_Should_Be_Success_And_AffectRoleSource()
    {
        var repository = new Mock<IRoleRepository>();
        var factory = Mock.Of<IRoleDeepValidatorFactory>();
        var roleService = new RoleService(null!, repository.Object, factory, _backGroundJobOptions);

        await roleService.InsertRolesAsync(It.IsAny<IEnumerable<RefRoleCsv>>(), DataSources.PENNYLANE.ToString());

        repository.Verify(x => x.AddRolesAsync(It.IsAny<IEnumerable<RefRoleCsv>>(), DataSources.PENNYLANE.ToString()), Times.Once);
    }


    [Theory]
    [InlineData(1, 1)]
    [InlineData(2000, 1)]
    public async Task AddRoleAsync_Adds_Role_With2000Roles(int roleCsvLenght, int functionTimeCalled)
    {
        // Arrange
        var roles = _fixture.Build<RefRoleCsv>()
            .CreateMany(roleCsvLenght);

        var roleRepository = new Mock<IRoleRepository>();
        roleRepository.Setup(r => r.AddRolesAsync(It.IsAny<IEnumerable<RefRoleCsv>>(), null)).
            Callback<IEnumerable<RefRoleCsv>, string>((data, roleSource) =>
            {
                Assert.Null(roleSource);
                data.Count().Should().BeGreaterThanOrEqualTo(roles.Count());
            })
            .Returns(Task.CompletedTask);


        var loggerMock = new Mock<ILogger<RoleService>>(MockBehavior.Default);

        // Act
        var factory = Mock.Of<IRoleDeepValidatorFactory>();
        var roleService = new RoleService(null!, roleRepository.Object, factory, _backGroundJobOptions);
        await roleService.InsertRolesAsync(roles);

        roleRepository.VerifyAll();
        roleRepository.Verify(a => a.AddRolesAsync(It.IsAny<IEnumerable<RefRoleCsv>>(), null), Times.AtLeast(functionTimeCalled));
    }

    [Fact]
    public async Task CreateValidRolesOperationsAsync_When_OperationType_Insert()
    {
        // Arrange
        var refRole = _fixture.Create<RefRoleEntity>();
        refRole.OperationType = OperationAction.Insert;

        var roles = new List<RefRoleEntity>() { refRole };

        var roleRepositoryMock = new Mock<IRoleRepository>(MockBehavior.Loose);
        roleRepositoryMock.Setup(r => r.GetDeepValidationFailedRoles(_backGroundJobOptions.Value.NumberOfDaysToRetryFailedRoles))
            .ReturnsAsync(roles);

        var roleDeepValidatorMock = new Mock<IRoleDeepValidator>();
        var sequence = new MockSequence();
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.Instantiate(It.IsAny<RefRoleEntity>()))
            .Callback<RefRoleEntity>((role) =>
            {
                Assert.Equal(role, refRole);
            }).ReturnsAsync(roleDeepValidatorMock.Object).Verifiable();
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.ContactShouldExistInPulseOrOperations())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.AccountShouldExistInPulseOrOperations())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.RoleShouldShouldNotExistInPulseOrOperations())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.TryAddOperation())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.Validate())
            .ReturnsAsync(true);

        var loggerMock = new Mock<ILogger<RoleService>>();
        var roleDeepValidatorFactoryMock = new Mock<IRoleDeepValidatorFactory>();
        roleDeepValidatorFactoryMock.Setup(rf => rf.Create())
            .Returns(roleDeepValidatorMock.Object);

        // Act
        var roleService = new RoleService(loggerMock.Object, roleRepositoryMock.Object, roleDeepValidatorFactoryMock.Object, _backGroundJobOptions);
        var result = await roleService.ReviewFailedRolesOperationsAsync();

        // Assert
        roleDeepValidatorMock.Verify();
        result.Should().Contain(true);
    }

    [Fact]
    public async Task CreateValidRolesOperationsAsync_When_OperationType_Delete()
    {
        // Arrange
        var refRole = _fixture.Create<RefRoleEntity>();
        refRole.OperationType = OperationAction.Delete;

        var roles = new List<RefRoleEntity>() { refRole };

        var roleRepositoryMock = new Mock<IRoleRepository>(MockBehavior.Loose);
        roleRepositoryMock.Setup(r => r.GetDeepValidationFailedRoles(_backGroundJobOptions.Value.NumberOfDaysToRetryFailedRoles))
            .ReturnsAsync(roles);

        var roleDeepValidatorMock = new Mock<IRoleDeepValidator>();
        var sequence = new MockSequence();
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.Instantiate(It.IsAny<RefRoleEntity>()))
            .Callback<RefRoleEntity>((role) =>
            {
                Assert.Equal(role, refRole);
            }).ReturnsAsync(roleDeepValidatorMock.Object).Verifiable();
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.ContactShouldExistInPulse())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.AccountShouldExistInPulse())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.RoleShouldExistInPulse())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.TryAddOperation())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.Validate())
            .ReturnsAsync(true);

        var loggerMock = new Mock<ILogger<RoleService>>();
        var roleDeepValidatorFactoryMock = new Mock<IRoleDeepValidatorFactory>();
        roleDeepValidatorFactoryMock.Setup(rf => rf.Create())
            .Returns(roleDeepValidatorMock.Object);

        // Act
        var roleService = new RoleService(loggerMock.Object, roleRepositoryMock.Object, roleDeepValidatorFactoryMock.Object, _backGroundJobOptions);
        var result = await roleService.ReviewFailedRolesOperationsAsync();

        // Assert
        roleDeepValidatorMock.Verify();
        result.Should().Contain(true);
    }

    [Fact]
    public async Task CreateValidRolesOperationsAsync_Should_ProcessAllRoles()
    {
        // Arrange
        var roles = _fixture.CreateMany<RefRoleEntity>(3).ToList();
        roles[0].OperationType = OperationAction.Insert;
        roles[1].OperationType = OperationAction.Delete;

        var roleRepositoryMock = new Mock<IRoleRepository>();
        roleRepositoryMock.Setup(r => r.GetUnprocessedRoles()).Returns(roles);

        var roleDeepValidatorMock = new Mock<IRoleDeepValidator>();
        roleDeepValidatorMock
            .SetupSequence(v => v.Validate())
            .ReturnsAsync(true)
            .ReturnsAsync(true);

        roleDeepValidatorMock
            .Setup(v => v.Instantiate(It.IsAny<RefRoleEntity>())).ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock
            .Setup(v => v.ContactShouldExistInPulseOrOperations()).ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock
            .Setup(v => v.AccountShouldExistInPulseOrOperations()).ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock
            .Setup(v => v.RoleShouldShouldNotExistInPulseOrOperations()).ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock
            .Setup(v => v.ContactShouldExistInPulse()).ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock
            .Setup(v => v.AccountShouldExistInPulse()).ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock
            .Setup(v => v.RoleShouldExistInPulse()).ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock
            .Setup(v => v.TryAddOperation()).ReturnsAsync(roleDeepValidatorMock.Object);

        var roleDeepValidatorFactoryMock = new Mock<IRoleDeepValidatorFactory>();
        roleDeepValidatorFactoryMock.Setup(f => f.Create()).Returns(roleDeepValidatorMock.Object);

        var roleService = new RoleService(
            Mock.Of<ILogger<RoleService>>(),
            roleRepositoryMock.Object,
            roleDeepValidatorFactoryMock.Object,
            _backGroundJobOptions);

        // Act
        var results = await roleService.CreateValidRolesOperationsAsync();

        // Assert
        results.Should().HaveCount(2); // One role ignored because of default case in switch
        results.Should().OnlyContain(r => r == true);

        // Verify UpdateRefRoleAsync called for each role
        foreach (var role in roles)
        {
            roleRepositoryMock.Verify(r => r.UpdateRefRoleAsync(role), Times.Once);
        }
    }

    [Fact]
    public async Task ReviewFailedRolesOperationsAsync_When_UnhandledOperationType_Should_IgnoreRole()
    {
        // Arrange
        var roles = new List<RefRoleEntity>()
        {
            _fixture.Create<RefRoleEntity>()
        };
        roles[0].OperationType = "UNKNOWN"; // Opération non gérée explicitement

        var roleRepositoryMock = new Mock<IRoleRepository>();
        roleRepositoryMock.Setup(r => r.GetDeepValidationFailedRoles(_backGroundJobOptions.Value.NumberOfDaysToRetryFailedRoles))
            .ReturnsAsync(roles);

        // Créer un mock IRoleDeepValidator même si ne sera pas utilisé
        var roleDeepValidatorMock = new Mock<IRoleDeepValidator>();

        var roleDeepValidatorFactoryMock = new Mock<IRoleDeepValidatorFactory>();
        roleDeepValidatorFactoryMock.Setup(f => f.Create())
            .Returns(roleDeepValidatorMock.Object);

        var roleService = new RoleService(
            Mock.Of<ILogger<RoleService>>(),
            roleRepositoryMock.Object,
            roleDeepValidatorFactoryMock.Object,
            _backGroundJobOptions);

        // Act
        var results = await roleService.ReviewFailedRolesOperationsAsync();

        // Assert
        results.Should().BeEmpty();
    }


    [Fact]
    public async Task InsertRolesAsync_With_Source_Should_PassSourceToRepository()
    {
        // Arrange
        var roles = _fixture.CreateMany<RefRoleCsv>(2);

        var repository = new Mock<IRoleRepository>();

        var factory = Mock.Of<IRoleDeepValidatorFactory>();
        var roleService = new RoleService(null!, repository.Object, factory, _backGroundJobOptions);

        // Act
        await roleService.InsertRolesAsync(roles, "TESTSOURCE");

        // Assert
        repository.Verify(x => x.AddRolesAsync(roles, "TESTSOURCE"), Times.Once);
    }

    [Fact]
    public async Task RestRoleDuplicateCounter_When_AccountNumbersIsNull_Should_LogWarning_And_Return()
    {
        // Arrange
        var roleRepositoryMock = new Mock<IRoleRepository>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<RoleService>>();
        var validatorFactory = Mock.Of<IRoleDeepValidatorFactory>();

        var service = new RoleService(loggerMock.Object, roleRepositoryMock.Object, validatorFactory, _backGroundJobOptions);
        List<string>? accountNumbers = null;
        var email = "user@test.com";

        // Act
        await service.RestRoleDuplicateCounter(accountNumbers, email);

        // Assert
        roleRepositoryMock.VerifyNoOtherCalls();
        loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("No account numbers provided for user")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RestRoleDuplicateCounter_When_AccountNumbersIsEmpty_Should_LogWarning_And_Return()
    {
        // Arrange
        var roleRepositoryMock = new Mock<IRoleRepository>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<RoleService>>();
        var validatorFactory = Mock.Of<IRoleDeepValidatorFactory>();

        var service = new RoleService(loggerMock.Object, roleRepositoryMock.Object, validatorFactory, _backGroundJobOptions);
        var accountNumbers = new List<string>();
        var email = "user@test.com";

        // Act
        await service.RestRoleDuplicateCounter(accountNumbers, email);

        // Assert
        roleRepositoryMock.VerifyNoOtherCalls();
        loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("No account numbers provided for user")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RestRoleDuplicateCounter_When_NoRoleFound_Should_LogWarning_And_Skip_Update()
    {
        // Arrange
        var accountNumbers = new List<string> { "ACC1" };
        var email = "user@test.com";

        var roleRepositoryMock = new Mock<IRoleRepository>();
        roleRepositoryMock
            .Setup(r => r.GetPulseRole(email, "ACC1"))
            .ReturnsAsync((RoleEntity?)null);

        var loggerMock = new Mock<ILogger<RoleService>>();
        var validatorFactory = Mock.Of<IRoleDeepValidatorFactory>();

        var service = new RoleService(loggerMock.Object, roleRepositoryMock.Object, validatorFactory, _backGroundJobOptions);

        // Act
        await service.RestRoleDuplicateCounter(accountNumbers, email);

        // Assert
        roleRepositoryMock.Verify(r => r.GetPulseRole(email, "ACC1"), Times.Once);
        roleRepositoryMock.Verify(r => r.UpdatePulseRole(It.IsAny<RoleEntity>()), Times.Never);

        loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("No role found for user")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RestRoleDuplicateCounter_When_CounterGreaterThanZero_And_UpdateSucceeds_Should_Reset_And_LogInformation()
    {
        // Arrange
        var accountNumbers = new List<string> { "ACC1" };
        var email = "user@test.com";

        var roleEntity = new RoleEntity
        {
            RoleDuplicatesCounter = 3,
            ContactId = 12345,
            AccountId = 12345
        };

        var roleRepositoryMock = new Mock<IRoleRepository>();
        roleRepositoryMock
            .Setup(r => r.GetPulseRole(email, "ACC1"))
            .ReturnsAsync(roleEntity);
        roleRepositoryMock
            .Setup(r => r.UpdatePulseRole(roleEntity))
            .ReturnsAsync(true);

        var loggerMock = new Mock<ILogger<RoleService>>();
        var validatorFactory = Mock.Of<IRoleDeepValidatorFactory>();

        var service = new RoleService(loggerMock.Object, roleRepositoryMock.Object, validatorFactory, _backGroundJobOptions);

        // Act
        await service.RestRoleDuplicateCounter(accountNumbers, email);

        // Assert
        roleEntity.RoleDuplicatesCounter.Should().Be(0);
        roleRepositoryMock.Verify(r => r.UpdatePulseRole(roleEntity), Times.Once);

        loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Successfully reset RoleDuplicatesCounter")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    [Fact]
    public async Task RestRoleDuplicateCounter_When_CounterGreaterThanZero_And_UpdateFails_Should_LogError()
    {
        // Arrange
        var accountNumbers = new List<string> { "ACC1" };
        var email = "user@test.com";

        var roleEntity = new RoleEntity
        {
            RoleDuplicatesCounter = 2,
            ContactId = 12345,
            AccountId = 12345
        };

        var roleRepositoryMock = new Mock<IRoleRepository>();
        roleRepositoryMock
            .Setup(r => r.GetPulseRole(email, "ACC1"))
            .ReturnsAsync(roleEntity);
        roleRepositoryMock
            .Setup(r => r.UpdatePulseRole(roleEntity))
            .ReturnsAsync(false);

        var loggerMock = new Mock<ILogger<RoleService>>();
        var validatorFactory = Mock.Of<IRoleDeepValidatorFactory>();

        var service = new RoleService(loggerMock.Object, roleRepositoryMock.Object, validatorFactory, _backGroundJobOptions);

        // Act
        await service.RestRoleDuplicateCounter(accountNumbers, email);

        // Assert
        roleEntity.RoleDuplicatesCounter.Should().Be(0);
        roleRepositoryMock.Verify(r => r.UpdatePulseRole(roleEntity), Times.Once);

        loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Failed to update RoleDuplicatesCounter")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

}

using Application.Consts;
using Application.Enums;
using Application.Interfaces;
using Application.Interfaces.RuleValidators;
using Application.Models;
using Application.Options;
using Application.Services;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Pulse.Registry.Domain.Entities;
using Pulse.Registry.Domain.Entities.Accounts;

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

    #region CreateValidRolesOperationsAsync Tests

    [Fact]
    public async Task CreateValidRolesOperationsAsync_When_NoUnprocessedRoles_Should_ReturnEmptyList()
    {
        // Arrange
        var roleRepositoryMock = new Mock<IRoleRepository>();
        roleRepositoryMock.Setup(r => r.GetUnprocessedRoles())
            .Returns(new List<RefRoleEntity>());

        var validatorFactory = Mock.Of<IRoleDeepValidatorFactory>();
        var loggerMock = Mock.Of<ILogger<RoleService>>();

        var roleService = new RoleService(
            loggerMock,
            roleRepositoryMock.Object,
            validatorFactory,
            _backGroundJobOptions);

        // Act
        var results = await roleService.CreateValidRolesOperationsAsync();

        // Assert
        results.Should().BeEmpty();
        roleRepositoryMock.Verify(r => r.UpdateRefRoleAsync(It.IsAny<RefRoleEntity>()), Times.Never);
    }

    [Fact]
    public async Task CreateValidRolesOperationsAsync_When_ValidationFails_Should_ReturnFalse()
    {
        // Arrange
        var refRole = _fixture.Create<RefRoleEntity>();
        refRole.OperationType = OperationAction.Insert;
        var roles = new List<RefRoleEntity> { refRole };

        var roleRepositoryMock = new Mock<IRoleRepository>();
        roleRepositoryMock.Setup(r => r.GetUnprocessedRoles()).Returns(roles);

        var roleDeepValidatorMock = new Mock<IRoleDeepValidator>();
        roleDeepValidatorMock.Setup(v => v.Instantiate(It.IsAny<RefRoleEntity>()))
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.ContactShouldExistInPulseOrOperations())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.AccountShouldExistInPulseOrOperations())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.RoleShouldShouldNotExistInPulseOrOperations())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.TryAddOperation())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.Validate())
            .ReturnsAsync(false); // Validation fails

        var roleDeepValidatorFactoryMock = new Mock<IRoleDeepValidatorFactory>();
        roleDeepValidatorFactoryMock.Setup(f => f.Create())
            .Returns(roleDeepValidatorMock.Object);

        var roleService = new RoleService(
            Mock.Of<ILogger<RoleService>>(),
            roleRepositoryMock.Object,
            roleDeepValidatorFactoryMock.Object,
            _backGroundJobOptions);

        // Act
        var results = await roleService.CreateValidRolesOperationsAsync();

        // Assert
        results.Should().HaveCount(1);
        results.Should().Contain(false);
        roleRepositoryMock.Verify(r => r.UpdateRefRoleAsync(refRole), Times.Once);
    }

    [Fact]
    public async Task CreateValidRolesOperationsAsync_Should_Set_ValidationDate_Before_Processing()
    {
        // Arrange
        var refRole = _fixture.Create<RefRoleEntity>();
        refRole.OperationType = OperationAction.Insert;
        refRole.ValidationDate = null;
        var roles = new List<RefRoleEntity> { refRole };

        var roleRepositoryMock = new Mock<IRoleRepository>();
        roleRepositoryMock.Setup(r => r.GetUnprocessedRoles()).Returns(roles);

        var roleDeepValidatorMock = new Mock<IRoleDeepValidator>();
        roleDeepValidatorMock.Setup(v => v.Instantiate(It.IsAny<RefRoleEntity>()))
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.ContactShouldExistInPulseOrOperations())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.AccountShouldExistInPulseOrOperations())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.RoleShouldShouldNotExistInPulseOrOperations())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.TryAddOperation())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.Validate())
            .ReturnsAsync(true);

        var roleDeepValidatorFactoryMock = new Mock<IRoleDeepValidatorFactory>();
        roleDeepValidatorFactoryMock.Setup(f => f.Create())
            .Returns(roleDeepValidatorMock.Object);

        var beforeTest = DateTime.UtcNow;

        var roleService = new RoleService(
            Mock.Of<ILogger<RoleService>>(),
            roleRepositoryMock.Object,
            roleDeepValidatorFactoryMock.Object,
            _backGroundJobOptions);

        // Act
        await roleService.CreateValidRolesOperationsAsync();

        var afterTest = DateTime.UtcNow;

        // Assert
        refRole.ValidationDate.Should().NotBeNull();
        refRole.ValidationDate.Should().BeOnOrAfter(beforeTest);
        refRole.ValidationDate.Should().BeOnOrBefore(afterTest);
        roleRepositoryMock.Verify(r => r.UpdateRefRoleAsync(refRole), Times.Once);
    }

    [Fact]
    public async Task CreateValidRolesOperationsAsync_With_UnknownOperationType_Should_Skip_Validation()
    {
        // Arrange
        var refRole = _fixture.Create<RefRoleEntity>();
        refRole.OperationType = "UNKNOWN";
        var roles = new List<RefRoleEntity> { refRole };

        var roleRepositoryMock = new Mock<IRoleRepository>();
        roleRepositoryMock.Setup(r => r.GetUnprocessedRoles()).Returns(roles);

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
        var results = await roleService.CreateValidRolesOperationsAsync();

        // Assert
        results.Should().BeEmpty();
        roleRepositoryMock.Verify(r => r.UpdateRefRoleAsync(refRole), Times.Once);
        roleDeepValidatorMock.Verify(v => v.Validate(), Times.Never);
    }

    #endregion

    #region ReviewFailedRolesOperationsAsync Tests

    [Fact]
    public async Task ReviewFailedRolesOperationsAsync_With_StringOperationType_INSERT_Should_Work()
    {
        // Arrange
        var refRole = _fixture.Create<RefRoleEntity>();
        refRole.OperationType = "INSERT"; // String literal, not enum

        var roles = new List<RefRoleEntity> { refRole };

        var roleRepositoryMock = new Mock<IRoleRepository>();
        roleRepositoryMock.Setup(r => r.GetDeepValidationFailedRoles(_backGroundJobOptions.Value.NumberOfDaysToRetryFailedRoles))
            .ReturnsAsync(roles);

        var roleDeepValidatorMock = new Mock<IRoleDeepValidator>();
        roleDeepValidatorMock.Setup(v => v.Instantiate(It.IsAny<RefRoleEntity>()))
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.ContactShouldExistInPulseOrOperations())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.AccountShouldExistInPulseOrOperations())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.RoleShouldShouldNotExistInPulseOrOperations())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.TryAddOperation())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.Validate())
            .ReturnsAsync(true);

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
        results.Should().HaveCount(1);
        results.Should().Contain(true);
        roleDeepValidatorMock.Verify(v => v.Validate(), Times.Once);
    }

    [Fact]
    public async Task ReviewFailedRolesOperationsAsync_With_StringOperationType_DELETE_Should_Work()
    {
        // Arrange
        var refRole = _fixture.Create<RefRoleEntity>();
        refRole.OperationType = "DELETE"; // String literal, not enum

        var roles = new List<RefRoleEntity> { refRole };

        var roleRepositoryMock = new Mock<IRoleRepository>();
        roleRepositoryMock.Setup(r => r.GetDeepValidationFailedRoles(_backGroundJobOptions.Value.NumberOfDaysToRetryFailedRoles))
            .ReturnsAsync(roles);

        var roleDeepValidatorMock = new Mock<IRoleDeepValidator>();
        roleDeepValidatorMock.Setup(v => v.Instantiate(It.IsAny<RefRoleEntity>()))
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.ContactShouldExistInPulse())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.AccountShouldExistInPulse())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.RoleShouldExistInPulse())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.TryAddOperation())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.Validate())
            .ReturnsAsync(true);

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
        results.Should().HaveCount(1);
        results.Should().Contain(true);
        roleDeepValidatorMock.Verify(v => v.Validate(), Times.Once);
    }

    [Fact]
    public async Task ReviewFailedRolesOperationsAsync_When_ExceptionThrown_Should_LogError_And_ContinueProcessing()
    {
        // Arrange
        var role1 = _fixture.Create<RefRoleEntity>();
        role1.OperationType = "INSERT";
        var entityId1 = role1.EntityId; // Conserver l'EntityId généré

        var role2 = _fixture.Create<RefRoleEntity>();
        role2.OperationType = "DELETE";
        var entityId2 = role2.EntityId; // Conserver l'EntityId généré

        var roles = new List<RefRoleEntity> { role1, role2 };

        var roleRepositoryMock = new Mock<IRoleRepository>();
        roleRepositoryMock.Setup(r => r.GetDeepValidationFailedRoles(_backGroundJobOptions.Value.NumberOfDaysToRetryFailedRoles))
            .ReturnsAsync(roles);

        var roleDeepValidatorMock1 = new Mock<IRoleDeepValidator>();
        roleDeepValidatorMock1.Setup(v => v.Instantiate(It.IsAny<RefRoleEntity>()))
            .ThrowsAsync(new Exception("Validation error for ROLE1"));

        var roleDeepValidatorMock2 = new Mock<IRoleDeepValidator>();
        roleDeepValidatorMock2.Setup(v => v.Instantiate(It.IsAny<RefRoleEntity>()))
            .ReturnsAsync(roleDeepValidatorMock2.Object);
        roleDeepValidatorMock2.Setup(v => v.ContactShouldExistInPulse())
            .ReturnsAsync(roleDeepValidatorMock2.Object);
        roleDeepValidatorMock2.Setup(v => v.AccountShouldExistInPulse())
            .ReturnsAsync(roleDeepValidatorMock2.Object);
        roleDeepValidatorMock2.Setup(v => v.RoleShouldExistInPulse())
            .ReturnsAsync(roleDeepValidatorMock2.Object);
        roleDeepValidatorMock2.Setup(v => v.TryAddOperation())
            .ReturnsAsync(roleDeepValidatorMock2.Object);
        roleDeepValidatorMock2.Setup(v => v.Validate())
            .ReturnsAsync(true);

        var roleDeepValidatorFactoryMock = new Mock<IRoleDeepValidatorFactory>();
        roleDeepValidatorFactoryMock.SetupSequence(f => f.Create())
            .Returns(roleDeepValidatorMock1.Object)
            .Returns(roleDeepValidatorMock2.Object);

        var loggerMock = new Mock<ILogger<RoleService>>();

        var roleService = new RoleService(
            loggerMock.Object,
            roleRepositoryMock.Object,
            roleDeepValidatorFactoryMock.Object,
            _backGroundJobOptions);

        // Act
        var results = await roleService.ReviewFailedRolesOperationsAsync();

        // Assert
        results.Should().HaveCount(1);
        results.Should().Contain(true);

        // Verify error was logged for first role
        loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(entityId1.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ReviewFailedRolesOperationsAsync_When_ValidationFails_Should_ReturnFalse()
    {
        // Arrange
        var refRole = _fixture.Create<RefRoleEntity>();
        refRole.OperationType = "INSERT";

        var roles = new List<RefRoleEntity> { refRole };

        var roleRepositoryMock = new Mock<IRoleRepository>();
        roleRepositoryMock.Setup(r => r.GetDeepValidationFailedRoles(_backGroundJobOptions.Value.NumberOfDaysToRetryFailedRoles))
            .ReturnsAsync(roles);

        var roleDeepValidatorMock = new Mock<IRoleDeepValidator>();
        roleDeepValidatorMock.Setup(v => v.Instantiate(It.IsAny<RefRoleEntity>()))
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.ContactShouldExistInPulseOrOperations())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.AccountShouldExistInPulseOrOperations())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.RoleShouldShouldNotExistInPulseOrOperations())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.TryAddOperation())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.Setup(v => v.Validate())
            .ReturnsAsync(false); // Validation fails

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
        results.Should().HaveCount(1);
        results.Should().Contain(false);
    }

    #endregion

    #region RestRoleDuplicateCounter Tests

    [Fact]
    public async Task RestRoleDuplicateCounter_When_CounterAlreadyZero_Should_LogDebug_And_Skip_Update()
    {
        // Arrange
        var accountNumbers = new List<string> { "ACC1" };
        var email = "user@test.com";

        var roleEntity = new RoleEntity
        {
            RoleDuplicatesCounter = 0, // Already zero
            ContactId = 12345,
            AccountId = 12345
        };

        var roleRepositoryMock = new Mock<IRoleRepository>();
        roleRepositoryMock.Setup(r => r.GetPulseRole(email, "ACC1"))
            .ReturnsAsync(roleEntity);

        var loggerMock = new Mock<ILogger<RoleService>>();
        var validatorFactory = Mock.Of<IRoleDeepValidatorFactory>();

        var roleService = new RoleService(
            loggerMock.Object,
            roleRepositoryMock.Object,
            validatorFactory,
            _backGroundJobOptions);

        // Act
        await roleService.RestRoleDuplicateCounter(accountNumbers, email);

        // Assert
        roleRepositoryMock.Verify(r => r.UpdatePulseRole(It.IsAny<RoleEntity>()), Times.Never);

        loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Debug),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("RoleDuplicatesCounter already zero")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RestRoleDuplicateCounter_When_ExceptionDuringGet_Should_LogError_And_Continue()
    {
        // Arrange
        var accountNumbers = new List<string> { "ACC1", "ACC2" };
        var email = "user@test.com";

        var roleRepositoryMock = new Mock<IRoleRepository>();
        roleRepositoryMock.Setup(r => r.GetPulseRole(email, "ACC1"))
            .ThrowsAsync(new Exception("Database connection error"));

        var roleEntity2 = new RoleEntity
        {
            RoleDuplicatesCounter = 2,
            ContactId = 12345,
            AccountId = 12345
        };

        roleRepositoryMock.Setup(r => r.GetPulseRole(email, "ACC2"))
            .ReturnsAsync(roleEntity2);
        roleRepositoryMock.Setup(r => r.UpdatePulseRole(roleEntity2))
            .ReturnsAsync(true);

        var loggerMock = new Mock<ILogger<RoleService>>();
        var validatorFactory = Mock.Of<IRoleDeepValidatorFactory>();

        var roleService = new RoleService(
            loggerMock.Object,
            roleRepositoryMock.Object,
            validatorFactory,
            _backGroundJobOptions);

        // Act
        await roleService.RestRoleDuplicateCounter(accountNumbers, email);

        // Assert
        // Exception logged for ACC1
        loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("ACC1")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // ACC2 processed successfully
        roleRepositoryMock.Verify(r => r.UpdatePulseRole(roleEntity2), Times.Once);
    }

    [Fact]
    public async Task RestRoleDuplicateCounter_With_MultipleAccounts_Should_ProcessAll()
    {
        // Arrange
        var accountNumbers = new List<string> { "ACC1", "ACC2", "ACC3" };
        var email = "user@test.com";

        var roleEntity1 = new RoleEntity { RoleDuplicatesCounter = 5, ContactId = 1, AccountId = 1 };
        var roleEntity2 = new RoleEntity { RoleDuplicatesCounter = 0, ContactId = 2, AccountId = 2 };
        var roleEntity3 = new RoleEntity { RoleDuplicatesCounter = 3, ContactId = 3, AccountId = 3 };

        var roleRepositoryMock = new Mock<IRoleRepository>();
        roleRepositoryMock.Setup(r => r.GetPulseRole(email, "ACC1")).ReturnsAsync(roleEntity1);
        roleRepositoryMock.Setup(r => r.GetPulseRole(email, "ACC2")).ReturnsAsync(roleEntity2);
        roleRepositoryMock.Setup(r => r.GetPulseRole(email, "ACC3")).ReturnsAsync(roleEntity3);

        roleRepositoryMock.Setup(r => r.UpdatePulseRole(roleEntity1)).ReturnsAsync(true);
        roleRepositoryMock.Setup(r => r.UpdatePulseRole(roleEntity3)).ReturnsAsync(false); // Fails

        var loggerMock = new Mock<ILogger<RoleService>>();
        var validatorFactory = Mock.Of<IRoleDeepValidatorFactory>();

        var roleService = new RoleService(
            loggerMock.Object,
            roleRepositoryMock.Object,
            validatorFactory,
            _backGroundJobOptions);

        // Act
        await roleService.RestRoleDuplicateCounter(accountNumbers, email);

        // Assert
        roleEntity1.RoleDuplicatesCounter.Should().Be(0);
        roleEntity2.RoleDuplicatesCounter.Should().Be(0); // No change
        roleEntity3.RoleDuplicatesCounter.Should().Be(0);

        roleRepositoryMock.Verify(r => r.UpdatePulseRole(roleEntity1), Times.Once);
        roleRepositoryMock.Verify(r => r.UpdatePulseRole(roleEntity2), Times.Never); // Already zero
        roleRepositoryMock.Verify(r => r.UpdatePulseRole(roleEntity3), Times.Once);

        // Verify completion log
        loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Completed reset of RoleDuplicatesCounter")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RestRoleDuplicateCounter_When_ExceptionDuringUpdate_Should_LogError()
    {
        // Arrange
        var accountNumbers = new List<string> { "ACC1" };
        var email = "user@test.com";

        var roleEntity = new RoleEntity
        {
            RoleDuplicatesCounter = 5,
            ContactId = 12345,
            AccountId = 12345
        };

        var roleRepositoryMock = new Mock<IRoleRepository>();
        roleRepositoryMock.Setup(r => r.GetPulseRole(email, "ACC1"))
            .ReturnsAsync(roleEntity);
        roleRepositoryMock.Setup(r => r.UpdatePulseRole(roleEntity))
            .ThrowsAsync(new Exception("Update failed"));

        var loggerMock = new Mock<ILogger<RoleService>>();
        var validatorFactory = Mock.Of<IRoleDeepValidatorFactory>();

        var roleService = new RoleService(
            loggerMock.Object,
            roleRepositoryMock.Object,
            validatorFactory,
            _backGroundJobOptions);

        // Act
        await roleService.RestRoleDuplicateCounter(accountNumbers, email);

        // Assert
        roleEntity.RoleDuplicatesCounter.Should().Be(0);

        loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Exception resetting RoleDuplicatesCounter")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RestRoleDuplicateCounter_Should_Log_Start_And_Completion_Messages()
    {
        // Arrange
        var accountNumbers = new List<string> { "ACC1", "ACC2" };
        var email = "user@test.com";

        var roleEntity1 = new RoleEntity { RoleDuplicatesCounter = 1, ContactId = 1, AccountId = 1 };
        var roleEntity2 = new RoleEntity { RoleDuplicatesCounter = 2, ContactId = 2, AccountId = 2 };

        var roleRepositoryMock = new Mock<IRoleRepository>();
        roleRepositoryMock.Setup(r => r.GetPulseRole(email, "ACC1")).ReturnsAsync(roleEntity1);
        roleRepositoryMock.Setup(r => r.GetPulseRole(email, "ACC2")).ReturnsAsync(roleEntity2);
        roleRepositoryMock.Setup(r => r.UpdatePulseRole(It.IsAny<RoleEntity>())).ReturnsAsync(true);

        var loggerMock = new Mock<ILogger<RoleService>>();
        var validatorFactory = Mock.Of<IRoleDeepValidatorFactory>();

        var roleService = new RoleService(
            loggerMock.Object,
            roleRepositoryMock.Object,
            validatorFactory,
            _backGroundJobOptions);

        // Act
        await roleService.RestRoleDuplicateCounter(accountNumbers, email);

        // Assert
        // Verify starting log
        loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Starting reset of RoleDuplicatesCounter")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify completion log
        loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Completed reset of RoleDuplicatesCounter")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}

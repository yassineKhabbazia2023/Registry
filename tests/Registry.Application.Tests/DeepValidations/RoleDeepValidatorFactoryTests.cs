using System;
using Xunit;
using Moq;
using Application.Factories;
using Application.Interfaces;
using Application.Services;
using Application.DeepValidations;

public class RoleDeepValidatorFactoryTests
{
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IOperationRepository> _operationRepositoryMock;
    private readonly Mock<IDeepValidationRepository> _deepValidationRepositoryMock;
    private readonly Mock<IContactRepository> _contactRepositoryMock;
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly RoleDeepValidatorFactory _factory;

    public RoleDeepValidatorFactoryTests()
    {
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _operationRepositoryMock = new Mock<IOperationRepository>();
        _deepValidationRepositoryMock = new Mock<IDeepValidationRepository>();
        _contactRepositoryMock = new Mock<IContactRepository>();
        _accountRepositoryMock = new Mock<IAccountRepository>();

        _factory = new RoleDeepValidatorFactory(
            _roleRepositoryMock.Object,
            _operationRepositoryMock.Object,
            _deepValidationRepositoryMock.Object,
            _contactRepositoryMock.Object,
            _accountRepositoryMock.Object);
    }

    [Fact]
    public void Create_ShouldReturnNewInstanceOfRoleDeepValidator()
    {
        var result = _factory.Create();
        Assert.NotNull(result);
        Assert.IsType<RoleDeepValidator>(result);
    }
}

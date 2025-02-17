//// <copyright file="RoleDeepValidatorTests.cs" company="Pulse">
//// Copyright (c) Pulse. All rights reserved.
//// </copyright>

using Moq;
using Application.Interfaces;
using Pulse.Registry.Domain.Entities;
using Application.DeepValidations;


namespace Registry.Application.Tests.DeepValidations
{
    public class RoleDeepValidatorTests
    {
        private readonly Mock<IRoleRepository> _roleRepositoryMock;
        private readonly Mock<IOperationRepository> _operationRepositoryMock;
        private readonly Mock<IDeepValidationRepository> _deepValidationRepositoryMock;
        private readonly Mock<IContactRepository> _contactRepositoryMock;
        private readonly Mock<IAccountRepository> _accountRepositoryMock;
        private readonly RoleDeepValidator _validator;
        private readonly RefRoleEntity _testRoleEntity;

        public RoleDeepValidatorTests()
        {
            _roleRepositoryMock = new Mock<IRoleRepository>();
            _operationRepositoryMock = new Mock<IOperationRepository>();
            _deepValidationRepositoryMock = new Mock<IDeepValidationRepository>();
            _contactRepositoryMock = new Mock<IContactRepository>();
            _accountRepositoryMock = new Mock<IAccountRepository>();

            _validator = new RoleDeepValidator(
                _roleRepositoryMock.Object,
                _operationRepositoryMock.Object,
                _deepValidationRepositoryMock.Object,
                _contactRepositoryMock.Object,
                _accountRepositoryMock.Object);

            _testRoleEntity = new RefRoleEntity
            {
                ContactEmail = "test@example.com",
                AccountNumber = "123456",
                OperationType = "INSERT",
                EntityId = Guid.NewGuid(),
            };
        }

        [Fact]
        public async Task Instantiate_ShouldSetRefRoleAndReturnValidator()
        {
            var result = await _validator.Instantiate(_testRoleEntity);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ContactShouldExistsInPulse_ShouldValidateContactExistence()
        {
            _contactRepositoryMock.Setup(repo => repo.IsContactExisted(_testRoleEntity.ContactEmail,null)).ReturnsAsync(true);
            await _validator.Instantiate(_testRoleEntity);
            var result = await _validator.ContactShouldExistInPulse();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ContactShouldExistsInPulseOrOperations_ShouldValidateContact()
        {
            _contactRepositoryMock.Setup(repo => repo.IsContactExisted(_testRoleEntity.ContactEmail,null)).ReturnsAsync(false);
            _contactRepositoryMock.Setup(repo => repo.DoesContactExistInOperations(_testRoleEntity.ContactEmail, "INSERT", "READY")).ReturnsAsync(true);

            await _validator.Instantiate(_testRoleEntity);
            var result = await _validator.ContactShouldExistInPulseOrOperations();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task AccountShouldExistsInPulse_ShouldValidateAccountExistence()
        {
            _accountRepositoryMock.Setup(repo => repo.DoesAccountExist(_testRoleEntity.AccountNumber)).ReturnsAsync(true);
            await _validator.Instantiate(_testRoleEntity);
            var result = await _validator.AccountShouldExistInPulse();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task AccountShouldExistsInPulseOrOperations_ShouldValidateAccount()
        {
            _accountRepositoryMock.Setup(repo => repo.DoesAccountExist(_testRoleEntity.AccountNumber)).ReturnsAsync(false);
            _accountRepositoryMock.Setup(repo => repo.DoesAccountExistInOperations(_testRoleEntity.AccountNumber, "INSERT", "READY")).ReturnsAsync(true);

            await _validator.Instantiate(_testRoleEntity);
            var result = await _validator.AccountShouldExistInPulseOrOperations();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task RoleShouldShouldNotExistsInPulse_ShouldValidateRoleAbsence()
        {
            _roleRepositoryMock.Setup(repo => repo.DoesRoleExistInPulse(_testRoleEntity.AccountNumber, _testRoleEntity.ContactEmail)).Returns(false);
            await _validator.Instantiate(_testRoleEntity);
            var result = await _validator.RoleShouldShouldNotExistInPulseOrOperations();
            Assert.NotNull(result);
        }

        // Cas de test a revoir

        //[Fact]
        //public async Task RoleShouldExistsInPulse_ShouldValidateRolePresence()
        //{
        //    _roleRepositoryMock.Setup(repo => repo.DoesRoleExistInPulse(_testRoleEntity.AccountNumber, _testRoleEntity.ContactEmail)).Returns(true);
        //    await _validator.Instantiate(_testRoleEntity);
        //    var result = await _validator.RoleShouldExistInPulse();
        //    Assert.NotNull(result);
        //}

        [Fact]
        public async Task TryAddOperation_ShouldInvokeOperationRepository()
        {
            await _validator.Instantiate(_testRoleEntity);
            var result = await _validator.TryAddOperation();
            _operationRepositoryMock.Verify(repo => repo.AddOperationAsync(It.IsAny<RegOperationEntity>()), Times.Once);
        }

        [Fact]
        public async Task Validate_ShouldReturnCorrectValidationState()
        {
            await _validator.Instantiate(_testRoleEntity);
            var result = await _validator.Validate();
            Assert.True(result);
        }
    }

}

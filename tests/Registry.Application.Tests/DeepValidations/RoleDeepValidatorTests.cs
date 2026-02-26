using Application.Consts;
using Application.Enums;
using Application.Interfaces;
using Application.Requests;
using Application.DeepValidations;
using Moq;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;
using Application.Models.Contacts;
using Pulse.Registry.Domain.Entities.Audits;
using Pulse.Registry.Domain.Entities.Accounts;

namespace Registry.Infrastructure.Tests.DeepValidations;

public class RoleDeepValidatorTests
{
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IOperationRepository> _operationRepositoryMock;
    private readonly Mock<IDeepValidationRepository> _deepValidationRepositoryMock;
    private readonly Mock<IContactRepository> _contactRepositoryMock;
    private readonly Mock<IAccountRepository> _accountRepositoryMock;

    private RoleDeepValidator CreateValidator() =>
        new RoleDeepValidator(
            _roleRepositoryMock.Object,
            _operationRepositoryMock.Object,
            _deepValidationRepositoryMock.Object,
            _contactRepositoryMock.Object,
            _accountRepositoryMock.Object);

    private RefRoleEntity _validRefRoleEntity = new RefRoleEntity
    {
        AccountNumber = "TEST_ACC",
        ContactEmail = "test@example.com",
        OperationType = OperationAction.Insert,
        EntityId = Guid.NewGuid()
    };

    public RoleDeepValidatorTests()
    {
        _roleRepositoryMock = new Mock<IRoleRepository>(MockBehavior.Strict);
        _operationRepositoryMock = new Mock<IOperationRepository>(MockBehavior.Strict);
        _deepValidationRepositoryMock = new Mock<IDeepValidationRepository>(MockBehavior.Strict);
        _contactRepositoryMock = new Mock<IContactRepository>(MockBehavior.Strict);
        _accountRepositoryMock = new Mock<IAccountRepository>(MockBehavior.Strict);
    }

    #region Instantiate Tests

    [Fact]
    public async Task Instantiate_ShouldThrowArgumentNullException_IfRefRoleIsNull()
    {
        // Arrange
        var validator = CreateValidator();

        // Act
        Func<Task> act = async () => await validator.Instantiate(null!);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task Instantiate_ShouldSetRefRoleAndReturnValidator()
    {
        // Arrange
        var validator = CreateValidator();

        // Act
        var result = await validator.Instantiate(_validRefRoleEntity);

        // Assert
        Assert.NotNull(result);
        Assert.Same(validator, result);
    }

    #endregion

    #region ContactShouldExistInPulse Tests

    [Fact]
    public async Task ContactShouldExistInPulse_WhenContactExists_ValidationRemainsTrue()
    {
        // Arrange
        _contactRepositoryMock
            .Setup(r => r.DoesContactExistByEmailOrIdAsync(_validRefRoleEntity.ContactEmail, null))
            .ReturnsAsync(true);

        var validator = CreateValidator();
        await validator.Instantiate(_validRefRoleEntity);

        // Act
        var result = await validator.ContactShouldExistInPulse();
        bool isValid = await result.Validate();

        // Assert
        Assert.Same(validator, result);
        Assert.True(isValid);
        _contactRepositoryMock.VerifyAll();
    }

    [Fact]
    public async Task ContactShouldExistInPulse_WhenContactDoesNotExist_AddsDeepValidationAndIsInvalid()
    {
        // Arrange
        _contactRepositoryMock
            .Setup(r => r.DoesContactExistByEmailOrIdAsync(_validRefRoleEntity.ContactEmail, null))
            .ReturnsAsync(false);

        _deepValidationRepositoryMock
            .Setup(d => d.DoesDeepValidationLineExistsAsync(_validRefRoleEntity.EntityId, OperationCategory.ROLE))
            .ReturnsAsync(false);
        _deepValidationRepositoryMock
            .Setup(d => d.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()))
            .ReturnsAsync(true);

        var validator = CreateValidator();
        await validator.Instantiate(_validRefRoleEntity);

        // Act
        var result = await validator.ContactShouldExistInPulse();
        bool isValid = await result.Validate();

        // Assert
        Assert.False(isValid);
        _contactRepositoryMock.VerifyAll();
        _deepValidationRepositoryMock.VerifyAll();
    }

    #endregion

    #region ContactShouldExistInPulseOrOperations Tests

    [Fact]
    public async Task ContactShouldExistInPulseOrOperations_IfEitherExists_ValidatorRemainsValid()
    {
        _contactRepositoryMock
            .Setup(r => r.DoesContactExistByEmailOrIdAsync(_validRefRoleEntity.ContactEmail, null))
            .ReturnsAsync(false);
        _operationRepositoryMock
            .Setup(r => r.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.CONTACT,
                _validRefRoleEntity.ContactEmail,
                false,
                null))
            .ReturnsAsync(new List<RegOperationEntity> {
                    new RegOperationEntity {
                        Operation = OperationAction.Insert,
                        ProcessStatus = ProcessStatus.Ready,
                        EntityId = _validRefRoleEntity.EntityId,
                        ApprovalStatus = ApprovalStatus.Approved

                    }
            });

        var validator = CreateValidator();
        await validator.Instantiate(_validRefRoleEntity);

        // Act
        var result = await validator.ContactShouldExistInPulseOrOperations();
        var isValid = await result.Validate();

        // Assert
        Assert.True(isValid);
        _contactRepositoryMock.VerifyAll();
        _operationRepositoryMock.VerifyAll();
        _deepValidationRepositoryMock.Verify(
            d => d.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()),
            Times.Never);
    }

    [Fact]
    public async Task ContactShouldExistInPulseOrOperations_IfMissingEverywhere_AddsDeepValidationAndInvalid()
    {
        _contactRepositoryMock
            .Setup(r => r.DoesContactExistByEmailOrIdAsync(_validRefRoleEntity.ContactEmail, null))
            .ReturnsAsync(false);

        _operationRepositoryMock
            .Setup(r => r.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.CONTACT,
                _validRefRoleEntity.ContactEmail,
                false,
                null))
            .ReturnsAsync(Array.Empty<RegOperationEntity>());

        _deepValidationRepositoryMock
            .Setup(d => d.DoesDeepValidationLineExistsAsync(_validRefRoleEntity.EntityId, OperationCategory.ROLE))
            .ReturnsAsync(false);
        _deepValidationRepositoryMock
            .Setup(d => d.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()))
            .ReturnsAsync(true);

        var validator = CreateValidator();
        await validator.Instantiate(_validRefRoleEntity);

        // Act
        var result = await validator.ContactShouldExistInPulseOrOperations();
        var isValid = await result.Validate();

        // Assert
        Assert.False(isValid);
        _contactRepositoryMock.VerifyAll();
        _operationRepositoryMock.VerifyAll();
        _deepValidationRepositoryMock.VerifyAll();
    }

    #endregion

    #region AccountShouldExistInPulse Tests

    [Fact]
    public async Task AccountShouldExistInPulse_WhenPulseHasAccount_RemainsValid()
    {
        // Arrange
        _accountRepositoryMock
            .Setup(a => a.DoesAccountExist(_validRefRoleEntity.AccountNumber))
            .ReturnsAsync(true);

        var validator = CreateValidator();
        await validator.Instantiate(_validRefRoleEntity);

        // Act
        var result = await validator.AccountShouldExistInPulse();
        var isValid = await result.Validate();

        // Assert
        Assert.True(isValid);
        _accountRepositoryMock.VerifyAll();
    }

    [Fact]
    public async Task AccountShouldExistInPulse_WhenPulseMissingAccount_AddsDeepValidationAndInvalid()
    {
        // Arrange
        _accountRepositoryMock
            .Setup(a => a.DoesAccountExist(_validRefRoleEntity.AccountNumber))
            .ReturnsAsync(false);

        _deepValidationRepositoryMock
            .Setup(d => d.DoesDeepValidationLineExistsAsync(_validRefRoleEntity.EntityId, OperationCategory.ROLE))
            .ReturnsAsync(false);
        _deepValidationRepositoryMock
            .Setup(d => d.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()))
            .ReturnsAsync(true);

        var validator = CreateValidator();
        await validator.Instantiate(_validRefRoleEntity);

        // Act
        var result = await validator.AccountShouldExistInPulse();
        var isValid = await result.Validate();

        // Assert
        Assert.False(isValid);
        _accountRepositoryMock.VerifyAll();
        _deepValidationRepositoryMock.VerifyAll();
    }

    #endregion

    #region AccountShouldExistInPulseOrOperations Tests

    [Fact]
    public async Task AccountShouldExistInPulseOrOperations_WhenInOperations_StaysValid()
    {
        // Arrange
        _accountRepositoryMock
            .Setup(a => a.DoesAccountExist(_validRefRoleEntity.AccountNumber))
            .ReturnsAsync(false);

        // Simulate account found in Insert, Ready operation
        _operationRepositoryMock
            .Setup(r => r.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.ACCOUNT,
                _validRefRoleEntity.AccountNumber,
                false,
                null))
            .ReturnsAsync(new List<RegOperationEntity>
            {
                    new RegOperationEntity
                    {
                        Operation = OperationAction.Insert,
                        ProcessStatus = ProcessStatus.Ready,
                        EntityId = _validRefRoleEntity.EntityId,
                        ApprovalStatus = ApprovalStatus.Approved

                    }
            });

        var validator = CreateValidator();
        await validator.Instantiate(_validRefRoleEntity);

        // Act
        var result = await validator.AccountShouldExistInPulseOrOperations();
        var isValid = await result.Validate();

        // Assert
        Assert.True(isValid);
        _accountRepositoryMock.VerifyAll();
        _operationRepositoryMock.VerifyAll();
        _deepValidationRepositoryMock.Verify(
            d => d.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()),
            Times.Never);
    }

    [Fact]
    public async Task AccountShouldExistInPulseOrOperations_IfMissingEverywhere_AddsDeepValidationAndInvalid()
    {
        // Arrange
        _accountRepositoryMock
            .Setup(a => a.DoesAccountExist(_validRefRoleEntity.AccountNumber))
            .ReturnsAsync(false);

        _operationRepositoryMock
            .Setup(r => r.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.ACCOUNT,
                _validRefRoleEntity.AccountNumber,
                false,
                null))
            .ReturnsAsync(Array.Empty<RegOperationEntity>());

        _deepValidationRepositoryMock
            .Setup(d => d.DoesDeepValidationLineExistsAsync(_validRefRoleEntity.EntityId, OperationCategory.ROLE))
            .ReturnsAsync(false);
        _deepValidationRepositoryMock
            .Setup(d => d.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()))
            .ReturnsAsync(true);

        var validator = CreateValidator();
        await validator.Instantiate(_validRefRoleEntity);

        // Act
        var result = await validator.AccountShouldExistInPulseOrOperations();
        var isValid = await result.Validate();

        // Assert
        Assert.False(isValid);
        _accountRepositoryMock.VerifyAll();
        _operationRepositoryMock.VerifyAll();
        _deepValidationRepositoryMock.VerifyAll();
    }

    #endregion

    #region RoleShouldShouldNotExistInPulseOrOperations Tests

    [Fact]
    public async Task RoleShouldShouldNotExistInPulseOrOperations_WhenRoleMissingEverywhere_StaysValid()
    {
        // Arrange
        _roleRepositoryMock
            .Setup(r => r.DoesRoleExistInPulse(_validRefRoleEntity.AccountNumber, _validRefRoleEntity.ContactEmail))
            .Returns(false);

        // No operations found for role
        _operationRepositoryMock
            .Setup(r => r.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.ROLE,
                _validRefRoleEntity.AccountNumber,
                false,
                _validRefRoleEntity.ContactEmail))
            .ReturnsAsync(Array.Empty<RegOperationEntity>());

        var validator = CreateValidator();
        await validator.Instantiate(_validRefRoleEntity);

        // Act
        var result = await validator.RoleShouldShouldNotExistInPulseOrOperations();
        var isValid = await result.Validate();

        // Assert
        Assert.True(isValid);
        _roleRepositoryMock.VerifyAll();
        _operationRepositoryMock.VerifyAll();
    }

    [Fact]
    public async Task RoleShouldShouldNotExistInPulseOrOperations_WhenRoleAlreadyExists_SetsInvalid()
    {
        // Arrange
        _roleRepositoryMock
            .Setup(r => r.DoesRoleExistInPulse(_validRefRoleEntity.AccountNumber, _validRefRoleEntity.ContactEmail))
            .Returns(true);

        _roleRepositoryMock.Setup(r => r.GetPulseRole(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((email, account) =>
            {
                Assert.Equal(_validRefRoleEntity.ContactEmail, email);
                Assert.Equal(_validRefRoleEntity.AccountNumber, account);
            })
        .ReturnsAsync(It.IsAny<RoleEntity>());

        _operationRepositoryMock.Setup(o => o.FetchOperationsByCriteriaAsync(It.IsAny<OperationSearchCriteria>(), It.IsAny<OperationStrategyType>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<string>()))
            .Callback<OperationSearchCriteria, OperationStrategyType, string, bool?, string>((criteria, strategy, account, isDeep, email) =>
            {
                Assert.Equal(OperationStrategyType.ROLE, strategy);
                Assert.Equal(_validRefRoleEntity.AccountNumber, account);
                Assert.Equal(_validRefRoleEntity.ContactEmail, email);
            }).ReturnsAsync(new List<RegOperationEntity> { new RegOperationEntity() {
                EntityId = _validRefRoleEntity.EntityId,
                Operation = OperationAction.Insert,
                ProcessStatus = ProcessStatus.Ready,
                ApprovalStatus = ApprovalStatus.Approved
             } });


        var validator = CreateValidator();
        await validator.Instantiate(_validRefRoleEntity);

        // Act
        await validator.RoleShouldShouldNotExistInPulseOrOperations();
        var isValid = await validator.Validate();

        // Assert
        Assert.False(isValid);
        _roleRepositoryMock.VerifyAll();
    }

        [Fact]
        public async Task RoleShouldShouldNotExistInPulseOrOperations_WhenContactFlagDiffers_ShouldStayValid()
        {
            // Arrange
            var refRole = new RefRoleEntity
            {
                AccountNumber = _validRefRoleEntity.AccountNumber,
                ContactEmail = _validRefRoleEntity.ContactEmail,
                OperationType = _validRefRoleEntity.OperationType,
                EntityId = _validRefRoleEntity.EntityId,
                ContactFlagPortailFactures = true
            };

            _roleRepositoryMock
                .Setup(r => r.DoesRoleExistInPulse(refRole.AccountNumber, refRole.ContactEmail))
                .Returns(true);

            _roleRepositoryMock
                .Setup(r => r.GetPulseRole(refRole.ContactEmail, refRole.AccountNumber))
                .ReturnsAsync(new RoleEntity
                {
                    AccountId = 1,
                    ContactId = 2,
                    ContactFlagPortailFactures = false
                });

            _operationRepositoryMock
                .Setup(r => r.FetchOperationsByCriteriaAsync(
                    It.IsAny<OperationSearchCriteria>(),
                    OperationStrategyType.ROLE,
                    refRole.AccountNumber,
                    false,
                    refRole.ContactEmail))
                .ReturnsAsync(Array.Empty<RegOperationEntity>());

            var validator = CreateValidator();
            await validator.Instantiate(refRole);

            // Act
            await validator.RoleShouldShouldNotExistInPulseOrOperations();
            var isValid = await validator.Validate();

            // Assert
            Assert.True(isValid);
            _roleRepositoryMock.VerifyAll();
            _operationRepositoryMock.VerifyAll();
        }

    [Fact]
    public async Task RoleShouldShouldNotExistInPulseOrOperations_WhenCollaboratorWithDescriptionAndNoPendingDuplicate_StaysValid()
    {
        // Arrange
        var refRole = new RefRoleEntity
        {
            AccountNumber = _validRefRoleEntity.AccountNumber,
            ContactEmail = _validRefRoleEntity.ContactEmail,
            OperationType = OperationAction.Insert,
            EntityId = Guid.NewGuid(),
            Description = "CLP"
        };

        _contactRepositoryMock
            .Setup(c => c.GetContactByEmailOrIdAsync(refRole.ContactEmail, null))
            .ReturnsAsync(new Contact { ContactId = 1, Email = refRole.ContactEmail, Type = "collaborator" });
        _contactRepositoryMock
            .Setup(c => c.GetRefContactByEmailAsync(refRole.ContactEmail))
            .ReturnsAsync(new RefContactEntity { Email = refRole.ContactEmail, IsCustomer = false });

        _operationRepositoryMock
            .Setup(r => r.DoesRoleInsertOperationExistAsync(refRole.AccountNumber, refRole.ContactEmail, "CLP"))
            .ReturnsAsync(false);

        var validator = CreateValidator();
        await validator.Instantiate(refRole);

        // Act
        await validator.RoleShouldShouldNotExistInPulseOrOperations();
        var isValid = await validator.Validate();

        // Assert
        Assert.True(isValid);
        _operationRepositoryMock.Verify(r => r.DoesRoleInsertOperationExistAsync(refRole.AccountNumber, refRole.ContactEmail, "CLP"), Times.Once);
    }

    [Fact]
    public async Task RoleShouldShouldNotExistInPulseOrOperations_WhenCollaboratorWithDescriptionAndPendingDuplicate_SetsInvalid()
    {
        // Arrange
        var refRole = new RefRoleEntity
        {
            AccountNumber = _validRefRoleEntity.AccountNumber,
            ContactEmail = _validRefRoleEntity.ContactEmail,
            OperationType = OperationAction.Insert,
            EntityId = Guid.NewGuid(),
            Description = "CLP"
        };

        _contactRepositoryMock
            .Setup(c => c.GetContactByEmailOrIdAsync(refRole.ContactEmail, null))
            .ReturnsAsync(new Contact { ContactId = 1, Email = refRole.ContactEmail, Type = "collaborator" });
        _contactRepositoryMock
            .Setup(c => c.GetRefContactByEmailAsync(refRole.ContactEmail))
            .ReturnsAsync(new RefContactEntity { Email = refRole.ContactEmail, IsCustomer = false });

        _operationRepositoryMock
            .Setup(r => r.DoesRoleInsertOperationExistAsync(refRole.AccountNumber, refRole.ContactEmail, "CLP"))
            .ReturnsAsync(true);

        var validator = CreateValidator();
        await validator.Instantiate(refRole);

        // Act
        await validator.RoleShouldShouldNotExistInPulseOrOperations();
        var isValid = await validator.Validate();

        // Assert
        Assert.False(isValid);
        _operationRepositoryMock.Verify(r => r.DoesRoleInsertOperationExistAsync(refRole.AccountNumber, refRole.ContactEmail, "CLP"), Times.Once);
    }

    [Fact]
    public async Task RoleShouldShouldNotExistInPulseOrOperations_WhenCustomerWithDescription_UsesExistingBehavior()
    {
        // Arrange
        var refRole = new RefRoleEntity
        {
            AccountNumber = _validRefRoleEntity.AccountNumber,
            ContactEmail = _validRefRoleEntity.ContactEmail,
            OperationType = OperationAction.Insert,
            EntityId = Guid.NewGuid(),
            Description = "CLP"
        };

        _contactRepositoryMock
            .Setup(c => c.GetContactByEmailOrIdAsync(refRole.ContactEmail, null))
            .ReturnsAsync(new Contact { ContactId = 1, Email = refRole.ContactEmail, Type = "customer" });
        _contactRepositoryMock
            .Setup(c => c.GetRefContactByEmailAsync(refRole.ContactEmail))
            .ReturnsAsync(new RefContactEntity { Email = refRole.ContactEmail, IsCustomer = true });

        _roleRepositoryMock
            .Setup(r => r.DoesRoleExistInPulse(refRole.AccountNumber, refRole.ContactEmail))
            .Returns(false);

        _operationRepositoryMock
            .Setup(r => r.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.ROLE,
                refRole.AccountNumber,
                false,
                refRole.ContactEmail))
            .ReturnsAsync(Array.Empty<RegOperationEntity>());

        var validator = CreateValidator();
        await validator.Instantiate(refRole);

        // Act
        await validator.RoleShouldShouldNotExistInPulseOrOperations();
        var isValid = await validator.Validate();

        // Assert
        Assert.True(isValid);
        _operationRepositoryMock.Verify(r => r.DoesRoleInsertOperationExistAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _roleRepositoryMock.Verify(r => r.DoesRoleExistInPulse(refRole.AccountNumber, refRole.ContactEmail), Times.Once);
    }

    [Fact]
    public async Task RoleShouldShouldNotExistInPulseOrOperations_WhenNoDescription_UsesExistingBehavior()
    {
        // Arrange
        var refRole = new RefRoleEntity
        {
            AccountNumber = _validRefRoleEntity.AccountNumber,
            ContactEmail = _validRefRoleEntity.ContactEmail,
            OperationType = OperationAction.Insert,
            EntityId = Guid.NewGuid(),
            Description = null
        };

        _roleRepositoryMock
            .Setup(r => r.DoesRoleExistInPulse(refRole.AccountNumber, refRole.ContactEmail))
            .Returns(false);

        _operationRepositoryMock
            .Setup(r => r.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.ROLE,
                refRole.AccountNumber,
                false,
                refRole.ContactEmail))
            .ReturnsAsync(Array.Empty<RegOperationEntity>());

        var validator = CreateValidator();
        await validator.Instantiate(refRole);

        // Act
        await validator.RoleShouldShouldNotExistInPulseOrOperations();
        var isValid = await validator.Validate();

        // Assert
        Assert.True(isValid);
        _roleRepositoryMock.Verify(r => r.DoesRoleExistInPulse(refRole.AccountNumber, refRole.ContactEmail), Times.Once);
        _operationRepositoryMock.Verify(r => r.FetchOperationsByCriteriaAsync(
            It.IsAny<OperationSearchCriteria>(),
            OperationStrategyType.ROLE,
            refRole.AccountNumber,
            false,
            refRole.ContactEmail), Times.Once);
        _operationRepositoryMock.Verify(r => r.DoesRoleInsertOperationExistAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

        #endregion

    #region RoleShouldExistInPulse Tests

    [Fact]
    public async Task RoleShouldExistInPulse_WhenItDoesNotExist_AddsDeepValidationAndInvalid()
    {
        // Arrange
        _roleRepositoryMock
            .Setup(r => r.DoesRoleExistInPulse(_validRefRoleEntity.AccountNumber, _validRefRoleEntity.ContactEmail))
            .Returns(false);

        _deepValidationRepositoryMock
            .Setup(d => d.DoesDeepValidationLineExistsAsync(_validRefRoleEntity.EntityId, OperationCategory.ROLE))
            .ReturnsAsync(false);
        _deepValidationRepositoryMock
            .Setup(d => d.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()))
            .ReturnsAsync(true);

        var validator = CreateValidator();
        await validator.Instantiate(_validRefRoleEntity);

        //Act
        var result = await validator.RoleShouldExistInPulse();
        var isValid = await validator.Validate();

        //Assert
        Assert.False(isValid);
        _roleRepositoryMock.VerifyAll();
        _deepValidationRepositoryMock.VerifyAll();
    }

    [Fact]
    public async Task RoleShouldExistInPulse_WhenItExists_StaysValid()
    {
        // Arrange
        _roleRepositoryMock
            .Setup(r => r.DoesRoleExistInPulse(_validRefRoleEntity.AccountNumber, _validRefRoleEntity.ContactEmail))
            .Returns(true);
        _roleRepositoryMock.Setup(r => r.GetPulseRole(_validRefRoleEntity.ContactEmail, _validRefRoleEntity.AccountNumber))
            .ReturnsAsync(It.IsAny<RoleEntity>());

        var validator = CreateValidator();
        await validator.Instantiate(_validRefRoleEntity);

        // Act
        await validator.RoleShouldExistInPulse();
        bool isValid = await validator.Validate();

            // Assert
            Assert.True(isValid);
            _roleRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task RoleShouldExistInPulse_WhenContactFlagDiffers_ShouldAddDeepValidation()
        {
            // Arrange
            var refRole = new RefRoleEntity
            {
                AccountNumber = _validRefRoleEntity.AccountNumber,
                ContactEmail = _validRefRoleEntity.ContactEmail,
                OperationType = _validRefRoleEntity.OperationType,
                EntityId = _validRefRoleEntity.EntityId,
                ContactFlagPortailFactures = true
            };

            _roleRepositoryMock
                .Setup(r => r.DoesRoleExistInPulse(refRole.AccountNumber, refRole.ContactEmail))
                .Returns(true);

            _roleRepositoryMock
                .Setup(r => r.GetPulseRole(refRole.ContactEmail, refRole.AccountNumber))
                .ReturnsAsync(new RoleEntity
                {
                    AccountId = 1,
                    ContactId = 2,
                    ContactFlagPortailFactures = false
                });

            _deepValidationRepositoryMock
                .Setup(d => d.DoesDeepValidationLineExistsAsync(refRole.EntityId, OperationCategory.ROLE))
                .ReturnsAsync(false);
            _deepValidationRepositoryMock
                .Setup(d => d.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()))
                .ReturnsAsync(true);

            var validator = CreateValidator();
            await validator.Instantiate(refRole);

            // Act
            await validator.RoleShouldExistInPulse();
            var isValid = await validator.Validate();

            // Assert
            Assert.False(isValid);
            _roleRepositoryMock.VerifyAll();
            _deepValidationRepositoryMock.VerifyAll();
        }

    #endregion

    #region TryAddOperation Tests

    [Fact]
    public async Task TryAddOperation_WhenValid_CreatesOperationEntity()
    {
        // Arrange
        var validator = CreateValidator();
        await validator.Instantiate(_validRefRoleEntity);

            _roleRepositoryMock
                .Setup(r => r.GetPulseRole(_validRefRoleEntity.ContactEmail, _validRefRoleEntity.AccountNumber))
                .ReturnsAsync((RoleEntity?)null);

            _contactRepositoryMock
                .Setup(c => c.GetContactByEmailOrIdAsync(_validRefRoleEntity.ContactEmail, null))
                .ReturnsAsync(new Contact { ContactId = 101, Email = _validRefRoleEntity.ContactEmail });

        _contactRepositoryMock
            .Setup(c => c.GetRefContactByEmailAsync(_validRefRoleEntity.ContactEmail))
            .ReturnsAsync(new RefContactEntity { Email = _validRefRoleEntity.ContactEmail });

        _operationRepositoryMock
            .Setup(r => r.CreateOperationAsync(It.IsAny<RegOperationEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await validator.TryAddOperation();
        var isValid = await validator.Validate();

        // Assert
        Assert.Same(validator, result);
        Assert.True(isValid);
        _operationRepositoryMock.Verify(r => r.CreateOperationAsync(It.Is<RegOperationEntity>(
            op => op.Operation == _validRefRoleEntity.OperationType &&
                  op.Type == OperationCategory.ROLE &&
                  op.EntityId == _validRefRoleEntity.EntityId)));
    }

    [Fact]
    public async Task TryAddOperation_WhenInvalid_SkipsCreation()
    {
        // Arrange
        var validator = CreateValidator();
        await validator.Instantiate(_validRefRoleEntity);

        _accountRepositoryMock
            .Setup(a => a.DoesAccountExist(_validRefRoleEntity.AccountNumber))
            .ReturnsAsync(false);

        _deepValidationRepositoryMock
            .Setup(d => d.DoesDeepValidationLineExistsAsync(_validRefRoleEntity.EntityId, OperationCategory.ROLE))
            .ReturnsAsync(false);

        _deepValidationRepositoryMock
            .Setup(d => d.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()))
            .ReturnsAsync(true);

        await validator.AccountShouldExistInPulse();
        bool afterAccountCheck = await validator.Validate();
        Assert.False(afterAccountCheck, "Validator should be invalid now.");

        _operationRepositoryMock
            .Setup(r => r.CreateOperationAsync(It.IsAny<RegOperationEntity>()))
            .Throws(new Exception("Should not be called!"));

        // Act
        var result = await validator.TryAddOperation();
        bool finalIsValid = await result.Validate();

        // Assert
        Assert.False(finalIsValid);
        _operationRepositoryMock.Verify(r => r.CreateOperationAsync(It.IsAny<RegOperationEntity>()), Times.Never);
    }

    #endregion

    #region Validate Tests

    [Fact]
    public async Task Validate_DefaultsToTrueAfterInstantiation()
    {
        // Arrange
        var validator = CreateValidator();
        await validator.Instantiate(_validRefRoleEntity);

        // Act
        bool result = await validator.Validate();

        // Assert
        Assert.True(result);
    }

    #endregion
}

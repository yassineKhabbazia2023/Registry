using Application.Consts;
using Application.Enums;
using Application.Exceptions;
using Application.Interfaces;
using Application.Requests;
using Application.services;
using Microsoft.Extensions.Logging;
using Moq;
using Pulse.Registry.Domain.Entities;
using Pulse.Registry.Domain.Entities.Audits;
using Registry.Application.Consts;

namespace Registry.Infrastructure.Tests.Services;

public class ContactsDeepValidationsServiceTests
{
    private readonly Mock<IContactRepository> _contactRepositoryMock;
    private readonly Mock<IOperationRepository> _operationRepositoryMock;
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IDeepValidationRepository> _deepValidationRepoMock;
    private readonly Mock<ILogger<ContactsDeepValidationsService>> _loggerMock;
    private readonly ContactsDeepValidationsService _service;

    public ContactsDeepValidationsServiceTests()
    {
        _contactRepositoryMock = new Mock<IContactRepository>(MockBehavior.Strict);
        _operationRepositoryMock = new Mock<IOperationRepository>(MockBehavior.Strict);
        _roleRepositoryMock = new Mock<IRoleRepository>(MockBehavior.Strict);
        _deepValidationRepoMock = new Mock<IDeepValidationRepository>(MockBehavior.Strict);
        _loggerMock = new Mock<ILogger<ContactsDeepValidationsService>>();

        _service = new ContactsDeepValidationsService(
            _contactRepositoryMock.Object,
            _operationRepositoryMock.Object,
            _loggerMock.Object,
            _roleRepositoryMock.Object,
            _deepValidationRepoMock.Object
        );
    }

    #region Helpers

    private void SetupPagingSequence(params RefContactEntity[] contacts)
    {
        var firstPage = contacts.ToList();
        var secondPage = new List<RefContactEntity>();

        _contactRepositoryMock
            .SetupSequence(repo => repo.GetContactsWithoutOperationsPagedAsync(
                It.IsAny<int>(), It.IsAny<Guid?>()))
            .ReturnsAsync(firstPage)
            .ReturnsAsync(secondPage);
    }

    private void SetupDeepValidationAlwaysSucceeds()
    {
        _deepValidationRepoMock
            .Setup(dv => dv.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()))
            .ReturnsAsync(true);
    }

    #endregion

    #region INSERT Tests

    [Fact]
    public async Task CreateValidContactsOperationsAsync_Insert_ContactExists_TransformsToUpdate_CreatesUpdateOperation()
    {
        // Arrange
        var contact = new RefContactEntity
        {
            EntityId = Guid.NewGuid(),
            Email = "insertexists@test.com",
            OperationType = OperationAction.Insert
        };
        SetupPagingSequence(contact);
        SetupDeepValidationAlwaysSucceeds();

        // Uses DoesContactGlobalUniqueIdExistByEmailAsync (not DoesContactExistByEmailAsync)
        _contactRepositoryMock
            .Setup(r => r.DoesContactGlobalUniqueIdExistByEmailAsync(contact.Email))
            .ReturnsAsync(true);

        _operationRepositoryMock
            .Setup(r => r.FetchOperationsByCriteriaAsync(
                It.Is<OperationSearchCriteria>(c => c.OperationName == OperationAction.Insert),
                OperationStrategyType.CONTACT,
                contact.Email,
                It.IsAny<bool?>(),
                null))
            .ReturnsAsync(Enumerable.Empty<RegOperationEntity>());

        _operationRepositoryMock
            .Setup(r => r.CreateOperationAsync(It.Is<RegOperationEntity>(op =>
                op.Operation == OperationAction.Update &&
                op.Type == OperationCategory.CONTACT &&
                op.EntityId == contact.EntityId &&
                op.ProcessStatus == ProcessStatus.Ready &&
                op.ApprovalStatus == ApprovalStatus.Approved)))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _service.CreateValidContactsOperationsAsync();

        // Assert - transformed to Update, no deep validation
        _operationRepositoryMock.VerifyAll();
        _deepValidationRepoMock.Verify(
            dv => dv.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateValidContactsOperationsAsync_Insert_ContactNotExist_ButInsertOperationAlreadyExists_TransformsToUpdate_CreatesUpdateOperation()
    {
        // Arrange
        var contact = new RefContactEntity
        {
            EntityId = Guid.NewGuid(),
            Email = "insertopexists@test.com",
            OperationType = OperationAction.Insert
        };
        SetupPagingSequence(contact);
        SetupDeepValidationAlwaysSucceeds();

        var existingInsertOperation = new RegOperationEntity
        {
            Operation = OperationAction.Insert,
            Type = OperationCategory.CONTACT,
            EntityId = contact.EntityId,
        };

        // Contact does NOT exist globally, but a pending Insert operation already exists
        _contactRepositoryMock
            .Setup(r => r.DoesContactGlobalUniqueIdExistByEmailAsync(contact.Email))
            .ReturnsAsync(false);

        _operationRepositoryMock
            .Setup(r => r.FetchOperationsByCriteriaAsync(
                It.Is<OperationSearchCriteria>(c => c.OperationName == OperationAction.Insert),
                OperationStrategyType.CONTACT,
                contact.Email,
                It.IsAny<bool?>(),
                null))
            .ReturnsAsync(new List<RegOperationEntity> { existingInsertOperation });

        _operationRepositoryMock
            .Setup(r => r.CreateOperationAsync(It.Is<RegOperationEntity>(op =>
                op.Operation == OperationAction.Update &&
                op.Type == OperationCategory.CONTACT &&
                op.EntityId == contact.EntityId &&
                op.ProcessStatus == ProcessStatus.Ready &&
                op.ApprovalStatus == ApprovalStatus.Approved)))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _service.CreateValidContactsOperationsAsync();

        // Assert - transformed to Update, no deep validation
        _operationRepositoryMock.VerifyAll();
        _deepValidationRepoMock.Verify(
            dv => dv.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateValidContactsOperationsAsync_Insert_ContactExistsAndInsertOperationsExist_TransformsToUpdate_CreatesUpdateOperationOnce()
    {
        // Arrange
        var contact = new RefContactEntity
        {
            EntityId = Guid.NewGuid(),
            Email = "insertbothexist@test.com",
            OperationType = OperationAction.Insert
        };
        SetupPagingSequence(contact);
        SetupDeepValidationAlwaysSucceeds();

        var existingInsertOperation = new RegOperationEntity
        {
            Operation = OperationAction.Insert,
            Type = OperationCategory.CONTACT,
            EntityId = contact.EntityId,
        };

        // Both: contact exists globally AND a pending Insert operation exists
        _contactRepositoryMock
            .Setup(r => r.DoesContactGlobalUniqueIdExistByEmailAsync(contact.Email))
            .ReturnsAsync(true);

        _operationRepositoryMock
            .Setup(r => r.FetchOperationsByCriteriaAsync(
                It.Is<OperationSearchCriteria>(c => c.OperationName == OperationAction.Insert),
                OperationStrategyType.CONTACT,
                contact.Email,
                It.IsAny<bool?>(),
                null))
            .ReturnsAsync(new List<RegOperationEntity> { existingInsertOperation });

        _operationRepositoryMock
            .Setup(r => r.CreateOperationAsync(It.Is<RegOperationEntity>(op =>
                op.Operation == OperationAction.Update &&
                op.Type == OperationCategory.CONTACT &&
                op.EntityId == contact.EntityId &&
                op.ProcessStatus == ProcessStatus.Ready &&
                op.ApprovalStatus == ApprovalStatus.Approved)))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _service.CreateValidContactsOperationsAsync();

        // Assert - exactly one Update created, no Insert, no deep validation
        _operationRepositoryMock.VerifyAll();
        _operationRepositoryMock.Verify(
            r => r.CreateOperationAsync(It.Is<RegOperationEntity>(op =>
                op.Operation == OperationAction.Insert)),
            Times.Never);
        _deepValidationRepoMock.Verify(
            dv => dv.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateValidContactsOperationsAsync_Insert_ContactNotExist_NoExistingOperations_CreatesInsertOperation()
    {
        // Arrange
        var contact = new RefContactEntity
        {
            EntityId = Guid.NewGuid(),
            Email = "insertnew@test.com",
            OperationType = OperationAction.Insert
        };
        SetupPagingSequence(contact);
        SetupDeepValidationAlwaysSucceeds();

        // Contact does NOT exist globally, no existing operations → plain Insert
        _contactRepositoryMock
            .Setup(r => r.DoesContactGlobalUniqueIdExistByEmailAsync(contact.Email))
            .ReturnsAsync(false);

        _operationRepositoryMock
            .Setup(r => r.FetchOperationsByCriteriaAsync(
                It.Is<OperationSearchCriteria>(c => c.OperationName == OperationAction.Insert),
                OperationStrategyType.CONTACT,
                contact.Email,
                It.IsAny<bool?>(),
                null))
            .ReturnsAsync(Enumerable.Empty<RegOperationEntity>());

        _operationRepositoryMock
            .Setup(r => r.CreateOperationAsync(It.Is<RegOperationEntity>(op =>
                op.Operation == OperationAction.Insert &&
                op.Type == OperationCategory.CONTACT &&
                op.EntityId == contact.EntityId &&
                op.ProcessStatus == ProcessStatus.Ready &&
                op.ApprovalStatus == ApprovalStatus.Approved)))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _service.CreateValidContactsOperationsAsync();

        // Assert - Insert created as-is, no transformation, no deep validation
        _operationRepositoryMock.VerifyAll();
        _deepValidationRepoMock.Verify(
            dv => dv.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()),
            Times.Never);
    }


    [Fact]
    public async Task CreateValidContactsOperationsAsync_Update_ContactExists_CreatesUpdateOperation()
    {
        var contact = new RefContactEntity
        {
            EntityId = Guid.NewGuid(),
            Email = "update@test.com",
            OperationType = OperationAction.Update
        };
        SetupPagingSequence(contact);
        SetupDeepValidationAlwaysSucceeds();

        _contactRepositoryMock
            .Setup(r => r.DoesContactExistByEmailAsync(contact.Email))
            .ReturnsAsync(true);

        _operationRepositoryMock
            .Setup(r => r.FetchOperationsByCriteriaAsync(
                It.Is<OperationSearchCriteria>(c => c.OperationName == OperationAction.Insert),
                OperationStrategyType.CONTACT,
                contact.Email,
                false,
                null))
            .ReturnsAsync(Enumerable.Empty<RegOperationEntity>());

        _operationRepositoryMock
            .Setup(r => r.CreateOperationAsync(It.Is<RegOperationEntity>(op =>
                op.Operation == OperationAction.Update &&
                op.Type == OperationCategory.CONTACT &&
                op.EntityId == contact.EntityId)))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _service.CreateValidContactsOperationsAsync();

        // Assert
        _operationRepositoryMock.VerifyAll();
        _deepValidationRepoMock.Verify(dv => dv.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()), Times.Never);
    }

    [Fact]
    public async Task CreateValidContactsOperationsAsync_Update_ContactDoesNotExist_NoInsertReady_TransformItToInsert()
    {
        var contact = new RefContactEntity
        {
            EntityId = Guid.NewGuid(),
            Email = "updatenoexists@test.com",
            OperationType = OperationAction.Update
        };
        SetupPagingSequence(contact);
        SetupDeepValidationAlwaysSucceeds();

        _contactRepositoryMock
            .Setup(r => r.DoesContactExistByEmailAsync(contact.Email))
            .ReturnsAsync(false);

        _operationRepositoryMock
            .Setup(r => r.FetchOperationsByCriteriaAsync(
                It.Is<OperationSearchCriteria>(c => c.OperationName == OperationAction.Insert),
                OperationStrategyType.CONTACT,
                contact.Email,
                false,
                null))
            .ReturnsAsync(Enumerable.Empty<RegOperationEntity>());

        _operationRepositoryMock.Setup(r => r.CreateOperationAsync(It.Is<RegOperationEntity>(op =>
            op.Operation == OperationAction.Insert &&
            op.Type == OperationCategory.CONTACT &&
            op.EntityId == contact.EntityId)))
            .Returns(Task.CompletedTask)
            .Verifiable();



        // Act
        await _service.CreateValidContactsOperationsAsync();

        // Assert
        _operationRepositoryMock.VerifyAll();
        _operationRepositoryMock.Verify(r => r.CreateOperationAsync(It.IsAny<RegOperationEntity>()), Times.Once);
    }
    #endregion

    #region DELETE Tests

    [Fact]
    public async Task CreateValidContactsOperationsAsync_Delete_ContactExists_CreatesDeleteOperation()
    {
        // Arrange
        var contact = new RefContactEntity
        {
            EntityId = Guid.NewGuid(),
            Email = "deleteexists@test.com",
            OperationType = OperationAction.Delete
        };
        SetupPagingSequence(contact);
        SetupDeepValidationAlwaysSucceeds();

        _contactRepositoryMock
            .Setup(r => r.DoesContactExistByEmailAsync(contact.Email))
            .ReturnsAsync(true);

        var sequence = new MockSequence();
        _operationRepositoryMock
            .InSequence(sequence)
            .Setup(r => r.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.CONTACT,
                contact.Email,
                false,
                null)).
                Callback<OperationSearchCriteria, OperationStrategyType, string, bool?, string?>((criteria, strategy, email, isEmail, entityId) =>
                {
                    Assert.Equal(OperationAction.Insert, criteria.OperationName);
                })
            .ReturnsAsync(Enumerable.Empty<RegOperationEntity>());

        _operationRepositoryMock
            .InSequence(sequence)
            .Setup(r => r.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.CONTACT,
                contact.Email,
                false,
                null)).
                Callback<OperationSearchCriteria, OperationStrategyType, string, bool?, string?>((criteria, strategy, email, isEmail, entityId) =>
                {
                    Assert.Equal(OperationAction.Delete, criteria.OperationName);
                })
            .ReturnsAsync(Enumerable.Empty<RegOperationEntity>());

        _operationRepositoryMock.Setup(r => r.CreateOperationAsync(It.IsAny<RegOperationEntity>()))
            .Callback<RegOperationEntity>(op =>
            {
                Assert.Equal(OperationAction.Delete, op.Operation);
                Assert.Equal(OperationCategory.CONTACT, op.Type);
                Assert.Equal(contact.EntityId, op.EntityId);
            })
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _service.CreateValidContactsOperationsAsync();

        // Assert
        _operationRepositoryMock.VerifyAll();
    }

    [Fact]
    public async Task CreateValidContactsOperationsAsync_Delete_ContactDoesNotExist_NoInsertReady_AddsDeepValidation()
    {
        // Arrange
        var contact = new RefContactEntity
        {
            EntityId = Guid.NewGuid(),
            Email = "deletenotexists@test.com",
            OperationType = OperationAction.Delete
        };
        SetupPagingSequence(contact);
        SetupDeepValidationAlwaysSucceeds();

        // contact not existing:
        _contactRepositoryMock
            .Setup(r => r.DoesContactExistByEmailAsync(contact.Email))
            .ReturnsAsync(false);

        _operationRepositoryMock.Setup(r => r.CreateOperationAsync(It.IsAny<RegOperationEntity>()))
            .Callback<RegOperationEntity>(op =>
            {
                Assert.Equal(OperationAction.Delete, op.Operation);
                Assert.Equal(OperationCategory.CONTACT, op.Type);
                Assert.Equal(contact.EntityId, op.EntityId);
            })
            .Returns(Task.CompletedTask)
            .Verifiable();

        // no Insert ready
        var sequence = new MockSequence();
        _operationRepositoryMock
            .InSequence(sequence)
            .Setup(r => r.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.CONTACT,
                contact.Email,
                false,
                null)).
                Callback<OperationSearchCriteria, OperationStrategyType, string, bool?, string?>((criteria, strategy, email, isEmail, entityId) =>
                {
                    Assert.Equal(OperationAction.Insert, criteria.OperationName);
                })
            .ReturnsAsync(Enumerable.Empty<RegOperationEntity>());

        _operationRepositoryMock
            .InSequence(sequence)
            .Setup(r => r.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.CONTACT,
                contact.Email,
                false,
                null)).
                Callback<OperationSearchCriteria, OperationStrategyType, string, bool?, string?>((criteria, strategy, email, isEmail, entityId) =>
                {
                    Assert.Equal(OperationAction.Delete, criteria.OperationName);
                })
            .ReturnsAsync(Enumerable.Empty<RegOperationEntity>());

        _deepValidationRepoMock
            .Setup(dv => dv.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()))
            .Callback<DeepValidationEntity>(dpv =>
            {
                dpv.EntityId = contact.EntityId;
                dpv.Reason.Contains("skipping creating a delete contact operation", StringComparison.OrdinalIgnoreCase);
            })
            .ReturnsAsync(true);

        // Act
        await _service.CreateValidContactsOperationsAsync();

        // Assert
        _operationRepositoryMock.Verify(r => r.CreateOperationAsync(It.IsAny<RegOperationEntity>()), Times.Never);
        _deepValidationRepoMock.VerifyAll();
    }

    #endregion
}

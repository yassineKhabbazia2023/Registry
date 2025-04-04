using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Consts;
using Application.Enums;
using Application.Exceptions;
using Application.Interfaces;
using Application.Requests;
using Application.services;
using Domain.Entities.Audits;
using Microsoft.Extensions.Logging;
using Moq;
using Pulse.Registry.Domain.Entities;
using Xunit;

namespace Registry.Infrastructure.Tests.Services
{
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
        public async Task CreateValidContactsOperationsAsync_Insert_ContactExists_AddsDeepValidation_NoNewOperation()
        {
            // Arrange
            var contact = new RefContactEntity
            {
                EntityId = Guid.NewGuid(),
                Email = "insert@test.com",
                OperationType = OperationAction.Insert
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
                    It.IsAny<bool?>(),
                    null))
                .ReturnsAsync(Enumerable.Empty<RegOperationEntity>()); // or we could return something

            _deepValidationRepoMock
                .Setup(d => d.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()))
                .ReturnsAsync(true);

            // Act
            await _service.CreateValidContactsOperationsAsync();

            // Assert
            _contactRepositoryMock.Verify(r => r.DoesContactExistByEmailAsync(contact.Email), Times.Once);
            _operationRepositoryMock.Verify(r => r.CreateOperationAsync(It.IsAny<RegOperationEntity>()), Times.Never);
            _deepValidationRepoMock.Verify(dv => dv.AddDeepValidationAsync(It.Is<DeepValidationEntity>(dv =>
                dv.EntityId == contact.EntityId &&
                dv.Type == "CONTACT" &&
                dv.Reason.Contains("skipping", StringComparison.OrdinalIgnoreCase))),
                Times.Once);
        }

        [Fact]
        public async Task CreateValidContactsOperationsAsync_Insert_ContactDoesNotExistAndNoOperation_CreatesOperation()
        {
            // Arrange
            var contact = new RefContactEntity
            {
                EntityId = Guid.NewGuid(),
                Email = "newinsert@test.com",
                OperationType = OperationAction.Insert
            };
            SetupPagingSequence(contact);
            SetupDeepValidationAlwaysSucceeds();

            _contactRepositoryMock
                .Setup(r => r.DoesContactExistByEmailAsync(contact.Email))
                .ReturnsAsync(false);

            _operationRepositoryMock
                .Setup(r => r.FetchOperationsByCriteriaAsync(
                    It.IsAny<OperationSearchCriteria>(),
                    OperationStrategyType.CONTACT,
                    contact.Email,
                    It.IsAny<bool?>(),
                    null))
                .ReturnsAsync(Enumerable.Empty<RegOperationEntity>());

            _operationRepositoryMock
                .Setup(r => r.CreateOperationAsync(It.Is<RegOperationEntity>(op =>
                    op.Operation == OperationAction.Insert &&
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
        public async Task CreateValidContactsOperationsAsync_Insert_ThrowsDbOperationException_AddsDeepValidation()
        {
            // Arrange
            var contact = new RefContactEntity
            {
                EntityId = Guid.NewGuid(),
                Email = "inserterr@test.com",
                OperationType = OperationAction.Insert
            };
            SetupPagingSequence(contact);
            SetupDeepValidationAlwaysSucceeds();

            _contactRepositoryMock
                .Setup(r => r.DoesContactExistByEmailAsync(contact.Email))
                .ReturnsAsync(false);

            _operationRepositoryMock
                .Setup(r => r.FetchOperationsByCriteriaAsync(
                    It.IsAny<OperationSearchCriteria>(),
                    OperationStrategyType.CONTACT,
                    contact.Email,
                    It.IsAny<bool?>(),
                    null))
                .ReturnsAsync(Enumerable.Empty<RegOperationEntity>());

            var exception = new Exception("Inner exception");
            _operationRepositoryMock
                .Setup(r => r.CreateOperationAsync(It.IsAny<RegOperationEntity>()))
                .ThrowsAsync(new DbOperationException("DB error", exception));

            _deepValidationRepoMock
                   .Setup(d => d.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()))
                   .Callback<DeepValidationEntity>((dp) =>
                   {
                       Assert.Equal(dp.EntityId, contact.EntityId);
                       Assert.Contains("Unable to add operation", dp.Reason);
                   })
                   .ReturnsAsync(true);

            // Act
            await _service.CreateValidContactsOperationsAsync();

            // Assert
            _operationRepositoryMock.Verify(r => r.CreateOperationAsync(It.IsAny<RegOperationEntity>()), Times.Once);
            _deepValidationRepoMock.VerifyAll();
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
        public async Task CreateValidContactsOperationsAsync_Update_ContactDoesNotExist_NoInsertReady_AddsDeepValidation()
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

            _deepValidationRepoMock
                .Setup(dv => dv.AddDeepValidationAsync(It.IsAny<DeepValidationEntity>()))
                .Callback<DeepValidationEntity>(dpv =>
                {
                    dpv.EntityId = contact.EntityId;
                    dpv.Reason.Contains("skipping creating an update contact operation", StringComparison.OrdinalIgnoreCase);
                })
                .ReturnsAsync(true);

            // Act
            await _service.CreateValidContactsOperationsAsync();

            // Assert
            _operationRepositoryMock.Verify(r => r.CreateOperationAsync(It.IsAny<RegOperationEntity>()), Times.Never);
            _deepValidationRepoMock.VerifyAll();
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
}

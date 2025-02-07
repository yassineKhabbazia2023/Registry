using Application.Consts;
using Application.Exceptions;
using Application.Interfaces;
using Application.services;
using Domain.Entities.Accounts;
using Microsoft.Extensions.Logging;
using Moq;
using Pulse.ContactRegistry.Domain.Entities;
using Registry.Application.Consts;
using System.Reflection.Emit;

namespace Registry.Infrastructure.Tests.Services
{
    public class ContactsDeepValidationsServiceTest
    {
        private const string TestEmail = "test@example.com";
        private const string TestFirstName = "Test";
        private const string TestLastName = "User";

        private readonly Mock<IContactRepository> _mockContactRepository;
        private readonly Mock<IOperationRepository> _mockOperationRepository;
        private readonly Mock<IRoleRepository> _mockRoleRepository;
        private readonly Mock<ILogger<ContactsDeepValidationsService>> _mockLogger;
        private readonly IContactsDeepValidationsService _service;

        public ContactsDeepValidationsServiceTest()
        {
            _mockContactRepository = new Mock<IContactRepository>();
            _mockOperationRepository = new Mock<IOperationRepository>();
            _mockLogger = new Mock<ILogger<ContactsDeepValidationsService>>();
            _mockRoleRepository = new Mock<IRoleRepository>();

            _service = new ContactsDeepValidationsService(
                _mockContactRepository.Object,
                _mockOperationRepository.Object,
                _mockLogger.Object,
                _mockRoleRepository.Object);
        }

        /// <summary>
        /// Helper to simulate one page of results followed by an empty page.
        /// </summary>
        private void SetupGetRefContactsPagedAsync(RefContactEntity contact)
        {
            _mockContactRepository.SetupSequence(repo => repo.GetContactsWithoutOperationsPagedAsync(It.IsAny<int>(), It.IsAny<Guid?>()))
                .ReturnsAsync(new List<RefContactEntity> { contact })
                .ReturnsAsync(new List<RefContactEntity>());
        }

        private void SetupGetContactPulseRolesAsync(List<RoleEntity> roles)
        {
            _mockRoleRepository.Setup(r => r.GetRolesForContactAsync(It.IsAny<string>()))
                .ReturnsAsync(roles);
        }

        #region INSERT Branch Tests

        [Fact]
        public async Task ValidateContactsOperationsAsync_Insert_ContactExists_CallsInsertContactNewAudit()
        {
            // Arrange
            var contact = new RefContactEntity
            {
                EntityId = Guid.NewGuid(),
                Email = TestEmail,
                FirstName = TestFirstName,
                LastName = TestLastName,
                OperationType = OperationName.Insert
            };

            SetupGetRefContactsPagedAsync(contact);

            _mockContactRepository
                .Setup(repo => repo.DoesContactExistAsync(TestFirstName, TestLastName, TestEmail))
                .ReturnsAsync(true);

            string capturedMessage = null;

            _mockContactRepository
                .Setup(repo => repo.InsertContactNewAudit(contact, It.IsAny<string>()))
                .Callback<RefContactEntity, string>((c, msg) =>
                {
                    capturedMessage = msg;
                    Assert.Equal(TestEmail, c.Email);
                    Assert.Contains("skipping creating an insert contact operation", msg, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains(TestEmail, msg);
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await _service.CreateValidContactsOperationsAsync();

            // Assert
            _mockContactRepository.Verify(repo => repo.InsertContactNewAudit(contact, It.IsAny<string>()), Times.Once);
            _mockOperationRepository.Verify(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()), Times.Never);
            Assert.NotNull(capturedMessage);
        }

        [Fact]
        public async Task ValidateContactsOperationsAsync_Insert_ContactNotExists_SuccessfulOperation_CallsInsertNewOperation()
        {
            // Arrange
            var contact = new RefContactEntity
            {
                EntityId = Guid.NewGuid(),
                Email = TestEmail,
                FirstName = TestFirstName,
                LastName = TestLastName,
                OperationType = OperationName.Insert
            };

            SetupGetRefContactsPagedAsync(contact);

            _mockContactRepository
                .Setup(repo => repo.DoesContactExistAsync(TestFirstName, TestLastName, TestEmail))
                .ReturnsAsync(false);

            string capturedOperationType = null;

            _mockOperationRepository
                .Setup(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()))
                .Callback<RegOperationEntity>(op =>
                {
                    capturedOperationType = op.Operation;
                    Assert.Equal(contact.EntityId, op.EntityId);
                    Assert.Equal("CONTACT", op.Type);
                    Assert.Equal(ApprovalStatus.Approved, op.ApprovalStatus);
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await _service.CreateValidContactsOperationsAsync();

            // Assert
            _mockOperationRepository.Verify(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()), Times.Once);
            Assert.Equal(OperationName.Insert, capturedOperationType);
            _mockContactRepository.Verify(repo => repo.InsertContactNewAudit(It.IsAny<RefContactEntity>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ValidateContactsOperationsAsync_Insert_ContactNotExists_When_AddOperationThrows_Exeception_CallsInsertContactNewAudit()
        {
            // Arrange
            var contact = new RefContactEntity
            {
                EntityId = Guid.NewGuid(),
                Email = TestEmail,
                FirstName = TestFirstName,
                LastName = TestLastName,
                OperationType = OperationName.Insert
            };

            SetupGetRefContactsPagedAsync(contact);

            _mockContactRepository
                .Setup(repo => repo.DoesContactExistAsync(TestFirstName, TestLastName, TestEmail))
                .ReturnsAsync(false);

            _mockOperationRepository
                .Setup(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()))
                .ThrowsAsync(new DbOperationException("Error", new Exception("Inner exception")));

            string capturedAuditMessage = null;
            _mockContactRepository
                .Setup(repo => repo.InsertContactNewAudit(contact, It.IsAny<string>()))
                .Callback<RefContactEntity, string>((c, msg) =>
                {
                    capturedAuditMessage = msg;
                    Assert.Contains("Unable to add operation", msg, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains(contact.EntityId.ToString(), msg);
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await _service.CreateValidContactsOperationsAsync();

            // Assert
            _mockContactRepository.Verify(repo => repo.InsertContactNewAudit(contact, It.IsAny<string>()), Times.Once);
            Assert.NotNull(capturedAuditMessage);
        }

        #endregion

        #region DELETE Branch Tests

        [Fact]
        public async Task ValidateContactsOperationsAsync_Delete_ContactNotExists_CallsInsertContactNewAudit()
        {
            // Arrange
            var contact = new RefContactEntity
            {
                EntityId = Guid.NewGuid(),
                Email = TestEmail,
                FirstName = TestFirstName,
                LastName = TestLastName,
                OperationType = OperationName.Delete
            };

            SetupGetRefContactsPagedAsync(contact);

            _mockContactRepository
                .Setup(repo => repo.DoesContactExistAsync(TestFirstName, TestLastName, TestEmail))
                .ReturnsAsync(false);

            _mockOperationRepository
                .Setup(repo => repo.FindContactsReadyOperationsAsync(TestEmail, OperationName.Delete))
                .ReturnsAsync(0);

            string capturedMessage = null;
            _mockContactRepository
                .Setup(repo => repo.InsertContactNewAudit(contact, It.IsAny<string>()))
                .Callback<RefContactEntity, string>((c, msg) =>
                {
                    capturedMessage = msg;
                    Assert.Contains("skipping creating an delete contact operation", msg, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains(TestEmail, msg);
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await _service.CreateValidContactsOperationsAsync();

            // Assert
            _mockContactRepository.Verify(repo => repo.InsertContactNewAudit(contact, It.IsAny<string>()), Times.Once);
            Assert.NotNull(capturedMessage);
            _mockOperationRepository.Verify(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()), Times.Never);
        }

        [Fact]
        public async Task ValidateContactsOperationsAsync_Delete_ContactNotExists_FindReadyOperationsNull_CallsInsertContactNewAudit()
        {
            // Arrange
            var contact = new RefContactEntity
            {
                EntityId = Guid.NewGuid(),
                Email = TestEmail,
                FirstName = TestFirstName,
                LastName = TestLastName,
                OperationType = OperationName.Delete
            };

            SetupGetRefContactsPagedAsync(contact);

            _mockContactRepository
                .Setup(repo => repo.DoesContactExistAsync(TestFirstName, TestLastName, TestEmail))
                .ReturnsAsync(false);

            _mockOperationRepository
                .Setup(repo => repo.FindContactsReadyOperationsAsync(TestEmail, OperationName.Delete))
                .ReturnsAsync(0);

            string capturedMessage = null;
            _mockContactRepository
                .Setup(repo => repo.InsertContactNewAudit(contact, It.IsAny<string>()))
                .Callback<RefContactEntity, string>((c, msg) =>
                {
                    capturedMessage = msg;
                    Assert.Contains("skipping creating an delete contact operation", msg, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains(TestEmail, msg);
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await _service.CreateValidContactsOperationsAsync();

            // Assert
            _mockContactRepository.Verify(repo => repo.InsertContactNewAudit(contact, It.IsAny<string>()), Times.Once);
            Assert.NotNull(capturedMessage);
            _mockOperationRepository.Verify(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()), Times.Never);
        }

        [Fact]
        public async Task ValidateContactsOperationsAsync_Delete_ContactNotExists_MultipleReadyOperations_CallsInsertContactNewAudit()
        {
            // Arrange
            var contact = new RefContactEntity
            {
                EntityId = Guid.NewGuid(),
                Email = TestEmail,
                FirstName = TestFirstName,
                LastName = TestLastName,
                OperationType = OperationName.Delete
            };

            var existingOperation = new RegOperationEntity()
            {
                ApprovalStatus = ApprovalStatus.Approved,
                Operation = OperationName.Insert,
                ProcessStatus = ProcessStatus.Ready,
                EntityId = contact.EntityId,
            };

            SetupGetRefContactsPagedAsync(contact);

            _mockContactRepository
                .Setup(repo => repo.DoesContactExistAsync(TestEmail))
                .ReturnsAsync(false);

            var readyOperations = new List<RegOperationEntity> { existingOperation, existingOperation };
            _mockOperationRepository
                .Setup(repo => repo.FindContactsReadyOperationsAsync(TestEmail, OperationName.Insert))
                .ReturnsAsync(readyOperations.Count());

            string capturedMessage = null;
            _mockContactRepository
                .Setup(repo => repo.InsertContactNewAudit(contact, It.IsAny<string>()))
                .Callback<RefContactEntity, string>((c, msg) =>
                {
                    capturedMessage = msg;
                    Assert.Contains("found an unexpected behaviour", msg, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains(TestEmail, msg);
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await _service.CreateValidContactsOperationsAsync();

            // Assert
            _mockContactRepository.Verify(repo => repo.InsertContactNewAudit(contact, It.IsAny<string>()), Times.Once);
            Assert.NotNull(capturedMessage);
            _mockOperationRepository.Verify(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()), Times.Never);
        }

        [Fact]
        public async Task ValidateContactsOperationsAsync_Delete_ContactNotExists_OneReadyOperation_CallsInsertNewOperation()
        {
            // Arrange
            var contact = new RefContactEntity
            {
                EntityId = Guid.NewGuid(),
                Email = TestEmail,
                FirstName = TestFirstName,
                LastName = TestLastName,
                OperationType = OperationName.Delete
            };

            var existingOperation = new RegOperationEntity()
            {
                ApprovalStatus = ApprovalStatus.Approved,
                Operation = OperationName.Insert,
                ProcessStatus = ProcessStatus.Ready,
                EntityId = contact.EntityId,
            };

            SetupGetRefContactsPagedAsync(contact);

            _mockContactRepository
                .Setup(repo => repo.DoesContactExistAsync(TestEmail))
                .ReturnsAsync(false);

            var readyOperations = new List<RegOperationEntity> { existingOperation };
            _mockOperationRepository
                .Setup(repo => repo.FindContactsReadyOperationsAsync(TestEmail, OperationName.Insert))
                .ReturnsAsync(readyOperations.Count());

            string capturedMessage = null;
            _mockContactRepository
                .Setup(repo => repo.InsertContactNewAudit(contact, It.IsAny<string>()))
                .Callback<RefContactEntity, string>((c, msg) =>
                {
                    capturedMessage = msg;
                    Assert.Contains("found an unexpected behaviour", msg, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains(TestEmail, msg);
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            string capturedOperationType = null;
            _mockOperationRepository
                .Setup(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()))
                .Callback<RegOperationEntity>(op =>
                {
                    capturedOperationType = op.Operation;
                    Assert.Equal(OperationName.Delete, op.Operation);
                })
                .Returns(Task.CompletedTask)
                .Verifiable();
            // Act
            await _service.CreateValidContactsOperationsAsync();

            // Assert
            _mockContactRepository.Verify(repo => repo.InsertContactNewAudit(contact, It.IsAny<string>()), Times.Never);
            Assert.Equal(OperationName.Delete, capturedOperationType);
            _mockOperationRepository.Verify(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()), Times.Once);
        }

        [Fact]
        public async Task ValidateContactsOperationsAsync_Delete_ContactExists_CallsInsertNewOperation()
        {
            // Arrange
            var contact = new RefContactEntity
            {
                EntityId = Guid.NewGuid(),
                Email = TestEmail,
                FirstName = TestFirstName,
                LastName = TestLastName,
                OperationType = OperationName.Delete
            };

            SetupGetRefContactsPagedAsync(contact);

            _mockContactRepository
                .Setup(repo => repo.DoesContactExistAsync(TestEmail))
                .ReturnsAsync(true);

            string capturedOperationType = null;
            _mockOperationRepository
                .Setup(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()))
                .Callback<RegOperationEntity>(op =>
                {
                    capturedOperationType = op.Operation;
                    Assert.Equal(OperationName.Delete, op.Operation);
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await _service.CreateValidContactsOperationsAsync();

            // Assert
            _mockOperationRepository.Verify(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()), Times.Once);
            Assert.Equal(OperationName.Delete, capturedOperationType);
            _mockContactRepository.Verify(repo => repo.InsertContactNewAudit(It.IsAny<RefContactEntity>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ValidateContactsOperationsAsync_Delete_ContactNotExists_OneReadyOperation_ContactHasRoles_CallsDeleteRoles()
        {
            // Arrange
            var contact = new RefContactEntity
            {
                EntityId = Guid.NewGuid(),
                Email = TestEmail,
                FirstName = TestFirstName,
                LastName = TestLastName,
                OperationType = OperationName.Delete
            };

            var existingOperation = new RegOperationEntity()
            {
                ApprovalStatus = ApprovalStatus.Approved,
                Operation = OperationName.Insert,
                ProcessStatus = ProcessStatus.Ready,
                EntityId = contact.EntityId,
            };

            var existingRoles = new List<RoleEntity>() {
                new RoleEntity()
                {
                    ContactEmail = TestEmail,
                    AccountId = 1,
                    ContactId = 1,
                    RoleDuplicatesCounter = 0
                },
                new RoleEntity()
                {
                    ContactEmail = TestEmail,
                    AccountId = 2,
                    ContactId = 1,
                    RoleDuplicatesCounter = 0
                },
            };

            SetupGetRefContactsPagedAsync(contact);
            SetupGetContactPulseRolesAsync(existingRoles);

            _mockContactRepository
                .Setup(repo => repo.DoesContactExistAsync(TestEmail))
                .ReturnsAsync(false);

            var readyOperations = new List<RegOperationEntity> { existingOperation };
            _mockOperationRepository
                .Setup(repo => repo.FindContactsReadyOperationsAsync(TestEmail, OperationName.Insert))
                .ReturnsAsync(readyOperations.Count());

            string capturedMessage = null;
            _mockContactRepository
                .Setup(repo => repo.InsertContactNewAudit(contact, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var sequence = new MockSequence();

            string capturedOperationType = null;

            _mockOperationRepository.InSequence(sequence)
                .Setup(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()))
                .Callback<RegOperationEntity>(op =>
                {
                    capturedOperationType = op.Operation;
                    Assert.Equal(OperationName.Delete, op.Operation);
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            _mockOperationRepository.InSequence(sequence)
                .Setup(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()))
                .Callback<RegOperationEntity>(op =>
                {
                    Assert.Equal(contact.EntityId, op.EntityId);
                    Assert.Equal("ROLE", op.Type);
                    Assert.Equal(OperationName.Delete, op.Operation);
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            _mockOperationRepository.InSequence(sequence)
                .Setup(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()))
                .Callback<RegOperationEntity>(op =>
                {
                    Assert.Equal(contact.EntityId, op.EntityId);
                    Assert.Equal("ROLE", op.Type);
                    Assert.Equal(OperationName.Delete, op.Operation);
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await _service.CreateValidContactsOperationsAsync();

            // Assert
            _mockContactRepository.Verify(repo => repo.InsertContactNewAudit(contact, It.IsAny<string>()), Times.Never);
            Assert.Equal(OperationName.Delete, capturedOperationType);
            _mockOperationRepository.Verify(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()), Times.Exactly(3));
            _mockRoleRepository.Verify(r => r.GetRolesForContactAsync(TestEmail), Times.Once);
            _mockRoleRepository.Verify(r => r.UpdatePulseRole(It.IsAny<RoleEntity>()), Times.Never);
        }

        [Fact]
        public async Task ValidateContactsOperationsAsync_Delete_ContactNotExists_OneReadyOperation_ContactHasRolesDuplicates_CallsDeleteRoles()
        {
            // Arrange
            var contact = new RefContactEntity
            {
                EntityId = Guid.NewGuid(),
                Email = TestEmail,
                FirstName = TestFirstName,
                LastName = TestLastName,
                OperationType = OperationName.Delete
            };

            var existingOperation = new RegOperationEntity()
            {
                ApprovalStatus = ApprovalStatus.Approved,
                Operation = OperationName.Insert,
                ProcessStatus = ProcessStatus.Ready,
                EntityId = contact.EntityId,
            };

            var existingRoles = new List<RoleEntity>() {
                new RoleEntity()
                {
                    ContactEmail = TestEmail,
                    AccountId = 1,
                    ContactId = 1,
                    RoleDuplicatesCounter = 4
                },
                new RoleEntity()
                {
                    ContactEmail = TestEmail,
                    AccountId = 2,
                    ContactId = 1,
                    RoleDuplicatesCounter = 0
                },
            };

            SetupGetRefContactsPagedAsync(contact);
            SetupGetContactPulseRolesAsync(existingRoles);

            _mockContactRepository
                .Setup(repo => repo.DoesContactExistAsync(TestEmail))
                .ReturnsAsync(false);

            var readyOperations = new List<RegOperationEntity> { existingOperation };
            _mockOperationRepository
                .Setup(repo => repo.FindContactsReadyOperationsAsync(TestEmail, OperationName.Insert))
                .ReturnsAsync(readyOperations.Count());

            string capturedMessage = null;
            _mockContactRepository
                .Setup(repo => repo.InsertContactNewAudit(contact, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var sequence = new MockSequence();

            string capturedOperationType = null;

            _mockOperationRepository.InSequence(sequence)
                .Setup(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()))
                .Callback<RegOperationEntity>(op =>
                {
                    capturedOperationType = op.Operation;
                    Assert.Equal(OperationName.Delete, op.Operation);
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            _mockOperationRepository.InSequence(sequence)
                .Setup(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()))
                .Callback<RegOperationEntity>(op =>
                {
                    Assert.Equal(contact.EntityId, op.EntityId);
                    Assert.Equal("ROLE", op.Type);
                    Assert.Equal(OperationName.Delete, op.Operation);
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            _mockOperationRepository.InSequence(sequence)
                .Setup(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()))
                .Callback<RegOperationEntity>(op =>
                {
                    Assert.Equal(contact.EntityId, op.EntityId);
                    Assert.Equal("ROLE", op.Type);
                    Assert.Equal(OperationName.Delete, op.Operation);
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await _service.CreateValidContactsOperationsAsync();

            // Assert
            _mockContactRepository.Verify(repo => repo.InsertContactNewAudit(contact, It.IsAny<string>()), Times.Never);
            Assert.Equal(OperationName.Delete, capturedOperationType);
            _mockOperationRepository.Verify(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()), Times.Exactly(3));
            _mockRoleRepository.Verify(r => r.GetRolesForContactAsync(TestEmail), Times.Once);
            _mockRoleRepository.Verify(r => r.UpdatePulseRole(It.IsAny<RoleEntity>()), Times.Once);
        }

        [Fact]
        public async Task ValidateContactsOperationsAsync_Delete_ContactExists_HasRoles_CallsDeleteRoles()
        {
            // Arrange
            var contact = new RefContactEntity
            {
                EntityId = Guid.NewGuid(),
                Email = TestEmail,
                FirstName = TestFirstName,
                LastName = TestLastName,
                OperationType = OperationName.Delete
            };

            var existingRoles = new List<RoleEntity>() {
                new RoleEntity()
                {
                    ContactEmail = TestEmail,
                    AccountId = 1,
                    ContactId = 1,
                    RoleDuplicatesCounter = 0
                }
            };

            SetupGetRefContactsPagedAsync(contact);
            SetupGetContactPulseRolesAsync(existingRoles);

            var sequence = new MockSequence();

            _mockContactRepository
                .Setup(repo => repo.DoesContactExistAsync(TestEmail))
                .ReturnsAsync(true);

            string capturedOperationType = null;
            _mockOperationRepository.InSequence(sequence)
                .Setup(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()))
                .Callback<RegOperationEntity>(op =>
                {
                    capturedOperationType = op.Operation;
                    Assert.Equal(OperationName.Delete, op.Operation);
                })
                .Returns(Task.CompletedTask)
                .Verifiable();
            _mockOperationRepository.InSequence(sequence)
                 .Setup(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()))
                 .Callback<RegOperationEntity>(op =>
                 {
                     Assert.Equal(contact.EntityId, op.EntityId);
                     Assert.Equal("ROLE", op.Type);
                     Assert.Equal(OperationName.Delete, op.Operation);
                 })
                 .Returns(Task.CompletedTask)
                 .Verifiable();

 
            // Act
             await _service.CreateValidContactsOperationsAsync();

            // Assert
            _mockOperationRepository.Verify(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()), Times.Exactly(2));
            Assert.Equal(OperationName.Delete, capturedOperationType);
            _mockContactRepository.Verify(repo => repo.InsertContactNewAudit(It.IsAny<RefContactEntity>(), It.IsAny<string>()), Times.Never);
            _mockRoleRepository.Verify(r => r.GetRolesForContactAsync(TestEmail), Times.Once);
            _mockRoleRepository.Verify(r => r.UpdatePulseRole(It.IsAny<RoleEntity>()), Times.Never);
        }

        #endregion

        #region UPDATE Branch Tests

        [Fact]
        public async Task ValidateContactsOperationsAsync_Update_ContactExists_CallsInsertNewOperation()
        {
            // Arrange
            var contact = new RefContactEntity
            {
                EntityId = Guid.NewGuid(),
                Email = TestEmail,
                FirstName = TestFirstName,
                LastName = TestLastName,
                OperationType = OperationName.Update
            };

            SetupGetRefContactsPagedAsync(contact);

            _mockContactRepository
                .Setup(repo => repo.DoesContactExistAsync(TestEmail))
                .ReturnsAsync(true);

            _mockOperationRepository
                .Setup(repo => repo.FindContactsReadyOperationsAsync(TestEmail, OperationName.Insert))
                .ReturnsAsync(0);

            string capturedOperationType = null;

            _mockOperationRepository
                .Setup(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()))
                .Callback<RegOperationEntity>(op =>
                {
                    capturedOperationType = op.Operation;
                    Assert.Equal(OperationName.Update, op.Operation);
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await _service.CreateValidContactsOperationsAsync();

            // Assert
            _mockOperationRepository.Verify(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()), Times.Once);
            Assert.Equal(OperationName.Update, capturedOperationType);
            _mockContactRepository.Verify(repo => repo.InsertContactNewAudit(It.IsAny<RefContactEntity>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ValidateContactsOperationsAsync_Update_ContactNotExists_FindReadyOperationsNull_CallsInsertContactNewAudit()
        {
            // Arrange
            var contact = new RefContactEntity
            {
                EntityId = Guid.NewGuid(),
                Email = TestEmail,
                FirstName = TestFirstName,
                LastName = TestLastName,
                OperationType = OperationName.Update
            };

            SetupGetRefContactsPagedAsync(contact);

            _mockContactRepository
                .Setup(repo => repo.DoesContactExistAsync(TestFirstName, TestLastName, TestEmail))
                .ReturnsAsync(false);

            _mockOperationRepository
                .Setup(repo => repo.FindContactsReadyOperationsAsync(TestEmail, OperationName.Insert))
                .ReturnsAsync(0);

            string capturedMessage = null;

            _mockContactRepository
                .Setup(repo => repo.InsertContactNewAudit(contact, It.IsAny<string>()))
                .Callback<RefContactEntity, string>((c, msg) =>
                {
                    capturedMessage = msg;
                    Assert.Contains("skipping creating an update contact operation", msg, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains(TestEmail, msg);
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await _service.CreateValidContactsOperationsAsync();

            // Assert
            _mockContactRepository.Verify(repo => repo.InsertContactNewAudit(contact, It.IsAny<string>()), Times.Once);
            Assert.NotNull(capturedMessage);
            _mockOperationRepository.Verify(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()), Times.Never);
        }

        [Fact]
        public async Task ValidateContactsOperationsAsync_Update_ContactNotExists_MultipleReadyOperations_CallsInsertContactNewAudit()
        {
            // Arrange
            var contact = new RefContactEntity
            {
                EntityId = Guid.NewGuid(),
                Email = TestEmail,
                FirstName = TestFirstName,
                LastName = TestLastName,
                OperationType = OperationName.Update
            };

            var existingOperation = new RegOperationEntity()
            {
                ApprovalStatus = ApprovalStatus.Approved,
                Operation = OperationName.Insert,
                ProcessStatus = ProcessStatus.Ready,
                EntityId = contact.EntityId,
            };

            SetupGetRefContactsPagedAsync(contact);

            _mockContactRepository
                .Setup(repo => repo.DoesContactExistAsync(TestFirstName, TestLastName, TestEmail))
                .ReturnsAsync(false);

            var readyOperations = new List<RegOperationEntity> { existingOperation , existingOperation };
            _mockOperationRepository
                .Setup(repo => repo.FindContactsReadyOperationsAsync(TestEmail, OperationName.Insert))
                .ReturnsAsync(readyOperations.Count());

            string capturedMessage = null;
            _mockContactRepository
                .Setup(repo => repo.InsertContactNewAudit(contact, It.IsAny<string>()))
                .Callback<RefContactEntity, string>((c, msg) =>
                {
                    capturedMessage = msg;
                    Assert.Contains("found an unexpected behaviour", msg, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains(TestEmail, msg);
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await _service.CreateValidContactsOperationsAsync();

            // Assert
            _mockContactRepository.Verify(repo => repo.InsertContactNewAudit(contact, It.IsAny<string>()), Times.Once);
            Assert.NotNull(capturedMessage);
            _mockOperationRepository.Verify(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()), Times.Never);
        }

        [Fact]
        public async Task ValidateContactsOperationsAsync_Update_ContactNotExists_OneReadyOperation_CallsInsertNewOperation()
        {
            // Arrange
            var contact = new RefContactEntity
            {
                EntityId = Guid.NewGuid(),
                Email = TestEmail,
                FirstName = TestFirstName,
                LastName = TestLastName,
                OperationType = OperationName.Update
            };

            var operation = new RegOperationEntity()
            {
                ApprovalStatus = ApprovalStatus.Approved,
                ProcessStatus = ProcessStatus.Ready,
                EntityId = contact.EntityId,
                Operation = OperationName.Insert
            };

            SetupGetRefContactsPagedAsync(contact);

            _mockContactRepository
                .Setup(repo => repo.DoesContactExistAsync(TestFirstName, TestLastName, TestEmail))
                .ReturnsAsync(false);

            var readyOperations = new List<RegOperationEntity> { operation };
            _mockOperationRepository
                .Setup(repo => repo.FindContactsReadyOperationsAsync(TestEmail, OperationName.Insert))
                .ReturnsAsync(readyOperations.Count());

            string capturedOperationType = null;
            _mockOperationRepository
                .Setup(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()))
                .Callback<RegOperationEntity>(op =>
                {
                    capturedOperationType = op.Operation;
                    Assert.Equal(OperationName.Update, op.Operation);
                })
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await _service.CreateValidContactsOperationsAsync();

            // Assert
            _mockOperationRepository.Verify(repo => repo.InsertNewOperation(It.IsAny<RegOperationEntity>()), Times.Once);
            Assert.Equal(OperationName.Update, capturedOperationType);
            _mockContactRepository.Verify(repo => repo.InsertContactNewAudit(It.IsAny<RefContactEntity>(), It.IsAny<string>()), Times.Never);
        }

        #endregion
    }
}

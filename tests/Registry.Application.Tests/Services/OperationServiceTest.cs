using Application.Consts;
using Application.Enums;
using Application.Interfaces;
using Application.Models;
using Application.Options;
using Application.Requests;
using Application.Services;
using AutoFixture;
using FluentAssertions;
using Kpmg.ExceptionMiddleware.AdvancedException;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;

namespace Registry.Application.Tests.Services
{
    public class OperationServiceTests
    {
        private readonly Fixture _fixture;
        private readonly Mock<IOperationRepository> _operationRepositoryMock;
        private readonly Mock<ILogger<OperationService>> _loggerMock;
        private readonly IOptions<BackGroundJobOptions> _options;
        private readonly IOptions<OperationOptions> _operationOptions;
        private readonly OperationService _operationService;

        public OperationServiceTests()
        {
            _fixture = new Fixture();
            _operationRepositoryMock = new Mock<IOperationRepository>(MockBehavior.Strict);
            _loggerMock = new Mock<ILogger<OperationService>>();
            _options = Options.Create(new BackGroundJobOptions
            {
                Chunk = 5,
                TimeToWaitBeforeEachStep = 1
            });
            _operationOptions = Options.Create(new OperationOptions
            {
                MaxAccountGettingOperations = 10
            });

            _operationService = new OperationService(
                _operationRepositoryMock.Object,
                _loggerMock.Object,
                _options,
                _operationOptions
            );
        }

        #region GetOperationsAsync Tests

        [Fact]
        public async Task GetOperationsAsync_WhenNoOperationsInMainTables_FallbacksToAccountMainAndContactRefTables()
        {
            // Arrange
            string testAccountNumber = "ACC123";
            var searchCriteria = new OperationSearchCriteria
            {
                OperationName = OperationAction.Insert
            };
            var expectedDetails = new List<RegOperationDetail?>
            {
                new RegOperationDetail
                {
                    OperationId = 10,
                    OperationName = OperationAction.Insert,
                    Email = "fallback@test.com",
                    AccountNumber = testAccountNumber
                }
            };

            _operationRepositoryMock
                .Setup(repo => repo.FetchOperationsByAccountNumberAsync(
                    testAccountNumber,
                    searchCriteria,
                    OperationSource.MainTables))
                .ReturnsAsync(Enumerable.Empty<RegOperationDetail?>());

            _operationRepositoryMock
                .Setup(repo => repo.FetchOperationsByAccountNumberAsync(
                    testAccountNumber,
                    searchCriteria,
                    OperationSource.AccountMainAndContactRefTables))
                .ReturnsAsync(expectedDetails);

            // Act
            var result = await _operationService.GetOperationsAsync(testAccountNumber, searchCriteria);

            // Assert
            _operationRepositoryMock.VerifyAll();
            result.Should().BeEquivalentTo(expectedDetails);
        }

        [Fact]
        public async Task GetOperationsAsync_ThrowsArgumentNullException_IfAccountNumberIsNull()
        {
            // Arrange
            string accountNumber = null!;
            var searchCriteria = new OperationSearchCriteria();

            // Act
            Func<Task> act = async () => await _operationService.GetOperationsAsync(accountNumber, searchCriteria);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        #endregion

        #region UpdateOperationByIdAsync Tests

        [Fact]
        public async Task UpdateOperationByIdAsync_SetsLastStatusUpdatedBy_AndReturnsUpdatedOperation()
        {
            // Arrange
            int operationId = 1;
            string email = "email@test.fr";
            var providedRegOperation = new RegOperation
            {
                Id = operationId,
                Status = "PENDING",
                LastStatusUpdatedBy = null
            };
            var expectedUpdatedOperation = new RegOperation
            {
                Id = operationId,
                Status = "PENDING",
                LastStatusUpdatedBy = email
            };

            _operationRepositoryMock
                .Setup(repo => repo.UpdateOperationByIdAsync(operationId, It.IsAny<RegOperation>()))
                .ReturnsAsync(expectedUpdatedOperation);

            // Act
            var updated = await _operationService.UpdateOperationByIdAsync(operationId, email, providedRegOperation);

            // Assert
            _operationRepositoryMock.Verify(
                repo => repo.UpdateOperationByIdAsync(operationId,
                    It.Is<RegOperation>(r => r.LastStatusUpdatedBy == email)),
                Times.Once);
            updated.Should().NotBeNull();
            updated!.LastStatusUpdatedBy.Should().Be(email);
            updated.Status.Should().Be("PENDING");
        }

        [Fact]
        public async Task UpdateOperationByIdAsync_ThrowsArgumentNullException_IfEmailIsNull()
        {
            // Arrange
            string email = null!;
            var sampleOperation = new RegOperation { Id = 1, Status = "PENDING" };

            // Act
            Func<Task> act = async () => await _operationService.UpdateOperationByIdAsync(1, email, sampleOperation);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        #endregion

        #region GetOperationByIdAsync Tests

        [Fact]
        public async Task GetOperationByIdAsync_ReturnsExpectedOperation()
        {
            // Arrange
            int operationId = 99;
            var expectedOperation = new RegOperation
            {
                Id = operationId,
                Status = "APPROVED"
            };

            _operationRepositoryMock
                .Setup(r => r.GetOperationByIdAsync(operationId))
                .ReturnsAsync(expectedOperation);

            // Act
            var result = await _operationService.GetOperationByIdAsync(operationId);

            // Assert
            result.Should().BeEquivalentTo(expectedOperation);
            _operationRepositoryMock.Verify(r => r.GetOperationByIdAsync(operationId), Times.Once);
        }

        #endregion

        #region UpdateContactOperations Tests

        [Fact]
        public async Task UpdateContactOperations_UpdatesOperationsAndReturnsTrue_WhenOperationsExist()
        {
            // Arrange
            var searchCriteria = new OperationSearchCriteria { OperationName = OperationAction.Insert };
            string email = "contact@domain.com";

            var existingOperations = new List<RegOperationEntity>
            {
                new RegOperationEntity { EntityId = Guid.NewGuid(), Id = 1, ProcessStatus = ProcessStatus.Ready, ApprovalStatus = "APPROVED" },
                new RegOperationEntity {EntityId = Guid.NewGuid(), Id = 2, ProcessStatus = ProcessStatus.Ready, ApprovalStatus = "APPROVED" }
            };

            _operationRepositoryMock
                .Setup(r => r.FetchOperationsByCriteriaAsync(
                    searchCriteria,
                    OperationStrategyType.CONTACT,
                    email,
                    false,
                    null))
                .ReturnsAsync(existingOperations);

            _operationRepositoryMock
                .Setup(r => r.BulkUpdateOperationsStatusAsync(ProcessStatus.Succeeded, existingOperations))
                .ReturnsAsync(true);

            // Act
            var result = await _operationService.UpdateContactOperations(searchCriteria, email);

            // Assert
            result.Should().BeTrue();
            _operationRepositoryMock.VerifyAll();
        }

        [Fact]
        public async Task UpdateContactOperations_LogsWarningAndReturnsTrue_WhenNoOperationsExist()
        {
            // Arrange
            var searchCriteria = new OperationSearchCriteria { OperationName = OperationAction.Update };
            string email = "contact@domain.com";
            IEnumerable<RegOperationEntity> emptyList = Array.Empty<RegOperationEntity>();

            _operationRepositoryMock
                .Setup(r => r.FetchOperationsByCriteriaAsync(
                    searchCriteria,
                    OperationStrategyType.CONTACT,
                    email,
                    false,
                    null))
                .ReturnsAsync(emptyList);

            // Act
            var result = await _operationService.UpdateContactOperations(searchCriteria, email);

            // Assert
            result.Should().BeTrue();
            _operationRepositoryMock.Verify(
                r => r.BulkUpdateOperationsStatusAsync(It.IsAny<string>(), It.IsAny<IEnumerable<RegOperationEntity>>()),
                Times.Never);
        }

        #endregion

        #region TryToProceedUntilTimeoutAsync Tests

        [Fact]
        public async Task TryToProceedUntilTimeoutAsync_WhenOperationsRemainAfterTimeout_SetsThemFailed()
        {
            // Arrange
            string entityType = OperationCategory.ACCOUNT;
            string operationType = OperationAction.Insert;
            var entityList = new List<RegOperationEntity>
            {
                new RegOperationEntity {EntityId = Guid.NewGuid(), Id = 10, ProcessStatus = ProcessStatus.Sent , ApprovalStatus = ApprovalStatus.Approved}
            };

            _operationRepositoryMock.SetupSequence(repo => repo.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                It.IsAny<OperationStrategyType>(),
                It.IsAny<string>(),
                false,
                null))
            .ReturnsAsync(entityList)
            .ReturnsAsync(entityList)
            .ReturnsAsync(entityList);

            _operationRepositoryMock
                .Setup(r => r.BulkUpdateOperationsStatusAsync(ProcessStatus.Failed, entityList))
                .ReturnsAsync(true);

            _operationRepositoryMock
                .Setup(repo => repo.FetchOperationsByCriteriaAsync(
                    It.IsAny<OperationSearchCriteria>(),
                    It.IsAny<OperationStrategyType>(),
                    It.IsAny<string>(),
                    false,
                    null))
                .ReturnsAsync(entityList);

            // Act
            await _operationService.TryToProceedUntilTimeoutAsync(entityType, operationType);

            // Assert
            _operationRepositoryMock.Verify(
                r => r.BulkUpdateOperationsStatusAsync(ProcessStatus.Failed, entityList),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task TryToProceedUntilTimeoutAsync_WhenNoOperationsAfterRetries_DoesNotSetThemFailed()
        {
            // Arrange
            string entityType = OperationCategory.CONTACT;
            string operationType = OperationAction.Update;
            var entityList = new List<RegOperationEntity>(); // empty from the start

            _operationRepositoryMock
                .Setup(repo => repo.FetchOperationsByCriteriaAsync(
                    It.IsAny<OperationSearchCriteria>(),
                    It.IsAny<OperationStrategyType>(),
                    It.IsAny<string>(),
                    false,
                    null))
                .ReturnsAsync(entityList);

            // Act
            await _operationService.TryToProceedUntilTimeoutAsync(entityType, operationType);

            // Assert
            _operationRepositoryMock.Verify(
                r => r.BulkUpdateOperationsStatusAsync(It.IsAny<string>(), It.IsAny<IEnumerable<RegOperationEntity>>()),
                Times.Never);
        }

        #endregion

        #region UpdateOperationStatusListASync Tests

        [Fact]
        public async Task UpdateOperationStatusListASync_CallsRepositoryBulkUpdate()
        {
            // Arrange
            string newStatus = ProcessStatus.Sent;
            var ops = new List<RegOperationEntity>
            {
                new RegOperationEntity {EntityId = Guid.NewGuid(), Id = 101, Operation = OperationAction.Insert, ApprovalStatus = ApprovalStatus.Approved },
                new RegOperationEntity {EntityId = Guid.NewGuid(), Id = 102, Operation = OperationAction.Update, ApprovalStatus = ApprovalStatus.Approved }
            };

            _operationRepositoryMock
                .Setup(r => r.BulkUpdateOperationsStatusAsync(newStatus, ops))
                .ReturnsAsync(true);

            // Act
            await _operationService.UpdateOperationStatusListASync(newStatus, ops);

            // Assert
            _operationRepositoryMock.Verify(r => r.BulkUpdateOperationsStatusAsync(newStatus, ops), Times.Once);
        }

        #endregion

        #region GetAccountOperationRecordsBatch Tests

        [Fact]
        public void GetAccountOperationRecordsBatch_ReturnsLimitedResultBasedOnChunkSize()
        {
            // Arrange
            string operationName = OperationAction.Insert;
            int chunkSize = 2;
            var records = new List<AccountOperationRecord>
            {
                new AccountOperationRecord { Operation = new RegOperationEntity { EntityId = Guid.NewGuid(),ApprovalStatus = ApprovalStatus.Approved, Id = 1 }, Account = null },
                new AccountOperationRecord { Operation = new RegOperationEntity { EntityId = Guid.NewGuid(),ApprovalStatus = ApprovalStatus.Approved, Id = 2 }, Account = null },
                new AccountOperationRecord { Operation = new RegOperationEntity { EntityId = Guid.NewGuid(),ApprovalStatus = ApprovalStatus.Approved, Id = 3 }, Account = null }
            };

            var mockQueryable = records.AsQueryable();
            _operationRepositoryMock
                .Setup(r => r.GetAccountOperationDetails(operationName))
                .Returns(mockQueryable);

            // Act
            var batch = _operationService.GetAccountOperationRecordsBatch(operationName, chunkSize);

            // Assert
            batch.Count.Should().Be(chunkSize);
        }

        #endregion

        #region GeContactOperationRecords Tests

        [Fact]
        public void GeContactOperationRecords_ReturnsAllContactOperationRecords()
        {
            // Arrange
            string operationType = OperationAction.Insert;
            var expectedRecords = new List<ContactOperationRecord>
            {
                new ContactOperationRecord
                {
                    Operation = new RegOperationEntity {EntityId= Guid.NewGuid(), ApprovalStatus = ApprovalStatus.Approved, Id = 10 },
                    RefContactEntity = new Pulse.Registry.Domain.Entities.RefContactEntity()
                }
            };

            _operationRepositoryMock
                .Setup(r => r.GeContactOperationDetails(operationType))
                .Returns(expectedRecords.AsQueryable());

            // Act
            var result = _operationService.GeContactOperationRecords(operationType);

            // Assert
            result.Should().BeEquivalentTo(expectedRecords);
        }

        #endregion

        #region GetRoleOperationRecordsAsync Tests

        [Fact]
        public async Task GetRoleOperationRecordsAsync_CallsRepositoryMethod()
        {
            // Arrange
            string operationName = OperationAction.Delete;
            int chunkSize = 5;
            bool? fetchSystemCreated = false;

            var expectedRecords = new List<RoleOperationRecord>
            {
                new RoleOperationRecord
                {
                    Operation = new RegOperationEntity {EntityId = Guid.NewGuid(), ApprovalStatus =  ApprovalStatus.Approved, Id = 111},
                    Role = new Pulse.Registry.Domain.Entities.RefRoleEntity(),
                    RoleCount = 2
                }
            };

            _operationRepositoryMock
                .Setup(r => r.GeRoleOperationDetailsAsync(operationName, chunkSize, fetchSystemCreated))
                .ReturnsAsync(expectedRecords);

            // Act
            var result = await _operationService.GetRoleOperationRecordsAsync(operationName, chunkSize, fetchSystemCreated);

            // Assert
            result.Should().BeEquivalentTo(expectedRecords);
            _operationRepositoryMock.Verify(
                r => r.GeRoleOperationDetailsAsync(operationName, chunkSize, fetchSystemCreated),
                Times.Once);
        }

        #endregion

        #region GetPendingRoleApprovalsAsync

        [Theory]
        [InlineData(1, 10, 1)]
        [InlineData(1, 10, 100)]
        [InlineData(1, 10, 0)]
        public async Task GetPendingRoleApprovalsAsync_ReturnsPagedResult(int pageNumber, int pageSize, int totalCount)
        {
            // Arrange
            int skip = (pageNumber - 1) * pageSize;
            int contactId = this._fixture.Create<int>();
            string? search = null;
            int expectedTotalPage = (int)Math.Ceiling((double)totalCount / pageSize);
            var expectedRecords = this._fixture.CreateMany<PendingRoleApprovals>(totalCount);
            var expectedWrappedResult = new PendingRoleApprovalsResult()
            {
                PendingRoleApprovals = expectedRecords,
                TotalItems = totalCount
            };
            _operationRepositoryMock.Setup(r => r.GetPendingRoleApprovalsAsync(contactId, skip, pageSize, search)).ReturnsAsync(expectedWrappedResult);

            // Act
            var result = await _operationService.GetPendingRoleApprovalsAsync(contactId, pageNumber, pageSize, search);

            // Assert
            result.Items.Should().BeEquivalentTo(expectedWrappedResult.PendingRoleApprovals);
            result.CurrentPage.Should().Be(pageNumber);
            result.PageSize.Should().Be(pageSize);
            result.TotalItems.Should().Be(expectedWrappedResult.TotalItems);
            result.TotalPages.Should().Be(expectedTotalPage);
            _operationRepositoryMock.Verify(r => r.GetPendingRoleApprovalsAsync(contactId, skip, pageSize, search), Times.Once);
        }

        [Fact]
        public async Task GetPendingRoleApprovalsAsync_ThrowsArgumentNullException_IfContactIdIsZero()
        {
            // Arrange
            int contactId = 0;
            int pageNumber = 1;
            int pageSize = 10;
            string? search = null;

            // Act
            Func<Task> act = async () => await _operationService.GetPendingRoleApprovalsAsync(contactId, pageNumber, pageSize, search);

            // Assert
            await act.Should().ThrowAsync<TechnicalException>();
        }

        [Fact]
        public async Task GetPendingRoleApprovalsAsync_ThrowsTechnicalException_WhenPageSizeExcessed()
        {
            // Arrange
            int contactId = 1;
            int pageNumber = 1;
            int pageSize = 15;
            string? search = null;

            // Act
            Func<Task> act = async () => await _operationService.GetPendingRoleApprovalsAsync(contactId, pageNumber, pageSize, search);

            // Assert
            await act.Should().ThrowAsync<TechnicalException>();
        }

        [Fact]
        public async Task GetPendingRoleApprovalsAsync_WithSearchParam_ReturnsFilteredResults()
        {
            // Arrange
            int pageNumber = 1;
            int pageSize = 10;
            int contactId = this._fixture.Create<int>();
            string search = "ACC1";
            var expectedRecords = this._fixture.CreateMany<PendingRoleApprovals>(10);
            var expectedWrappedResult = new PendingRoleApprovalsResult()
            {
                PendingRoleApprovals = expectedRecords,
                TotalItems = 10
            };
            _operationRepositoryMock.Setup(r => r.GetPendingRoleApprovalsAsync(contactId, 0, pageSize, search)).ReturnsAsync(expectedWrappedResult);

            // Act
            var result = await _operationService.GetPendingRoleApprovalsAsync(contactId, pageNumber, pageSize, search);

            // Assert
            result.Items.Should().BeEquivalentTo(expectedWrappedResult.PendingRoleApprovals);
            result.CurrentPage.Should().Be(pageNumber);
            result.PageSize.Should().Be(pageSize);
            result.TotalItems.Should().Be(expectedWrappedResult.TotalItems);
            result.TotalPages.Should().Be(1);
            _operationRepositoryMock.Verify(r => r.GetPendingRoleApprovalsAsync(contactId, 0, pageSize, search), Times.Once);
        }

        #endregion GetPendingRoleApprovalsAsync
    }
}

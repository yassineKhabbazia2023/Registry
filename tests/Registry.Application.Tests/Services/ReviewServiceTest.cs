using Application.Interfaces;
using Application.services;
using Application.Services;
using AutoFixture;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.Tests.Services
{
    public class ReviewServiceTest
    {
        private readonly Fixture _fixture;
        private readonly Mock<ILogger<ReviewService>> _logger;
        private readonly Mock<IReviewRepository> _reviewRepository;
        private readonly Mock<IRoleService> _roleService;

        public ReviewServiceTest()
        {
            _fixture = new Fixture();
            _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            _logger = new Mock<ILogger<ReviewService>>();
            _reviewRepository = new Mock<IReviewRepository>();
            _roleService = new Mock<IRoleService>();
        }

        #region Happy Path Tests

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Be_Success()
        {
            // Arrange
            _reviewRepository.Setup(x => x.ReviewChangeEmailAsync())
                .Returns(Task.CompletedTask);

            _roleService.Setup(x => x.ReviewFailedRolesOperationsAsync())
                .ReturnsAsync(new List<bool>());

            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act
            await service.ReviewChangeEmailAsync();

            // Assert
            _roleService.Verify(x => x.ReviewFailedRolesOperationsAsync(), Times.Once);
            _reviewRepository.Verify(x => x.ReviewChangeEmailAsync(), Times.Once);
        }

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Complete_Without_Throwing()
        {
            // Arrange
            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act
            var exception = await Record.ExceptionAsync(() => service.ReviewChangeEmailAsync());

            // Assert
            Assert.Null(exception);
        }

        #endregion

        #region Logging Tests - Start

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Log_Information_On_Start()
        {
            // Arrange
            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act
            await service.ReviewChangeEmailAsync();

            // Assert
            _logger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Review change email started")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Log_Start_With_Timestamp()
        {
            // Arrange
            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act
            await service.ReviewChangeEmailAsync();

            // Assert
            _logger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ReviewChangeEmailAsync")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        #endregion

        #region Logging Tests - Completion

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Log_Information_On_Completion()
        {
            // Arrange
            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act
            await service.ReviewChangeEmailAsync();

            // Assert
            _logger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Review change email finished")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Log_Completion_With_Timestamp()
        {
            // Arrange
            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act
            await service.ReviewChangeEmailAsync();

            // Assert
            _logger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ReviewChangeEmailAsync")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Exactly(2)); // Start and finish logs
        }

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Log_Two_Information_Messages()
        {
            // Arrange
            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act
            await service.ReviewChangeEmailAsync();

            // Assert
            _logger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Exactly(2));
        }

        #endregion

        #region Error Tests - RoleService Failures

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Log_Error_When_RoleService_Fails()
        {
            // Arrange
            var expectedException = new Exception("Role service failed");
            _roleService.Setup(x => x.ReviewFailedRolesOperationsAsync())
                .ThrowsAsync(expectedException);

            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewChangeEmailAsync());

            _logger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Review change email failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Throw_InvalidOperationException_When_RoleService_Fails()
        {
            // Arrange
            var expectedException = new Exception("Role service failed");
            _roleService.Setup(x => x.ReviewFailedRolesOperationsAsync())
                .ThrowsAsync(expectedException);

            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewChangeEmailAsync());
            Assert.Equal("Failed to review change email operations", exception.Message);
            Assert.NotNull(exception.InnerException);
            Assert.Equal("Role service failed", exception.InnerException.Message);
        }

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Not_Call_Repository_When_RoleService_Fails()
        {
            // Arrange
            _roleService.Setup(x => x.ReviewFailedRolesOperationsAsync())
                .ThrowsAsync(new Exception("Role service failed"));

            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewChangeEmailAsync());

            _reviewRepository.Verify(x => x.ReviewChangeEmailAsync(), Times.Never);
        }

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Log_Error_With_Exception_Details_When_RoleService_Fails()
        {
            // Arrange
            var expectedException = new Exception("Detailed role service error");
            _roleService.Setup(x => x.ReviewFailedRolesOperationsAsync())
                .ThrowsAsync(expectedException);

            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewChangeEmailAsync());

            _logger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.Is<Exception>(ex => ex.Message == "Detailed role service error"),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Wrap_Exception_When_RoleService_Fails()
        {
            // Arrange
            var expectedException = new Exception("Specific role service error");
            _roleService.Setup(x => x.ReviewFailedRolesOperationsAsync())
                .ThrowsAsync(expectedException);

            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewChangeEmailAsync());
            Assert.Same(expectedException, exception.InnerException);
        }

        #endregion

        #region Error Tests - Repository Failures

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Log_Error_When_Repository_Fails()
        {
            // Arrange
            var expectedException = new Exception("Repository failed");
            _reviewRepository.Setup(x => x.ReviewChangeEmailAsync())
                .ThrowsAsync(expectedException);

            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewChangeEmailAsync());

            _logger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Review change email failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Throw_InvalidOperationException_When_Repository_Fails()
        {
            // Arrange
            var expectedException = new Exception("Repository failed");
            _reviewRepository.Setup(x => x.ReviewChangeEmailAsync())
                .ThrowsAsync(expectedException);

            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewChangeEmailAsync());
            Assert.Equal("Failed to review change email operations", exception.Message);
            Assert.NotNull(exception.InnerException);
            Assert.Equal("Repository failed", exception.InnerException.Message);
        }

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Call_RoleService_Before_Repository_Failure()
        {
            // Arrange
            _reviewRepository.Setup(x => x.ReviewChangeEmailAsync())
                .ThrowsAsync(new Exception("Repository failed"));

            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewChangeEmailAsync());

            _roleService.Verify(x => x.ReviewFailedRolesOperationsAsync(), Times.Once);
        }

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Log_Error_With_Exception_Details_When_Repository_Fails()
        {
            // Arrange
            var expectedException = new Exception("Detailed repository error");
            _reviewRepository.Setup(x => x.ReviewChangeEmailAsync())
                .ThrowsAsync(expectedException);

            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewChangeEmailAsync());

            _logger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.Is<Exception>(ex => ex.Message == "Detailed repository error"),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Wrap_Exception_When_Repository_Fails()
        {
            // Arrange
            var expectedException = new Exception("Specific repository error");
            _reviewRepository.Setup(x => x.ReviewChangeEmailAsync())
                .ThrowsAsync(expectedException);

            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewChangeEmailAsync());
            Assert.Same(expectedException, exception.InnerException);
        }

        #endregion

        #region Execution Order Tests

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Call_RoleService_Before_Repository()
        {
            // Arrange
            var callOrder = new List<string>();

            _roleService.Setup(x => x.ReviewFailedRolesOperationsAsync())
                .Callback(() => callOrder.Add("RoleService"))
                .ReturnsAsync(new List<bool>());

            _reviewRepository.Setup(x => x.ReviewChangeEmailAsync())
                .Callback(() => callOrder.Add("Repository"))
                .Returns(Task.CompletedTask);

            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act
            await service.ReviewChangeEmailAsync();

            // Assert
            Assert.Equal(2, callOrder.Count);
            Assert.Equal("RoleService", callOrder[0]);
            Assert.Equal("Repository", callOrder[1]);
        }

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Log_Start_Before_RoleService_Call()
        {
            // Arrange
            var callOrder = new List<string>();

            _logger.Setup(x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("started")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                .Callback(() => callOrder.Add("StartLog"));

            _roleService.Setup(x => x.ReviewFailedRolesOperationsAsync())
                .Callback(() => callOrder.Add("RoleService"))
                .ReturnsAsync(new List<bool>());

            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act
            await service.ReviewChangeEmailAsync();

            // Assert
            Assert.True(callOrder.IndexOf("StartLog") < callOrder.IndexOf("RoleService"));
        }

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Log_Finish_After_Repository_Call()
        {
            // Arrange
            var callOrder = new List<string>();

            _reviewRepository.Setup(x => x.ReviewChangeEmailAsync())
                .Callback(() => callOrder.Add("Repository"))
                .Returns(Task.CompletedTask);

            _logger.Setup(x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("finished")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                .Callback(() => callOrder.Add("FinishLog"));

            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act
            await service.ReviewChangeEmailAsync();

            // Assert
            Assert.True(callOrder.IndexOf("Repository") < callOrder.IndexOf("FinishLog"));
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Not_Log_Completion_When_RoleService_Fails()
        {
            // Arrange
            _roleService.Setup(x => x.ReviewFailedRolesOperationsAsync())
                .ThrowsAsync(new Exception("Role service failed"));

            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewChangeEmailAsync());

            _logger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("finished")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Not_Log_Completion_When_Repository_Fails()
        {
            // Arrange
            _reviewRepository.Setup(x => x.ReviewChangeEmailAsync())
                .ThrowsAsync(new Exception("Repository failed"));

            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewChangeEmailAsync());

            _logger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("finished")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Log_Start_Even_When_RoleService_Fails()
        {
            // Arrange
            _roleService.Setup(x => x.ReviewFailedRolesOperationsAsync())
                .ThrowsAsync(new Exception("Role service failed"));

            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewChangeEmailAsync());

            _logger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("started")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Log_Start_Even_When_Repository_Fails()
        {
            // Arrange
            _reviewRepository.Setup(x => x.ReviewChangeEmailAsync())
                .ThrowsAsync(new Exception("Repository failed"));

            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewChangeEmailAsync());

            _logger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("started")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Log_Only_One_Error_When_Exception_Occurs()
        {
            // Arrange
            _roleService.Setup(x => x.ReviewFailedRolesOperationsAsync())
                .ThrowsAsync(new Exception("Failed"));

            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewChangeEmailAsync());

            _logger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Include_Exception_In_Error_Log()
        {
            // Arrange
            var expectedException = new Exception("Test exception");
            _roleService.Setup(x => x.ReviewFailedRolesOperationsAsync())
                .ThrowsAsync(expectedException);

            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewChangeEmailAsync());

            _logger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Different Exception Types

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Handle_ArgumentNullException_From_RoleService()
        {
            // Arrange
            var expectedException = new ArgumentNullException("param");
            _roleService.Setup(x => x.ReviewFailedRolesOperationsAsync())
                .ThrowsAsync(expectedException);

            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewChangeEmailAsync());
            Assert.IsType<ArgumentNullException>(exception.InnerException);
        }

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Handle_InvalidOperationException_From_Repository()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Invalid operation");
            _reviewRepository.Setup(x => x.ReviewChangeEmailAsync())
                .ThrowsAsync(expectedException);

            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewChangeEmailAsync());
            Assert.Equal("Failed to review change email operations", exception.Message);
            Assert.Same(expectedException, exception.InnerException);
        }

        [Fact]
        public async Task ReviewChangeEmailAsync_Should_Handle_TimeoutException_From_RoleService()
        {
            // Arrange
            var expectedException = new TimeoutException("Operation timed out");
            _roleService.Setup(x => x.ReviewFailedRolesOperationsAsync())
                .ThrowsAsync(expectedException);

            var service = new ReviewService(_reviewRepository.Object, _roleService.Object, _logger.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewChangeEmailAsync());
            Assert.IsType<TimeoutException>(exception.InnerException);
        }

        #endregion
    }
}
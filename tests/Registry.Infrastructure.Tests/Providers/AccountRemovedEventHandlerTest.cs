using Application.Consts;
using Application.Interfaces;
using Infrastructure.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Registry.Infrastructure.Tests.Providers
{
    public class AccountRemovedEventHandlerTest
    {
        [Fact]
        public async Task HandleAsync_WithValidMessage_ShouldTriggerSyncAndUpdate()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<AccountRemovedEventHandler>>();
            var accountServiceMock = new Mock<IAccountService>(MockBehavior.Strict);

            var testData = new AccountRemovedEventData { AccountId = 123 };
            var eventType = "AccountRemovedEvent";
            var accountNumber = "accountnumber";

            accountServiceMock.Setup(x => x.GetAccountNumberByIdAsync(It.IsAny<int>()))
                .Returns(Task.FromResult<string?>(accountNumber));

            accountServiceMock.Setup(x => x.SyncAcountAsync(It.IsAny<AccountStateEventData>(), It.IsAny<string>()))
                .Callback<AccountStateEventData, string>((data, operation) =>
                {
                    Assert.Equal(123, data.AccountId);
                    Assert.Equal(OperationAction.Delete, operation);
                })
                .ReturnsAsync(true);

            accountServiceMock.Setup(x => x.UpdateAccountProcessStatusAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((accNumber, operation) =>
                {
                    Assert.Equal(accountNumber, accNumber);
                    Assert.Equal(OperationAction.Delete, operation);
                })
                .Returns(Task.CompletedTask);

            var handler = new AccountRemovedEventHandler(loggerMock.Object, accountServiceMock.Object);

            // Act
            var message = "{\"EventType\":\"AccountRemovedEvent\",\"Data\":{\"AccountId\":123}}";
            await handler.HandleAsync(message);

            // Assert
            accountServiceMock.Verify(x => x.GetAccountNumberByIdAsync(It.IsAny<int>()), Times.Once);
            accountServiceMock.Verify(x => x.SyncAcountAsync(It.IsAny<AccountStateEventData>(), It.IsAny<string>()), Times.Once);
            accountServiceMock.Verify(x => x.UpdateAccountProcessStatusAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithNullMessage_ShouldNotTriggerSyncAndUpdate()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<AccountRemovedEventHandler>>();
            var accountServiceMock = new Mock<IAccountService>(MockBehavior.Strict);

            var handler = new AccountRemovedEventHandler(loggerMock.Object, accountServiceMock.Object);

            // Act
            await handler.HandleAsync(null!);

            // Assert
            accountServiceMock.Verify(x => x.GetAccountNumberByIdAsync(It.IsAny<int>()), Times.Never);
            accountServiceMock.Verify(x => x.SyncAcountAsync(It.IsAny<AccountStateEventData>(), It.IsAny<string>()), Times.Never);
            accountServiceMock.Verify(x => x.UpdateAccountProcessStatusAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}

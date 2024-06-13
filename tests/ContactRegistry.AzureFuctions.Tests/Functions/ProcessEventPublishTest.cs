using Azure.Messaging.ServiceBus;
using ContactRegistry.AzureFuctions.Const;
using ContactRegistry.AzureFuctions.Functions;
using ContactRegistry.AzureFuctions.Managers;
using ContactRegistry.AzureFuctions.Message;
using ContactRegistry.Infrastructure.Tests.Utils;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Context;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using System.Data;
using System.Text;

namespace ContactRegistry.AzureFuctions.Tests.Functions
{
    public class ProcessEventPublishTest
    {
        private ApplicationDbContext context;

        public ProcessEventPublishTest()
        {
            context = DbContextMockExtensions.CreateInMemoryDbContext();
        }

        [Fact]
        public async Task ProcessEventPublish_orchestrator_messageActions_Null_Throws_ArgumentNullException()
        {
            // Arrange

            var messageBody = Encoding.UTF8.GetBytes("This is a test message");

            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: new BinaryData(messageBody),
                messageId: "123",
                partitionKey: "test-partition-key",
                contentType: "application/json"
            );

            ServiceBusMessageActions messageActions = null;

            var logger = new Mock<ILogger<ProcessEventPublish>>();
            var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>(MockBehavior.Strict);
            var notificationManager = new Mock<INotificationManager>(MockBehavior.Strict);

            // Act
            var function = new ProcessEventPublish(logger.Object, dbContextFactory.Object, notificationManager.Object);
            async Task Act() => await function.Run(receivedMessage, messageActions);

            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(Act);
        }

        [Fact]
        public async Task ProcessEventPublish_orchestrator_When_Operation_Insert_Type_Contact()
        {
            // Arrange
            var contactOperation = new CreOperation()
            {
                EntityId = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
                Operation = OperationName.Insert,
                Id = 1,
                PublishedAt = null,
                Type = OperationType.Contact
            };

            var creContact = new CreContact
            {
                Id = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
                OfficeId = Guid.NewGuid(),
                IsCustomer = true,
                IsActive = true,
                Updated = DateTime.UtcNow,
                Deleted = null,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                LandPhone = "123-456-7890",
                MobilePhone = "098-765-4321",
                JobDescription = "Developer",
                Source = "Internal",
                Roles = new List<CreRole>()
            };

            context.CreOperations.Add(contactOperation);
            context.CreContacts.Add(creContact);

            await context.SaveChangesAsync();

            var expectedEventData = new RegistryContactCreatedEventData()
            {
                Id = creContact.Id,
                IsCustomer = creContact.IsCustomer,
                FirstName = creContact.FirstName,
                LastName = creContact.LastName,
                Email = creContact.Email,
                OfficeId = creContact.OfficeId,
                LandPhone = creContact.LandPhone,
                MobilePhone = creContact.MobilePhone,
                JobDescription = creContact.JobDescription,
                Source = creContact.Source,
            };
            var registryContactCreatedEvent = new RegistryContactCreatedEvent(expectedEventData);

            var registryEntityType = new RegistryEntityType() { EntityType = OperationType.Contact };
            var messageBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(registryEntityType));

            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: new BinaryData(messageBody),
                messageId: "123",
                partitionKey: "test-partition-key",
                contentType: "application/json"
            );

            var messageActions = new Mock<ServiceBusMessageActions>();
            messageActions.Setup(x => x.CompleteMessageAsync(receivedMessage, It.IsAny<CancellationToken>()));

            var logger = new Mock<ILogger<ProcessEventPublish>>();

            var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>(MockBehavior.Strict);
            dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(context);

            var notificationManager = new Mock<INotificationManager>(MockBehavior.Strict);
            notificationManager.Setup(r => r.PublishAsync(It.IsAny<RegistryContactCreatedEvent>(), It.IsAny<string?>()))
                .Callback<BaseEvent<RegistryContactCreatedEventData>, string?>((data, t) =>
                {
                    t.Should().BeNull();
                    data.Data.Should().NotBeNull();
                    data.Data.Should().BeEquivalentTo(expectedEventData);
                })
                .Returns(Task.CompletedTask);

            var operation = await context.CreOperations.FirstAsync();

            // Act
            var function = new ProcessEventPublish(logger.Object, dbContextFactory.Object, notificationManager.Object);
            await function.Run(receivedMessage, messageActions.Object);

            // Assert
            notificationManager.VerifyAll();
            operation.PublishedAt.Should().NotBeNull();

        }

        [Fact]
        public async Task ProcessEventPublish_orchestrator_When_Operation_Update_Type_Contact()
        {
            // Arrange
            var contactOperation = new CreOperation()
            {
                EntityId = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
                Operation = OperationName.Update,
                Id = 1,
                PublishedAt = null,
                Type = OperationType.Contact
            };

            var creContact = new CreContact
            {
                Id = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
                OfficeId = Guid.NewGuid(),
                IsCustomer = true,
                IsActive = true,
                Updated = DateTime.UtcNow,
                Deleted = null,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                LandPhone = "123-456-7890",
                MobilePhone = "098-765-4321",
                JobDescription = "Developer",
                Source = "Internal",
                Roles = new List<CreRole>()
            };

            context.CreOperations.Add(contactOperation);
            context.CreContacts.Add(creContact);

            await context.SaveChangesAsync();

            var expectedEventData = new RegistryContactUpdatedEventData()
            {
                Id = creContact.Id,
                Email = creContact.Email,
                OfficeId = creContact.OfficeId,
                JobDescription = creContact.JobDescription,
                MobilePhone = "098-765-4321",
                LandPhone = "123-456-7890",
                FirstName = "John",
                LastName = "Doe",
                IsCustomer = true,
            };

            var registryContactUpdatedEvent = new RegistryContactUpdatedEvent(expectedEventData);

            var registryEntityType = new RegistryEntityType() { EntityType = OperationType.Contact };
            var messageBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(registryEntityType));

            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: new BinaryData(messageBody),
                messageId: "123",
                partitionKey: "test-partition-key",
                contentType: "application/json"
            );

            var messageActions = new Mock<ServiceBusMessageActions>();
            messageActions.Setup(x => x.CompleteMessageAsync(receivedMessage, It.IsAny<CancellationToken>()));

            var logger = new Mock<ILogger<ProcessEventPublish>>();

            var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>(MockBehavior.Strict);
            dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(context);

            var notificationManager = new Mock<INotificationManager>(MockBehavior.Loose);
            notificationManager.Setup(r => r.PublishAsync(It.IsAny<RegistryContactUpdatedEvent>(), It.IsAny<string?>()))
                .Callback<BaseEvent<RegistryContactUpdatedEventData>, string?>((data, t) =>
                {
                    t.Should().BeNull();
                    data.Data.Should().NotBeNull();
                    data.Data.Should().BeEquivalentTo(expectedEventData);
                })
                .Returns(Task.CompletedTask);

            var operation = await context.CreOperations.FirstAsync();

            // Act
            var function = new ProcessEventPublish(logger.Object, dbContextFactory.Object, notificationManager.Object);
            await function.Run(receivedMessage, messageActions.Object);

            // Assert
            notificationManager.VerifyAll();
            operation.PublishedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task ProcessEventPublish_orchestrator_When_Operation_Remove_Type_Contact()
        {
            // Arrange
            var contactOperation = new CreOperation()
            {
                EntityId = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
                Operation = OperationName.Delete,
                Id = 1,
                PublishedAt = null,
                Type = OperationType.Contact
            };

            var creContact = new CreContact
            {
                Id = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
                OfficeId = Guid.NewGuid(),
                IsCustomer = true,
                IsActive = true,
                Updated = DateTime.UtcNow,
                Deleted = null,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                LandPhone = "123-456-7890",
                MobilePhone = "098-765-4321",
                JobDescription = "Developer",
                Source = "Internal",
                Roles = new List<CreRole>()
            };

            context.CreOperations.Add(contactOperation);
            context.CreContacts.Add(creContact);

            await context.SaveChangesAsync();

            var expectedEventData = new RegistryContactRemovedEventData()
            {
                Id = creContact.Id,
                Email = creContact.Email,
            };

            var registryContactRemovedEventData = new RegistryContactRemovedEvent(expectedEventData);

            var registryEntityType = new RegistryEntityType() { EntityType = OperationType.Contact };
            var messageBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(registryEntityType));

            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: new BinaryData(messageBody),
                messageId: "123",
                partitionKey: "test-partition-key",
                contentType: "application/json"
            );

            var messageActions = new Mock<ServiceBusMessageActions>();
            messageActions.Setup(x => x.CompleteMessageAsync(receivedMessage, It.IsAny<CancellationToken>()));

            var logger = new Mock<ILogger<ProcessEventPublish>>();

            var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>(MockBehavior.Strict);
            dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(context);

            var notificationManager = new Mock<INotificationManager>(MockBehavior.Loose);
            notificationManager.Setup(r => r.PublishAsync(It.IsAny<RegistryContactRemovedEvent>(), It.IsAny<string?>()))
                .Callback<BaseEvent<RegistryContactRemovedEventData>, string?>((data, t) =>
                {
                    t.Should().BeNull();
                    data.Data.Should().NotBeNull();
                    data.Data.Should().BeEquivalentTo(expectedEventData);
                })
                .Returns(Task.CompletedTask);

            var operation = await context.CreOperations.FirstAsync();

            // Act
            var function = new ProcessEventPublish(logger.Object, dbContextFactory.Object, notificationManager.Object);
            await function.Run(receivedMessage, messageActions.Object);

            // Assert
            notificationManager.VerifyAll();
            operation.PublishedAt.Should().NotBeNull();

        }

        [Fact]
        public async Task ProcessEventPublish_orchestrator_When_Operation_Insert_Type_Account()
        {
            // Arrange
            var accountOperation = new CreOperation()
            {
                EntityId = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
                Operation = OperationName.Insert,
                Id = 1,
                PublishedAt = null,
                Type = OperationType.Account
            };

            var creAccount = new CreAccount
            {
                AccountGlobalUniqueIdentifier = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
                AccountNumber = "12340",
                LegalName = "MyAccount",
                Updated = null,
                AccountFlagEscActif = true
            };

            context.CreOperations.Add(accountOperation);
            context.CreAccounts.Add(creAccount);

            await context.SaveChangesAsync();

            var expectedEventData = creAccount.ToRegistryAccountCreatedEventData();

            var registryAccountCreatedEvent = new RegistryAccountCreatedEvent(expectedEventData);

            var registryEntityType = new RegistryEntityType() { EntityType = OperationType.Account };
            var messageBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(registryEntityType));

            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: new BinaryData(messageBody),
                messageId: "123",
                partitionKey: "test-partition-key",
                contentType: "application/json"
            );

            var messageActions = new Mock<ServiceBusMessageActions>();
            messageActions.Setup(x => x.CompleteMessageAsync(receivedMessage, It.IsAny<CancellationToken>()));

            var logger = new Mock<ILogger<ProcessEventPublish>>();

            var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>(MockBehavior.Strict);
            dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(context);

            var notificationManager = new Mock<INotificationManager>(MockBehavior.Loose);
            notificationManager.Setup(r => r.PublishAsync(It.IsAny<RegistryAccountCreatedEvent>(), It.IsAny<string?>()))
                .Callback<BaseEvent<RegistryAccountCreatedEventData>, string?>((data, t) =>
                {
                    t.Should().BeNull();
                    data.Data.Should().NotBeNull();
                    data.Data.Should().BeEquivalentTo(expectedEventData);
                })
                .Returns(Task.CompletedTask);

            var operation = await context.CreOperations.FirstAsync();

            // Act
            var function = new ProcessEventPublish(logger.Object, dbContextFactory.Object, notificationManager.Object);
            await function.Run(receivedMessage, messageActions.Object);

            // Assert
            notificationManager.VerifyAll();
            operation.PublishedAt.Should().NotBeNull();

        }

        [Fact]
        public async Task ProcessEventPublish_orchestrator_When_Operation_Update_Type_Account()
        {
            // Arrange
            var accountOperation = new CreOperation()
            {
                EntityId = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
                Operation = OperationName.Update,
                Id = 1,
                PublishedAt = null,
                Type = OperationType.Account
            };

            var creAccount = new CreAccount
            {
                AccountGlobalUniqueIdentifier = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
                AccountNumber = "12340",
                LegalName = "MyAccount",
                Updated = null,
                AccountFlagEscActif = true,
            };

            context.CreOperations.Add(accountOperation);
            context.CreAccounts.Add(creAccount);

            await context.SaveChangesAsync();

            var expectedEventData = creAccount.ToRegistryAccountUpdatedEventData();
            var registryAccountUpdatedEvent = new RegistryAccountUpdatedEvent(expectedEventData);

            var registryEntityType = new RegistryEntityType() { EntityType = OperationType.Account };
            var messageBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(registryEntityType));

            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: new BinaryData(messageBody),
                messageId: "123",
                partitionKey: "test-partition-key",
                contentType: "application/json"
            );

            var messageActions = new Mock<ServiceBusMessageActions>();
            messageActions.Setup(x => x.CompleteMessageAsync(receivedMessage, It.IsAny<CancellationToken>()));

            var logger = new Mock<ILogger<ProcessEventPublish>>();

            var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>(MockBehavior.Strict);
            dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(context);

            var notificationManager = new Mock<INotificationManager>(MockBehavior.Loose);
            notificationManager.Setup(r => r.PublishAsync(It.IsAny<RegistryAccountUpdatedEvent>(), It.IsAny<string?>()))
                .Callback<BaseEvent<RegistryAccountUpdatedEventData>, string?>((data, t) =>
                {
                    t.Should().BeNull();
                    data.Data.Should().NotBeNull();
                    data.Data.Should().BeEquivalentTo(expectedEventData);
                })
                .Returns(Task.CompletedTask);

            var operation = await context.CreOperations.FirstAsync();

            // Act
            var function = new ProcessEventPublish(logger.Object, dbContextFactory.Object, notificationManager.Object);
            await function.Run(receivedMessage, messageActions.Object);

            // Assert
            notificationManager.VerifyAll();
            operation.PublishedAt.Should().NotBeNull();

        }

        [Fact]
        public async Task ProcessEventPublish_orchestrator_When_Operation_Remove_Type_Account()
        {
            // Arrange
            var accountOperation = new CreOperation()
            {
                EntityId = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
                Operation = OperationName.Delete,
                Id = 1,
                PublishedAt = null,
                Type = OperationType.Account
            };

            var creAccount = new CreAccount
            {
                AccountGlobalUniqueIdentifier = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
                AccountNumber = "12340",
                LegalName = "MyAccount",
                Updated = null
            };

            context.CreOperations.Add(accountOperation);
            context.CreAccounts.Add(creAccount);

            await context.SaveChangesAsync();

            var expectedEventData = new RegistryAccountRemovedEventData()
            {
                AccountGlobalUniqueIdentifier = creAccount.AccountGlobalUniqueIdentifier,
                AccountNumber = creAccount.AccountNumber,
            };

            var registryAccountRemovedEvent = new RegistryAccountRemovedEvent(expectedEventData);

            var registryEntityType = new RegistryEntityType() { EntityType = OperationType.Account };
            var messageBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(registryEntityType));

            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: new BinaryData(messageBody),
                messageId: "123",
                partitionKey: "test-partition-key",
                contentType: "application/json"
            );

            var messageActions = new Mock<ServiceBusMessageActions>();
            messageActions.Setup(x => x.CompleteMessageAsync(receivedMessage, It.IsAny<CancellationToken>()));

            var logger = new Mock<ILogger<ProcessEventPublish>>();

            var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>(MockBehavior.Strict);
            dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(context);

            var notificationManager = new Mock<INotificationManager>(MockBehavior.Loose);
            notificationManager.Setup(r => r.PublishAsync(It.IsAny<RegistryAccountRemovedEvent>(), It.IsAny<string?>()))
                .Callback<BaseEvent<RegistryAccountRemovedEventData>, string?>((data, t) =>
                {
                    t.Should().BeNull();
                    data.Data.Should().NotBeNull();
                    data.Data.Should().BeEquivalentTo(expectedEventData);
                })
                .Returns(Task.CompletedTask);

            var operation = await context.CreOperations.FirstAsync();

            // Act
            var function = new ProcessEventPublish(logger.Object, dbContextFactory.Object, notificationManager.Object);
            await function.Run(receivedMessage, messageActions.Object);

            // Assert
            notificationManager.VerifyAll();
            operation.PublishedAt.Should().NotBeNull();

        }

        [Fact]
        public async Task ProcessEventPublish_orchestrator_When_Operation_Insert_Type_Role()
        {
            // Arrange
            var roleOperation = new CreOperation()
            {
                EntityId = new Guid("1ef7aeba-2285-4cc8-8bbb-da6ddbe28bdf"),
                Operation = OperationName.Insert,
                Id = 1,
                PublishedAt = null,
                Type = OperationType.Role
            };

            var creAccount = new CreAccount
            {
                AccountGlobalUniqueIdentifier = new Guid("1e5a9480-f771-486e-ab3d-b712da9c2257"),
                AccountNumber = "12340",
                LegalName = "MyAccount",
                Updated = null
            };

            var creContact = new CreContact
            {
                Id = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
                OfficeId = Guid.NewGuid(),
                IsCustomer = true,
                IsActive = true,
                Updated = DateTime.UtcNow,
                Deleted = null,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                LandPhone = "123-456-7890",
                MobilePhone = "098-765-4321",
                JobDescription = "Developer",
                Source = "Internal",
                Roles = new List<CreRole>()
            };

            var creRole = new CreRole()
            {
                RoleId = new Guid("1ef7aeba-2285-4cc8-8bbb-da6ddbe28bdf"),
                ContactId = creAccount.AccountGlobalUniqueIdentifier,
                AccountId = creContact.Id,
                Deleted = null,
                Contact = creContact,
                Account = creAccount
            };

            context.CreOperations.Add(roleOperation);
            context.CreRoles.Add(creRole);

            await context.SaveChangesAsync();

            var expectedEventData = new RegistryRoleCreatedEventData()
            {
                AccountId = creRole.AccountId,
                Email = creContact.Email,
                AccountNumber = creAccount.AccountNumber,
                ContactId = creRole.ContactId,
            };

            var registryRoleCreatedEvent = new RegistryRoleCreatedEvent(expectedEventData);

            var registryEntityType = new RegistryEntityType() { EntityType = OperationType.Role };
            var messageBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(registryEntityType));

            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: new BinaryData(messageBody),
                messageId: "123",
                partitionKey: "test-partition-key",
                contentType: "application/json"
            );

            var messageActions = new Mock<ServiceBusMessageActions>();
            messageActions.Setup(x => x.CompleteMessageAsync(receivedMessage, It.IsAny<CancellationToken>()));

            var logger = new Mock<ILogger<ProcessEventPublish>>();

            var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>(MockBehavior.Strict);
            dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(context);

            var notificationManager = new Mock<INotificationManager>(MockBehavior.Loose);
            notificationManager.Setup(r => r.PublishAsync(It.IsAny<RegistryRoleCreatedEvent>(), It.IsAny<string?>()))
                .Callback<BaseEvent<RegistryRoleCreatedEventData>, string?>((data, t) =>
                {
                    t.Should().BeNull();
                    data.Data.Should().NotBeNull();
                    data.Data.Should().BeEquivalentTo(expectedEventData);
                })
                .Returns(Task.CompletedTask);

            var operation = await context.CreOperations.FirstAsync();

            // Act
            var function = new ProcessEventPublish(logger.Object, dbContextFactory.Object, notificationManager.Object);
            await function.Run(receivedMessage, messageActions.Object);

            // Assert
            notificationManager.VerifyAll();
            operation.PublishedAt.Should().NotBeNull();

        }

        [Fact]
        public async Task ProcessEventPublish_orchestrator_When_Operation_Remove_Type_Role()
        {
            // Arrange
            var roleOperation = new CreOperation()
            {
                EntityId = new Guid("1ef7aeba-2285-4cc8-8bbb-da6ddbe28bdf"),
                Operation = OperationName.Delete,
                Id = 1,
                PublishedAt = null,
                Type = OperationType.Role
            };

            var creAccount = new CreAccount
            {
                AccountGlobalUniqueIdentifier = new Guid("1e5a9480-f771-486e-ab3d-b712da9c2257"),
                AccountNumber = "12340",
                LegalName = "MyAccount",
                Updated = null
            };

            var creContact = new CreContact
            {
                Id = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
                OfficeId = Guid.NewGuid(),
                IsCustomer = true,
                IsActive = true,
                Updated = DateTime.UtcNow,
                Deleted = null,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                LandPhone = "123-456-7890",
                MobilePhone = "098-765-4321",
                JobDescription = "Developer",
                Source = "Internal",
                Roles = new List<CreRole>()
            };

            var creRole = new CreRole()
            {
                RoleId = new Guid("1ef7aeba-2285-4cc8-8bbb-da6ddbe28bdf"),
                ContactId = creAccount.AccountGlobalUniqueIdentifier,
                AccountId = creContact.Id,
                Deleted = DateTime.UtcNow,
                Contact = creContact,
                Account = creAccount
            };

            await context.CreRoles.AddAsync(creRole);
            context.CreOperations.Add(roleOperation);

            await context.SaveChangesAsync();

            var expectedEventData = new RegistryRoleRemovedEventData()
            {
                AccountId = creRole.AccountId,
                Email = creRole.Contact.Email,
                AccountNumber = creRole.Account.AccountNumber,
                ContactId = creRole.ContactId,
            };

            var registryRoleRemovedEvent = new RegistryRoleRemovedEvent(expectedEventData);

            var registryEntityType = new RegistryEntityType() { EntityType = OperationType.Role };
            var messageBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(registryEntityType));

            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: new BinaryData(messageBody),
                messageId: "123",
                partitionKey: "test-partition-key",
                contentType: "application/json"
            );

            var messageActions = new Mock<ServiceBusMessageActions>();
            messageActions.Setup(x => x.CompleteMessageAsync(receivedMessage, It.IsAny<CancellationToken>()));

            var logger = new Mock<ILogger<ProcessEventPublish>>();

            var dbContextFactory = new Mock<IDbContextFactory<ApplicationDbContext>>(MockBehavior.Strict);
            dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(context);

            var notificationManager = new Mock<INotificationManager>(MockBehavior.Loose);
            notificationManager.Setup(r => r.PublishAsync(It.IsAny<RegistryRoleRemovedEvent>(), It.IsAny<string?>()))
                .Callback<BaseEvent<RegistryRoleRemovedEventData>, string?>((data, t) =>
                {
                    t.Should().BeNull();
                    data.Data.Should().NotBeNull();
                    data.Data.Should().BeEquivalentTo(expectedEventData);
                })
                .Returns(Task.CompletedTask);

            var operation = await context.CreOperations.FirstAsync();

            // Act
            var function = new ProcessEventPublish(logger.Object, dbContextFactory.Object, notificationManager.Object);
            await function.Run(receivedMessage, messageActions.Object);

            // Assert
            notificationManager.VerifyAll();
            operation.PublishedAt.Should().NotBeNull();

        }
    }
}


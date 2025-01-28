// <copyright file="ProcessRegEventPublishTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Azure.Messaging.ServiceBus;
using ContactRegistry.AzureFuctions.Const;
using ContactRegistry.AzureFuctions.Functions;
using ContactRegistry.AzureFuctions.Managers;
using ContactRegistry.AzureFuctions.Message;
using ContactRegistry.AzureFuctions.Options;
using ContactRegistry.Infrastructure.Tests.Utils;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.ContactRegistry.Domain.Context;
using Pulse.ContactRegistry.Domain.Entities;
using Registry.AzureFuctions.Const;
using System.Text;

namespace ContactRegistry.AzureFuctions.Tests.Functions;

public class ProcessRegEventPublishTest
{
    private readonly RefContext context;

    public ProcessRegEventPublishTest()
    {
        context = DbContextMockExtensions.CreateInMemoryRefContext();
    }

    [Fact]
    public async Task ProcessRegEventPublish_orchestrator_messageActions_Null_Throws_ArgumentNullException()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create<ProcessEventPublishOptions>(new ProcessEventPublishOptions()
        {
            ProcessEventPublishBatchSize = 200,
        });

        var messageBody = Encoding.UTF8.GetBytes("This is a test message");

        var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: new BinaryData(messageBody),
            messageId: "123",
            partitionKey: "test-partition-key",
            contentType: "application/json"
        );

        ServiceBusMessageActions messageActions = null!;

        var logger = new Mock<ILogger<ProcessRegEventPublish>>();
        var dbContextFactory = new Mock<IDbContextFactory<RefContext>>(MockBehavior.Strict);
        var notificationManager = new Mock<INotificationManager>(MockBehavior.Strict);
        var serviceBusMessageFactory = new Mock<IServiceBusMessageFactory>(MockBehavior.Strict);

        // Act
        var function = new ProcessRegEventPublish(
            logger.Object,
            dbContextFactory.Object,
            notificationManager.Object,
            serviceBusMessageFactory.Object,
            options);

        async Task Act() => await function.Run(receivedMessage, messageActions);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(Act);
    }

    [Fact]
    public async Task ProcessRegEventPublish_orchestrator_When_Operation_Insert_Type_Contact()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create<ProcessEventPublishOptions>(new ProcessEventPublishOptions()
        {
            ProcessEventPublishBatchSize = 200,
        });

        var contactOperation = new RegOperationEntity()
        {
            EntityId = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
            Operation = OperationName.Insert,
            Id = 1,
            PublishedAt = null,
            Type = OperationType.Contact,
            ApprovalStatus = ApprovalStatus.Approved
        };

        var regContactEntity = new RegContactEntity
        {
            Id = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
            OfficeCode = "La defense",
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
            RegRoleEntity = new List<RegRoleEntity>()
        };

        context.RegOperationEntity.Add(contactOperation);
        context.RegContactEntity.Add(regContactEntity);

        await context.SaveChangesAsync();

        var expectedEventData = new RegistryContactCreatedEventData()
        {
            Id = regContactEntity.Id,
            IsCustomer = regContactEntity.IsCustomer,
            FirstName = regContactEntity.FirstName,
            LastName = regContactEntity.LastName,
            Email = regContactEntity.Email,
            OfficeCode = regContactEntity.OfficeCode,
            LandPhone = regContactEntity.LandPhone,
            MobilePhone = regContactEntity.MobilePhone,
            JobDescription = regContactEntity.JobDescription,
            Source = regContactEntity.Source,
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

        var logger = new Mock<ILogger<ProcessRegEventPublish>>();

        var dbContextFactory = new Mock<IDbContextFactory<RefContext>>(MockBehavior.Strict);
        dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(context);

        var notificationManager = new Mock<INotificationManager>(MockBehavior.Strict);
        notificationManager.Setup(r => r.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string?>()))
            .Callback<List<ServiceBusMessage>, string?>((data, t) =>
            {
                t.Should().BeNull();
                data.Count.Should().Be(1);
            })
            .Returns(Task.CompletedTask);

        var serviceBusMessageFactory = new Mock<IServiceBusMessageFactory>(MockBehavior.Strict);
        serviceBusMessageFactory.Setup(s => s.CreateMessage(It.IsAny<RegistryContactCreatedEvent>(), It.IsAny<string>()))
            .Returns(new ServiceBusMessage());


        // Act
        var function = new ProcessRegEventPublish(
            logger.Object,
            dbContextFactory.Object,
            notificationManager.Object,
            serviceBusMessageFactory.Object,
            options);
        await function.Run(receivedMessage, messageActions.Object);

        // Assert
        notificationManager.VerifyAll();
        serviceBusMessageFactory.Verify(s => s.CreateMessage(It.IsAny<RegistryContactCreatedEvent>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ProcessRegEventPublish_orchestrator_When_Operation_Update_Type_Contact()
    {
        var options = Microsoft.Extensions.Options.Options.Create<ProcessEventPublishOptions>(new ProcessEventPublishOptions()
        {
            ProcessEventPublishBatchSize = 200,
        });

        // Arrange
        var contactOperation = new RegOperationEntity()
        {
            EntityId = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
            Operation = OperationName.Update,
            Id = 1,
            PublishedAt = null,
            Type = OperationType.Contact,
            ApprovalStatus = ApprovalStatus.Approved,
            ProcessStatus = ProcessStatus.Ready
        };

        var regContactEntity = new RegContactEntity
        {
            Id = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
            OfficeCode = "La defense",
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
            RegRoleEntity = new List<RegRoleEntity>()
        };

        context.RegOperationEntity.Add(contactOperation);
        context.RegContactEntity.Add(regContactEntity);

        await context.SaveChangesAsync();

        var expectedEventData = new RegistryContactUpdatedEventData()
        {
            Id = regContactEntity.Id,
            Email = regContactEntity.Email,
            OfficeCode = regContactEntity.OfficeCode,
            JobDescription = regContactEntity.JobDescription,
            MobilePhone = "098-765-4321",
            LandPhone = "123-456-7890",
            FirstName = "John",
            LastName = "Doe",
            IsCustomer = true,
            IsActive= true,
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

        var logger = new Mock<ILogger<ProcessRegEventPublish>>();

        var dbContextFactory = new Mock<IDbContextFactory<RefContext>>(MockBehavior.Strict);
        dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(context);

        var notificationManager = new Mock<INotificationManager>(MockBehavior.Strict);
        notificationManager.Setup(r => r.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string?>()))
            .Callback<List<ServiceBusMessage>, string?>((data, t) =>
            {
                t.Should().BeNull();
                data.Count.Should().Be(1);
            })
            .Returns(Task.CompletedTask);

        var serviceBusMessageFactory = new Mock<IServiceBusMessageFactory>(MockBehavior.Strict);
        serviceBusMessageFactory.Setup(s => s.CreateMessage(It.IsAny<RegistryContactUpdatedEvent>(), It.IsAny<string>()))
            .Returns(new ServiceBusMessage());

        // Act
        var function = new ProcessRegEventPublish(
            logger.Object,
            dbContextFactory.Object,
            notificationManager.Object,
            serviceBusMessageFactory.Object,
            options);

        await function.Run(receivedMessage, messageActions.Object);

        // Assert
        notificationManager.VerifyAll();
        serviceBusMessageFactory.Verify(s => s.CreateMessage(It.IsAny<RegistryContactUpdatedEvent>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ProcessRegEventPublish_orchestrator_When_Operation_Remove_Type_Contact()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create<ProcessEventPublishOptions>(new ProcessEventPublishOptions()
        {
            ProcessEventPublishBatchSize = 200,
        });

        var contactOperation = new RegOperationEntity()
        {
            EntityId = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
            Operation = OperationName.Delete,
            Id = 1,
            PublishedAt = null,
            Type = OperationType.Contact, 
            ApprovalStatus = ApprovalStatus.Approved,
            ProcessStatus = ProcessStatus.Ready
        };

        var regContactEntity = new RegContactEntity
        {
            Id = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
            OfficeCode = "La defense",
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
            RegRoleEntity = new List<RegRoleEntity>()
        };

        context.RegOperationEntity.Add(contactOperation);
        context.RegContactEntity.Add(regContactEntity);

        await context.SaveChangesAsync();

        var expectedEventData = new RegistryContactRemovedEventData()
        {
            Id = regContactEntity.Id,
            Email = regContactEntity.Email,
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

        var logger = new Mock<ILogger<ProcessRegEventPublish>>();

        var dbContextFactory = new Mock<IDbContextFactory<RefContext>>(MockBehavior.Strict);
        dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(context);

        var notificationManager = new Mock<INotificationManager>(MockBehavior.Strict);
        notificationManager.Setup(r => r.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string?>()))
            .Callback<List<ServiceBusMessage>, string?>((data, t) =>
            {
                t.Should().BeNull();
                data.Count.Should().Be(1);
            })
            .Returns(Task.CompletedTask);

        var serviceBusMessageFactory = new Mock<IServiceBusMessageFactory>(MockBehavior.Strict);
        serviceBusMessageFactory.Setup(s => s.CreateMessage(It.IsAny<RegistryContactRemovedEvent>(), It.IsAny<string>()))
            .Returns(new ServiceBusMessage());


        // Act
        var function = new ProcessRegEventPublish(
            logger.Object,
            dbContextFactory.Object,
            notificationManager.Object,
            serviceBusMessageFactory.Object,
            options);
        await function.Run(receivedMessage, messageActions.Object);

        // Assert
        notificationManager.VerifyAll();
        serviceBusMessageFactory.Verify(s => s.CreateMessage(It.IsAny<RegistryContactRemovedEvent>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ProcessRegEventPublish_orchestrator_When_Operation_Insert_Type_Account()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create<ProcessEventPublishOptions>(new ProcessEventPublishOptions()
        {
            ProcessEventPublishBatchSize = 200,
        });

        var accountOperation = new RegOperationEntity()
        {
            EntityId = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
            Operation = OperationName.Insert,
            Id = 1,
            PublishedAt = null,
            Type = OperationType.Account,
            ApprovalStatus = ApprovalStatus.Approved,
            ProcessStatus = ProcessStatus.Ready
        };

        var regAccountEntity = new RegAccountEntity
        {
            Id = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
            AccountNumber = "12340",
            LegalName = "MyAccount",
            Updated = null,
            AccountFlagEscactif = true
        };

        context.RegOperationEntity.Add(accountOperation);
        context.RegAccountEntity.Add(regAccountEntity);

        await context.SaveChangesAsync();

        var expectedEventData = regAccountEntity.ToRegAccountCreatedEventData();

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

        var logger = new Mock<ILogger<ProcessRegEventPublish>>();

        var dbContextFactory = new Mock<IDbContextFactory<RefContext>>(MockBehavior.Strict);
        dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(context);

        var notificationManager = new Mock<INotificationManager>(MockBehavior.Strict);
        notificationManager.Setup(r => r.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string?>()))
            .Callback<List<ServiceBusMessage>, string?>((data, t) =>
            {
                t.Should().BeNull();
                data.Count.Should().Be(1);
            })
            .Returns(Task.CompletedTask);

        var serviceBusMessageFactory = new Mock<IServiceBusMessageFactory>(MockBehavior.Strict);
        serviceBusMessageFactory.Setup(s => s.CreateMessage(It.IsAny<RegistryAccountCreatedEvent>(), It.IsAny<string>()))
            .Returns(new ServiceBusMessage());

        // Act
        var function = new ProcessRegEventPublish(
            logger.Object,
            dbContextFactory.Object,
            notificationManager.Object,
            serviceBusMessageFactory.Object,
            options);
        await function.Run(receivedMessage, messageActions.Object);

        // Assert
        notificationManager.VerifyAll();
        serviceBusMessageFactory.Verify(s => s.CreateMessage(It.IsAny<RegistryAccountCreatedEvent>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ProcessRegEventPublish_orchestrator_When_Operation_Update_Type_Account()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create<ProcessEventPublishOptions>(new ProcessEventPublishOptions()
        {
            ProcessEventPublishBatchSize = 200,
        });

        var accountOperation = new RegOperationEntity()
        {
            EntityId = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
            Operation = OperationName.Update,
            Id = 1,
            PublishedAt = null,
            Type = OperationType.Account,
            ApprovalStatus = ApprovalStatus.Approved,
            ProcessStatus = ProcessStatus.Ready
        };

        var regAccountEntity = new RegAccountEntity
        {
            Id = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
            AccountNumber = "12340",
            LegalName = "MyAccount",
            Updated = null,
            AccountFlagEscactif = true,
        };

        context.RegOperationEntity.Add(accountOperation);
        context.RegAccountEntity.Add(regAccountEntity);

        await context.SaveChangesAsync();

        var expectedEventData = regAccountEntity.ToRegAccountUpdatedEventData();
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

        var logger = new Mock<ILogger<ProcessRegEventPublish>>();

        var dbContextFactory = new Mock<IDbContextFactory<RefContext>>(MockBehavior.Strict);
        dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(context);

        var notificationManager = new Mock<INotificationManager>(MockBehavior.Strict);
        notificationManager.Setup(r => r.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string?>()))
            .Callback<List<ServiceBusMessage>, string?>((data, t) =>
            {
                t.Should().BeNull();
                data.Count.Should().Be(1);
            })
            .Returns(Task.CompletedTask);

        var serviceBusMessageFactory = new Mock<IServiceBusMessageFactory>(MockBehavior.Strict);
        serviceBusMessageFactory.Setup(s => s.CreateMessage(It.IsAny<RegistryAccountUpdatedEvent>(), It.IsAny<string>()))
            .Returns(new ServiceBusMessage());

        // Act
        var function = new ProcessRegEventPublish(
            logger.Object,
            dbContextFactory.Object,
            notificationManager.Object,
            serviceBusMessageFactory.Object,
            options);

        await function.Run(receivedMessage, messageActions.Object);

        // Assert
        notificationManager.VerifyAll();
        serviceBusMessageFactory.Verify(s => s.CreateMessage(It.IsAny<RegistryAccountUpdatedEvent>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ProcessRegEventPublish_orchestrator_When_Operation_Remove_Type_Account()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create<ProcessEventPublishOptions>(new ProcessEventPublishOptions()
        {
            ProcessEventPublishBatchSize = 200,
        });

        var accountOperation = new RegOperationEntity()
        {
            EntityId = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
            Operation = OperationName.Delete,
            Id = 1,
            PublishedAt = null,
            Type = OperationType.Account,
            ApprovalStatus = ApprovalStatus.Approved,
            ProcessStatus = ProcessStatus.Ready
        };

        var regAccountEntity = new RegAccountEntity
        {
            Id = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
            AccountNumber = "12340",
            LegalName = "MyAccount",
            Updated = null
        };

        context.RegOperationEntity.Add(accountOperation);
        context.RegAccountEntity.Add(regAccountEntity);

        await context.SaveChangesAsync();

        var expectedEventData = new RegistryAccountRemovedEventData()
        {
            AccountGlobalUniqueIdentifier = regAccountEntity.Id,
            AccountNumber = regAccountEntity.AccountNumber,
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

        var logger = new Mock<ILogger<ProcessRegEventPublish>>();

        var dbContextFactory = new Mock<IDbContextFactory<RefContext>>(MockBehavior.Strict);
        dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(context);

        var notificationManager = new Mock<INotificationManager>(MockBehavior.Strict);
        notificationManager.Setup(r => r.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string?>()))
            .Callback<List<ServiceBusMessage>, string?>((data, t) =>
            {
                t.Should().BeNull();
                data.Count.Should().Be(1);
            })
            .Returns(Task.CompletedTask);

        var serviceBusMessageFactory = new Mock<IServiceBusMessageFactory>(MockBehavior.Strict);
        serviceBusMessageFactory.Setup(s => s.CreateMessage(It.IsAny<RegistryAccountRemovedEvent>(), It.IsAny<string>()))
            .Returns(new ServiceBusMessage());


        // Act
        var function = new ProcessRegEventPublish(
            logger.Object,
            dbContextFactory.Object,
            notificationManager.Object,
            serviceBusMessageFactory.Object,
            options);

        await function.Run(receivedMessage, messageActions.Object);

        // Assert
        notificationManager.VerifyAll();
        serviceBusMessageFactory.Verify(s => s.CreateMessage(It.IsAny<RegistryAccountRemovedEvent>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ProcessRegEventPublish_orchestrator_When_Operation_Insert_Type_Role()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create<ProcessEventPublishOptions>(new ProcessEventPublishOptions()
        {
            ProcessEventPublishBatchSize = 200,
        });

        var roleOperation = new RegOperationEntity()
        {
            EntityId = new Guid("1ef7aeba-2285-4cc8-8bbb-da6ddbe28bdf"),
            Operation = OperationName.Insert,
            Id = 1,
            PublishedAt = null,
            Type = OperationType.Role,
            ApprovalStatus = ApprovalStatus.Approved
        };

        var regAccountEntity = new RegAccountEntity
        {
            Id = new Guid("1e5a9480-f771-486e-ab3d-b712da9c2257"),
            AccountNumber = "12340",
            LegalName = "MyAccount",
            Updated = null
        };

        var regContactEntity = new RegContactEntity
        {
            Id = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
            OfficeCode = "La defense",
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
            RegRoleEntity = new List<RegRoleEntity>()
        };

        var regRoleEntity = new RegRoleEntity()
        {
            RoleId = new Guid("1ef7aeba-2285-4cc8-8bbb-da6ddbe28bdf"),
            ContactId = regAccountEntity.Id,
            AccountId = regContactEntity.Id,
            Deleted = null,
            ContactEmailNavigation = regContactEntity,
            AccountNumberNavigation = regAccountEntity
        };

        context.RegOperationEntity.Add(roleOperation);
        context.RegRoleEntity.Add(regRoleEntity);

        await context.SaveChangesAsync();

        var expectedEventData = new RegistryRoleCreatedEventData()
        {
            AccountId = regRoleEntity.AccountId,
            Email = regContactEntity.Email,
            AccountNumber = regAccountEntity.AccountNumber,
            ContactId = regRoleEntity.ContactId,
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

        var logger = new Mock<ILogger<ProcessRegEventPublish>>();

        var dbContextFactory = new Mock<IDbContextFactory<RefContext>>(MockBehavior.Strict);
        dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(context);

        var notificationManager = new Mock<INotificationManager>(MockBehavior.Strict);
        notificationManager.Setup(r => r.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string?>()))
            .Callback<List<ServiceBusMessage>, string?>((data, t) =>
            {
                t.Should().BeNull();
                data.Count.Should().Be(1);
            })
            .Returns(Task.CompletedTask);

        var serviceBusMessageFactory = new Mock<IServiceBusMessageFactory>(MockBehavior.Strict);
        serviceBusMessageFactory.Setup(s => s.CreateMessage(It.IsAny<RegistryRoleCreatedEvent>(), It.IsAny<string>()))
            .Returns(new ServiceBusMessage());


        // Act
        var function = new ProcessRegEventPublish(
            logger.Object,
            dbContextFactory.Object,
            notificationManager.Object,
            serviceBusMessageFactory.Object,
            options);

        await function.Run(receivedMessage, messageActions.Object);

        // Assert
        notificationManager.VerifyAll();
        serviceBusMessageFactory.Verify(s => s.CreateMessage(It.IsAny<RegistryRoleCreatedEvent>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ProcessRegEventPublish_orchestrator_When_Operation_Remove_Type_Role()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create<ProcessEventPublishOptions>(new ProcessEventPublishOptions()
        {
            ProcessEventPublishBatchSize = 200,
        });

        var roleOperation = new RegOperationEntity()
        {
            EntityId = new Guid("1ef7aeba-2285-4cc8-8bbb-da6ddbe28bdf"),
            Operation = OperationName.Delete,
            Id = 1,
            PublishedAt = null,
            Type = OperationType.Role,
            ApprovalStatus = ApprovalStatus.Approved,
            ProcessStatus = ProcessStatus.Ready
        };

        var regAccountEntity = new RegAccountEntity
        {
            Id = new Guid("1e5a9480-f771-486e-ab3d-b712da9c2257"),
            AccountNumber = "12340",
            LegalName = "MyAccount",
            Updated = null
        };

        var regContactEntity = new RegContactEntity
        {
            Id = new Guid("35e7a4c7-d82b-493f-a780-eb85f40b6b7a"),
            OfficeCode = "La defense",
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
            RegRoleEntity = new List<RegRoleEntity>()
        };

        var regRoleEntity = new RegRoleEntity()
        {
            RoleId = new Guid("1ef7aeba-2285-4cc8-8bbb-da6ddbe28bdf"),
            ContactId = regAccountEntity.Id,
            AccountId = regContactEntity.Id,
            Deleted = DateTime.UtcNow,
            ContactEmailNavigation = regContactEntity,
            AccountNumberNavigation = regAccountEntity
        };

        await context.RegRoleEntity.AddAsync(regRoleEntity);
        context.RegOperationEntity.Add(roleOperation);

        await context.SaveChangesAsync();

        var expectedEventData = new RegistryRoleRemovedEventData()
        {
            AccountId = regRoleEntity.AccountId,
            Email = regRoleEntity.ContactEmail,
            AccountNumber = regRoleEntity.AccountNumber,
            ContactId = regRoleEntity.ContactId,
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

        var logger = new Mock<ILogger<ProcessRegEventPublish>>();

        var dbContextFactory = new Mock<IDbContextFactory<RefContext>>(MockBehavior.Strict);
        dbContextFactory.Setup(d => d.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(context);

        var notificationManager = new Mock<INotificationManager>(MockBehavior.Strict);
        notificationManager.Setup(r => r.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string?>()))
            .Callback<List<ServiceBusMessage>, string?>((data, t) =>
            {
                t.Should().BeNull();
                data.Count.Should().Be(1);
            })
            .Returns(Task.CompletedTask);

        var serviceBusMessageFactory = new Mock<IServiceBusMessageFactory>(MockBehavior.Strict);
        serviceBusMessageFactory.Setup(s => s.CreateMessage(It.IsAny<RegistryRoleRemovedEvent>(), It.IsAny<string>()))
            .Returns(new ServiceBusMessage());

        // Act
        var function = new ProcessRegEventPublish(
            logger.Object,
            dbContextFactory.Object,
            notificationManager.Object,
            serviceBusMessageFactory.Object,
            options);

        await function.Run(receivedMessage, messageActions.Object);

        // Assert
        notificationManager.VerifyAll();
        serviceBusMessageFactory.Verify(s => s.CreateMessage(It.IsAny<RegistryRoleRemovedEvent>(), It.IsAny<string>()), Times.Once);
    }
}


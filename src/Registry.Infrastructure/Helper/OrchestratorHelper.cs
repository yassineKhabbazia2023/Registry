using Application.Consts;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Registry.Infrastructure.Managers;

namespace Infrastructure.Helper
{
    public static class OrchestratorHelper
    {
        public static async Task SendBatchMessageAsync<T>(
            List<ServiceBusMessage> messagesToSendInBatch,
            INotificationManager notificationManager,
            ILogger<T> logger)
        {
            if (messagesToSendInBatch.Count == 0)
            {
                return;
            }

            await notificationManager.BulkPublishAsync(messagesToSendInBatch);
            logger.LogInformation("ProcessRegEventPublish : SendBatchMessageAsync publish '{Count}' events success.", messagesToSendInBatch.Count);
            messagesToSendInBatch.Clear();
        }

        public static RegOperationEntity UpdateOperationsToPublisAt(RegOperationEntity operation)
        {
            operation.PublishedAt = DateTime.UtcNow;
            operation.ProcessStatus = ProcessStatus.Sent;
            return operation;
        }


        public static async Task UpdateOperationsAsync(RefContext dbContext)
        {
            await dbContext.SaveChangesAsync();
        }
    }
}

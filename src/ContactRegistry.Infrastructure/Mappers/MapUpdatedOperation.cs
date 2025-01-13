using Application.Models;
using Pulse.ContactRegistry.Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class MapUpdatedOperation
{
    public static void MapToUpdatedStatusOperation(this RegOperationEntity existingOperation, RegOperation newOperation)
    {
        if (existingOperation.Status != newOperation.Status)
        {
            existingOperation.Status = newOperation.Status!;
            existingOperation.LastStatusUpdatedBy = newOperation.LastStatusUpdatedBy!;
            existingOperation.LastStatusUpdatedDate = DateTime.UtcNow;
        }
    }

    public static RegOperation? MapEntityToModel(this RegOperationEntity creOperationEntity)
    {
        return creOperationEntity == null ? null! : new RegOperation()
        {
            Id = creOperationEntity.Id,
            CreationDate = creOperationEntity.CreationDate,
            Status = creOperationEntity.Status,
            EntityId = creOperationEntity.EntityId,
            LastStatusUpdatedBy = creOperationEntity.LastStatusUpdatedBy,
            LastStatusUpdatedDate = creOperationEntity.LastStatusUpdatedDate,
            Operation = creOperationEntity.Operation,
            PublishedAt = creOperationEntity.PublishedAt,
            Type = creOperationEntity.Type
        };
    }
}

using Application.Models;
using Pulse.ContactRegistry.Domain.Entities;

namespace Infrastructure.Mappers;

public static class MapUpdatedOperation
{
    public static void MapToUpdatedStatusOperation(this RegOperationEntity existingOperation, RegOperation newOperation)
    {
        if (existingOperation.ApprovalStatus != newOperation.Status)
        {
            existingOperation.ApprovalStatus= newOperation.Status!;
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
            Status = creOperationEntity.ApprovalStatus,
            EntityId = creOperationEntity.EntityId,
            LastStatusUpdatedBy = creOperationEntity.LastStatusUpdatedBy,
            LastStatusUpdatedDate = creOperationEntity.LastStatusUpdatedDate,
            Operation = creOperationEntity.Operation,
            PublishedAt = creOperationEntity.PublishedAt,
            Type = creOperationEntity.Type
        };
    }
}

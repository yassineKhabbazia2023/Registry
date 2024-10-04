using Application.Models;
using CreOperationEntity = Domain.Entities.CreOperation;

namespace Infrastructure.Mappers;

public static class MapUpdatedOperation
{
    public static void MapToUpdatedStatusOperation(this CreOperationEntity existingOperation, CreOperation newOperation)
    {
        if (existingOperation.Status != newOperation.Status)
        {
            existingOperation.Status = newOperation.Status!;
            existingOperation.LastStatusUpdatedBy = newOperation.LastStatusUpdatedBy!;
            existingOperation.LastStatusUpdatedDate = DateTime.UtcNow;
        }
    }

    public static CreOperation? MapEntityToModel(this CreOperationEntity creOperationEntity)
    {
        return creOperationEntity == null ? null! : new CreOperation()
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

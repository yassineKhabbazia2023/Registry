// <copyright file="MapDbEntityToModel.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Domain.Entities;
using CreOperationEntity = Domain.Entities.CreOperation;

namespace Infrastructure.Mappers;

public static class MapDbEntityToModel
{
    public static Application.Models.CreOperationDetail? MapDbOperationEntityToOperationDetailModel(CreOperationEntity creOperationEntity, CreRole creRole, CreContact creContact, string accountNumber)
    {
        return creOperationEntity == null ? null : new Application.Models.CreOperationDetail()
        {
            OperationId = creOperationEntity.Id,
            RoleId = creRole == null ? Guid.Empty : creRole.RoleId,
            OperationName = creOperationEntity.Operation,
            OperationType = creOperationEntity.Type!,
            CreationDate = creOperationEntity.CreationDate,
            Status = creOperationEntity.Status,
            Email = creContact!.Email ?? null!,
            FirstName = creContact!.FirstName ?? null!,
            LastName = creContact!.LastName ?? null!,
            AccountNumber = accountNumber
        };
    }

    public static Application.Models.CreOperation? MapDbOperationEntityToOperationModel(this CreOperationEntity creOperationEntity)
    {
        return creOperationEntity == null ? null! : new Application.Models.CreOperation()
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

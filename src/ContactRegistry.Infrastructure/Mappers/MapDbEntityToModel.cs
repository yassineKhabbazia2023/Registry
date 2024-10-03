// <copyright file="MapDbEntityToModel.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Domain.Entities;
using CreOperationEntity = Domain.Entities.CreOperation;

namespace Infrastructure.Mappers;

public static class MapDbEntityToModel
{
    public static Application.Models.CreOperation? MapDbOperationEntityToOperationModel(CreOperationEntity creOperationEntity, CreRole creRole, CreContact creContact, string accountNumber)
    {
        return creOperationEntity == null ? null : new Application.Models.CreOperation()
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
}

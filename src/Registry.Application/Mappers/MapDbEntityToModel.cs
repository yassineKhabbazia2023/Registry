// <copyright file="MapDbEntityToModel.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Pulse.Registry.Domain.Entities;

namespace Application.Mappers;

public static class MapDbEntityToModel
{
    public static Application.Models.RegOperationDetail? MapDbOperationEntityToOperationDetailModel(RegOperationEntity operation, RefRoleEntity role, RefContactEntity contact, string accountNumber)
    {
        return operation == null ? null : new Application.Models.RegOperationDetail()
        {
            OperationId = operation.Id,
            RoleId = role == null ? Guid.Empty : role.EntityId,
            OperationName = operation.Operation,
            OperationType = operation.Type!,
            CreationDate = operation.CreationDate,
            Status = operation.ApprovalStatus,
            Email = contact!.Email ?? null!,
            FirstName = contact!.FirstName ?? null!,
            LastName = contact!.LastName ?? null!,
            AccountNumber = accountNumber
        };
    }

    public static Application.Models.RegOperation? MapDbOperationEntityToOperationModel(this RegOperationEntity creOperationEntity)
    {
        return creOperationEntity == null ? null! : new Application.Models.RegOperation()
        {
            Id = creOperationEntity.Id,
            CreationDate = creOperationEntity.CreationDate,
            Status = creOperationEntity.ApprovalStatus,
            EntityId = creOperationEntity.EntityId,
            LastStatusUpdatedBy = creOperationEntity.LastStatusApprovalBy,
            LastStatusUpdatedDate = creOperationEntity.LastStatusApprovalDate,
            Operation = creOperationEntity.Operation,
            PublishedAt = creOperationEntity.PublishedAt,
            Type = creOperationEntity.Type
        };
    }
}

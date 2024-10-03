// <copyright file="OperationRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Application.Requests;
using Infrastructure.Context;
using Infrastructure.Mappers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Pulse.Account.Core.Constants;

namespace Infrastructure.Repository;

public class OperationRepository : IOperationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public OperationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<CreOperation?>> GetOperationsAsync(string accountNumber, OperationSearchCriteria operationSearchCriteria)
    {
        var operationStatus = operationSearchCriteria.Status?.Split('|');
        var deployments = _dbContext.CreOperations
                .Join(
                    _dbContext.CreRoles,
                    operation => operation.EntityId,
                    role => role.RoleId,
                    (operation, role) => new { operation, role }
                )
                .Join(
                    _dbContext.CreAccounts,
                    roleOperation => roleOperation.role.AccountId,
                    account => account.Id,
                    (roleOperation, account) => new { roleOperation, account.AccountNumber }
                )
                .Join(
                    _dbContext.CreContacts,
                    roleAccountOperation => roleAccountOperation.roleOperation.role.ContactId,
                    contact => contact.Id,
                    (roleAccountOperation, contact) => new { roleAccountOperation, contact }
                )
                .Where(item => item!.roleAccountOperation.roleOperation.operation.Operation == operationSearchCriteria.OperationName
                                && (operationStatus != null && operationStatus.Contains(item!.roleAccountOperation.roleOperation.operation.Status))
                                && item!.roleAccountOperation.AccountNumber == accountNumber
                                && item!.roleAccountOperation.roleOperation.operation.Type == GlobalConstants.OPERATIONTYPEROLE)
                .Select(item => MapDbEntityToModel.MapDbOperationEntityToOperationModel(
                                    item.roleAccountOperation.roleOperation.operation,
                                    item.roleAccountOperation.roleOperation.role,
                                    item.contact,
                                    item.roleAccountOperation.AccountNumber!));

        return await deployments.ToListAsync();
    }
}

// <copyright file="OperationRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Interfaces;
using Application.Models;
using Application.Requests;
using Infrastructure.Context;
using Infrastructure.Mappers;
using Kpmg.ExceptionMiddleware.AdvancedExceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using Pulse.ContactRegistry.Domain.Constants;
using CreOperationEntity = Domain.Entities.CreOperation;

namespace Infrastructure.Repository;

public class OperationRepository : IOperationRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly AsyncRetryPolicy _retryPolicy;

    public OperationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;

        _retryPolicy = Policy
                    .Handle<SqlException>()
                    .WaitAndRetryAsync(
                        retryCount: 1,
                        sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(GlobalConstants.RETRYTIMESPAN));
    }

    public async Task<IEnumerable<CreOperationDetail?>> GetOperationsAsync(string accountNumber, OperationSearchCriteria operationSearchCriteria)
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
                                && !(item!.roleAccountOperation.roleOperation.operation.PublishedAt).HasValue
                                && item!.roleAccountOperation.roleOperation.operation.Type == GlobalConstants.OPERATIONTYPEROLE)
                .Select(item => MapDbEntityToModel.MapDbOperationEntityToOperationDetailModel(
                                    item.roleAccountOperation.roleOperation.operation,
                                    item.roleAccountOperation.roleOperation.role,
                                    item.contact,
                                    item.roleAccountOperation.AccountNumber!));

        return await deployments.ToListAsync();
    }

    public async Task<CreOperation?> GetOperationByIdAsync(int operationId)
    {
        CreOperationEntity? creOperation = await _retryPolicy.ExecuteAsync(async () =>
        {
            return await _dbContext.CreOperations
                   .AsNoTracking()
                   .FirstOrDefaultAsync(a => a.Id == operationId);
        });

        if (creOperation == null)
        {
            throw new NotFoundException(Errors.NotFoundOperationCode, string.Format(Errors.NotFoundOperationMessage, operationId));
        }

        return creOperation.MapDbOperationEntityToOperationModel();
    }

    public async Task<CreOperation?> UpdateOperationAsync(int operationId, CreOperation creOperation)
    {
        CreOperation? updatedOperation = null!;

        await _retryPolicy.ExecuteAsync(async () =>
        {
            var existingOperation = await _dbContext.CreOperations.FirstOrDefaultAsync(x => x.Id == operationId);
            if (existingOperation == null)
            {
                throw new NotFoundException(Errors.NotFoundOperationCode, string.Format(Errors.NotFoundOperationMessage, operationId));
            }

            existingOperation.MapToUpdatedStatusOperation(creOperation);
            _dbContext.CreOperations.Update(existingOperation);
            updatedOperation = existingOperation.MapEntityToModel();
            await _dbContext.SaveChangesAsync();
        });

        return updatedOperation;

    }
}

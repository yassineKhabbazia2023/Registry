// <copyright file="OperationRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Interfaces;
using Application.Models;
using Application.Requests;
using Infrastructure.Mappers;
using Kpmg.ExceptionMiddleware.AdvancedExceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using Pulse.ContactRegistry.Domain.Constants;
using Pulse.ContactRegistry.Domain.Context;
using Pulse.ContactRegistry.Domain.Entities;
using System.Buffers;


namespace Infrastructure.Repository;

public class OperationRepository : IOperationRepository
{
    private readonly RefContext _dbContext;
    private readonly AsyncRetryPolicy _retryPolicy;

    public OperationRepository(RefContext dbContext)
    {
        _dbContext = dbContext;

        _retryPolicy = Policy
                    .Handle<SqlException>()
                    .WaitAndRetryAsync(
                        retryCount: 1,
                        sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(GlobalConstants.RETRYTIMESPAN));
    }

    public async Task<IEnumerable<RegOperationDetail?>> GetOperationsAsync(
    string accountNumber, OperationSearchCriteria operationSearchCriteria)
    {
        var operationStatus = operationSearchCriteria.Status != null
            ? new HashSet<string>(
                operationSearchCriteria.Status.Split('|', StringSplitOptions.RemoveEmptyEntries),
                StringComparer.OrdinalIgnoreCase)
            : null;

        var query = _dbContext.RegOperationEntity
            .Join(_dbContext.RegRoleEntity,
                operation => operation.EntityId,
                role => role.RoleId,
                (operation, role) => new { operation, role })
            .Join(_dbContext.RegAccountEntity,
                roleOperation => roleOperation.role.AccountNumber,
                account => account.AccountNumber,
                (roleOperation, account) => new { roleOperation.operation, roleOperation.role, account.AccountNumber })
            .Join(_dbContext.RegContactEntity,
                roleAccountOperation => roleAccountOperation.role.ContactEmail,
                contact => contact.Email,
                (roleAccountOperation, contact) => new
                {
                    Operation = roleAccountOperation.operation,
                    Role = roleAccountOperation.role,
                    AccountNumber = roleAccountOperation.AccountNumber,
                    Contact = contact
                })
            .Where(item =>
                item.Operation.Operation == operationSearchCriteria.OperationName &&
                (operationStatus == null || operationStatus.Contains(item.Operation.ApprovalStatus)) &&
                item.AccountNumber == accountNumber &&
                !item.Operation.PublishedAt.HasValue &&
                item.Operation.Type == GlobalConstants.OPERATIONTYPEROLE);

        var deployments = query.Select(item =>
            MapDbEntityToModel.MapDbOperationEntityToOperationDetailModel(
                item.Operation,
                item.Role,
                item.Contact,
                item.AccountNumber
            ));

        return await deployments.ToListAsync();
    }


    public async Task<RegOperation?> GetOperationByIdAsync(int operationId)
    {
        RegOperationEntity? creOperation = await _retryPolicy.ExecuteAsync(async () =>
        {
            return await _dbContext.RegOperationEntity
                   .AsNoTracking()
                   .FirstOrDefaultAsync(a => a.Id == operationId);
        });

        if (creOperation == null)
        {
            throw new NotFoundException(Errors.NotFoundOperationCode, string.Format(Errors.NotFoundOperationMessage, operationId));
        }

        return creOperation.MapDbOperationEntityToOperationModel();
    }

    public async Task<RegOperation?> UpdateOperationAsync(int operationId, RegOperation creOperation)
    {
        RegOperation? updatedOperation = null!;

        await _retryPolicy.ExecuteAsync(async () =>
        {
            var existingOperation = await _dbContext.RegOperationEntity.FirstOrDefaultAsync(x => x.Id == operationId);
            if (existingOperation == null)
            {
                throw new NotFoundException(Errors.NotFoundOperationCode, string.Format(Errors.NotFoundOperationMessage, operationId));
            }

            existingOperation.MapToUpdatedStatusOperation(creOperation);
            _dbContext.RegOperationEntity.Update(existingOperation);
            updatedOperation = existingOperation.MapEntityToModel();
            await _dbContext.SaveChangesAsync();
        });

        return updatedOperation;

    }
}

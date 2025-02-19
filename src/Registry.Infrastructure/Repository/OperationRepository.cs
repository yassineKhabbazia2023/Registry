// <copyright file="OperationRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Exceptions;
using Application.Interfaces;
using Application.Mappers;
using Application.Models;
using Application.Requests;
using Azure;
using Kpmg.ExceptionMiddleware.AdvancedExceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Pulse.Registry.Domain.Constants;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;


namespace Infrastructure.Repository;

public class OperationRepository : IOperationRepository
{
    private readonly RefContext _dbContext;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly ILogger<OperationRepository> _logger;
    private readonly string[] acceptedProcessStatus = { ProcessStatus.Sent, ProcessStatus.Failed };

    public OperationRepository(RefContext dbContext, ILogger<OperationRepository> logger)
    {
        _dbContext = dbContext;

        _retryPolicy = Policy
                    .Handle<SqlException>()
                    .WaitAndRetryAsync(
                        retryCount: 1,
                        sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(GlobalConstants.RETRYTIMESPAN));
        _logger = logger;
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
            .Join(_dbContext.RefRoleEntity,
                operation => operation.EntityId,
                role => role.EntityId,
                (operation, role) => new { operation, role })
            .Join(_dbContext.RefAccountEntity,
                roleOperation => roleOperation.role.AccountNumber,
                account => account.AccountNumber,
                (roleOperation, account) => new { roleOperation.operation, roleOperation.role, account.AccountNumber })
            .Join(_dbContext.RefContactEntity,
                roleAccountOperation => roleAccountOperation.role.ContactEmail,
                contact => contact.Email,
                (roleAccountOperation, contact) => new
                {
                    Operation = roleAccountOperation.operation,
                    Role = roleAccountOperation.role,
                    roleAccountOperation.AccountNumber,
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

    public async Task<bool> AddOperationAsync(RegOperationEntity regOperation)
    {
        ArgumentNullException.ThrowIfNull(nameof(regOperation));

        try
        {
            await _dbContext.RegOperationEntity.AddAsync(regOperation);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();
            return true;
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError($"[Method]: {nameof(AddOperationAsync)}; [Error]: {dbEx.Message}");
            return false;
        }
        catch (InvalidOperationException ioEx)
        {
            _logger.LogError($"[Method]: {nameof(AddOperationAsync)}; [Error]: {ioEx.Message}");
            return false;
        }
    }

    public async Task<IEnumerable<RegOperationEntity>> FindAccountOperationAsync(OperationSearchCriteria criteria, string accountNumber)
    {
        return await _dbContext.RegOperationEntity
            .Where(op => op.Type == "ACCOUNT" && op.Operation == criteria.OperationName && acceptedProcessStatus.Contains(op.ProcessStatus))
            .Join(_dbContext.RefAccountEntity,
                operation => operation.EntityId,
                account => account.EntityId,
                (operation, account) => new { operation, account.AccountNumber })
            .Where(joined => joined.AccountNumber == accountNumber && joined.operation.ApprovalStatus.Equals(ApprovalStatus.Approved))
            .Select(joined => joined.operation)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<RegOperationEntity>> FindContactOperationAsync(OperationSearchCriteria criteria, string email)
    {
        return await _dbContext.RegOperationEntity
            .Where(op => op.Type == "CONTACT" && op.Operation == criteria.OperationName && acceptedProcessStatus.Contains(op.ProcessStatus) && op.PublishedAt != null)
            .Join(_dbContext.RefContactEntity,
                operation => operation.EntityId,
        contact => contact.EntityId,
                (operation, contact) => new { operation, contact.Email })
            .Where(joined => joined.Email == email)
            .Select(joined => joined.operation)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<RegOperationEntity>> FindRoleOperationAsync(OperationSearchCriteria criteria, string email, string accountNumber)
    {
        return await _dbContext.RegOperationEntity
            .Where(op => op.Type == "ROLE" && op.Operation == criteria.OperationName && acceptedProcessStatus.Contains(op.ProcessStatus))
            .Join(_dbContext.RefRoleEntity,
                operation => operation.EntityId,
                role => role.EntityId,
                (operation, role) => new { operation, role })
            .Where(role => role.role.ContactEmail == email && role.role.AccountNumber == accountNumber)
            .Select(joined => joined.operation)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task UpdateOperationProcessStatusAsync(string processStatus, RegOperationEntity operationEntity)
    {
        operationEntity.ProcessStatus = processStatus;
        _dbContext.RegOperationEntity.Update(operationEntity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> UpdateOperationStatusListASync(string processStatus, IEnumerable<RegOperationEntity> regOperationEntities)
    {
        foreach (var operation in regOperationEntities)
        {
            operation.ProcessStatus = processStatus;
        }
        try
        {
            _dbContext.UpdateRange(regOperationEntities);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Method] {UpdateOperationStatusListASync}; Error: Failed to update process status for list of operations; [Exception]:{ex.Message}");
            return false;
        }
    }

    public async Task InsertNewOperation(RegOperationEntity operationEntity)
    {
        _dbContext.RegOperationEntity.Add(operationEntity);
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw new DbOperationException($"Something went wrong while creating operation for entity {operationEntity.EntityId}", ex.InnerException);
        }
    }

    public async Task<int> FindContactsReadyOperationsAsync(string email, string operationType)
    {
        return await _dbContext.RegOperationEntity.Where(
            op => op.Type == "CONTACT" &&
            op.Operation.ToLower().Equals(OperationName.Insert.ToLower()) &&
            op.ProcessStatus.ToLower().Equals(ProcessStatus.Ready.ToLower()))
            .Join(_dbContext.RefContactEntity,
            operation => operation.EntityId, contact => contact.EntityId,
            (operation, contact) => new { operation, contact.Email })
            .Where(joined =>
            joined.Email.Equals(email))
            .AsNoTracking().CountAsync();
    }

    public async Task<List<RegOperationEntity>> FindSentOperationsAsync(string entityType, string operationType)
    {
        DateTime twentyFourHoursAgo = DateTime.UtcNow.AddHours(-24);

        return await _dbContext.RegOperationEntity.AsNoTracking().Where(
            op => op.Type.ToLower() == entityType.ToLower() &&
            op.Operation.ToLower().Equals(operationType.ToLower()) &&
            op.ProcessStatus.ToLower().Equals(ProcessStatus.Sent.ToLower()) &&
            op.PublishedAt >= twentyFourHoursAgo)
            .ToListAsync();
    }

}

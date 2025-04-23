// <copyright file="OperationRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Enums;
using Application.Exceptions;
using Application.Interfaces;
using Application.Mappers;
using Application.Models;
using Application.Requests;
using EFCore.BulkExtensions;
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

namespace Infrastructure.Repository
{
    /// <summary>
    /// Provides a concrete implementation of the <see cref="IOperationRepository"/> interface.
    /// This repository handles all data access operations related to operations.
    /// </summary>
    public class OperationRepository : IOperationRepository
    {
        private readonly RefContext _dbContext;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly ILogger<OperationRepository> _logger;
        private readonly IDictionary<OperationStrategyType, IOperationQueryStrategy> _strategies;
        private readonly string[] acceptedProcessStatus = { ProcessStatus.Sent, ProcessStatus.Failed };

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationRepository"/> class.
        /// </summary>
        /// <param name="dbContext">The database context for operations.</param>
        /// <param name="logger">The logger instance for logging errors and diagnostics.</param>
        /// <param name="strategies">A dictionary of query strategies keyed by operation type.</param>
        public OperationRepository(RefContext dbContext, ILogger<OperationRepository> logger, IDictionary<OperationStrategyType, IOperationQueryStrategy> strategies)
        {
            _dbContext = dbContext;
            _logger = logger;
            _strategies = strategies;
            _retryPolicy = Policy
                .Handle<SqlException>()
                .WaitAndRetryAsync(
                    retryCount: 1,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(GlobalConstants.RETRYTIMESPAN));
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<RegOperationDetail?>> FetchOperationsByAccountNumberAsync(string accountNumber, OperationSearchCriteria criteria, OperationSource source)
        {
            var operationStatus = !string.IsNullOrWhiteSpace(criteria.OperationApprovalStatus)
                ? new HashSet<string>(criteria.OperationApprovalStatus.Split('|', StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase)
                : null;

            var baseQuery = _dbContext.RegOperationEntity.Where(op =>
                op.Operation == criteria.OperationName &&
                (operationStatus == null || operationStatus.Contains(op.ApprovalStatus)) &&
                !op.PublishedAt.HasValue &&
                op.Type == OperationCategory.ROLE);

            IQueryable<RegOperationDetail?> query = source switch
            {
                OperationSource.RefTables =>
                    from op in baseQuery
                    join role in _dbContext.RefRoleEntity on op.EntityId equals role.EntityId
                    join account in _dbContext.RefAccountEntity on role.AccountNumber equals account.AccountNumber
                    join contact in _dbContext.RefContactEntity on role.ContactEmail equals contact.Email
                    where account.AccountNumber == accountNumber
                    select MapDbEntityToModel.MapDbOperationEntityToOperationDetailModel(op, role, contact, account.AccountNumber),

                OperationSource.MainTables =>
                    from op in baseQuery
                    join role in _dbContext.RefRoleEntity on op.EntityId equals role.EntityId
                    join account in _dbContext.AccountEntities on role.AccountNumber equals account.AccountNumber
                    join contact in _dbContext.ContactEntities on role.ContactEmail equals contact.Email
                    where account.AccountNumber == accountNumber
                    select MapDbEntityToModel.MapDbOperationEntityToOperationDetailModel(op, role, contact, account.AccountNumber),

                OperationSource.AccountMainAndContactRefTables =>
                    from op in baseQuery
                    join role in _dbContext.RefRoleEntity on op.EntityId equals role.EntityId
                    join account in _dbContext.AccountEntities on role.AccountNumber equals account.AccountNumber
                    join contact in _dbContext.RefContactEntity on role.ContactEmail equals contact.Email
                    where account.AccountNumber == accountNumber
                    select MapDbEntityToModel.MapDbOperationEntityToOperationDetailModel(op, role, contact, account.AccountNumber),

                _ => throw new ArgumentException("Invalid operation source", nameof(source))
            };

            return await query.ToListAsync();
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public async Task<IEnumerable<RegOperationEntity>> FetchOperationsByCriteriaAsync(
            OperationSearchCriteria criteria,
            OperationStrategyType category,
            string primaryFilter,
            bool? filterByPublished = false,
            string? secondaryFilter = null)
        {
            if (!_strategies.TryGetValue(category, out var strategy))
            {
                throw new ArgumentException("Invalid operation category", nameof(category));
            }

            var query = strategy.BuildQuery(criteria, primaryFilter, filterByPublished, secondaryFilter);
            return await query.AsNoTracking().ToListAsync();
        }

        /// <inheritdoc/>
        public IQueryable<AccountOperationRecord> GetAccountOperationDetails(string operationName)
        {
            var query = (from operation in _dbContext.RegOperationEntity
                         join account in _dbContext.RefAccountEntity
                         on operation.EntityId equals account.EntityId
                         where operation.Type == OperationCategory.ACCOUNT &&
                               operation.Operation.Equals(operationName) &&
                               operation.PublishedAt == null &&
                               operation.ApprovalStatus == ApprovalStatus.Approved
                         select new AccountOperationRecord()
                         {
                             Operation = operation,
                             Account = account
                         })
                        .AsNoTracking();

            return query;
        }

        /// <inheritdoc/>
        public IQueryable<ContactOperationRecord> GeContactOperationDetails(string operationType)
        {
            var query = (from operation in _dbContext.RegOperationEntity
                         join contact in _dbContext.RefContactEntity
                         on operation.EntityId equals contact.EntityId
                         where operation.Type == OperationCategory.CONTACT &&
                               operation.PublishedAt == null &&
                               operation.ApprovalStatus == ApprovalStatus.Approved &&
                               operation.Operation == operationType
                         orderby operation.CreationDate
                         select new ContactOperationRecord
                         {
                             Operation = operation,
                             RefContactEntity = contact
                         })
                        .AsNoTracking();

            return query;
        }

        /// <inheritdoc/>
        public async Task<List<RoleOperationRecord>> GeRoleOperationDetailsAsync(
            string operationName,
            int chuckSize,
            bool? fetchSystemCreatedOperations = false)
        {
            IQueryable<RoleOperationRecord> query = default;
            if (fetchSystemCreatedOperations.HasValue && fetchSystemCreatedOperations.Value)
            {
                var filteredOperations = _dbContext.RegOperationEntity
                    .Where(o => o.Type == OperationCategory.ROLE &&
                                o.Operation.Equals(operationName) &&
                                o.PublishedAt == null &&
                                o.ApprovalStatus == ApprovalStatus.Approved &&
                                o.CreatedBySystem == true);

                query = from op in filteredOperations
                        join account in _dbContext.AccountEntities
                            on op.EntityId equals account.AccountGlobalUniqueId
                        join role in _dbContext.RoleEntities
                            on account.AccountId equals role.AccountId
                        group new { op, account, role }
                              by new { account.AccountNumber, role.ContactEmail } into g
                        select new RoleOperationRecord
                        {
                            Operation = g.First().op,
                            Role = new RefRoleEntity
                            {
                                AccountNumber = g.Key.AccountNumber,
                                ContactEmail = g.Key.ContactEmail
                            }
                        };
            }
            else
            {
                query = from operation in _dbContext.RegOperationEntity
                        join role in _dbContext.RefRoleEntity
                        on operation.EntityId equals role.EntityId
                        where operation.Type == OperationCategory.ROLE &&
                              operation.Operation.Equals(operationName) &&
                              operation.PublishedAt == null &&
                              operation.ApprovalStatus == ApprovalStatus.Approved &&
                              (operation.CreatedBySystem == false || operation.CreatedBySystem == null)
                        select new RoleOperationRecord()
                        {
                            Operation = operation,
                            Role = role,
                        };
            }

            var operationBatch = await query
                .Take(chuckSize)
                .ToListAsync();

            return operationBatch;
        }

        /// <inheritdoc/>
        public async Task<RegOperation?> UpdateOperationByIdAsync(int operationId, RegOperation creOperation)
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

        /// <inheritdoc/>
        public async Task<bool> BulkUpdateOperationsStatusAsync(string processStatus, IEnumerable<RegOperationEntity> regOperationEntities)
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
                _logger.LogError($"[Method] {nameof(BulkUpdateOperationsStatusAsync)}; Error: Failed to update process status for list of operations; [Exception]:{ex.Message}");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task CreateOperationAsync(RegOperationEntity operationEntity)
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
            catch (InvalidOperationException ioEx)
            {
                throw new DbOperationException($"Something went wrong while creating operation for entity {operationEntity.EntityId}", ioEx.InnerException);
            }
        }

        /// <inheritdoc/>
        public async Task<PendingRoleApprovalsResult> GetPendingRoleApprovalsAsync(int contactId, int skip, int pageSize, string? search)
        {
            // STEP 1: Build the base query with necessary joins.
            var baseQuery = from role in _dbContext.RoleEntities.AsNoTracking()
                            join account in _dbContext.AccountEntities.AsNoTracking() on role.AccountId equals account.AccountId
                            join refRole in _dbContext.RefRoleEntity.AsNoTracking() on account.AccountNumber equals refRole.AccountNumber
                            join operation in _dbContext.RegOperationEntity.AsNoTracking() on refRole.EntityId equals operation.EntityId
                            where role.ContactId == contactId &&
                                  operation.ApprovalStatus == ApprovalStatus.Pending &&
                                  !operation.PublishedAt.HasValue &&
                                  operation.Type == OperationCategory.ROLE &&
                                  operation.Operation == OperationAction.Insert
                            select new
                            {
                                account.AccountNumber,
                                account.LegalName,
                                operation.Id,
                                operation.CreationDate,
                                refRole.ContactEmail
                            };

            // STEP 2: Apply the search filter if provided.
            if (!string.IsNullOrWhiteSpace(search))
            {
                baseQuery = baseQuery.Where(x => (x.AccountNumber ?? string.Empty).Contains(search) || (x.LegalName ?? string.Empty).Contains(search));
            }

            // STEP 3: Build two alternative queries joining with contact data.
            var queryFromPulseContact = from item in baseQuery
                                        join contactRef in _dbContext.ContactEntities.AsNoTracking()
                                          on item.ContactEmail equals contactRef.Email
                                        select new
                                        {
                                            item.AccountNumber,
                                            item.LegalName,
                                            item.Id,
                                            item.CreationDate,
                                            item.ContactEmail,
                                            contactRef.FirstName,
                                            contactRef.LastName
                                        };

            var queryFromRefContact = from item in baseQuery
                                      join contactRef in _dbContext.RefContact.AsNoTracking()
                                        on item.ContactEmail equals contactRef.Email
                                      select new
                                      {
                                          item.AccountNumber,
                                          item.LegalName,
                                          item.Id,
                                          item.CreationDate,
                                          item.ContactEmail,
                                          contactRef.FirstName,
                                          contactRef.LastName
                                      };

            // Choose the primary contact data if available.
            bool hasPulseResults = await queryFromPulseContact.AnyAsync();
            var finalQuery = hasPulseResults ? queryFromPulseContact : queryFromRefContact;

            // STEP 4: Server-side grouping and pagination for the group summaries.
            var groupSummaries = (
                from item in finalQuery
                group item by new { item.AccountNumber, item.LegalName } into g
                select new
                {
                    g.Key.AccountNumber,
                    g.Key.LegalName,
                    LatestCreationDate = g.Max(x => x.CreationDate)
                })
            .OrderByDescending(x => x.LatestCreationDate)
            .Skip(skip)
            .Take(pageSize)
            .AsQueryable();

            // STEP 5: Extract the keys for the groups selected.
            var accountNumbers = groupSummaries.Select(g => g.AccountNumber);

            // STEP 6: Retrieve the detail records for the selected groups.
            var detailRecords = finalQuery
                .Where(x => accountNumbers.Contains(x.AccountNumber))
                .OrderByDescending(x => x.CreationDate);

            // STEP 7: Assemble the final grouped results.
            var groupedResults = await groupSummaries.Select(g => new
            {
                g.AccountNumber,
                AccountName = g.LegalName,
                g.LatestCreationDate,
                Operations = detailRecords
                    .Where(d => d.AccountNumber == g.AccountNumber)
                    .Select(d => new PendingRoleApprovalsDetails
                    {
                        OperationId = d.Id,
                        CreationDate = d.CreationDate!.Value,
                        Email = d.ContactEmail,
                        FirstName = d.FirstName,
                        LastName = d.LastName,
                        AccountNumber = d.AccountNumber,
                        LegalName = d.LegalName,

                    })
                    .OrderByDescending(o => o.CreationDate)
                    .AsEnumerable()
            }).ToListAsync();

            //// STEP 8: (Optional) Retrieve total group count for pagination.
            int totalGroups = await finalQuery.Select(a => a.AccountNumber).Distinct().CountAsync();

            // STEP 9: Return the final result.
            return new PendingRoleApprovalsResult
            {
                PendingRoleApprovals = groupedResults.Select(g => new PendingRoleApprovals
                {
                    AccountNumber = g.AccountNumber,
                    AccountName = g.AccountName,
                    Operations = g.Operations
                }),
                TotalItems = totalGroups
            };
        }

        public async Task<List<RegOperationEntity>> GetInsertRoleOperationDuplicates(RegOperation approvedOperation)
        {
            var roleOperationRefData = await _dbContext.RefRoleEntity
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EntityId == approvedOperation.EntityId);

            if (roleOperationRefData != null)
            {
                var duplicates = await _dbContext.RefRoleEntity
                    .AsNoTracking()
                    .Where(r =>
                        r.OperationType == OperationAction.Insert &&
                        r.ContactEmail == roleOperationRefData.ContactEmail &&
                        r.AccountNumber == roleOperationRefData.AccountNumber &&
                        r.EntityId != roleOperationRefData.EntityId
                    ).Select(r => r.EntityId).ToListAsync();

                if (duplicates != null && duplicates.Count > 0)
                {
                    var duplicateOperation = await _dbContext.RegOperationEntity
                        .Where(o => duplicates.Contains(o.EntityId) && o.ApprovalStatus == ApprovalStatus.Pending)
                        .ToListAsync();

                    return duplicateOperation;
                }
            }

            return new List<RegOperationEntity>();
        }
    }
}

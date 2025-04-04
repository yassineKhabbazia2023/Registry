using Application.Consts;
using Application.Interfaces;
using Application.Requests;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;

namespace Infrastructure.Strategies
{
    /// <summary>
    /// Implements the account operation query strategy to build queries
    /// that retrieve account operations from the database based on specified criteria.
    /// </summary>
    public class AccountOperationQueryStrategy : IOperationQueryStrategy
    {
        private readonly RefContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountOperationQueryStrategy"/> class.
        /// </summary>
        /// <param name="dbContext">The database context used to build the query.</param>
        public AccountOperationQueryStrategy(RefContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <inheritdoc/>
        public IQueryable<RegOperationEntity> BuildQuery(
            OperationSearchCriteria criteria,
            string accountNumber,
            bool? filterByPublished = false,
            string? secondaryFilter = null)
        {
            var approvalStatusSet = GetApprovalStatusSet(criteria);
            var query = BuildBaseQuery(criteria);
            query = ApplyPublishedAtFilter(query, criteria, filterByPublished);

            var joinQuery = query
                .Join(_dbContext.RefAccountEntity,
                      operation => operation.EntityId,
                      account => account.EntityId,
                      (operation, account) => new AccountJoinResult
                      {
                          Operation = operation,
                          AccountNumber = account.AccountNumber
                      });

            joinQuery = ApplyAccountNumberFilter(joinQuery, accountNumber);

            joinQuery = joinQuery.Where(joined =>
                approvalStatusSet == null || approvalStatusSet.Contains(joined.Operation.ApprovalStatus));

            return joinQuery.AsNoTracking().Select(joined => joined.Operation);
        }

        /// <summary>
        /// Builds the base query for account operations by filtering on type, operation name,
        /// and process status based on the provided criteria.
        /// </summary>
        /// <param name="criteria">The search criteria containing the operation name and process status filter.</param>
        /// <returns>
        /// An <see cref="IQueryable{T}"/> of <see cref="RegOperationEntity"/> representing the base query.
        /// </returns>
        private IQueryable<RegOperationEntity> BuildBaseQuery(OperationSearchCriteria criteria)
        {
            return _dbContext.RegOperationEntity
                .Where(op => op.Type == OperationCategory.ACCOUNT
                             && op.Operation == criteria.OperationName
                             && criteria.OperationProcessStatus!.Contains(op.ProcessStatus));
        }

        /// <summary>
        /// Retrieves a set of approval statuses from the criteria.
        /// If the <see cref="OperationSearchCriteria.OperationApprovalStatus"/> property is provided,
        /// the string is split into a HashSet of statuses; otherwise, null is returned.
        /// </summary>
        /// <param name="criteria">The search criteria containing the approval status string.</param>
        /// <returns>
        /// A <see cref="HashSet{T}"/> of approval statuses if provided; otherwise, null.
        /// </returns>
        private HashSet<string>? GetApprovalStatusSet(OperationSearchCriteria criteria)
        {
            if (!string.IsNullOrWhiteSpace(criteria.OperationApprovalStatus))
            {
                return new HashSet<string>(
                    criteria.OperationApprovalStatus.Split('|', StringSplitOptions.RemoveEmptyEntries),
                    StringComparer.OrdinalIgnoreCase);
            }
            return null;
        }

        /// <summary>
        /// Applies filtering on the <see cref="RegOperationEntity.PublishedAt"/> field.
        /// <para>
        /// - If <see cref="OperationSearchCriteria.IncludeNullPublishedAt"/> is true, only operations with a null PublishedAt are returned.
        /// - If a <see cref="OperationSearchCriteria.PublishedAt"/> date is provided, the query is filtered using the 
        ///   <see cref="OperationSearchCriteria.PublishedAtGreaterThan"/> flag for comparison.
        /// - Otherwise, if <paramref name="filterByPublished"/> is true, only operations with a non-null PublishedAt are returned.
        /// </para>
        /// </summary>
        /// <param name="query">The current query for operations.</param>
        /// <param name="criteria">The search criteria containing published date filters.</param>
        /// <param name="filterByPublished">
        /// A boolean flag indicating whether to force filtering out operations with a null PublishedAt.
        /// </param>
        /// <returns>
        /// An <see cref="IQueryable{T}"/> of <see cref="RegOperationEntity"/> with the PublishedAt filter applied.
        /// </returns>
        private IQueryable<RegOperationEntity> ApplyPublishedAtFilter(
            IQueryable<RegOperationEntity> query,
            OperationSearchCriteria criteria,
            bool? filterByPublished)
        {
            if (criteria.IncludeNullPublishedAt.HasValue && criteria.IncludeNullPublishedAt.Value)
            {
                // Only get operations where PublishedAt is null.
                query = query.Where(op => op.PublishedAt == null);
            }
            else if (criteria.PublishedAt.HasValue)
            {
                // Compare based on PublishedAtGreaterThan flag.
                if (criteria.PublishedAtGreaterThan.HasValue && criteria.PublishedAtGreaterThan.Value)
                {
                    query = query.Where(op => op.PublishedAt.HasValue && op.PublishedAt >= criteria.PublishedAt.Value);
                }
                else
                {
                    query = query.Where(op => op.PublishedAt.HasValue && op.PublishedAt <= criteria.PublishedAt.Value);
                }
            }
            else if (filterByPublished.HasValue && filterByPublished.Value)
            {
                // Fallback: filter out operations where PublishedAt is not null.
                query = query.Where(op => op.PublishedAt != null);
            }

            return query;
        }

        /// <summary>
        /// Applies filtering based on the account number.
        /// If an empty or null account number is provided, no filtering is applied.
        /// </summary>
        /// <param name="query">The query containing the join results with account numbers.</param>
        /// <param name="accountNumber">
        /// The account number to filter by. If empty, all records are returned.
        /// </param>
        /// <returns>
        /// An <see cref="IQueryable{T}"/> of <see cref="AccountJoinResult"/> filtered by account number.
        /// </returns>
        private IQueryable<AccountJoinResult> ApplyAccountNumberFilter(
            IQueryable<AccountJoinResult> query,
            string accountNumber)
        {
            if (!string.IsNullOrEmpty(accountNumber))
            {
                query = query.Where(joined => joined.AccountNumber == accountNumber);
            }
            return query;
        }

        /// <summary>
        /// Represents the result of joining an operation with its account data.
        /// </summary>
        private class AccountJoinResult
        {
            /// <summary>
            /// Gets or sets the account operation.
            /// </summary>
            public RegOperationEntity Operation { get; set; } = null!;

            /// <summary>
            /// Gets or sets the account number associated with the operation.
            /// </summary>
            public string AccountNumber { get; set; } = null!;
        }
    }
}

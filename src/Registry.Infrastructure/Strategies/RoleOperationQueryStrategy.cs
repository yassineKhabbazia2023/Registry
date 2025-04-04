using Application.Consts;
using Application.Interfaces;
using Application.Requests;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;

namespace Infrastructure.Strategies
{
    /// <summary>
    /// Implements a query strategy for retrieving role operations.
    /// </summary>
    public class RoleOperationQueryStrategy : IOperationQueryStrategy
    {
        private readonly RefContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleOperationQueryStrategy"/> class.
        /// </summary>
        /// <param name="dbContext">The database context used to build queries.</param>
        public RoleOperationQueryStrategy(RefContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <inheritdoc/>
        public IQueryable<RegOperationEntity> BuildQuery(
            OperationSearchCriteria criteria,
            string email,
            bool? filterByPublished = false,
            string? accountNumber = null)
        {
            // Build the base query for role operations.
            var query = BuildBaseQuery(criteria);

            // If system-generated operations are requested, filter by CreatedBySystem.
            if (criteria.FetchSystemGeneratedOperation.HasValue && criteria.FetchSystemGeneratedOperation.Value)
            {
                query = query.Where(op => op.CreatedBySystem == true);
            }

            // Apply PublishedAt filtering.
            query = ApplyPublishedAtFilter(query, criteria, filterByPublished);

            // Apply approval status filtering.
            var approvalStatusSet = GetApprovalStatusSet(criteria);
            if (!string.IsNullOrWhiteSpace(criteria.OperationApprovalStatus))
            {
                query = query.Where(op => approvalStatusSet.Contains(op.ApprovalStatus));
            }

            // For non-system-generated operations, join with the role reference table and filter by email and account number.
            if (!(criteria.FetchSystemGeneratedOperation.HasValue && criteria.FetchSystemGeneratedOperation.Value))
            {
                var joinQuery = query
                    .Join(_dbContext.RefRoleEntity,
                          operation => operation.EntityId,
                          role => role.EntityId,
                          (operation, role) => new RoleJoinResult
                          {
                              Operation = operation,
                              ContactEmail = role.ContactEmail,
                              AccountNumber = role.AccountNumber
                          });

                joinQuery = ApplyRoleFilters(joinQuery, email, accountNumber);

                return joinQuery.AsNoTracking().Select(joined => joined.Operation);
            }

            return query.AsNoTracking();
        }

        /// <summary>
        /// Builds the base query for role operations by filtering on type, operation name, and process status.
        /// </summary>
        /// <param name="criteria">The search criteria containing the operation name and process status.</param>
        /// <returns>An <see cref="IQueryable{T}"/> of <see cref="RegOperationEntity"/>.</returns>
        private IQueryable<RegOperationEntity> BuildBaseQuery(OperationSearchCriteria criteria)
        {
            return _dbContext.RegOperationEntity
                .Where(op => op.Type == OperationCategory.ROLE
                             && op.Operation == criteria.OperationName
                             && criteria.OperationProcessStatus!.Contains(op.ProcessStatus));
        }

        /// <summary>
        /// Retrieves a HashSet of approval statuses from the criteria.
        /// </summary>
        /// <param name="criteria">The search criteria containing the approval status string.</param>
        /// <returns>A HashSet of approval statuses if provided; otherwise, null.</returns>
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
        /// Applies filtering on the PublishedAt field.
        /// <para>
        /// - If a PublishedAt date is provided in criteria, it compares using the PublishedAtGreaterThan flag.
        /// - Otherwise, if filterByPublished is true, only operations with a non-null PublishedAt are returned.
        /// </para>
        /// </summary>
        /// <param name="query">The query for operations.</param>
        /// <param name="criteria">The search criteria containing published date filters.</param>
        /// <param name="filterByPublished">
        /// A flag indicating whether to filter out operations with a null PublishedAt.
        /// </param>
        /// <returns>The filtered query.</returns>
        private IQueryable<RegOperationEntity> ApplyPublishedAtFilter(
            IQueryable<RegOperationEntity> query,
            OperationSearchCriteria criteria,
            bool? filterByPublished)
        {
            if (criteria.PublishedAt.HasValue)
            {
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
                query = query.Where(op => op.PublishedAt != null);
            }

            return query;
        }

        /// <summary>
        /// Applies email and account number filters to the joined role data.
        /// If email or accountNumber is empty, that filter is skipped.
        /// </summary>
        /// <param name="query">The query containing role join results.</param>
        /// <param name="email">The contact email to filter by.</param>
        /// <param name="accountNumber">The account number to filter by.</param>
        /// <returns>The filtered query with role join results.</returns>
        private IQueryable<RoleJoinResult> ApplyRoleFilters(
            IQueryable<RoleJoinResult> query,
            string email,
            string? accountNumber)
        {
            if (!string.IsNullOrEmpty(email))
            {
                query = query.Where(joined => joined.ContactEmail == email);
            }
            if (!string.IsNullOrEmpty(accountNumber))
            {
                query = query.Where(joined => joined.AccountNumber == accountNumber);
            }
            return query;
        }

        /// <summary>
        /// Represents the result of joining a role operation with its role reference data.
        /// </summary>
        private class RoleJoinResult
        {
            /// <summary>
            /// Gets or sets the role operation.
            /// </summary>
            public RegOperationEntity Operation { get; set; } = null!;

            /// <summary>
            /// Gets or sets the contact email from the role reference.
            /// </summary>
            public string ContactEmail { get; set; } = null!;

            /// <summary>
            /// Gets or sets the account number from the role reference.
            /// </summary>
            public string AccountNumber { get; set; } = null!;
        }
    }
}

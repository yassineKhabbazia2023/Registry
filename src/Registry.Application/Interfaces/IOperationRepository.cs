// <copyright file="IOperationRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Interfaces
{
    using Application.Enums;
    using Application.Models;
    using Application.Requests;
    using Pulse.Registry.Domain.Entities;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines a contract for performing operations on operation entities.
    /// This repository interface encapsulates methods for fetching, updating,
    /// and creating operations, as well as retrieving operation details.
    /// </summary>
    public interface IOperationRepository
    {
        /// <summary>
        /// Fetches a collection of operation entities based on the provided search criteria and filters.
        /// </summary>
        /// <param name="criteria">
        /// The criteria containing filters for the operations (such as operation name, process status, published date, and approval status).
        /// </param>
        /// <param name="category">
        /// The category of operation (e.g. Account, Contact, or Role).
        /// </param>
        /// <param name="primaryFilter">
        /// The primary filter value (for example, an account number, email, etc.) to narrow the query.
        /// If an empty string is provided, no filtering on this field is applied.
        /// </param>
        /// <param name="filterByPublished">
        /// A flag indicating whether to filter operations to include only those with a non-null <see cref="RegOperationEntity.PublishedAt"/> field,
        /// unless overridden by the search criteria.
        /// </param>
        /// <param name="secondaryFilter">
        /// An optional secondary filter to apply additional restrictions.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an enumerable collection of <see cref="RegOperationEntity"/>.
        /// </returns>
        Task<IEnumerable<RegOperationEntity>> FetchOperationsByCriteriaAsync(
            OperationSearchCriteria criteria,
            OperationStrategyType category,
            string primaryFilter,
            bool? filterByPublished = false,
            string? secondaryFilter = null);

        /// <summary>
        /// Fetches a collection of operation details for a given account number using the specified criteria and source.
        /// </summary>
        /// <param name="accountNumber">The account number to filter operations by.</param>
        /// <param name="criteria">
        /// The criteria containing filters for the operations, such as operation name, process status, published date, and approval status.
        /// </param>
        /// <param name="source">
        /// The source specifying which join strategy to use.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an enumerable collection of <see cref="RegOperationDetail"/>,
        /// which may include null elements.
        /// </returns>
        Task<IEnumerable<RegOperationDetail?>> FetchOperationsByAccountNumberAsync(
            string accountNumber,
            OperationSearchCriteria criteria,
            OperationSource source);

        /// <summary>
        /// Updates an operation entity identified by its ID with the provided updated data.
        /// </summary>
        /// <param name="operationId">The unique identifier of the operation to update.</param>
        /// <param name="creOperation">The operation object containing the updated information.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the updated <see cref="RegOperation"/>.
        /// </returns>
        Task<RegOperation?> UpdateOperationByIdAsync(int operationId, RegOperation creOperation);

        /// <summary>
        /// Retrieves an operation entity by its unique identifier.
        /// </summary>
        /// <param name="operationId">The unique identifier of the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the <see cref="RegOperation"/> if found; otherwise, null.
        /// </returns>
        Task<RegOperation?> GetOperationByIdAsync(int operationId);

        /// <summary>
        /// Performs a bulk update of the process status for a collection of operation entities.
        /// </summary>
        /// <param name="processStatus">The new process status to assign to the operations.</param>
        /// <param name="regOperationEntities">The collection of operation entities to update.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result indicates whether the update succeeded.
        /// </returns>
        Task<bool> BulkUpdateOperationsStatusAsync(string processStatus, IEnumerable<RegOperationEntity> regOperationEntities);

        /// <summary>
        /// Creates a new operation entity.
        /// </summary>
        /// <param name="operationEntity">The operation entity to create.</param>
        /// <returns>
        /// A task that represents the asynchronous creation operation.
        /// </returns>
        Task CreateOperationAsync(RegOperationEntity operationEntity);

        /// <summary>
        /// Retrieves a queryable collection of account operation records based on the operation name.
        /// </summary>
        /// <param name="operationName">The name of the operation to filter by.</param>
        /// <returns>
        /// An <see cref="IQueryable{T}"/> of <see cref="AccountOperationRecord"/> containing account operation details.
        /// </returns>
        IQueryable<AccountOperationRecord> GetAccountOperationDetails(string operationName);

        /// <summary>
        /// Retrieves a queryable collection of contact operation records based on the operation type.
        /// </summary>
        /// <param name="operationType">The type of operation to filter by.</param>
        /// <returns>
        /// An <see cref="IQueryable{T}"/> of <see cref="ContactOperationRecord"/> containing contact operation details.
        /// </returns>
        IQueryable<ContactOperationRecord> GeContactOperationDetails(string operationType);

        /// <summary>
        /// Retrieves a list of role operation records based on the operation name and a specified chunk size.
        /// </summary>
        /// <param name="operationName">The name of the operation to filter by.</param>
        /// <param name="chuckSize">
        /// The maximum number of records to retrieve.
        /// </param>
        /// <param name="fetchSystemCreatedOperations">
        /// A flag indicating whether to include system-generated role operations.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a list of <see cref="RoleOperationRecord"/>.
        /// </returns>
        Task<List<RoleOperationRecord>> GeRoleOperationDetailsAsync(
            string operationName,
            int chuckSize,
            bool? fetchSystemCreatedOperations = false);

        /// <summary>
        /// Retrieves a list of pending role approvals for a given contact, grouped by accounts.
        /// </summary>
        /// <param name="contactId">The unique identifier of the contact.</param>
        /// <param name="skip">The number of records to skip for pagination.</param>
        /// <param name="pageSize">The number of records to take for pagination.</param>
        /// <param name="search">Optional search term to filter the results.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an instance  of <see cref="PendingRoleApprovals"/> objects,
        /// each representing an account with its associated pending role operations.
        /// </returns>
        Task<PendingRoleApprovalsResult> GetPendingRoleApprovalsAsync(int contactId, int skip, int pageSize, string? search);

        /// <summary>
        /// Retrieves a list of duplicate operation IDs for a given "Insert Role" operation.
        /// </summary>
        /// <param name="approvedOperation">
        /// The operation object containing the details of the "Insert Role" operation to check for duplicates.
        /// This includes the entity ID, contact email, and account number associated with the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a list of operation IDs
        /// that are duplicates of the provided operation. Duplicates are determined based on matching entity ID,
        /// contact email, and account number, and only operations with a pending approval status are included.
        /// </returns>
        Task<List<RegOperationEntity>> GetInsertRoleOperationDuplicates(RegOperation approvedOperation);

        /// <summary>
        /// Checks whether a pending Insert Role operation already exists for the given account, contact, and description.
        /// </summary>
        /// <param name="accountNumber">The account number.</param>
        /// <param name="contactEmail">The contact email.</param>
        /// <param name="description">The role description (e.g. "CLP", "AM").</param>
        /// <returns>True if a matching pending operation exists; otherwise, false.</returns>
        Task<bool> DoesRoleInsertOperationExistAsync(string accountNumber, string contactEmail, string description);
    }
}

using Application.Requests;
using Pulse.Registry.Domain.Entities;
using System.Linq;

namespace Application.Interfaces
{
    /// <summary>
    /// Provides a strategy for building queries to retrieve <see cref="RegOperationEntity"/> instances
    /// based on various search criteria and filtering options.
    /// </summary>
    public interface IOperationQueryStrategy
    {
        /// <summary>
        /// Builds an <see cref="IQueryable{T}"/> query for <see cref="RegOperationEntity"/> by applying filters
        /// based on the provided search criteria.
        /// </summary>
        /// <param name="criteria">
        /// The search criteria that contains parameters such as operation name, process status, published date filter, and approval status.
        /// </param>
        /// <param name="primaryFilter">
        /// The primary filter value (for example, an account number or email) used to narrow down the query.
        /// If an empty string is provided, no filtering on this field is applied.
        /// </param>
        /// <param name="filterByPublished">
        /// If set to true, the query will ensure that the <see cref="RegOperationEntity.PublishedAt"/> field is not null,
        /// unless overridden by criteria.
        /// </param>
        /// <param name="secondaryFilter">
        /// An optional secondary filter for applying additional restrictions to the query.
        /// </param>
        /// <returns>
        /// An <see cref="IQueryable{T}"/> of <see cref="RegOperationEntity"/> representing the filtered operations.
        /// </returns>
        IQueryable<RegOperationEntity> BuildQuery(
            OperationSearchCriteria criteria,
            string primaryFilter,
            bool? filterByPublished = false,
            string? secondaryFilter = null);
    }
}

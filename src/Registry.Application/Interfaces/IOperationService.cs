// <copyright file="IOperationService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Interfaces;

using Application.Models;
using Application.Requests;
using Pulse.Registry.Domain.Entities;

public interface IOperationService
{
    Task<IEnumerable<RegOperationDetail?>> GetOperationsAsync(string accountNumber, OperationSearchCriteria operationSearchCriteria);

    Task<RegOperation?> UpdateOperationAsync(int operationId, string email, RegOperation creOperation);

    Task<RegOperation?> GetOperationByIdAsync(int operationId);

    Task<bool> UpdateContactOperations(OperationSearchCriteria searchCriteria, string email);

    Task TryToProceedUntilTimeoutAsync(string entityType, string operationtType);
    Task UpdateOperationStatusListASync(string processStatus, List<RegOperationEntity> operation);
}

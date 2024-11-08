// <copyright file="IOperationService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Interfaces;

using Application.Models;
using Application.Requests;

public interface IOperationService
{
    Task<IEnumerable<CreOperationDetail?>> GetOperationsAsync(string accountNumber, OperationSearchCriteria operationSearchCriteria);

    Task<CreOperation?> UpdateOperationAsync(int operationId, string email, CreOperation creOperation);

    Task<CreOperation?> GetOperationByIdAsync(int operationId);
}

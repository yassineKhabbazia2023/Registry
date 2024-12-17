// <copyright file="IOperationRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Interfaces;

using Application.Models;
using Application.Requests;

public interface IOperationRepository
{
    Task<IEnumerable<RegOperationDetail?>> GetOperationsAsync(string accountNumber, OperationSearchCriteria operationSearchCriteria);

    Task<RegOperation?> UpdateOperationAsync(int operationId, RegOperation creOperation);

    Task<RegOperation?> GetOperationByIdAsync(int operationId);
}

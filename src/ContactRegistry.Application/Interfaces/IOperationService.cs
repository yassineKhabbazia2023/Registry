// <copyright file="IOperationRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Interfaces;

using Application.Models;
using Application.Requests;

public interface IOperationRepository
{
    Task<IEnumerable<CreOperation?>> GetOperationsAsync(string accountNumber, OperationSearchCriteria operationSearchCriteria);
}

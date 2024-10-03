// <copyright file="IOperationService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Interfaces;

using Application.Models;
using Application.Requests;

public interface IOperationService
{
    Task<IEnumerable<CreOperation?>> GetOperationsAsync(string accountNumber, OperationSearchCriteria operationSearchCriteria);
}

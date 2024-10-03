// <copyright file="IOperationRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Interfaces;

using Application.Models;
public interface IOperationRepository
{
    Task<IEnumerable<CreOperation?>> GetOperationsAsync(string operationName, string status, string accountNumber);
}

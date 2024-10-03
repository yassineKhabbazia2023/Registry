// <copyright file="OperationService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;

namespace Application.Services;

public class OperationService : IOperationService
{
    private readonly IOperationRepository operationRepository;

    public OperationService(IOperationRepository roleRepository)
    {
        this.operationRepository = roleRepository;
    }

    public async Task<IEnumerable<CreOperation?>> GetOperationsAsync(string operationName, string status, string accountNumber)
    {
        ArgumentNullException.ThrowIfNull(accountNumber);
        ArgumentNullException.ThrowIfNull(operationName);
        ArgumentNullException.ThrowIfNull(status);

        return await this.operationRepository.GetOperationsAsync(operationName, status, accountNumber);
    }
}

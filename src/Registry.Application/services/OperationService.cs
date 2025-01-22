// <copyright file="OperationService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Application.Requests;

namespace Application.Services;

public class OperationService : IOperationService
{
    private readonly IOperationRepository operationRepository;

    public OperationService(IOperationRepository roleRepository)
    {
        this.operationRepository = roleRepository;
    }

    public async Task<IEnumerable<RegOperationDetail?>> GetOperationsAsync(string accountNumber, OperationSearchCriteria operationSearchCriteria)
    {
        ArgumentNullException.ThrowIfNull(accountNumber);

        return await this.operationRepository.GetOperationsAsync(accountNumber, operationSearchCriteria);
    }

    public async Task<RegOperation?> UpdateOperationAsync(int operationId, string email, RegOperation creOperation)
    {
        ArgumentNullException.ThrowIfNull(operationId);
        ArgumentNullException.ThrowIfNull(email);

        creOperation.LastStatusUpdatedBy = email;
        return await this.operationRepository.UpdateOperationAsync(operationId, creOperation);
    }

    public async Task<RegOperation?> GetOperationByIdAsync(int operationId)
    {
        return await this.operationRepository.GetOperationByIdAsync(operationId);
    }
}

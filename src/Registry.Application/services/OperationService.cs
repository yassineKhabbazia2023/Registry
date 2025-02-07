// <copyright file="OperationService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Interfaces;
using Application.Models;
using Application.Requests;
using Microsoft.Extensions.Logging;
using Pulse.ContactRegistry.Domain.Entities;

namespace Application.Services;

public class OperationService : IOperationService
{
    private readonly ILogger<OperationService> logger;
    private readonly IOperationRepository operationRepository;

    public OperationService(IOperationRepository roleRepository, ILogger<OperationService> logger)
    {
        this.operationRepository = roleRepository;
        this.logger = logger;
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

    public async Task<bool> UpdateContactOperations(OperationSearchCriteria searchCriteria, string email)
    {
        IEnumerable<RegOperationEntity> operationList = await this.operationRepository.FindContactOperationAsync(searchCriteria, email);
        if (!operationList.Any())
        {
            logger.LogWarning($"[Method]: {nameof(UpdateContactOperations)} operation List is empty!");
            return true;
        }
        var updateSucceed = await this.operationRepository.UpdateOperationStatusListASync(ProcessStatus.Succeeded, operationList);
        if (!updateSucceed)
        {
            logger.LogError($"[Method]: {nameof(UpdateContactOperations)}; [Error]: something went wrong while updating list of operation!");
        }
        return updateSucceed;
    }

 
}

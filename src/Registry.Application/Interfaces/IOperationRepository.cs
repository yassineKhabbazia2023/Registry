// <copyright file="IOperationRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Interfaces;

using Application.Models;
using Application.Requests;
using Pulse.ContactRegistry.Domain.Entities;

public interface IOperationRepository
{
    Task<IEnumerable<RegOperationDetail?>> GetOperationsAsync(string accountNumber, OperationSearchCriteria operationSearchCriteria);

    Task<RegOperation?> UpdateOperationAsync(int operationId, RegOperation creOperation);

    Task<RegOperation?> GetOperationByIdAsync(int operationId);

    Task UpdateOperationProcessStatusAsync(string processStatus, RegOperationEntity operationEntity);

    Task<IEnumerable<RegOperationEntity>> FindAccountOperationAsync(OperationSearchCriteria criteria, string accountNumber);
    
    Task<IEnumerable<RegOperationEntity>> FindContactOperationAsync(OperationSearchCriteria criteria, string email);
    
    Task<IEnumerable<RegOperationEntity>> FindRoleOperationAsync(OperationSearchCriteria criteria, string email, string accountNumber);
    Task<bool> UpdateOperationStatusListASync(string processStatus, IEnumerable<RegOperationEntity> regOperationEntities);
}

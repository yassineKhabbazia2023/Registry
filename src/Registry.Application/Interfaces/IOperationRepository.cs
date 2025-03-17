// <copyright file="IOperationRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Interfaces;

using Application.Models;
using Application.Requests;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;
using System.Runtime.InteropServices;

public interface IOperationRepository
{
    Task<IEnumerable<RegOperationDetail?>> GetOperationsByRefTablesAsync(string accountNumber, OperationSearchCriteria operationSearchCriteria);
    
    Task<IEnumerable<RegOperationDetail?>> GetOperationsByMainTablesAsync(string accountNumber, OperationSearchCriteria operationSearchCriteria);

    Task<IEnumerable<RegOperationDetail?>> GetOperationsByAccountMainAndContactRefTablesAsync(string accountNumber, OperationSearchCriteria operationSearchCriteria);

    Task<RegOperation?> UpdateOperationAsync(int operationId, RegOperation creOperation);

    Task<RegOperation?> GetOperationByIdAsync(int operationId);

    Task UpdateOperationProcessStatusAsync(string processStatus, RegOperationEntity operationEntity);

    Task<IEnumerable<RegOperationEntity>> FindAccountOperationAsync(OperationSearchCriteria criteria, string accountNumber);

    Task<IEnumerable<RegOperationEntity>> FindContactOperationAsync(OperationSearchCriteria criteria, string email);

    Task<IEnumerable<RegOperationEntity>> FindRoleOperationAsync(OperationSearchCriteria criteria, string email, string accountNumber);

    Task<bool> UpdateOperationStatusListASync(string processStatus, IEnumerable<RegOperationEntity> regOperationEntities);

    Task InsertNewOperation(RegOperationEntity operationEntity);

    Task<int> FindContactsReadyOperationsAsync(string email, string operationType);

    Task<bool> AddOperationAsync(RegOperationEntity regOperation);

    Task<List<RegOperationEntity>> FindSentOperationsAsync(string entityType, string operationType);
}

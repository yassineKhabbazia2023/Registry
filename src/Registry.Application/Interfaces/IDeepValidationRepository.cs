// <copyright file="IDeepValidationRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Pulse.Registry.Domain.Entities.Audits;

namespace Application.Interfaces;

public interface IDeepValidationRepository
{
    Task<bool> AddDeepValidationAsync(DeepValidationEntity deepValidation);

    Task<bool> DoesDeepValidationLineExistsAsync(Guid entityId, string operationType);
}

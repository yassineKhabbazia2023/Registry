// <copyright file="RoleDeepValidator.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Enums;
using Application.Interfaces;
using Application.Interfaces.RuleValidators;
using Application.Requests;
using Pulse.Registry.Domain.Entities;
using Pulse.Registry.Domain.Entities.Accounts;
using Pulse.Registry.Domain.Entities.Audits;
using Registry.Application.Consts;

namespace Application.DeepValidations;

public class RoleDeepValidator : IRoleDeepValidator
{
    private bool _isValid = false;
    private bool _canExecuteDeleteOperation = false;
    private bool _existingRoleHasOutdatedFlags = false;
    private const string RoleSource = "PENNYLANE";

    private RefRoleEntity? _refRole;
    private readonly IRoleRepository _roleRepository;
    private readonly IOperationRepository _operationRepository;
    private readonly IDeepValidationRepository _deepValidationRepository;
    private readonly IContactRepository _contactRepository;
    private readonly IAccountRepository _accountRepository;

    public RoleDeepValidator(IRoleRepository roleRepository,
        IOperationRepository operationRepository,
        IDeepValidationRepository deepValidationRepository,
        IContactRepository contactRepository,
        IAccountRepository accountRepository)
    {
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _operationRepository = operationRepository ?? throw new ArgumentNullException(nameof(operationRepository));
        _deepValidationRepository = deepValidationRepository ?? throw new ArgumentNullException(nameof(deepValidationRepository));
        _contactRepository = contactRepository ?? throw new ArgumentNullException(nameof(contactRepository));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
    }

    public async Task<IRoleDeepValidator> Instantiate(RefRoleEntity refRoleEntity)
    {
        _refRole = refRoleEntity ?? throw new ArgumentNullException(nameof(refRoleEntity));
        _isValid = true;
        return await Task.FromResult(this);
    }

    public async Task<IRoleDeepValidator> ContactShouldExistInPulse()
    {
        if (!_isValid) return this;
        _isValid = await DoesContactExistInPulse();
        if (!_isValid)
        {
            await AddDeepValidation($"Contact {_refRole.ContactEmail} Does not exists in Contacts.Contact");
        }
        return this;
    }

    public async Task<IRoleDeepValidator> ContactShouldExistInPulseOrOperations()
    {
        if (!_isValid) return this;
        bool existsInPulse = await DoesContactExistInPulse();
        bool existsInOperations = await DoesContactExistInOperation(_refRole.ContactEmail);
        _isValid = existsInPulse || existsInOperations;
        if (!_isValid)
        {
            await AddDeepValidation($"Contact {_refRole.ContactEmail} Does not exists in Contacts.Contact nor has an operation of type INSERT and ProcessStatus READY");
        }
        return this;
    }

    public async Task<IRoleDeepValidator> AccountShouldExistInPulse()
    {
        if (!_isValid) return this;
        string accountNumber = _refRole.AccountNumber;
        _isValid = await DoesAccountExistInPulse();
        if (!_isValid)
        {
            await AddDeepValidation($"Account {_refRole.AccountNumber} Does not exists in Accounts.Account");
        }
        return this;
    }

    public async Task<IRoleDeepValidator> AccountShouldExistInPulseOrOperations()
    {
        if (!_isValid) return this;
        bool isAccountExistsInPulse = await DoesAccountExistInPulse();
        bool isAccountExistsInOperations = await DoesAccountExistInOperation(_refRole.AccountNumber);
        _isValid = isAccountExistsInPulse || isAccountExistsInOperations;

        if (!_isValid)
        {
            await AddDeepValidation($"Account {_refRole.AccountNumber} Does not exists in Accounts.Account nor in Account Operations of type INSERT and ProcessStatus READY ");
        }
        return this;
    }


    /// <summary>
    /// Role should not already existed in pulse for insert operations
    /// </summary>
    /// <returns></returns>
    public async Task<IRoleDeepValidator> RoleShouldShouldNotExistInPulseOrOperations()
    {
        if (!_isValid) return this;

        // Collaborateur avec Description (ex CLP,AM) : on autorise même si le rôle existe déjà,
        // sauf si une opération pending avec la même Description existe
        if (!string.IsNullOrWhiteSpace(_refRole.Description) && !await IsContactOfTypeCustomer())
        {
            bool duplicateExists = await _operationRepository.DoesRoleInsertOperationExistAsync(
                _refRole.AccountNumber, _refRole.ContactEmail, _refRole.Description);
            if (duplicateExists)
            {
                await AddDeepValidation($"Operation is in pending for account {_refRole.AccountNumber} and email {_refRole.ContactEmail} and desciption {_refRole.Description}");
                _isValid = false;
            }
            return this;
        }

        bool roleExistInPulse = await DoesRoleExistInPulse();

        if (roleExistInPulse)
        {
            await AddDeepValidation($"Role exist in pulse or operation is in pending for account {_refRole.AccountNumber} and email { _refRole.ContactEmail }");
            _isValid = false;
            return this;
        }

        // Si le rôle existe dans Pulse avec des flags différents, DoesRoleExistInPulse retourne false
        // intentionnellement pour autoriser une opération de synchronisation des flags. Dans ce cas
        // on ne doit pas non plus bloquer sur une opération pending existante.
        if (_existingRoleHasOutdatedFlags)
        {
            return this;
        }

        if (await DoesRoleExistInOperation(_refRole.AccountNumber, _refRole.ContactEmail))
        {
            await AddDeepValidation($"Role exist in pulse or operation is in pending for account {_refRole.AccountNumber} and email { _refRole.ContactEmail }");
            _isValid = false;
        }
        return this;
    }

    public async Task<IRoleDeepValidator> RoleShouldExistInPulse()
    {
        if (!_isValid) return this;
        bool roleExistInPulse = await DoesRoleExistInPulse();
        if (!roleExistInPulse)
        {
            _isValid = false;
            await AddDeepValidation($"Can not have operation Role DELETE for a role that does not exists in PULSE!");
        }
        return this;
    }

        public async Task<IRoleDeepValidator> TryAddOperation()
        {
            if (!_isValid) return this;
            bool canCreateNewOperation = true;
            RegOperationEntity operationEntity = new RegOperationEntity()
            {
                ApprovalStatus = ApprovalStatus.Approved,
                EntityId = _refRole.EntityId,
                CreationDate = DateTime.UtcNow,
                Operation = _refRole.OperationType,
                Type = OperationCategory.ROLE,
                ProcessStatus = ProcessStatus.Ready
            };
            switch (_refRole.OperationType)
            {
                case OperationAction.Insert:
                    #region Est-ce que pour ce type de rôle, si IsContactOfTypeCustomer est true, on aurait besoin d'une validation mod ? Si oui, on supprime cette région en gardant que l’existant, sinon on garde la région.
                    var existingRole = await _roleRepository.GetPulseRole(_refRole.ContactEmail, _refRole.AccountNumber);
                    if (existingRole != null && (_refRole.ContactFlagPortailFactures != existingRole.ContactFlagPortailFactures
                        || _refRole.ContactFlagMainContact != existingRole.ContactFlagMainContact))
                    {
                        operationEntity.ApprovalStatus = ApprovalStatus.Approved;
                    }
                    else
                    {
                        operationEntity.ApprovalStatus = await IsContactOfTypeCustomer() ?
                            !string.IsNullOrWhiteSpace(_refRole.RoleSource) && _refRole.RoleSource.Equals(DataSources.PENNYLANE.ToString(), StringComparison.OrdinalIgnoreCase) ?
                            ApprovalStatus.Approved : ApprovalStatus.Pending :
                            ApprovalStatus.Approved;
                    }
                    #endregion
                    break;
                case OperationAction.Delete:
                    canCreateNewOperation = _canExecuteDeleteOperation;
                    break;
            }
            if (canCreateNewOperation)
            {
                await _operationRepository.CreateOperationAsync(operationEntity);
            }
            return this;
        }

    public async Task<bool> Validate()
    {
        return await Task.FromResult(_isValid);
    }

    private async Task AddDeepValidation(string reason)
    {
        DeepValidationEntity deepValidationEntity = new DeepValidationEntity
        {
            EntityId = _refRole.EntityId,
            Type = OperationCategory.ROLE,
            CreationDate = DateTime.Now,
            Reason = reason
        };
        var doesDeepValidationLineExists = await _deepValidationRepository.DoesDeepValidationLineExistsAsync(deepValidationEntity.EntityId, OperationCategory.ROLE);
        if (!doesDeepValidationLineExists)
        {
            await _deepValidationRepository.AddDeepValidationAsync(deepValidationEntity);
        }
    }

    private async Task<bool> DoesContactExistInPulse()
    {
        return await _contactRepository.DoesContactExistByEmailOrIdAsync(email: _refRole.ContactEmail);
    }

    private async Task<bool> DoesAccountExistInPulse()
    {
        return await _accountRepository.DoesAccountExist(_refRole.AccountNumber);
    }

    private async Task<bool> IsContactOfTypeCustomer()
    {
        var contact = await _contactRepository.GetContactByEmailOrIdAsync(email: _refRole.ContactEmail);
        var refContact = await _contactRepository.GetRefContactByEmailAsync(_refRole.ContactEmail);

        if ((contact != null && contact.Type?.ToLower() == "customer")
            || (refContact != null && (refContact.IsCustomer ?? false)))
        {
            return true;
        }
        return false;
    }

    private async Task<bool> DoesContactExistInOperation(string email, string operation = OperationAction.Insert, string processStatus = "READY")
    {
        ArgumentNullException.ThrowIfNullOrEmpty(email);
        ArgumentNullException.ThrowIfNullOrEmpty(operation);
        ArgumentNullException.ThrowIfNullOrEmpty(processStatus);

        var criteria = new OperationSearchCriteria()
        {
            OperationName = operation,
            OperationProcessStatus = new[] { processStatus }
        };

        var operations = await _operationRepository.FetchOperationsByCriteriaAsync(criteria, OperationStrategyType.CONTACT, email);
        return operations.Any();
    }

    private async Task<bool> DoesAccountExistInOperation(string accountNumber, string operation = OperationAction.Insert, string processStatus = "READY")
    {
        ArgumentNullException.ThrowIfNullOrEmpty(accountNumber);
        ArgumentNullException.ThrowIfNullOrEmpty(operation);
        ArgumentNullException.ThrowIfNullOrEmpty(processStatus);

        var criteria = new OperationSearchCriteria()
        {
            OperationName = operation,
            OperationProcessStatus = new[] { processStatus }
        };

        var operations = await _operationRepository.FetchOperationsByCriteriaAsync(criteria, OperationStrategyType.ACCOUNT, accountNumber);
        return operations.Any();
    }

    private async Task<bool> DoesRoleExistInOperation(string accountNumber, string contactEmail, string operation = OperationAction.Insert, string processStatus = "READY")
    {
        var criteria = new OperationSearchCriteria()
        {
            OperationName = operation,
            OperationProcessStatus = new[] { processStatus }
        };

        var operations = await _operationRepository.FetchOperationsByCriteriaAsync(criteria, OperationStrategyType.ROLE, contactEmail, secondaryFilter: accountNumber);
        return operations.Any();
    }


    private async Task<bool> DoesRoleExistInPulse(bool shouldIncrementCounter = true)
    {
        bool roleExistInPulse = _roleRepository.DoesRoleExistInPulse(_refRole.AccountNumber, _refRole.ContactEmail);
        if (roleExistInPulse)
        {
            var existingRole = await _roleRepository.GetPulseRole(_refRole.ContactEmail, _refRole.AccountNumber);
            if (existingRole != null && (_refRole.ContactFlagPortailFactures != existingRole.ContactFlagPortailFactures
                || _refRole.ContactFlagMainContact != existingRole.ContactFlagMainContact) && _refRole.RoleSource?.ToUpper() != RoleSource)
            {
                // Ce comportement permet de ne pas bloquer la génération d’une opération d’insertion si seule la valeur de ContactFlagPortailFactures ou ContactFlagMainContact change.
                _existingRoleHasOutdatedFlags = true;
                return false;
            }
        }
        if (roleExistInPulse && shouldIncrementCounter)
        {
            await UpdateRoleIteratorBasedOnOperationType();
        }
        return roleExistInPulse;
    }

    private async Task<bool> UpdateRoleIteratorBasedOnOperationType()
    {
        string accountNumber = _refRole.AccountNumber;
        string email = _refRole.ContactEmail;
        RoleEntity? roleEntity = await _roleRepository.GetPulseRole(email, accountNumber);
        if (roleEntity == null)
        {
            return await Task.FromResult(false);
        }
        else
        {
            if (_refRole.OperationType == OperationAction.Insert)
            {
                roleEntity.RoleDuplicatesCounter += 1;
            }
            else if (_refRole.OperationType == OperationAction.Delete && roleEntity.RoleDuplicatesCounter > 0)
            {
                roleEntity.RoleDuplicatesCounter -= 1;
            }

            if (roleEntity.RoleDuplicatesCounter == 0)
            {
                _canExecuteDeleteOperation = true;
            }
        }

        var updated = await _roleRepository.UpdatePulseRole(roleEntity);
        return updated;
    }
}

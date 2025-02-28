using Application.Consts;
using Application.Interfaces;
using Application.Interfaces.RuleValidators;
using Domain.Entities.Accounts;
using Domain.Entities.Audits;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;

namespace Application.DeepValidations
{
    public class RoleDeepValidator : IRoleDeepValidator
    {
        private bool _isValid = false;
        private bool _canExecuteDeleteOperation = false;
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
            bool roleExistInPulse = await DoesRoleExistInPulse();
            bool roleExistInOperation = await DoesRoleExistInOperation(_refRole.AccountNumber, _refRole.ContactEmail);
            if (roleExistInPulse || roleExistInOperation)
            {
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
                Type = "ROLE",
                ProcessStatus = ProcessStatus.Ready
            };
            switch (_refRole.OperationType)
            {
                case "INSERT":
                    operationEntity.ApprovalStatus = await IsContactOfTypeCustomer() ? ApprovalStatus.Pending : ApprovalStatus.Approved;
                    break;
                case "DELETE":
                    canCreateNewOperation = _canExecuteDeleteOperation;
                    break;
            }
            if (canCreateNewOperation)
            {
                await _operationRepository.AddOperationAsync(operationEntity);
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
                Type = "ROLE",
                CreationDate = DateTime.Now,
                Reason = reason
            };
            var doesDeepValidationLineExists = await _deepValidationRepository.DoesDeepValidationLineExistsAsync(deepValidationEntity.EntityId, OperationType.ROLE);
            if(!doesDeepValidationLineExists)
            {
                await _deepValidationRepository.AddDeepValidationAsync(deepValidationEntity);
            }
        }

        private async Task<bool> DoesContactExistInPulse()
        {
            return await _contactRepository.IsContactExisted(email: _refRole.ContactEmail);
        }

        private async Task<bool> DoesAccountExistInPulse()
        {
            return await _accountRepository.DoesAccountExist(_refRole.AccountNumber);
        }

        private async Task<bool> IsContactOfTypeCustomer()
        {
            var contact = await _contactRepository.GetContactAsync(email: _refRole.ContactEmail);
            var refContact = await _contactRepository.GetRefContactAsync(_refRole.ContactEmail);

            if ((contact != null && contact.Type?.ToLower() == "customer")
                || (refContact != null && (refContact.IsCustomer ?? false)))
            {
                return true;
            }
            return false;
        }

        private async Task<bool> DoesContactExistInOperation(string email, string operation = "INSERT", string processStatus = "READY")
        {
            return await _contactRepository.DoesContactExistInOperations(email, operation, processStatus);
        }

        private async Task<bool> DoesAccountExistInOperation(string accountNumber, string operation = "INSERT", string processStatus = "READY")
        {
            return await _accountRepository.DoesAccountExistInOperations(accountNumber, operation, processStatus);
        }

        private async Task<bool> DoesRoleExistInOperation(string accountNumber, string contactEmail, string operation = "INSERT", string processStatus = "READY")
        {
            return await _roleRepository.DoesRoleExistInOperations(accountNumber, contactEmail, operation, processStatus);
        }

        //private async Task<bool> DoesRoleExistInPulse()
        //{
        //    bool roleExistInPulse = _roleRepository.DoesRoleExistInPulse(_refRole.AccountNumber, _refRole.ContactEmail);
        //    if (roleExistInPulse)
        //    {
        //        await UpdateRoleIteratorBasedOnOperationType();
        //    }
        //    return roleExistInPulse;
        //}

        private async Task<bool> DoesRoleExistInPulse(bool shouldIncrementCounter = true)
        {
            bool roleExistInPulse = _roleRepository.DoesRoleExistInPulse(_refRole.AccountNumber, _refRole.ContactEmail);
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
                if (_refRole.OperationType == OperationName.Insert)
                {
                    roleEntity.RoleDuplicatesCounter += 1;
                }
                else if (_refRole.OperationType == OperationName.Delete && roleEntity.RoleDuplicatesCounter > 0)
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
}

using Application.Interfaces.RuleValidators;
using Application.Interfaces;
using Application.Services;
using Application.DeepValidations;

namespace Application.Factories
{

    public class RoleDeepValidatorFactory : IRoleDeepValidatorFactory
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IOperationRepository _operationRepository;
        private readonly IDeepValidationRepository _deepValidationRepository;
        private readonly IContactRepository _contactRepository;
        private readonly IAccountRepository _accountRepository;

        public RoleDeepValidatorFactory(
            IRoleRepository roleRepository,
            IOperationRepository operationRepository,
            IDeepValidationRepository deepValidationRepository,
            IContactRepository contactRepository,
            IAccountRepository accountRepository)
        {
            _roleRepository = roleRepository;
            _operationRepository = operationRepository;
            _deepValidationRepository = deepValidationRepository;
            _contactRepository = contactRepository;
            _accountRepository = accountRepository;
        }

        public IRoleDeepValidator Create()
        {
            return new RoleDeepValidator(
                _roleRepository,
                _operationRepository,
                _deepValidationRepository,
                _contactRepository,
                _accountRepository);
        }
    }
}


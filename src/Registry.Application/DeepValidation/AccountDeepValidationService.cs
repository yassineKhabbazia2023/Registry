using Application.Interfaces;

namespace Application.DeepValidation
{
    public class AccountDeepValidationSerivce(IAccountRepository accountRepository) : IAccountDeepValidationService
    {
        private IAccountRepository _accountRepository = accountRepository;

        public async Task CreateValidAccountsOperationsAsync()
        {
            await _accountRepository.ValidateAccountOperation();
        }
    }
}

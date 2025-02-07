using Application.Services;
using Pulse.ContactRegistry.Domain.Entities;
using System.Threading.Tasks;

namespace Application.Interfaces.RuleValidators
{
    public interface IRoleDeepValidator
    {
        Task<IRoleDeepValidator> AccountShouldExistInPulse();
        Task<IRoleDeepValidator> ContactShouldExistInPulse();
        Task<IRoleDeepValidator> AccountShouldExistInPulseOrOperations();
        Task<IRoleDeepValidator> ContactShouldExistInPulseOrOperations();

        Task<IRoleDeepValidator> Instantiate(RefRoleEntity refRoleEntity);
        Task<IRoleDeepValidator> RoleShouldExistInPulse();
        Task<IRoleDeepValidator> RoleShouldShouldNotExistInPulse();
        Task<IRoleDeepValidator> TryAddOperation();
        Task<bool> Validate();
    }
}
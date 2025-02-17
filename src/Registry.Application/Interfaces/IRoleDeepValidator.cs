using Application.Services;
using Pulse.Registry.Domain.Entities;
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
        Task<IRoleDeepValidator> RoleShouldShouldNotExistInPulseOrOperations();
        Task<IRoleDeepValidator> TryAddOperation();
        Task<bool> Validate();
    }
}
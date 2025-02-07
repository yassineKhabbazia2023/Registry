using Application.Interfaces.RuleValidators;

namespace Application.Interfaces
{
    public interface IRoleDeepValidatorFactory
    {
        public IRoleDeepValidator Create();
    }
}
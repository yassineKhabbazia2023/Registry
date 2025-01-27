using Application.Models;

namespace Application.Interfaces
{
    public interface IValidationHelper<T> where T : class
    {
        IEnumerable<LightValidationResult> ValidateInstanceList(IEnumerable<T> contacts);
    }
}

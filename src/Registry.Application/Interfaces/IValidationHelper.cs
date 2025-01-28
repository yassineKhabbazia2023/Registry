using Application.Models;

namespace Application.Interfaces
{
    public interface IValidationHelper<T> where T : class
    {
        LightValidationResult<T> Validate(IEnumerable<T> models);
    }
}

using Domain.Entities.Audits;

namespace Application.Interfaces
{
    public interface IDeepValidationRepository
    {
        Task<bool> AddDeepValidationAsync(DeepValidationEntity deepValidation);

        Task<bool> DoesDeepValidationLineExistsAsync(Guid entityId, string operationType);
    }
}

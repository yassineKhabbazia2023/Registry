using Pulse.ContactRegistry.Domain.Entities;

namespace Application.Interfaces
{
    public interface IContactsDeepValidationsService
    {
        Task CreateValidContactsOperationsAsync();
    }
}

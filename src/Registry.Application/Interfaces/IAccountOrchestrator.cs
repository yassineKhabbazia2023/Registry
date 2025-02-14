

namespace Application.Interfaces
{
    public interface IAccountOrchestrator
    {
        Task ProcessAccountPublishAsync(string operationType);
    }
}

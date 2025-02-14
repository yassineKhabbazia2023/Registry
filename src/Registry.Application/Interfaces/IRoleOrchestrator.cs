
namespace Application.Interfaces
{
    public interface IRoleOrchestrator
    {
        Task ProcessRolePublishAsync(string operationType);
    }
}

using Pulse.Registry.Domain.Entities;

namespace Application.Models
{
    public class RoleOperationRecord
    {
        public RegOperationEntity Operation { get; set; }

        public RefRoleEntity Role { get; set; }

        public int RoleCount { get; set; }
    }
}

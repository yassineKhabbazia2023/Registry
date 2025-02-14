using Pulse.Registry.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Registry.AzureFuctions
{
    public class RoleOperationDetail
    {
        public RegOperationEntity Operation { get; set; }

        public RefRoleEntity Role { get; set; }

        public int RoleCount { get; set; }
    }
}

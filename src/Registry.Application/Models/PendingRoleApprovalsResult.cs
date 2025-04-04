using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models
{
    public class PendingRoleApprovalsResult
    {
        public IEnumerable<PendingRoleApprovals> PendingRoleApprovals { get; set; }
        public int TotalItems { get; set; }
    }
}

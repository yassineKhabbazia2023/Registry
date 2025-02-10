using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.Accounts
{
    public class Role
    {
        public required int AccountId { get; set; }

        public Guid AccountGlobalUniqueId { get; set; }

        public required int ContactId { get; set; }

        public Guid ContactGlobalUniqueId { get; set; }

        public string? AccountNumber { get; set; }

        public string? ContactEmail { get; set; }

        public int RoleDuplicatesCounter { get; set; }
    }
}

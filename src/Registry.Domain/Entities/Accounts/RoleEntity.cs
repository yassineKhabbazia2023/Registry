using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Accounts
{
    public class RoleEntity
    {
        public required int AccountId { get; set; }

        public Guid AccountGlobalUniqueId { get; set; }

        public required int ContactId { get; set; }

        public Guid ContactGlobalUniqueId { get; set; }

        public bool? IsSignatory { get; set; }

        public bool? IsFavorite { get; set; }

        public bool? IsDelegation { get; set; }

        public int? DelegatorContactId { get; set; }

        public string? AccountNumber { get; set; }

        public string? ContactEmail { get; set; }

        public int RoleDuplicatesCounter { get; set; }
    }
}

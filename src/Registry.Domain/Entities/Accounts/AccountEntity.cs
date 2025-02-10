using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Accounts
{
    public class AccountEntity
    {
        public Guid? AccountGlobalUniqueId { get; set; }
        public required int AccountId { get; set; }
        public required string AccountNumber { get; set; }
    }
}

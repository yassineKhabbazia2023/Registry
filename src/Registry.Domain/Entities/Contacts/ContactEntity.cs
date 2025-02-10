using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Contacts
{
    public class ContactEntity
    {
        public required int ContactId { get; set; }
        public Guid? ContactGlobalUniqueId { get; set; }
        public required string Email { get; set; }
        public string? Type { get; set; }
    }
}

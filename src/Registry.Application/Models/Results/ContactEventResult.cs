using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.Results
{
    public class ContactEventResult<T>
    {
        public required string EventName { get; set; }
        public required bool IsSentToAkuiteo { get; set; }
        public required bool IsRegisteredInDb { get; set; }
        public required bool IsOpeationProcessUpdated { get; set; }
        public T? Content { get; set; }
    }
}

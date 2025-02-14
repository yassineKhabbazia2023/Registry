using Pulse.Registry.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models
{
    public class ContactOperationDetail
    {
        public RegOperationEntity Operation { get; set; }

        public RefContactEntity Contact { get; set; }
    }
}

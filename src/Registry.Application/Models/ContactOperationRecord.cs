using Pulse.Registry.Domain.Entities;

namespace Application.Models
{
    public class ContactOperationRecord
    {
        public required RegOperationEntity Operation { get; set; }

        public required RefContactEntity RefContactEntity { get; set; }
    }
}

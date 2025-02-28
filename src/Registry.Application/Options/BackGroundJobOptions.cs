using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Options
{
    public class BackGroundJobOptions
    {
        public int Chunk { get; set; }
        public int TimeToWaitBeforeEachStep { get; set; }
        public bool ShouldTriggerEvents { get; set; }
        public int NumberOfDaysToRetryFailedRoles { get; set; } = -7;
    }
}

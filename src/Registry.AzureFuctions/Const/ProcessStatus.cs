using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Registry.AzureFuctions.Const
{
    /// <summary>
    /// Operation Process Status.
    /// </summary>
    public static class ProcessStatus
    {
        /// <summary>
        /// operation READY to be sent.
        /// </summary>
        public static readonly string Ready = "READY";

        /// <summary>
        /// operation SENT to the message bus.
        /// </summary>
        public static readonly string Sent = "SENT";

        /// <summary>
        /// operation sent but it was not consumed by other spokes.
        /// </summary>
        public static readonly string Failed = "FAILED";

        /// <summary>
        /// Operation sent and executed in the other spokes.
        /// </summary>
        public static readonly string Succeeded = "SUCCEEDED";

    }
}

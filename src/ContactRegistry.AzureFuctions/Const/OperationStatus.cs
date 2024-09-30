using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactRegistry.AzureFuctions.Const
{
    /// <summary>
    /// consts for Operation status.
    /// </summary>
    public static class OperationStatus
    {
        /// <summary>
        /// Operation with status Approvced.
        /// </summary>
        public const string Approved = nameof(Approved);

        /// <summary>
        /// Operation with status Refused.
        /// </summary>
        public const string Refused = nameof(Refused);

        /// <summary>
        /// Operation with status Pending.
        /// </summary>
        public const string Pending = nameof(Pending);
    }
}

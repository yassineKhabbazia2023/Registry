using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Registry.Application.Consts
{
    /// <summary>
    /// consts for Operation status.
    /// </summary>
    public static class ApprovalStatus
    {
        /// <summary>
        /// Operation with status Approvced.
        /// </summary>
        public static readonly string Approved = "APPROVED";

        /// <summary>
        /// Operation with status Refused.
        /// </summary>
        public static readonly string Rejected = "REJECTED";

        /// <summary>
        /// Operation with status Pending.
        /// </summary>
        public const string Pending = "PENDING";
    }
}

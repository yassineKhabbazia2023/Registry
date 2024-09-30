// <copyright file="CreOperation.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Domain.Entities
{
    public class CreOperation
    {
        #region private properties 
        private string status;
        public int Id { get; set; }
        #endregion

        #region public properties

        public string Operation { get; set; }

        public string? Type { get; set; }

        public DateTime? PublishedAt { get; set; }

        public Guid EntityId { get; set; }

        /// <summary>
        /// Status is the operation status that accept only pending, approved, refused
        /// </summary>
        public string Status
        {
            get { return status; }
            set
            {
                if (value == "Approved" || value == "Pending" || value == "Refused")
                {
                    status = value;
                }
                else
                {
                    status = "Pending";
                }
            }
        }

        /// <summary>
        /// the date registration of the last date the operation status where modified
        /// </summary>
        public Nullable<DateTime> LastStatusDate { get; set; }
        
        /// <summary>
        /// the collaborator Id that accept or refuse the operation
        /// </summary>

        public Nullable<int> LastStatusModifiedBy { get; set; }
        #endregion
    }
}

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

        public string Status
        {
            get { return status; }
            private set
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
        #endregion
    }
}

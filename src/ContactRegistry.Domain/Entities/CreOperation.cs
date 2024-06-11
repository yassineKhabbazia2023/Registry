// <copyright file="CreOperation.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Domain.Entities
{
    public class CreOperation
    {
        public int Id {  get; set; }

        public string Operation { get; set; }

        public string Type { get; set; }

        public DateTime? PublishedAt { get; set; }

        public Guid EntityId { get; set; }
    }
}

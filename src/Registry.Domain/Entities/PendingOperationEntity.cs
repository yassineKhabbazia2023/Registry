// <copyright file="PendingOperationEntity.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Domain.Entities
{
    public class PendingOperationEntity
    {
        public required string AccountNumber { get; set; }
        public required string LegalName { get; set; }
        public int Id { get; set; }
        public DateTime? CreationDate { get; set; }
        public required string ContactEmail { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public int CurrentContactId { get; set; }
    }
}

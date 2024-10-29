// <copyright file="RegOperation.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Domain.Entities;

public class RegOperation
{
    private string status;
    
    public int Id { get; set; }

    public string Operation { get; set; }

    public string? Type { get; set; }

    public DateTime? PublishedAt { get; set; }

    public Guid EntityId { get; set; }

    public string Status
    {
        get { return status; }
        set
        {
            if (value == "APPROVED" || value == "PENDING" || value == "REJECTED")
            {
                status = value;
            }
            else
            {
                status = "PENDING";
            }
        }
    }

    public Nullable<DateTime> LastStatusUpdatedDate { get; set; }
    
    public string? LastStatusUpdatedBy { get; set; }

    public Nullable<DateTime> CreationDate { get; set; } = DateTime.UtcNow;
}

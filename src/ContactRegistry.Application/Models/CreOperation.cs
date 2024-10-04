// <copyright file="CreOperation.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models;

public class CreOperation
{
    public int Id { get; set; }

    public string? Operation { get; set; }

    public string? Type { get; set; }

    public DateTime? PublishedAt { get; set; }

    public Guid EntityId { get; set; }

    public string? Status {  get; set; }

    public Nullable<DateTime> LastStatusUpdatedDate { get; set; }

    public string? LastStatusUpdatedBy { get; set; }

    public Nullable<DateTime> CreationDate { get; set; } = DateTime.UtcNow;
}

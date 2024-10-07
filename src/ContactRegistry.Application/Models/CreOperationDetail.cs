// <copyright file="CreOperationDetail.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models;

public class CreOperationDetail
{
    public required int OperationId { get; set; }

    public string? OperationName { get; set; }

    public string? OperationType { get; set; }

    public Guid RoleId { get; set; }

    public DateTime? CreationDate { get; set; }

    public string? Status { get; set; }

    public string? Email { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public required string AccountNumber { get; set; }
}

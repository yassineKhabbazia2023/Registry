// <copyright file="InvoiceEntity.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>
#nullable disable

namespace Pulse.Registry.Domain.Entities;

public partial class InvoiceEntity
{
    public int InvoiceId { get; set; }

    public string AccountNumber { get; set; }

    public string InvoiceNumber { get; set; }

    public DateTime InvoiceDate { get; set; }

    public string DocumentPath { get; set; }

    public string Type { get; set; }

    public string Operation { get; set; }

    public string Status { get; set; }

    public DateTime CreatedOn { get; set; }
}

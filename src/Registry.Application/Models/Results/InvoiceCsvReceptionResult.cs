// <copyright file="InvoiceCsvReceptionResult.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

/// <summary>
/// Summary of an invoices csv reception.
/// </summary>
public class InvoiceCsvReceptionResult
{
    public string BlobName { get; set; } = string.Empty;

    public int TotalLines { get; set; }

    public int ValidLines { get; set; }

    public int RejectedLines { get; set; }

    public List<InvoiceLineError> Errors { get; set; } = [];
}

/// <summary>
/// Content error of an invoices csv line.
/// </summary>
public class InvoiceLineError
{
    public int Line { get; set; }

    public string Reason { get; set; } = string.Empty;
}

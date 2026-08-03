// <copyright file="InvoiceCsvReceptionOutcome.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

/// <summary>
/// Outcome of an invoices csv reception.
/// </summary>
public class InvoiceCsvReceptionOutcome
{
    public bool IsAccepted { get; private init; }

    public string? ErrorMessage { get; private init; }

    public InvoiceCsvReceptionResult? Summary { get; private init; }

    public static InvoiceCsvReceptionOutcome Rejected(string errorMessage)
    {
        return new InvoiceCsvReceptionOutcome { ErrorMessage = errorMessage };
    }

    public static InvoiceCsvReceptionOutcome Accepted(InvoiceCsvReceptionResult summary)
    {
        return new InvoiceCsvReceptionOutcome { IsAccepted = true, Summary = summary };
    }
}

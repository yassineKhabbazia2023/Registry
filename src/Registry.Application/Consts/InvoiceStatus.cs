// <copyright file="InvoiceStatus.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Consts
{
    /// <summary>
    /// Invoice line statuses in ref.Invoice.
    /// </summary>
    public static class InvoiceStatus
    {
        public static readonly string Pending = "Pending";

        public static readonly string Processed = "Processed";

        public static readonly string Rejected = "Rejected";
    }
}

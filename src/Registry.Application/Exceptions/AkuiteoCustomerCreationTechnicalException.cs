// <copyright file="AkuiteoCustomerCreationTechnicalException.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Exceptions;

/// <summary>
/// Represents a technical failure while creating a customer in Akuiteo.
/// </summary>
public class AkuiteoCustomerCreationTechnicalException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoCustomerCreationTechnicalException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public AkuiteoCustomerCreationTechnicalException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoCustomerCreationTechnicalException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public AkuiteoCustomerCreationTechnicalException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
